#!/bin/bash

# Run tests with coverage collection
echo "Running tests with coverage collection..."
dotnet test --collect:"XPlat Code Coverage"

# Find the most recent coverage file
COVERAGE_FILE=$(find SpecRec.Tests/TestResults -name "coverage.cobertura.xml" -type f -exec stat -f "%m %N" {} \; | sort -nr | head -1 | cut -d' ' -f2-)

if [ -z "$COVERAGE_FILE" ]; then
    echo "Error: No coverage file found"
    exit 1
fi

echo "Found coverage file: $COVERAGE_FILE"

# Generate HTML and text summary report
echo "Generating coverage report..."
~/.dotnet/tools/reportgenerator \
    -reports:"$COVERAGE_FILE" \
    -targetdir:"coverage-report" \
    -reporttypes:"Html;TextSummary"

echo ""
echo "Coverage report generated successfully!"
echo "HTML report: coverage-report/index.html"
echo ""
echo "Summary:"
cat coverage-report/Summary.txt | head -20