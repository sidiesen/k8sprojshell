param (
    [Parameter(Mandatory=$false, Position=0)]
    [string]$SolutionFile,

    [Parameter(Mandatory=$false, Position=1)]
    [string]$Config
)

$RootDir = Join-Path $PSScriptRoot "..\.."
$BuildDir = Join-Path $PSScriptRoot ".."
$OutDir = Join-Path (Join-Path $RootDir "out") "$Config"

Import-Module $BuildDir/deployment/jsonValues.psm1

if("$Config" -eq "")
{
    $Config = Get-RepositoryConfigValue ".config.defaultConfig"
}

$BuildDockerImageEnv=$Env:buildDockerImage;
$BuildDockerImage=$true
if("$BuildDockerImageEnv" -eq "False")
{
    $BuildDockerImage = $false
}
elseif("$BuildDockerImageEnv" -ne "True")
{
    Write-Error("Invalid argument for parameter 'BuildDockerImage', allowed values: 'True'/'False'!");
    exit 1
}

$SolutionsToBuild = @()

if("$SolutionFile" -eq "")
{
    $solutionSet = (Get-RepositoryConfigValue ".config.solutions").trim("[]").split(",").trim("""")
    # build all projects and remove well known support projects from list
    foreach($sln in $solutionSet)
    {
        $SolutionsToBuild += $sln
    }
}
else {
    $SolutionsToBuild = @($SolutionFile)
}

foreach($sln in $SolutionsToBuild)
{
    $slnLang = (Get-RepositoryConfigValue ".solutions.$sln.language")
    if($slnLang -eq 'csharp')
    {
        $languageBuildScript = "$PSScriptRoot\languages\csharp\build.ps1"
        Invoke-Expression "$languageBuildScript -Solution $sln -Config $Config -OutDir $OutDir"
    }
    elseif($slnLang -eq 'go')
    {
        $languageBuildScript = "$PSScriptRoot\languages\go\build.ps1"
        Invoke-Expression "$languageBuildScript -Solution $sln -Config $Config -OutDir $OutDir"
    }
    else
    {
        Write-Error "'$slnLang' is an unknown build language! (Solution '$sln')"
        Exit 1
    }
}

# todo: sathish do we need to enable ?
# testsolution=`find / -name HybridCompute.sln`
# echo "test $testsolution"
# dotnet test $testsolution
# if [ $? -ne 0 ]; then
#     echo "Unit tests failed for $testsolution."
#     exit 1
# fi

# if (!$IsLinux)
# {
#     XCOPY /Q /S /Y "$RootDir\deployment\charts" "$OutDir\charts\"
#     XCOPY /Q /S /Y "$RootDir\deployment\ev2\ServiceGroupRoot" "$OutDir\ServiceGroupRoot\"
# }
# else
# {
#     cp -f -r $RootDir/deployment/charts $OutDir/charts/
#     cp -f -r $RootDir/deployment/ev2/ServiceGroupRoot $OutDir/ServiceGroupRoot
# }



# replace charts and default values with the Build version
if (Test-Path $RootDir\.version\numeric.fileversion.info.noleadingzeros)
{
    $FileContent = Get-Content $RootDir\.version\numeric.fileversion.info.noleadingzeros
    $BuildVersion = $FileContent -replace '[^\x09\x10\x13\x20-\x7e]+', ''

    $ChartFiles = Get-ChildItem -Path $OutDir\charts -Name "Chart.yaml" -Recurse
    foreach($chartFile in $ChartFiles)
    {
        ((Get-Content $OutDir\charts\$chartFile -Raw) -replace "!BUILDVERSION!","$BuildVersion") | Set-Content $OutDir\charts\$chartFile
    }
    $ValueFiles = Get-ChildItem -Path $OutDir\charts -Name "values.yaml" -Recurse
    foreach($valueFile in $ValueFiles)
    {
        ((Get-Content $OutDir\charts\$valueFile -Raw) -replace "!BUILDVERSION!","$BuildVersion") | Set-Content $OutDir\charts\$valueFile
    }
    $buildverFile = "$RootDir\deployment\ev2\ServiceGroupRoot\buildver.txt"
    ((Get-Content $buildverFile -Raw) -replace "!BUILDVERSION!","$BuildVersion") | Set-Content $buildverFile
    $deployScript = "$RootDir\deployment\ev2\ServiceGroupRoot\bin\deploy.ps1"
    ((Get-Content $deployScript -Raw) -replace "!BUILDVERSION!","$BuildVersion") | Set-Content $deployScript
}
else 
{
    Write-Warning "could not find CDPX generated version file $RootDir\.version\numeric.fileversion.info.noleadingzeros"
}

$SemanticBuildVersion=""

# replace CDPX build version for use in referencing the AME container registry tags
if(Test-Path $RootDir\.version\semantic.fileversion.info)
{
    $FileContent = Get-Content $RootDir\.version\semantic.fileversion.info
    $SemanticBuildVersion = $FileContent -replace '[^\x09\x10\x13\x20-\x7e]+', ''

    $deployScript = "$RootDir\deployment\ev2\ServiceGroupRoot\bin\deploy.ps1"
    ((Get-Content $deployScript -Raw) -replace "!CDPXBUILDVERSION!","$SemanticBuildVersion") | Set-Content $deployScript
}
else 
{
    Write-Warning "could not find CDPX generated version file semantic.fileversion.info"
}

# # move deploy charts to output folder
# if (!$IsLinux)
# {
#     mkdir -p $OutDir\ev2deploy 
#     mkdir -p $OutDir\ev2deploy\bin
#     mkdir -p $OutDir\ev2deploy\charts
#     if (Test-Path $OutDir\ServiceGroupRoot)
#     {
#         rm -Path "$OutDir\ServiceGroupRoot" -Force -Recurse
#     }
#     XCOPY /Q /S /i /Y "$RootDir\deployment\ev2\ServiceGroupRoot" "$OutDir\ServiceGroupRoot"
#     XCOPY /Q /S /i /Y "$OutDir\ServiceGroupRoot\bin" "$OutDir\ev2deploy\bin"
#     XCOPY /Q /S /i /Y "$OutDir\charts" "$OutDir\ev2deploy\charts"
# }
# else
# {
#     mkdir -p $OutDir/ev2deploy 
#     mkdir -p $OutDir/ev2deploy/bin
#     mkdir -p $OutDir/ev2deploy/charts
#     cp -r $RootDir/deployment/ev2/ServiceGroupRoot/. $OutDir/ServiceGroupRoot/
#     cp -r $OutDir/ServiceGroupRoot/bin/. $OutDir/ev2deploy/bin
#     cp -f -r $OutDir/charts/. $OutDir/ev2deploy/charts
# }

# # package deploy.sh and helm charts into tar ball
# Write-Output "Building Ev2 deploy package"
# tar -cvf $OutDir/ServiceGroupRoot/bin/ev2deploy.tar -C $OutDir ev2deploy 2> $null
# Remove-Item $OutDir\ev2deploy -r -fo
# Write-Output "Finished building Ev2 deploy package"

$TagParam = ""
if($SemanticBuildVersion)
{
    $TagParam = "-Tag $SemanticBuildVersion"
}

if($BuildDockerImage)
{
    foreach($sln in $SolutionsToBuild)
    {
        Push-Location $RootDir
        Invoke-Expression "$RootDir\build\scripts\publishImage.ps1 -Role $sln -Config $Config $TagParam -Actions build"
        Pop-Location
    }
}

exit 0