param (
    [Parameter(Mandatory=$false, Position=2)]
    [string]$Tag,

    [Parameter(Mandatory=$false, Position=3)]
    [string[]]$Actions,

    [Parameter(Mandatory=$false, Position=4)]
    [string]$Config = "Debug"
)

DynamicParam {
    $RootDir = Join-Path $PSScriptRoot "..\.."
    $BuildDir = Join-Path $PSScriptRoot ".."

    Import-Module $BuildDir/deployment/jsonValues.psm1

    $RoleParameterName = 'Role'
    $EnvParameterName = 'Env'
    $RuntimeParameterDictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary

    $RoleAttributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
    $ParameterAttribute = New-Object System.Management.Automation.ParameterAttribute
    $ParameterAttribute.Mandatory = $true
    $ParameterAttribute.Position = 1
    $RoleAttributeCollection.Add($ParameterAttribute)
    $solutionSet = (Get-RepositoryConfigValue ".config.solutions").trim("[]").split(",").trim("""")
    $RoleValidateSetAttribute = New-Object System.Management.Automation.ValidateSetAttribute([String[]]@() + $solutionSet)
    $RoleAttributeCollection.Add($RoleValidateSetAttribute)

    $EnvAttributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
    $ParameterAttribute = New-Object System.Management.Automation.ParameterAttribute
    $ParameterAttribute.Mandatory = $false
    $ParameterAttribute.Position = 0
    $EnvAttributeCollection.Add($ParameterAttribute)
    $solutionSet = (Get-RepositoryConfigValue ".config.environments").trim("[]").split(",").trim("""")
    $EnvValidateSetAttribute = New-Object System.Management.Automation.ValidateSetAttribute([String[]]@("Dev") + $solutionSet)
    $EnvAttributeCollection.Add($EnvValidateSetAttribute)

    # Create and return the dynamic parameter
    $RoleRuntimeParameter = New-Object System.Management.Automation.RuntimeDefinedParameter($RoleParameterName, [string], $RoleAttributeCollection)
    $RuntimeParameterDictionary.Add($RoleParameterName, $RoleRuntimeParameter)
    $EnvRuntimeParameter = New-Object System.Management.Automation.RuntimeDefinedParameter($EnvParameterName, [string], $EnvAttributeCollection)
    $RuntimeParameterDictionary.Add($EnvParameterName, $EnvRuntimeParameter)
    return $RuntimeParameterDictionary
}

begin {
    $Role = $PsBoundParameters[$RoleParameterName]
    $Env = $PsBoundParameters[$EnvParameterName]
}

process {

    $OutDir = Join-Path (Join-Path $RootDir "out") "$Config"
    $ScriptDir = Join-Path $BuildDir "deployment"
    
    $imageName = "$Role"
    if($Config -eq "Debug")
    {
        $imageName += "_debug"
    }
    if(-not [string]::IsNullOrEmpty($Tag))
    {
        $imageName += ":$Tag"
    }
    $imageName = $imageName.ToLower()

    $success = $true

    if($Actions -contains "build")
    {
        Write-Verbose "Build the image for $imageName" -Verbose
        $dockerfile = Get-SolutionConfigValue ".solution.dockerfile" $Role
        Invoke-Expression "$ScriptDir\build-image.ps1 -ImageName $imageName -BuildPath $OutDir/$Role -Dockerfile $OutDir/$Role/$dockerfile"
    }

    if($Actions -contains "push")
    {
        Write-Verbose "Push the image of $Role" -Verbose
        Invoke-Expression "$ScriptDir\push-image.ps1 -Env $Env -sourceImage $imageName"
    }

    if($Actions -contains "deploy")
    {
        Write-Verbose "Deploy the $Role" -Verbose
        $deploymentname = Get-RepositoryConfigValue ".config.deploymentname"
        Invoke-Expression "$ScriptDir\deploy-image.ps1 -Env $Env -Role $Role -DeploymentName $deploymentname -Config $Config"
    }
}