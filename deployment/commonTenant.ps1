param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$Tenant
)
    
$BuildDir = Join-Path $PSScriptRoot ".."
Import-Module $BuildDir/deployment/jsonValues.psm1

$SubscriptionId = Get-TenantConfigValue ".tenant.subscriptionId" $Tenant
$TenantId = Get-TenantConfigValue ".tenant.tenantId" $Tenant
$Ev2DeploymentApplication = Get-TenantConfigValue ".tenant.Ev2DeploymentApplication" $Tenant

return @{
    SubscriptionId=$SubscriptionId;
    TenantId=$TenantId;
    Ev2DeploymentApplication=$Ev2DeploymentApplication;
}

