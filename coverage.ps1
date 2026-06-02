param(
    [string]$ReportDir = "coveragereport"
)

$root = $PSScriptRoot

# 1. Delete stale coverage data so only the current run feeds the report.
$testResultsPath = Join-Path $root "TestResults"
if (Test-Path $testResultsPath) {
    Remove-Item $testResultsPath -Recurse -Force
    Write-Host "Cleaned TestResults directory." -ForegroundColor Cyan
}

# 2. Run all tests and collect coverage with our runsettings.
Write-Host "Running tests with coverage..." -ForegroundColor Cyan
dotnet test "$root\IdeaFusion.sln" `
    --collect:"XPlat Code Coverage" `
    --settings "$root\coverlet.runsettings" `
    --results-directory "$testResultsPath" `
    -- DataCollectionRunSettings.DataCollectors.DataCollector.Configuration.Format=cobertura

if ($LASTEXITCODE -ne 0) {
    Write-Warning "Some tests failed. Coverage report will still be generated from available data."
}

# 3. Collect all cobertura files from this run only.
$coberturaFiles = Get-ChildItem -Path $testResultsPath -Recurse -Filter "coverage.cobertura.xml" |
    ForEach-Object { $_.FullName }

if (-not $coberturaFiles) {
    Write-Error "No coverage files found in $testResultsPath"
    exit 1
}

$reports = $coberturaFiles -join ";"
Write-Host "Found $($coberturaFiles.Count) coverage file(s)." -ForegroundColor Cyan

# 4. Generate the HTML report, excluding auto-generated and infrastructure assemblies.
$outputDir = Join-Path $root $ReportDir
reportgenerator `
    "-reports:$reports" `
    "-targetdir:$outputDir" `
    "-reporttypes:Html;Badges" `
    "-assemblyfilters:-GrpcContracts;-*.Tests;-ServiceDefaults" `
    "-classfilters:-*Migrations*;-*ModelSnapshot*" `
    "-title:IdeaFusion Coverage"

Write-Host "Report generated at: $outputDir\index.html" -ForegroundColor Green
