# Create Azure Resources
# Login-AzAccount

$ScriptPath = Split-Path $MyInvocation.MyCommand.Path -Parent
$Ev2Path = "$ScriptPath/../ev2/"
$RootDir = Join-Path $PSScriptRoot "../.."
$BuildDir = Join-Path $PSScriptRoot ".."
# & $ScriptPath/../ev2/InstallEv2.ps1

Import-Module $BuildDir\deployment\jsonValues.psm1 -Force

# Ensure solution has been re-built before rolling out
Push-Location $RootDir
if(Test-Path "$RootDir/out/Release/")
{
    Remove-Item -Recurse "$RootDir/out/Release/"
}

$projectSet = (Get-RepositoryConfigValue ".config.solutions").trim("[]").split(",").trim("""")
foreach($project in $projectSet)
{
    if($PSVersionTable.PSEdition -eq "Desktop" -or $IsWindows)
    {
        Write-Output "Run on Windows"
        .\build.cmd $project Release
    }
    else 
    {
        Write-Output "Run on Linux"
        ./build.sh $project Release
    }
}
$buildResult = $?
Pop-Location

if(!$buildResult)
{
    Write-Error "Build failed, stopping deployment!"
    exit 1;
}

$Env = "Dev"

$EnvConfig = Invoke-Expression "$ScriptPath\commonEnv.ps1 -Env $Env"
$tenant = $EnvConfig.Tenant
$TenantConfig = Invoke-Expression "$ScriptPath\commonTenant.ps1 -Tenant $tenant"
$defaultSolution = Get-RepositoryConfigValue ".config.defaultSolution"
$SolutionConfig = Invoke-Expression "$ScriptPath\commonSolution.ps1 -Solution $defaultSolution"

Write-Output "++==============================++"
Write-Output "|| Onboarding environment '$Env' ||"
Write-Output "++==============================++"

Select-AzSubscription -Subscription $TenantConfig.SubscriptionId
$resourceGroup = $EnvConfig.ResourceGroup
$location = $EnvConfig.Location

# Create acr, aks, kv
#{
    $rg = Get-AzResourceGroup -Name $resourceGroup -Location $location
    if(!$rg)
    {
        New-AzResourceGroup -Name $resourceGroup -Location $location
    }

    $acr = $EnvConfig.ACRName
    $registry = Get-AzContainerRegistry -ResourceGroupName $resourceGroup -Name $acr
    if(!$registry)
    {
        $registry = New-AzContainerRegistry -ResourceGroupName $resourceGroup -Name $acr -Location $location -Sku Standard -EnableAdminUser
    }

    $aks = $EnvConfig.AKSCluster
    $aksInstance = Get-AzAks -ResourceGroupName $resourceGroup -Name $aks
    if(!$aksInstance)
    {
        $aksInstance = New-AzAks -ResourceGroupName $resourceGroup -Name $aks -Location $location -KubernetesVersion 1.12.8 -DnsNamePrefix "$aks-dns"
    }

    # Create KeyVault for prod ScriptPath/../ev2 and deployment secrets 
    $kv = $EnvConfig.KeyVault
    $kvInstance = Get-AzKeyVault -VaultName $kv
    if(!$kvInstance)
    {
        New-AzKeyVault -Name $kv -ResourceGroupName $resourceGroup -Location $location -EnabledForDeployment -EnabledForTemplateDeployment
    }    

    $creds = Get-AzContainerRegistryCredential -Registry $registry
    $SecretUser = ConvertTo-SecureString -String $creds.Username -AsPlainText -Force
    Set-AzKeyVaultSecret -VaultName $kv -Name $EnvConfig.DeployAccount -SecretValue $SecretUser
    $SecretPass = ConvertTo-SecureString -String $creds.Password -AsPlainText -Force
    Set-AzKeyVaultSecret -VaultName $kv -Name $EnvConfig.DeployKeySecretName -SecretValue $SecretPass
#}

# Grant access
#{
    # Grant access to Aks to pull image from ACR
    $aksId = $aksInstance.ServicePrincipalProfile.ClientId
    $acrId = $registry.Id
    $roleDefinition = (Get-AzRoleDefinition -Name acrpull).Name
    New-AzRoleAssignment -RoleDefinitionName $roleDefinition -Scope $acrId -ApplicationId $aksId
    
    # Store secrets to KeyVault
    # $s = ConvertTo-SecureString '***' -AsPlainText -Force
    # Set-AzKeyVaultSecret -VaultName $kv -Name $EnvConfig.DeployKey -SecretValue $s

    # SPN access CDPx, in redmond domain
    # $s = ConvertTo-SecureString '***' -AsPlainText -Force
    # Set-AzKeyVaultSecret -VaultName $kv -Name $EnvConfig.DeployAccount -SecretValue $s

    # $s = ConvertTo-SecureString '***' -AsPlainText -Force
    # Set-AzKeyVaultSecret -VaultName $kv -Name $EnvConfig.DeployKey -SecretValue $s
#}

<# # Create resources for service
{
    # Create CosmosDB
    $cosmos = "ccrpdevcosmosdb"
    $cosmosVN = "ccrpdevcosmosdbVN"

    $kv = 'hcrp-df-keyvault'    
    
    $s = ConvertTo-SecureString '***' -AsPlainText -Force
    Set-AzKeyVaultSecret -VaultName $kv -Name "CosmosDbAccountKey" -SecretValue $s

    $storageAccount = "hcrpdfstorage"
    $storageQueue = "hcrpdfqueue"
    
    $storage = New-AzStorageAccount -ResourceGroupName $resourceGroup -Name $storageAccount -Location $location -Kind StorageV2 -SkuName Standard_ZRS
    $key = (Get-AzStorageAccountKey -ResourceGroupName $resourceGroup -StorageAccountName $storageAccount)[0].Value
    $context = New-AzureStorageContext -StorageAccountName $storageAccount -StorageAccountKey $key
    $queue = New-AzureStorageQueue -Name $storageQueue -Context $context
    $token = New-AzureStorageQueueSASToken -Name $storageQueue -Permission a -ExpiryTime ([DateTime]::Now.AddYears(5)) -Context $context

    $s = ConvertTo-SecureString '***' -AsPlainText -Force
    Set-AzKeyVaultSecret -VaultName $kvhcrp -Name "QueueConnectionString" -SecretValue $s
} #>

# Create resources for ES
# {
    <# # Create AppInsightshcrp
    $appinsight="hcrp-df-aphcrpocation
    New-AzApplicationInsighhcrpb -ResourceGroupName $resourceGroup -Name $appinsight -location $location

    # Create Continuous Export
    $storageAccount = "hcrpdfstoragetelemetry"
    $storageQueue = "hcrpdfqueuetelemetry"
    
    $storage = New-AzStorageAccount -ResourceGroupName $resourceGroup -Name $storageAccount -Location $location -Kind StorageV2 -SkuName Standard_ZRS
    $key = (Get-AzStorageAccountKey -ResourceGroupName $resourceGroup -StorageAccountName $storageAccount)[0].Value
    $context = New-AzureStorageContext -StorageAccountName $storageAccount -StorageAccountKey $key
    New-AzureStorageQueue -Name $storageQueue -Context $context
    $token = New-AzureStorageQueueSASToken -Name $storageQueue -Permission a -ExpiryTime ([DateTime]::Now.AddYears(5)) -Context $context
    
    $queuesas = "https://$storageAccount.queue.core.windows.net/$storageQueue$token" 
    $document = ("Request","Exception","Custom Event","Trace","Metric","Page Load","Page View","Dependency","Availability","Performance Counter")
    New-AzApplicationInsightsContinuousExport -ResourceGroupName $resourceGroup -Name $appinsight -DocumentType $document -StorageAccountId $storage.Id -StorageLocation $location -StorageSASUri $queuesas

    $eventhub = "hcrp-df-eventhub-telemetry"
    New-AzEventHub -ResourceGroupName $resourceGroup -Name $eventhub -Location eastus2 #>

    # Create CloudVault for EV2
    New-AzStorageAccount -ResourceGroupName $resourceGroup -Name $EnvConfig.CloudVault -Location $location -Kind StorageV2 -SkuName Standard_ZRS -AccessTier Cool

    # Add AKS access to keyvault
    Push-Location $Ev2Path/Scripts
    .\PrepareAKSSecrets.ps1 -resourceGroupName $resourceGroup -clusterName $aks -keyVaultName $kv -deploymentName $EnvConfig.AKSSecrets
    Pop-Location

# }

$subscriptionId = $TenantConfig.SubscriptionId
$team = Get-AzADGroup -SearchString "TM-ARDS-ReadOnly-6e27"
$team2 = Get-AzADGroup -SearchString "TM-ARDS-ReadWrite-4961"

Set-AzKeyVaultAccessPolicy -VaultName $EnvConfig.KeyVault -ObjectId $team.Id -ApplicationId $TenantConfig.Ev2DeploymentApplication -PermissionsToSecrets Get -PermissionsToCertificates Get
Set-AzKeyVaultAccessPolicy -VaultName $EnvConfig.KeyVault -ObjectId $team2.Id -ApplicationId $TenantConfig.Ev2DeploymentApplication -PermissionsToSecrets Get -PermissionsToCertificates Get

# For Nginx-Ingress. https://docs.microsoft.com/en-us/azure/aks/ingress-own-tls
#{
    # $kvname = 'hcrp-df-keyvault'
    # $Policy = New-AzKeyVaultCertificatePolicy -SecretContentType "application/x-pkcs12" -SubjectName " CN=rp.hybridcompute-ppe.trafficmanager.net" -IssuerName "CN=Microsoft IT TLS CA 4, OU=Microsoft IT, O=Microsoft Corporation, L=Redmond, S=Washington, C=US" -ValidityInMonths 24 -ReuseKeyOnRenewal
    # Add-AzKeyVaultCertificate -VaultName kvname -Name "hcrp-df-frontend-clustercert" -CertificatePolicy $Policy

    # openssl> pkcs12 -in C:\Work\HybridRP\certs\his.hybridcompute-ppe.trafficmanager.net.pfx -out c:\Work\HybridRP\certs\hiscert.txt -nodes

    $deploymentName = $SolutionConfig.NginxDeploymentName
    Invoke-Expression "$ScriptPath\deploy-image.ps1 -Env $Env -Role nginx-ingress -DeploymentName $deploymentName"
#}

# push image dev registry
#{
    $kv = $EnvConfig.KeyVault
    $user = Get-AzKeyVaultSecret -VaultName $kv -Name $EnvConfig.DeployAccount | %{$_.SecretValueText}
    $key  = Get-AzKeyVaultSecret -VaultName $kv -Name $EnvConfig.DeployKeySecretName | %{$_.SecretValueText}
    $key | docker login "$($EnvConfig.ACRName).azurecr.io/" -u $user --password-stdin
    kubectl create secret docker-registry acrregistrykey --docker-server=$EnvConfig.ACR --docker-username=$user --docker-password=$key

    foreach($project in $projectSet)
    {
        Invoke-Expression "$BuildDir\scripts\publishImage.ps1 -Role $project -Env Dev -Config Release -Actions push,deploy"
    }
#}