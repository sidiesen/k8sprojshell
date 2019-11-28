<#
    .SYNOPSIS
        Pull AksConfig and save to KeyVault, so that EV2 can load to access AKS Cluster.

    .EXAMPLE
        .\PrepareAKSSecrets.ps1 -resourceGroupName ccrp-df -clusterName ccrpdfakscluster -keyVaultName ccrpdfkv -deploymentName deployakssecrets
#>

param(
    [Parameter(Mandatory=$true)][string]$resourceGroupName,
    [Parameter(Mandatory=$true)][string]$clusterName,
    [Parameter(Mandatory=$true)][string]$keyVaultName,
    [Parameter(Mandatory=$true)][string]$deploymentName
)

$parameters = @{}

$parameters.Add("clusterName", $clusterName)
$parameters.Add("keyVaultName", $keyVaultName)
$parameters.Add("AKSConfigSecretName", "AKSConfig")

Write-Host "`n====================== Preparing Secrets for AKS ======================"

# Use ARM template to pull secrets from AKS and save to KV
New-AzResourceGroupDeployment -Name $deploymentName -ResourceGroupName $resourceGroupName -TemplateParameterObject $parameters -TemplateFile .\AKSSecrets.json -Verbose

Write-Host "`n====================== Secrets saved in Key Vault ======================"
