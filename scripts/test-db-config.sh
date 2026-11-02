#!/usr/bin/env bash

parse_domora_test_connection() {
    local part key value key_lower
    DOMORA_TEST_DATABASE=""
    DOMORA_TEST_DATABASE_FIELDS=0
    DOMORA_TEST_HOST=""
    DOMORA_TEST_HOST_FIELDS=0
    DOMORA_TEST_PORT=""
    DOMORA_TEST_PORT_FIELDS=0
    DOMORA_TEST_USER=""
    DOMORA_TEST_USER_FIELDS=0
    DOMORA_TEST_PASSWORD=""

    IFS=';' read -r -a connection_parts <<< "${DomoraTest:-}"
    for part in "${connection_parts[@]}"; do
        part="${part#"${part%%[![:space:]]*}"}"
        part="${part%"${part##*[![:space:]]}"}"
        [[ "$part" == *=* ]] || continue
        key="${part%%=*}"
        value="${part#*=}"
        key="${key#"${key%%[![:space:]]*}"}"
        key="${key%"${key##*[![:space:]]}"}"
        value="${value#"${value%%[![:space:]]*}"}"
        value="${value%"${value##*[![:space:]]}"}"
        key_lower="$(printf '%s' "$key" | tr '[:upper:]' '[:lower:]')"
        case "$key_lower" in
            database|"initial catalog")
                ((DOMORA_TEST_DATABASE_FIELDS += 1))
                DOMORA_TEST_DATABASE="$value"
                ;;
            host|server|"host name"|"data source")
                ((DOMORA_TEST_HOST_FIELDS += 1))
                DOMORA_TEST_HOST="$value"
                ;;
            port)
                ((DOMORA_TEST_PORT_FIELDS += 1))
                DOMORA_TEST_PORT="$value"
                ;;
            username|"user id"|"user name"|user)
                ((DOMORA_TEST_USER_FIELDS += 1))
                DOMORA_TEST_USER="$value"
                ;;
            password|pwd) DOMORA_TEST_PASSWORD="$value" ;;
        esac
    done
}

verify_domora_test_connection_settings() {
    parse_domora_test_connection
    if [[ "$DOMORA_TEST_DATABASE_FIELDS" -ne 1 || "$DOMORA_TEST_DATABASE" != "domora_test" ]]; then
        echo "Refusing to continue: exactly one configured database setting must equal domora_test." >&2
        return 1
    fi
    if [[ "$DOMORA_TEST_HOST_FIELDS" -ne 1 || "$DOMORA_TEST_HOST" != "localhost" \
        || "$DOMORA_TEST_PORT_FIELDS" -ne 1 || "$DOMORA_TEST_PORT" != "5432" ]]; then
        echo "Refusing to continue: the test PostgreSQL endpoint must be exactly localhost:5432." >&2
        return 1
    fi
    if [[ "$DOMORA_TEST_USER_FIELDS" -ne 1 || "$DOMORA_TEST_USER" != "domora_app" ]]; then
        echo "Refusing to continue: DomoraTest must use domora_app." >&2
        return 1
    fi
}

verify_domora_test_database() {
    verify_domora_test_connection_settings

    local actual_database
    actual_database="$(PGPASSWORD="$DOMORA_TEST_PASSWORD" psql \
        -X -v ON_ERROR_STOP=1 -At \
        -h localhost -p 5432 -U domora_app -d domora_test \
        -c 'SELECT current_database() || '\''|'\'' || current_user || '\''|'\'' || inet_server_port()')"
    if [[ "$actual_database" != "domora_test|domora_app|5432" ]]; then
        echo "Refusing to continue: PostgreSQL did not confirm domora_test as domora_app on port 5432." >&2
        return 1
    fi
    echo "Verified PostgreSQL database: domora_test (domora_app, localhost:5432)"
}

verify_test_config_has_no_admin_settings() {
    local config_file="$1" line key
    while IFS= read -r line || [[ -n "$line" ]]; do
        line="${line#"${line%%[![:space:]]*}"}"
        [[ -z "$line" || "$line" == \#* || "$line" != *=* ]] && continue
        key="${line%%=*}"
        key="${key#"${key%%[![:space:]]*}"}"
        key="${key%"${key##*[![:space:]]}"}"
        if [[ "$key" =~ [Aa][Dd][Mm][Ii][Nn]|[Ss][Yy][Ss][Tt][Ee][Mm][Ii][Dd][Ee][Nn][Tt][Ii][Ff][Ii][Ee][Rr]|[Pp][Gg][Pp][Aa][Ss][Ss][Ff][Ii][Ll][Ee] ]]; then
            echo "Refusing to continue: test configuration contains an administrative setting." >&2
            return 1
        fi
    done < "$config_file"
}
