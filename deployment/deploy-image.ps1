param (
    [Parameter(Mandatory=$true, Position=2)]
    [string]$DeploymentName,

    [Parameter(Mandatory=$false, Position=3)]
    [ValidateSet("Debug", "Release")]
    [string]$Config = "Release"
)

DynamicParam {
    $RootDir = Join-Path $PSScriptRoot "../.."
    Import-Module $RootDir/deployment/jsonValues.psm1

    $RoleParameterName = 'Role'
    $EnvParameterName = 'Env'
    $RuntimeParameterDictionary = New-Object System.Management.Automation.RuntimeDefinedParameterDictionary

    $RoleAttributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
    $ParameterAttribute = New-Object System.Management.Automation.ParameterAttribute
    $ParameterAttribute.Mandatory = $true
    $ParameterAttribute.Position = 1
    $RoleAttributeCollection.Add($ParameterAttribute)
    $solutionSet = (Get-RepositoryConfigValue ".config.solutions").trim("[]").split(",").trim("""")
    $RoleValidateSetAttribute = New-Object System.Management.Automation.ValidateSetAttribute([String[]]@("nginx-ingress") + $solutionSet)
    $RoleAttributeCollection.Add($RoleValidateSetAttribute)

    $EnvAttributeCollection = New-Object System.Collections.ObjectModel.Collection[System.Attribute]
    $ParameterAttribute = New-Object System.Management.Automation.ParameterAttribute
    $ParameterAttribute.Mandatory = $true
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
    $ScriptPath = Split-Path $MyInvocation.MyCommand.Path -Parent

    $EnvConfig = Invoke-Expression "$ScriptPath/commonEnv.ps1 -Env $Env"
    $tenant = $EnvConfig.Tenant
    $TenantConfig = Invoke-Expression "$ScriptPath/commonTenant.ps1 -Tenant $tenant"

    $ChartsPath = "$ScriptPath\..\..\out\$Config\charts\" + $Role;
    $NginxChartsPath = "$ScriptPath\..\..\out\$Config\charts\nginxIngress";

    function Get-AksClusterConfig
    {
        Write-Verbose "Get-AksClusterConfig"
        Select-AzSubscription $TenantConfig.SubscriptionId
        $AKSConfig = Get-AzKeyVaultSecret -VaultName $EnvConfig.KeyVault -Name $EnvConfig.KubeConfigSecretName | %{$_.SecretValueText}
        $AKSConfig | Out-File $ScriptPath/aksConnectionConfig

        # Get KubeConfig
        Import-AzAksCredential -ResourceGroupName $EnvConfig.ResourceGroup -Name $EnvConfig.AKSCluster -Admin -Force
    }

    function Initialize-Helm
    {
        Get-AksClusterConfig
        
        Write-Verbose "Initialize-Helm"
        $AKSCluster = $EnvConfig.AKSCluster
        Invoke-Expression "kubectl config --kubeconfig=$ScriptPath/aksConnectionConfig use-context $AKSCluster"
        Invoke-Expression "kubectl create -f $ScriptPath/tiller-rbac-config.yaml"

        Invoke-Expression "helm init --upgrade --wait --service-account tiller"
    }

    function Install-NginxIngress
    {
        Initialize-Helm

        Push-Location $NginxChartsPath
        $AKSCluster = $EnvConfig.AKSCluster
        $NginXDeploymentNamespace = $EnvConfig.NginXDeploymentNamespace
        Invoke-Expression "helm upgrade $DeploymentName stable/nginx-ingress --install --namespace $NginXDeploymentNamespace --kubeconfig $ScriptPath/aksConnectionConfig --kube-context $AKSCluster --values ./values.yaml"
        #Invoke-Expression "helm install --kubeconfig $ScriptPath\aksConnectionConfig --kube-context $AKSCluster --values .\values.yaml --name haikuingress stable/nginx-ingress"
        Pop-Location

        # kubectl create secret tls ccrp-nginx-cert --key C:\Work\HybridRP\certs\hcrpdfcluster.key --cert C:\Work\HybridRP\certs\hcrpdfcluster.crt
    }

    function Install-Image
    {
        Initialize-Helm

        # upgrade the deploy, install if it doesn't exist. 
        Push-Location $ChartsPath
        $AKSCluster = $EnvConfig.AKSCluster
        $ValuesYaml = $EnvConfig.ValuesYaml
        $DeploymentNamespace = $EnvConfig.DeploymentNamespace
        Invoke-Expression "helm upgrade $DeploymentName . --install --namespace $DeploymentNamespace --kubeconfig $ScriptPath/aksConnectionConfig --kube-context $AKSCluster --values ./$ValuesYaml --recreate-pods"
        Pop-Location
    }

    if($Role -eq "nginx-ingress")
    {
        Install-NginxIngress
    }
    else 
    {
        Install-Image
    }
}