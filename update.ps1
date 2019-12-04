# figure out if environment branch is up to date
Invoke-Expression "git status -uno" -ErrorVariable err -OutVariable out 2> $null > $null
if($out -like '*branch is up to date with*')
{
    Write-Output "Environment is up to date!"
    Exit 0
}

Push-Location ./build
Invoke-Expression "git pull origin master" -ErrorVariable err -OutVariable out 2> $null > $null
$result = $?
Pop-Location

Copy-Item -Path ".\build\dropin\build.cmd" -Destination "." -Force
Copy-Item -Path ".\build\dropin\build.sh" -Destination "." -Force
Copy-Item -Path ".\build\dropin\.gitignore" -Destination "." -Force
Copy-Item -Path ".\build\dropin\src\Directory.Build.props" -Destination ".\src\" -Force

if($result)
{
    Write-Output "Environment updated!"
}
else
{
    Write-Output "Unable to update environment!"
    Write-Output "Try running 'git pull origin master' in ./build"
}