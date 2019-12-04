param(
    [Parameter(Mandatory=$true, Position=0)]
    [string]$Env,

    [Parameter(Mandatory=$false, Position=1)]
    [string]$Config = "Debug"
)

$RootDir = Join-Path $PSScriptRoot "../.."
Import-Module $RootDir/deployment/jsonValues.psm1

$ResourceGroup = Get-EnvironmentConfigValue ".environment.Infrastructure.resourceGroup" $Env
$Location = Get-EnvironmentConfigValue ".environment.location" $Env
$LocationShortName = Get-EnvironmentConfigValue ".environment.locationShortName" $Env
$AKSCluster = Get-EnvironmentConfigValue ".environment.AKS.AKSCluster" $Env
$AKSSecrets = Get-EnvironmentConfigValue ".environment.AKS.AKSSecrets" $Env
$ACR = Get-EnvironmentConfigValue ".environment.ACR.ACR" $Env
$ACRName = Get-EnvironmentConfigValue ".environment.ACR.ACRName" $Env
$KeyVault = Get-EnvironmentConfigValue ".environment.Infrastructure.keyVault" $Env
$CloudVault = Get-EnvironmentConfigValue ".environment.Infrastructure.cloudVault" $Env
$DeployAccount = Get-EnvironmentConfigValue ".environment.Deployment.deployAccount" $Env
$KubeConfigSecretName = Get-EnvironmentConfigValue ".environment.Infrastructure.kubeConfigSecretName" $Env
$Tenant = Get-EnvironmentConfigValue ".environment.Infrastructure.tenant" $Env
$DeployKeyVaultName = Get-EnvironmentConfigValue ".environment.Deployment.deployKeyVaultName" $Env
$DeployKeySecretName = Get-EnvironmentConfigValue ".environment.Deployment.deployKeySecretName" $Env
$ValuesYaml = Get-EnvironmentConfigValue ".environment.Helm.valuesYaml" $Env
$GlobalKeyVaultSecurityGroup = Get-EnvironmentConfigValue ".environment.Infrastructure.globalKeyVaultSecurityGroup" $Env
$GlobalResourceGroup = Get-EnvironmentConfigValue ".environment.Infrastructure.globalResourceGroup" $Env
$AadServerAppIdSecretName = Get-EnvironmentConfigValue ".environment.SecretNames.AadServerAppId" $Env
$AadServerAppKeySecretName = Get-EnvironmentConfigValue ".environment.SecretNames.AadServerAppKey" $Env
$AadClientAppIdSecretName = Get-EnvironmentConfigValue ".environment.SecretNames.AadClientAppId" $Env
$NginXDeploymentNamespace = Get-RepositoryConfigValue ".config.GlobalServiceConfig.NginXDeploymentNamespace"
$DeploymentNamespace = Get-RepositoryConfigValue ".config.GlobalServiceConfig.DeploymentNamespace"
$GlobalKeyVaultName = Get-EnvironmentConfigValue ".environment.Infrastructure.GlobalKeyVaultName" $Env
$DeploymentServicePrincipalName = Get-EnvironmentConfigValue ".environment.Deployment.DeploymentServicePrincipalName" $Env
$CanaryFallbackLocation = Get-EnvironmentConfigValue ".environment.Deployment.CanaryFallbackLocation" $Env
$Environment = Get-EnvironmentConfigValue ".environment.environment" $Env

return @{
    ResourceGroup=$ResourceGroup; 
    Location=$Location;
    AKSCluster=$AKSCluster;
    AKSSecrets=$AKSSecrets;
    ACR=$ACR;
    ACRName=$ACRName;
    KeyVault=$KeyVault;
    CloudVault=$CloudVault;
    DeployAccount=$DeployAccount;
    KubeConfigSecretName=$KubeConfigSecretName;
    Tenant=$Tenant;
    DeployKeyVaultName=$DeployKeyVaultName;
    DeployKeySecretName=$DeployKeySecretName;
    ValuesYaml=$ValuesYaml;
    LocationShortName=$LocationShortName;
    GlobalKeyVaultSecurityGroup=$GlobalKeyVaultSecurityGroup;
    GlobalResourceGroup=$GlobalResourceGroup;
    AadServerAppIdSecretName=$AadServerAppIdSecretName;
    AadServerAppKeySecretName=$AadServerAppKeySecretName;
    AadClientAppIdSecretName=$AadClientAppIdSecretName;
    NginXDeploymentNamespace=$NginXDeploymentNamespace;
    DeploymentNamespace=$DeploymentNamespace;
    GlobalKeyVaultName=$GlobalKeyVaultName;
    DeploymentServicePrincipalName=$DeploymentServicePrincipalName;
    CanaryFallbackLocation=$CanaryFallbackLocation;
    Environment=$Environment;
}

