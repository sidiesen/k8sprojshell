param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$Solution
)

$BuildDir = Join-Path $PSScriptRoot ".."
Import-Module $BuildDir/deployment/jsonValues.psm1

$NginxDeploymentName = Get-SolutionConfigValue ".solution.nginxDeploymentName" $Solution

return @{
    NginxDeploymentName=$NginxDeploymentName;
}

