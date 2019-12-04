<#
    .SYNOPSIS
        Builds the container image.

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

    .EXAMPLE
        .\Build-Image.ps1 `
            -ImageName logging-agent-windows `
            -BuildPath .\images\logging-agent `
            -Dockerfile .\images\logging-agent\windows\Dockerfile 
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
)

$p = Start-Process docker -ArgumentList "build -t $ImageName -f $Dockerfile $BuildPath" -Wait -NoNewWindow -PassThru

if ($p.ExitCode -ne 0) {
    throw "Failed to build the image '$ImageName'. Dockerfile: '$Dockerfile'"
}