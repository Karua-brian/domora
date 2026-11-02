#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
ENV_FILE="$ROOT_DIR/.env.test.local"
source "$ROOT_DIR/scripts/test-db-config.sh"

if [[ ! -f "$ENV_FILE" ]]; then
    echo "Missing .env.test.local" >&2
    exit 1
fi

verify_test_config_has_no_admin_settings "$ENV_FILE"

set -a
source "$ENV_FILE"
set +a

verify_domora_test_database
