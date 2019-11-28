$ScriptPath = Split-Path $MyInvocation.MyCommand.Path -Parent
Push-Location $ScriptPath
./AzureServiceDeployClient.ps1 -fromShortcut $true
Pop-Location