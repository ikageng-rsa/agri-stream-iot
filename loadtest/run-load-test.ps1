# Creates (or updates) and runs the Agri-Stream test in Azure Load Testing.
# Secrets are read from environment variables so they never live in the repo:
#   $env:AGRI_FUNC_KEY      - function key for the Function App
#   $env:AGRI_LOAD_PASSWORD - password of the 'loadtest' web app user
param(
    [Parameter(Mandatory)] [string] $ResourceGroup,
    [Parameter(Mandatory)] [string] $LoadTestResource,
    [Parameter(Mandatory)] [string] $WebHost,
    [Parameter(Mandatory)] [string] $FuncHost,
    [int] $Users = 100,
    [int] $ApiUsers = 30,
    [int] $DurationSeconds = 900,
    [int] $RampUpSeconds = 120
)

$ErrorActionPreference = 'Stop'

if (-not $env:AGRI_FUNC_KEY -or -not $env:AGRI_LOAD_PASSWORD) {
    throw 'Set $env:AGRI_FUNC_KEY and $env:AGRI_LOAD_PASSWORD before running.'
}

$testId = 'agri-stream-peak-load'
$envArgs = @(
    "webHost=$WebHost",
    "funcHost=$FuncHost",
    "funcKey=$env:AGRI_FUNC_KEY",
    'loadUser=loadtest',
    "loadPassword=$env:AGRI_LOAD_PASSWORD",
    "users=$Users",
    "apiUsers=$ApiUsers",
    "duration=$DurationSeconds",
    "rampUp=$RampUpSeconds"
)

$existing = az load test list --load-test-resource $LoadTestResource --resource-group $ResourceGroup --query "[?testId=='$testId'].testId" -o tsv
$verb = if ($existing) { 'update' } else { 'create' }

az load test $verb `
    --load-test-resource $LoadTestResource `
    --resource-group $ResourceGroup `
    --test-id $testId `
    --load-test-config-file (Join-Path $PSScriptRoot 'config.yaml') `
    --env @envArgs

$runId = "run-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
az load test-run create `
    --load-test-resource $LoadTestResource `
    --resource-group $ResourceGroup `
    --test-id $testId `
    --test-run-id $runId `
    --display-name "Peak load $Users users"

Write-Host "Started test run $runId. Follow it in the portal under $LoadTestResource > Tests > $testId."
