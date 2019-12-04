Write-Output ""
Write-Output "Initializing K8s build and deployment environment"
Write-Output "================================================="
Write-Output ""

Invoke-Expression "git status" -ErrorVariable err -OutVariable out 2> $null > $null
$isNotGit = ($err -like '*not a git repository*')
if($isNotGit)
{
    Write-Error "Script must be run in a git repository!"
    Exit 1
}

$repoUrl = "https://github.com/sidiesen/k8sprojshell.git"

Invoke-Expression "git submodule add $repoUrl build" -ErrorVariable err -OutVariable out 2> $null > $null

Copy-Item -Path ".\build\dropin\*" -Destination "." -Recurse -Force

New-Item -ItemType directory -Path ".\src\templates"
New-Item -ItemType directory -Path ".\.pipelines"