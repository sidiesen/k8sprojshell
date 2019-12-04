Write-Output "Initializing K8s build and deployment environment"
Write-Output "=================================================\n"

(& git status) | Out-Null
if(!$?)
{
    Write-Error "Script must be run in a git repository!"
    Exit 1
}

repoUrl = "https://github.com/sidiesen/k8sprojshell.git"

& git submodule add $repourl build



