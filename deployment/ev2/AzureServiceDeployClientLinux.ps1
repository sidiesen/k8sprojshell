$InstallDir = "/opt/microsoft/Ev2/"

if(!(Test-Path $InstallDir )) {
    New-Item -ItemType Directory $InstallDir | Out-Null
}

$env:LOCALAPPDATA=$InstallDir

./InstallEv2.ps1