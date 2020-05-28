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

    Import-Module $RootDir/build/deployment/jsonValues.psm1

    $RoleParameterName = 'Role'
    $EnvParameterName = 'Env'
    $RuntimeParameterDictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary

    $RoleAttributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
    $ParameterAttribute = New-Object System.Management.Automation.ParameterAttribute
    $ParameterAttribute.Mandatory = $true
    $ParameterAttribute.Position = 1
    $RoleAttributeCollection.Add($ParameterAttribute)
    $solutionSet = (Get-RepositoryConfigValue ".config.solutions").trim("[]").split(",").trim("""")
    $RoleValidateSetAttribute = New-Object System.Management.Automation.ValidateSetAttribute([String[]]@("all") + $solutionSet)

    $RoleAttributeCollection.Add($RoleValidateSetAttribute)

    $EnvAttributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
    $ParameterAttribute = New-Object System.Management.Automation.ParameterAttribute
    $ParameterAttribute.Mandatory = $false
    $ParameterAttribute.Position = 0
    $EnvAttributeCollection.Add($ParameterAttribute)
    $envSet = (Get-RepositoryConfigValue ".config.environments").trim("[]").split(",").trim("""")
    $EnvValidateSetAttribute = New-Object System.Management.Automation.ValidateSetAttribute([String[]]@("Dev") + $envSet)
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
    $SrcDir = Join-Path $RootDir "src"
    $ScriptDir = Join-Path $RootDir "build\deployment"
    
    $allRoles = [String[]]@()

    if("$Role" -eq "all")
    {
        $allRoles = $allRoles + $solutionSet
    }
    else 
    {
        $allRoles = $allRoles + "$Role"
    }

    foreach($currentRole in $allRoles)
    {
        $role = $currentRole
        $dockerPathAppend = ""
        if($Config -eq "Debug")
        {
            $currentRole += "_debug"
            $dockerPathAppend = "/debug"
        }
        if(-not [string]::IsNullOrEmpty($Tag))
        {
            $currentRole += ":$Tag"
        }
        $currentRole = $currentRole.ToLower()

        if($Actions -contains "build")
        {
            Write-Verbose "Build the image for $currentRole" -Verbose
            $dockerfile = Get-SolutionConfigValue ".solution.dockerfile" $role
            if((Get-SolutionConfigValue ".solution.dockerBuild" $role) -eq "true") 
            {
                $dockerSrcPath = Get-SolutionConfigValue ".solution.dockerfilePath" $role
                $newDockerSrcPath = "$dockerSrcPath$dockerPathAppend"
                if(Test-Path "$SrcDir/$newDockerSrcPath")
                {
                    $dockerSrcPath = $newDockerSrcPath
                }

                $buildPath = ""
                if((Get-SolutionConfigValue ".solution.build" $role) -eq "true")
                {
                    $buildPath = "$OutDir/$role"
                }
                else 
                {
                    $dockerContentPath = Get-SolutionConfigValue ".solution.dockerContentPath" $role
                    $buildPath = "$SrcDir/$dockerContentPath"
                }
                Invoke-Expression "$ScriptDir\build-image.ps1 -ImageName $currentRole -BuildPath $buildPath -Dockerfile $SrcDir/$dockerSrcPath/$dockerfile"
            }
        }

        if(($Actions -contains "push") -and ((Get-SolutionConfigValue ".solution.dockerBuild" $role) -eq "true"))
        {
            Write-Verbose "Push the image of $currentRole" -Verbose
            Invoke-Expression "$ScriptDir\push-image.ps1 -Env $Env -sourceImage $currentRole"
        }

        if($Actions -contains "deploy")
        {
            Write-Verbose "Deploy $currentRole" -Verbose
            Invoke-Expression "$ScriptDir\deploy-image.ps1 -Env $Env -Role $currentRole -Config $Config"
        }
    }
}