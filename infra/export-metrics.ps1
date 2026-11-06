# Exports Azure Monitor metrics and autoscale events for a load test window to docs/evidence
# (git-ignored) as CSV, to back up the portal screenshots.
param(
    [Parameter(Mandatory)] [string] $ResourceGroup,
    [Parameter(Mandatory)] [string] $AppServicePlan,
    [Parameter(Mandatory)] [string] $WebApp,
    [Parameter(Mandatory)] [string] $FunctionApp,
    [Parameter(Mandatory)] [datetime] $StartUtc,
    [Parameter(Mandatory)] [datetime] $EndUtc,
    [string] $Interval = 'PT1M'
)

$ErrorActionPreference = 'Stop'

$outDir = Join-Path $PSScriptRoot "..\docs\evidence\$($StartUtc.ToString('yyyyMMdd-HHmm'))"
New-Item -ItemType Directory -Force $outDir | Out-Null

$start = $StartUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')
$end = $EndUtc.ToString('yyyy-MM-ddTHH:mm:ssZ')

$planId = az appservice plan show -g $ResourceGroup -n $AppServicePlan --query id -o tsv
$webId = az webapp show -g $ResourceGroup -n $WebApp --query id -o tsv
$funcId = az functionapp show -g $ResourceGroup -n $FunctionApp --query id -o tsv
$autoscaleId = az monitor autoscale show -g $ResourceGroup -n "$AppServicePlan-autoscale" --query id -o tsv

function Export-Metric([string] $resourceId, [string] $metric, [string] $aggregation, [string] $file) {
    $json = az monitor metrics list --resource $resourceId --metric $metric --aggregation $aggregation `
        --start-time $start --end-time $end --interval $Interval -o json | ConvertFrom-Json

    $rows = foreach ($point in $json.value[0].timeseries[0].data) {
        [pscustomobject]@{
            TimestampUtc = $point.timeStamp
            Metric       = $metric
            Value        = $point.($aggregation.ToLowerInvariant())
        }
    }

    $rows | Export-Csv -NoTypeInformation -Path (Join-Path $outDir $file)
    Write-Host "Saved $file ($($rows.Count) points)"
}

Export-Metric $planId 'CpuPercentage' 'Average' 'plan-cpu.csv'
Export-Metric $planId 'MemoryPercentage' 'Average' 'plan-memory.csv'
Export-Metric $autoscaleId 'ObservedCapacity' 'Average' 'plan-instance-count.csv'
Export-Metric $webId 'Requests' 'Total' 'web-requests.csv'
Export-Metric $webId 'HttpResponseTime' 'Average' 'web-response-time.csv'
Export-Metric $webId 'Http5xx' 'Total' 'web-http5xx.csv'
Export-Metric $funcId 'FunctionExecutionCount' 'Total' 'func-executions.csv'

# Scale-out / scale-in actions taken by autoscale during the window.
az monitor activity-log list --resource-id $autoscaleId --start-time $start --end-time $end `
    --query "[?operationName.value=='Microsoft.Insights/AutoscaleSettings/Scaleup/Action' || operationName.value=='Microsoft.Insights/AutoscaleSettings/Scaledown/Action'].{TimestampUtc:eventTimestamp, Operation:operationName.localizedValue, Status:status.value, Description:description}" `
    -o json | ConvertFrom-Json | Export-Csv -NoTypeInformation -Path (Join-Path $outDir 'autoscale-events.csv')
Write-Host 'Saved autoscale-events.csv'

Write-Host "Evidence written to $((Resolve-Path $outDir).Path)"
