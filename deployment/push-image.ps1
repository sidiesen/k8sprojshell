<#
    .SYNOPSIS
        Pushes the container image into the ACI private repositories.

    .DESCRIPTION
        Use docker to push the container image to the private repositories.

    .PARAMETER User
        The user for the private repositories. It assumes all the repositories use the same user.
    .PARAMETER Pass
        The password for the user of the private repositories. It assumes the user uses the same password.
    .PARAMETER SourceImage
        The source image.
    .PARAMETER RegistryCategory<#
    .SYNOPSIS
        Pushes the container image into the ACI private repositories.

    .DESCRIPTION
        Use docker to push the container image to the private repositories.

    .PARAMETER User
        The user for the private repositories. It assumes all the repositories use the same user.
    .PARAMETER Pass
        The password for the user of the private repositories. It assumes the user uses the same password.
    .PARAMETER SourceImage
        The source image.
    .PARAMETER RegistryCategory
        The registry category.

    .EXAMPLE
        .\Push-Image.ps1 
            -SourceImage microsoft/nanoserver `
            -RegistryCategory caas
#>
param (
    [Parameter(Mandatory=$true, Position=0)]
    [string]$SourceImage,

    [Parameter(Mandatory=$true, Position=1)]
    [string]$Env
)

$ScriptPath = Split-Path $MyInvocation.MyCommand.Path -Parent

function PushImage {
    param (
        [Parameter(Mandatory=$true)]
        [string]$Registry,

        [Parameter(Mandatory=$true)]
        [string]$SourceImage,

        [Parameter(Mandatory=$true)]
        [string]$User,

        [Parameter(Mandatory=$true)]
        [string]$Pass
    )

    $remoteImage="$Registry$SourceImage"

    Write-Host "Pushing image '$SourceImage' to '$remoteImage'"

    $repo_server = $Registry

    if ($Registry.Contains('/')) {
        $repo_server = $Registry.Split('/')[0]
    }

    $p = Start-Process docker -ArgumentList "login $repo_server -u ""$User"" -p ""$Pass""" -Wait -NoNewWindow -PassThru
    
    if ($p.ExitCode -ne 0) {
        throw "Failed to login to repository '$repo_server' with user '$User'"
    }
   
    $p = Start-Process docker -ArgumentList "tag $SourceImage $remoteImage" -Wait -NoNewWindow -PassThru

    if ($p.ExitCode -ne 0) {
        throw "Failed to tag the image '$SourceImage' to '$remoteImage'"
    }

    $p = Start-Process docker -ArgumentList "push $remoteImage" -Wait -NoNewWindow -PassThru

    if ($p.ExitCode -ne 0) {
        throw "Failed to push '$remoteImage'"
    }
}

$EnvConfig = Invoke-Expression "$ScriptPath/commonEnv.ps1 -Env $Env"
$registry = $EnvConfig.ACR
$user = Get-AzKeyVaultSecret -VaultName $EnvConfig.KeyVault -Name $EnvConfig.DeployAccount | %{$_.SecretValueText}
$pass = Get-AzKeyVaultSecret -VaultName $EnvConfig.DeployKeyVaultName -Name $EnvConfig.DeployKeySecretName | %{$_.SecretValueText}

PushImage -Registry $registry -SourceImage $SourceImage -User $user -Pass $pass

Write-Host "Image '$SourceImage' is successfully pushed to all repositories for $registry"
