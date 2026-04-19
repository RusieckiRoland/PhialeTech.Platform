# Test suites

This project uses NUnit categories to split test runs:

- `Unit` - fast unit tests
- `Integration` - broader integration tests
- `MigrationGuard` - migration safety checks for UniversalEvents extraction

## Run all tests

```bash
dotnet test PhialeGis.Library.Tests/PhialeGis.Library.Tests.csproj -c Debug
```

## Run only unit tests

```bash
dotnet test PhialeGis.Library.Tests/PhialeGis.Library.Tests.csproj -c Debug --filter "TestCategory=Unit"
```

## Run only integration tests

```bash
dotnet test PhialeGis.Library.Tests/PhialeGis.Library.Tests.csproj -c Debug --filter "TestCategory=Integration"
```

## Run only migration guard tests

```bash
dotnet test PhialeGis.Library.Tests/PhialeGis.Library.Tests.csproj -c Debug --filter "TestCategory=MigrationGuard"
```

## Helper scripts

- PowerShell: `./scripts/tests.ps1 all|unit|integration|migration [Debug|Release]`
- Bash/WSL: `./scripts/tests.sh all|unit|integration|migration [Debug|Release]`
