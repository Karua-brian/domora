#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

ENV_FILE="$ROOT_DIR/.env.test.local"

if [[ ! -f "$ENV_FILE" ]]; then
    echo "Missing .env.test.local" >&2
    exit 1
fi

source "$ROOT_DIR/scripts/test-db-config.sh"
verify_test_config_has_no_admin_settings "$ENV_FILE"

set -a
source "$ENV_FILE"
set +a

verify_domora_test_connection_settings

"$ROOT_DIR/scripts/verify-test-db.sh"

echo "Running Domora tests against the dedicated test database..."

TEST_TARGET="${1:-$ROOT_DIR/Domora.slnx}"
if [[ "$#" -gt 0 ]]; then
    shift
fi

env -u PGPASSFILE \
    -u PGPASSWORD \
    -u DOMORA_TEST_ADMIN_PGPASSFILE \
    -u DOMORA_TEST_ADMIN_CONNECTION \
    -u DOMORA_TEST_SYSTEM_IDENTIFIER \
    -u DOMORA_TEST_EXPECTED_SYSTEM_IDENTIFIER \
    -u ConnectionStrings__DomoraTestAdmin \
    -u DomoraTestAdmin \
    dotnet test "$TEST_TARGET" \
    --no-restore \
    --disable-build-servers \
    -m:1 \
    "$@"
