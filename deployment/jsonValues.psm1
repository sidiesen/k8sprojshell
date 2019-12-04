function Get-JsonValue
{
param(
[Parameter(Mandatory=$true, Position=0)]
[string]$QueryPath,

[Parameter(Mandatory=$true, Position=1)]
[string]$JsonFile
)
    $RootDir = Join-Path $PSScriptRoot "../.."
    $fileContents = Get-Content (Join-Path $RootDir $JsonFile) | ConvertFrom-Json
    $accessors = $QueryPath.Split('.')
    $value = $fileContents

    foreach($accessor in $accessors)
    {
        if($accessor -ne "")
        {
            $value = $value.$accessor
        }
    }

    return $value
}

function Get-TenantConfigValue
{
param(
[Parameter(Mandatory=$true, Position=0)]
[string]$QueryPath,
[Parameter(Mandatory=$true, Position=1)]
[string]$tenant
)
    $queryPath = "$QueryPath".Replace(".tenant.", ".tenants.$Tenant.")
    $repoconfigFile = "repoconfig.json"
    return Get-JsonValue $queryPath $repoconfigFile
}

function Get-SolutionConfigValue
{
param(
[Parameter(Mandatory=$true, Position=0)]
[string]$QueryPath,
[Parameter(Mandatory=$true, Position=1)]
[string]$Solution
)
    $queryPath = "$QueryPath".Replace(".solution.", ".solutions.$Solution.")
    $repoconfigFile = "repoconfig.json"
    return Get-JsonValue $queryPath $repoconfigFile
}

function Get-RepositoryConfigValue
{
param(
[Parameter(Mandatory=$true, Position=0)]
[string]$QueryPath
)
    $repoconfigFile = "repoconfig.json"
    return Get-JsonValue $QueryPath $repoconfigFile
}

function Get-EnvironmentConfigValue
{
param(
[Parameter(Mandatory=$true, Position=0)]
[string]$QueryPath,
[Parameter(Mandatory=$true, Position=1)]
[string]$Location
)
    $repoconfigFile = "config/repoconfig.$Location.json"    

    $locValue = Get-JsonValue $queryPath $repoconfigFile

    if("$locValue" -ne "")
    {
        return $locValue
    }

    # localized value not found, fetch value from common config file
    $commonRepoconfigFile = "config/repoconfig.Common.json"
    return Get-JsonValue $queryPath $commonRepoconfigFile
}

Export-ModuleMember -Function *