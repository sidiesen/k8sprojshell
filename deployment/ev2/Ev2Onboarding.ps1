[CmdletBinding()]
param(
    [Parameter(Mandatory=$true, Position=0)]
    $Tenant
)

# RBAC Onboarding
$RootDir = Join-Path $PSScriptRoot "../.."
Import-Module $RootDir/deployment/scripts/jsonValues.psm1
$subscriptionId = Get-TenantConfigValue ".tenant.subscriptionId" $Tenant
$tenantId = Get-TenantConfigValue ".tenant.tenantId" $Tenant
Login-AzAccount -Subscription $subscriptionId -Tenant $tenantId
$team = Get-AzADGroup -SearchString "TM-ClusterConfig"
# $team2 = Get-AzADGroup -SearchString "TM-ARDS-ReadWrite-4961"
# $admin = get-azadgroup | ?{$_.DisplayName -eq "AD-HybridRP"}
New-AzRoleAssignment -ObjectId $team.Id -RoleDefinitionName Reader -Scope /subscriptions/$subscriptionId
# New-AzRoleAssignment -ObjectId $team2.Id -RoleDefinitionName Reader -Scope /subscriptions/$subscriptionId
# New-AzRoleAssignment -ObjectId $admin.Id -RoleDefinitionName Reader -Scope /subscriptions/87ee912b-89bc-4673-93d0-0bc13a96e349

New-AzRoleAssignment -ObjectId $team.Id -RoleDefinitionName "Azure Service Deploy Release Management Contributor" -Scope /subscriptions/$subscriptionId
# New-AzRoleAssignment -ObjectId $team2.Id -RoleDefinitionName "Azure Service Deploy Release Management Contributor" -Scope /subscriptions/$subscriptionId
# New-AzRoleAssignment -ObjectId $admin.Id -RoleDefinitionName "Azure Service Deploy Release Management Contributor" -Scope /subscriptions/87ee912b-89bc-4673-93d0-0bc13a96e349

# KeyVault


# CloudVault
# While configuring a release pipeline, add a new Artifact Source, selecting "CloudVault Builds" as the type.
# If you're in CDPx, select 'CloudVault API - CDPxBuddyBuilds'
# certreq -new cloudvaultaccesscert.inf
# certreq -accept hybridrp.cloudvault.msengsys.com.p7b
# nuget install cloudvault
# fsutil behavior set SymlinkEvaluation L2L:1 R2R:1 L2R:1 R2L:1
# cloudvault.exe /vup /vt=HybridRPCloudVault /m="build:ResourceProvider;branch:master;buildnumber:0.0.1" /t=c:\temp\cvtmp_n /logdir=c:\temp\cvlog_n /dir=C:\git\OneBranch\Compute\HybridRP\out /certificatethumbprint=B9C83AE9D1C2CB5359F2C4882084EDCBFBDCE484

# EV2