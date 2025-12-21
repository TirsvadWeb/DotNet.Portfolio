#!/bin/bash
set -euo pipefail

# Create results dir that can be mounted from host
mkdir -p /artifacts/TestResults

# Configuration (allow override from environment)
CONFIGURATION=${CONFIGURATION:-Release}

# Ensure we're in workspace containing the solution/repo
cd /workspace || exit 1

echo "Discovering test projects..."

# Run dotnet test for each test project found under the repo
exit_code=0

# Find all .Tests.csproj files and run tests
while IFS= read -r -d '' proj; do
  name=$(basename "$proj")
  name_no_ext="${name%.csproj}"
  proj_dir=$(dirname "$proj")
  echo "Running tests for project: $name"
  dotnet test "$proj" \
    --configuration "$CONFIGURATION" \
    --logger "trx;LogFileName=${name_no_ext}.trx" \
    --results-directory "/artifacts/TestResults" || exit_code=$?
done < <(find . -type f -name "*.Tests.csproj" -print0)

exit $exit_code
