param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$Solution
)

$RootDir = Join-Path $PSScriptRoot "../.."
Import-Module $RootDir/deployment/jsonValues.psm1

$NginxDeploymentName = Get-SolutionConfigValue ".solution.nginxDeploymentName" $Solution

return @{
    NginxDeploymentName=$NginxDeploymentName;
}

