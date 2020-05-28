param (
    [Parameter(Mandatory=$false, Position=0)]
    [string]$SolutionFile,

    [Parameter(Mandatory=$false, Position=1)]
    [string]$Config
)

$RootDir = Join-Path $PSScriptRoot ".."
$OutDir = Join-Path (Join-Path $RootDir "out") "$Config"

Import-Module $RootDir/deployment/jsonValues.psm1

if("$Config" -eq "")
{
    $Config = Get-RepositoryConfigValue ".config.defaultConfig"
}

# This file is provided by CDPX and we can use it to determine whether we're building locally or not
$IsOfficialBuild = $false
if (Test-Path $RootDir\.version\numeric.fileversion.info.noleadingzeros)
{
    $IsOfficialBuild = $true
    $Env:SKIP_LOCAL_SECRET_FETCH = "True"
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

if(("$SolutionFile" -eq "") -or ("$SolutionFile" -eq "all"))
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
    if((Get-SolutionConfigValue ".solution.build" $sln) -eq "true") 
    {
        $slnFile = Get-ChildItem -Path $RootDir\src\$proj -Name "*$sln.sln" -Recurse

        $RootDir = Join-Path $PSScriptRoot ".."
        $outPath = Join-Path $OutDir (Get-Item $RootDir\src\$slnFile).BaseName
        dotnet publish "$RootDir\src\$slnFile" -c $Config -o $outPath
        if(! $?) { 
            Write-Error "Failed to publish src\$slnFile."
        }
    }
}

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
    $ValueFiles = Get-ChildItem -Path "$OutDir\charts\*values*.yaml" -Recurse
    foreach($valueFile in $ValueFiles)
    {
        ((Get-Content $valueFile -Raw) -replace "!BUILDVERSION!","$BuildVersion") | Set-Content $valueFile
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

# move deploy charts to output folder
if (!$IsLinux)
{
    mkdir -p $OutDir\ev2deploy 
    mkdir -p $OutDir\ev2deploy\bin
    mkdir -p $OutDir\ev2deploy\charts
    if (Test-Path $OutDir\ServiceGroupRoot)
    {
        rm -Path "$OutDir\ServiceGroupRoot" -Force -Recurse
    }
    XCOPY /Q /S /i /Y "$RootDir\deployment\ev2\ServiceGroupRoot" "$OutDir\ServiceGroupRoot"
    XCOPY /Q /S /i /Y "$OutDir\ServiceGroupRoot\bin" "$OutDir\ev2deploy\bin"
    XCOPY /Q /S /i /Y "$OutDir\charts" "$OutDir\ev2deploy\charts"
}
else
{
    mkdir -p $OutDir/ev2deploy 
    mkdir -p $OutDir/ev2deploy/bin
    mkdir -p $OutDir/ev2deploy/charts
    cp -r $RootDir/deployment/ev2/ServiceGroupRoot/. $OutDir/ServiceGroupRoot/
    cp -r $OutDir/ServiceGroupRoot/bin/. $OutDir/ev2deploy/bin
    cp -f -r $OutDir/charts/. $OutDir/ev2deploy/charts
}

# package deploy.sh and helm charts into tar ball
Write-Output "Building Ev2 deploy package"
tar -cvf $OutDir/ServiceGroupRoot/bin/ev2deploy.tar -C $OutDir ev2deploy 1> $null 2> $null
Remove-Item $OutDir\ev2deploy -r -fo
Write-Output "Finished building Ev2 deploy package"

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
        Invoke-Expression "$RootDir\build\publishImage.ps1 -Role $sln -Config $Config $TagParam -Actions build"
        Pop-Location
    }
}

exit 0