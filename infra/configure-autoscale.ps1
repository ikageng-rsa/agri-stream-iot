# Configures scale-out/in rules on the App Service plan hosting the AgriStream MVC app.
# Autoscale requires the Standard (S1) tier or higher.
param(
    [Parameter(Mandatory)] [string] $ResourceGroup,
    [Parameter(Mandatory)] [string] $AppServicePlan,
    [int] $MinInstances = 1,
    [int] $MaxInstances = 5,
    [int] $CpuScaleOutPercent = 70,
    [int] $CpuScaleInPercent = 30,
    [int] $MemoryScaleOutPercent = 75
)

$ErrorActionPreference = 'Stop'
$autoscaleName = "$AppServicePlan-autoscale"

$planId = az appservice plan show --resource-group $ResourceGroup --name $AppServicePlan --query id -o tsv
$sku = az appservice plan show --resource-group $ResourceGroup --name $AppServicePlan --query sku.tier -o tsv
if ($sku -in @('Free', 'Shared', 'Basic')) {
    throw "App Service plan '$AppServicePlan' is on the $sku tier; scale it to Standard (S1) or higher first."
}

az monitor autoscale create `
    --resource-group $ResourceGroup `
    --name $autoscaleName `
    --resource $planId `
    --min-count $MinInstances `
    --max-count $MaxInstances `
    --count $MinInstances

# Scale out by one instance when average CPU stays high for 5 minutes.
az monitor autoscale rule create `
    --resource-group $ResourceGroup `
    --autoscale-name $autoscaleName `
    --condition "CpuPercentage > $CpuScaleOutPercent avg 5m" `
    --scale out 1 `
    --cooldown 5

# Scale out by one instance when average memory stays high for 5 minutes.
az monitor autoscale rule create `
    --resource-group $ResourceGroup `
    --autoscale-name $autoscaleName `
    --condition "MemoryPercentage > $MemoryScaleOutPercent avg 5m" `
    --scale out 1 `
    --cooldown 5

# Scale back in by one instance once CPU has been low for 10 minutes.
az monitor autoscale rule create `
    --resource-group $ResourceGroup `
    --autoscale-name $autoscaleName `
    --condition "CpuPercentage < $CpuScaleInPercent avg 10m" `
    --scale in 1 `
    --cooldown 10

az monitor autoscale show --resource-group $ResourceGroup --name $autoscaleName --query "profiles[0].rules[].{metric:metricTrigger.metricName, op:metricTrigger.operator, threshold:metricTrigger.threshold, direction:scaleAction.direction}" -o table
