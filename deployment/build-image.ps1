<#
    .SYNOPSIS
        Builds the container image for HybridRP.

    .DESCRIPTION
        Use docker to build the container image and push it to the private repositories.

    .PARAMETER User
        The user for the private repositories. It assumes all the repositories use the same user.
    .PARAMETER Pass
        The password for the user of the private repositories. It assumes the user uses the same password.
    .PARAMETER ImageName
        The image name.
    .PARAMETER BuildPath
        The build path.
    .PARAMETER Dockerfile
        The docker file.
    .PARAMETER RegistryCategory
        The registry category.

    .EXAMPLE
        .\Build-Image.ps1 `
            -ImageName logging-agent-windows `
            -BuildPath .\images\logging-agent `
            -Dockerfile .\images\logging-agent\windows\Dockerfile `
            -RegistryCategory caas
#>
param (
    [Parameter(Mandatory=$false, Position=0)]
    [string]$User,

    [Parameter(Mandatory=$false, Position=1)]
    [string]$Pass,

    [Parameter(Mandatory=$true, Position=2)]
    [string]$ImageName,

    [Parameter(Mandatory=$true, Position=3)]
    [string]$BuildPath,

    [Parameter(Mandatory=$true, Position=4)]
    [string]$Dockerfile

    # [Parameter(Mandatory=$true, Position=5)]
    # [ValidateSet("hybridrp")]
    # [string]$RegistryCategory
)

# if ($BUILD_SOURCEBRANCHNAME -eq "master" -and $BUILD_SOURCEBRANCH -ne "refs/heads/master") {
#     throw "'$BUILD_SOURCEBRANCH' cannot be used to build image, please use another source branch."
# }

$p = Start-Process docker -ArgumentList "build -t $ImageName -f $Dockerfile $BuildPath" -Wait -NoNewWindow -PassThru

if ($p.ExitCode -ne 0) {
    throw "Failed to build the image '$ImageName'. Dockerfile: '$Dockerfile'"
}

# $scriptDir = Split-Path $script:MyInvocation.MyCommand.Path

# Set-Location $scriptDir

# .\Push-Image.ps1 -User $User -Pass $Pass -SourceImage $ImageName -RegistryCategory $RegistryCategory