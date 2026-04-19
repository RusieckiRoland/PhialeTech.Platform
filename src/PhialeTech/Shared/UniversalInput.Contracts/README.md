# UniversalInput.Contracts

Standalone contract library for cross-platform input/event DTOs and enums.

This package is intended to be extracted and reused independently from the rest of the repository.

Key properties:
- no `PhialeGis.*` project dependencies
- no third-party package dependencies
- target framework: `netstandard2.0`
- NuGet package output: `..\\.nuget\\local-feed`

Scope:
- `Universal*` DTOs and EventArgs
- `IUniversalBase`
- helper enums in `EditorEnums` and `EventEnums`

Packaging:
- `dotnet pack UniversalInput.Contracts/UniversalInput.Contracts.csproj`
- package id: `UniversalInput.Contracts`
