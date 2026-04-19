#!/usr/bin/env bash
set -euo pipefail

suite="${1:-all}"
configuration="${2:-Debug}"
project="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)/PhialeGis.Library.Tests/PhialeGis.Library.Tests.csproj"

case "$suite" in
  all)
    dotnet test "$project" -c "$configuration"
    ;;
  unit)
    dotnet test "$project" -c "$configuration" --filter "TestCategory=Unit"
    ;;
  integration)
    dotnet test "$project" -c "$configuration" --filter "TestCategory=Integration"
    ;;
  migration)
    dotnet test "$project" -c "$configuration" --filter "TestCategory=MigrationGuard"
    ;;
  *)
    echo "Usage: $0 [all|unit|integration|migration] [Debug|Release]"
    exit 1
    ;;
esac
