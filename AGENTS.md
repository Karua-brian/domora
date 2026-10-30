## Test Environment

Domora uses PostgreSQL for integration tests.

- The test connection string is stored locally in `.env.test.local`.
- Never commit this file or expose its contents.
- Never invent or substitute a database connection string.
- Run database-backed tests using:

  `./scripts/test-with-db.sh`

- Never run migrations against development or production databases.
- Never execute database reset or deletion commands without explicit approval.
- Inspect the test database configuration before running destructive operations.
- Report tests that could not execute rather than claiming they passed.