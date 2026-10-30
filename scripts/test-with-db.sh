#!/usr/bin/env bash

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

ENV_FILE="$ROOT_DIR/.env.test.local"

if [[ ! -f "$ENV_FILE" ]]; then
    echo "Missing .env.test.local"
    exit 1
fi

set -a
source "$ENV_FILE"
set +a

if [[ -z "${DomoraTest:-}" ]]; then
    echo "DomoraTest is not configured."
    exit 1
fi

if [[ "$DomoraTest" != *"Database=domora_test"* ]]; then
    echo "Refusing to run: connection string does not target domora_test."
    exit 1
fi

echo "Running Domora tests against the dedicated test database..."

dotnet test "$ROOT_DIR/Domora.slnx" \
    --no-restore \
    --disable-build-servers \
    -m:1