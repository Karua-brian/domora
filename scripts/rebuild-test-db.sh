#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="$ROOT_DIR/.env.test.local"
TRUSTED_ID_FILE="$ROOT_DIR/.domora-test-cluster-id"
ADMIN_PASS_FILE="$HOME/.pgpass.domora-test-admin"
EXPECTED_ADMIN_ROLE="domora_test_admin"
EXPECTED_APP_ROLE="domora_app"
EXPECTED_DATABASE="domora_test"
EXPECTED_MAINTENANCE_DATABASE="postgres"
EXPECTED_HOST="localhost"
EXPECTED_PORT="5432"

source "$ROOT_DIR/scripts/test-db-config.sh"

fail() {
    echo "Refusing to rebuild: $1" >&2
    exit 1
}

if [[ ! -f "$ENV_FILE" || -L "$ENV_FILE" ]]; then
    fail "missing or unsafe .env.test.local"
fi
verify_test_config_has_no_admin_settings "$ENV_FILE"
set -a
source "$ENV_FILE"
set +a
verify_domora_test_connection_settings || exit 1

if [[ ! -f "$ADMIN_PASS_FILE" || -L "$ADMIN_PASS_FILE" || ! -r "$ADMIN_PASS_FILE" ]]; then
    fail "the dedicated PostgreSQL admin password file is missing or unreadable"
fi
admin_pass_mode="$(stat -f '%Lp' "$ADMIN_PASS_FILE")"
admin_pass_uid="$(stat -f '%u' "$ADMIN_PASS_FILE")"
if [[ "$admin_pass_mode" != "600" || "$admin_pass_uid" != "$(id -u)" ]]; then
    fail "the admin password file must be owned by the current user with mode 0600"
fi

if [[ ! -f "$TRUSTED_ID_FILE" || -L "$TRUSTED_ID_FILE" || ! -r "$TRUSTED_ID_FILE" ]]; then
    fail "the trusted local PostgreSQL system identifier file is missing or unreadable"
fi
trusted_id_mode="$(stat -f '%Lp' "$TRUSTED_ID_FILE")"
trusted_id_uid="$(stat -f '%u' "$TRUSTED_ID_FILE")"
if [[ "$trusted_id_mode" != "600" || "$trusted_id_uid" != "$(id -u)" ]]; then
    fail "the trusted system identifier file must be owned by the current user with mode 0600"
fi
if [[ "$(wc -l < "$TRUSTED_ID_FILE" | tr -d '[:space:]')" != "1" ]]; then
    fail "the trusted system identifier file must contain exactly one line"
fi
trusted_system_identifier="$(<"$TRUSTED_ID_FILE")"
if [[ ! "$trusted_system_identifier" =~ ^[0-9]+$ ]]; then
    fail "the trusted system identifier is not a decimal PostgreSQL identifier"
fi

admin_psql() {
    env -u PGPASSWORD PGPASSFILE="$ADMIN_PASS_FILE" psql \
        -X -v ON_ERROR_STOP=1 -h "$EXPECTED_HOST" -p "$EXPECTED_PORT" \
        -U "$EXPECTED_ADMIN_ROLE" "$@"
}

admin_state="$(admin_psql -At -F '|' -d "$EXPECTED_MAINTENANCE_DATABASE" -c \
    "SELECT current_user || '|' || current_database() || '|' || COALESCE(inet_server_addr()::text, '') || '|' || inet_server_port()::text || '|' || (SELECT system_identifier::text FROM pg_control_system()) || '|' || (SELECT rolcreatedb::text FROM pg_roles WHERE rolname = 'domora_test_admin') || '|' || (SELECT rolsuper::text FROM pg_roles WHERE rolname = 'domora_test_admin') || '|' || (SELECT rolcreatedb::text FROM pg_roles WHERE rolname = 'domora_app') || '|' || (SELECT rolsuper::text FROM pg_roles WHERE rolname = 'domora_app') || '|' || EXISTS (SELECT 1 FROM pg_database WHERE datname = 'domora_test')::text || '|' || EXISTS (SELECT 1 FROM pg_database WHERE datname = 'Domora')::text || '|' || COALESCE((SELECT pg_get_userbyid(datdba) FROM pg_database WHERE datname = 'Domora'), '')")"
IFS='|' read -r current_user maintenance_database server_address server_port live_system_identifier \
    admin_createdb admin_superuser app_createdb app_superuser test_database_exists \
    development_database_exists development_owner <<< "$admin_state"

if [[ "$current_user" != "$EXPECTED_ADMIN_ROLE" || "$maintenance_database" != "$EXPECTED_MAINTENANCE_DATABASE" ]]; then
    fail "the administrative connection did not authenticate to postgres as domora_test_admin"
fi
if [[ "$server_address" != "::1" && "$server_address" != "::1/128" \
    && "$server_address" != "127.0.0.1" && "$server_address" != "127.0.0.1/32" \
    && "$server_address" != "::ffff:127.0.0.1" && "$server_address" != "::ffff:127.0.0.1/128" ]]; then
    fail "the PostgreSQL server address is not loopback"
fi
if [[ "$server_port" != "$EXPECTED_PORT" ]]; then
    fail "the PostgreSQL server port is not 5432"
fi
if [[ "$live_system_identifier" != "$trusted_system_identifier" ]]; then
    fail "the live PostgreSQL system identifier does not match the trusted local value"
fi
if [[ "$admin_createdb" != "true" || "$admin_superuser" != "false" ]]; then
    fail "domora_test_admin must have CREATEDB and must not be a superuser"
fi
if [[ "$app_createdb" != "false" || "$app_superuser" != "false" ]]; then
    fail "domora_app must have neither CREATEDB nor SUPERUSER"
fi
if [[ "$development_database_exists" != "true" || "$development_owner" != "karuabrian" ]]; then
    fail "the protected development database identity is unexpected"
fi

app_maintenance_state="$(PGPASSWORD="$DOMORA_TEST_PASSWORD" psql \
    -X -v ON_ERROR_STOP=1 -At -F '|' -h "$EXPECTED_HOST" -p "$EXPECTED_PORT" \
    -U "$EXPECTED_APP_ROLE" -d "$EXPECTED_MAINTENANCE_DATABASE" \
    -c "SELECT current_user || '|' || current_database() || '|' || inet_server_port()::text")"
if [[ "$app_maintenance_state" != "domora_app|postgres|5432" ]]; then
    fail "the configured application credentials did not authenticate as domora_app on localhost:5432"
fi

if [[ "$test_database_exists" == "true" ]]; then
    existing_owner="$(admin_psql -At -d postgres -c \
        "SELECT pg_get_userbyid(datdba) FROM pg_database WHERE datname = 'domora_test'")"
    if [[ "$existing_owner" != "$EXPECTED_ADMIN_ROLE" ]]; then
        fail "the existing domora_test database is not owned by domora_test_admin"
    fi
    verify_domora_test_database || exit 1
fi

# Refresh the session inventory immediately before DROP DATABASE. A concurrent new
# connection can still race this check; PostgreSQL's ordinary DROP then fails safely.
active_sessions="$(admin_psql -At -F '|' -d postgres -c \
    "SELECT pid::text || '|' || COALESCE(application_name, '') || '|' || usename || '|' || COALESCE(state, '') FROM pg_stat_activity WHERE datname = 'domora_test' ORDER BY pid")"
if [[ -n "$active_sessions" ]]; then
    echo "Refusing to rebuild: active sessions remain connected to domora_test:" >&2
    printf '%s\n' "$active_sessions" >&2
    exit 1
fi

env -u PGPASSWORD PGPASSFILE="$ADMIN_PASS_FILE" dropdb \
    --if-exists --host="$EXPECTED_HOST" --port="$EXPECTED_PORT" \
    --username="$EXPECTED_ADMIN_ROLE" --maintenance-db="$EXPECTED_MAINTENANCE_DATABASE" \
    domora_test
env -u PGPASSWORD PGPASSFILE="$ADMIN_PASS_FILE" createdb \
    --host="$EXPECTED_HOST" --port="$EXPECTED_PORT" \
    --username="$EXPECTED_ADMIN_ROLE" --maintenance-db="$EXPECTED_MAINTENANCE_DATABASE" \
    --template=template0 --owner="$EXPECTED_ADMIN_ROLE" domora_test

admin_psql -d postgres -c 'GRANT CONNECT ON DATABASE domora_test TO domora_app' >/dev/null
admin_psql -d domora_test -c 'GRANT USAGE, CREATE ON SCHEMA public TO domora_app' >/dev/null

database_permissions="$(admin_psql -At -F '|' -d postgres -c \
    "SELECT pg_get_userbyid(datdba) || '|' || has_database_privilege('domora_app', 'domora_test', 'CONNECT')::text FROM pg_database WHERE datname = 'domora_test'")"
schema_permissions="$(admin_psql -At -F '|' -d domora_test -c \
    "SELECT pg_get_userbyid(nspowner) || '|' || has_schema_privilege('domora_app', 'public', 'USAGE')::text || '|' || has_schema_privilege('domora_app', 'public', 'CREATE')::text FROM pg_namespace WHERE nspname = 'public'")"
if [[ "$database_permissions" != "domora_test_admin|true" ]]; then
    fail "database ownership or app CONNECT privilege verification failed"
fi
IFS='|' read -r schema_owner app_schema_usage app_schema_create <<< "$schema_permissions"
if [[ -z "$schema_owner" || "$app_schema_usage" != "true" || "$app_schema_create" != "true" ]]; then
    fail "public schema ownership or app USAGE/CREATE privilege verification failed"
fi
verify_domora_test_database || exit 1

echo "Rebuilt PostgreSQL database: domora_test"
echo "Database owner: domora_test_admin; domora_app has CONNECT and public-schema USAGE, CREATE."
