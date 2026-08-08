# Contributing to IdemShield.NET

Thank you for helping improve IdemShield. Small, focused changes with tests and clear operational reasoning are easiest to review.

## Development setup

Requirements:

- .NET 8 SDK
- Docker or compatible Redis and SQL Server instances for integration tests

Restore, build, and run the unit tests:

```bash
dotnet restore IdemShield.sln --locked-mode
dotnet build IdemShield.sln --configuration Release --no-restore
dotnet format IdemShield.sln --verify-no-changes --no-restore
dotnet test IdemShield.sln --configuration Release --no-build --no-restore
```

Provider integration tests activate when these variables are set:

```text
IDEMSHIELD_REDIS_TEST_CONNECTION=localhost:6379
IDEMSHIELD_SQL_TEST_CONNECTION=Server=localhost,1433;Database=tempdb;User Id=sa;Password=...;Encrypt=True;TrustServerCertificate=True
```

Never commit real credentials.

## Pull requests

1. Explain the problem and intended behavior.
2. Add or update tests at the public behavior seam.
3. Keep unrelated refactoring out of the change.
4. Run the Release build and full test suite.
5. Update documentation and `CHANGELOG.md` for user-visible changes.

Pull requests must pass CI before merge. By participating, you agree to follow `CODE_OF_CONDUCT.md`.
