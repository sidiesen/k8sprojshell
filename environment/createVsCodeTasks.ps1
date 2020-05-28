$RootDir = Join-Path $PSScriptRoot "..\.."
$BuildDir = Join-Path $PSScriptRoot ".."

Import-Module $BuildDir/deployment/jsonValues.psm1

$solutionSet = (Get-RepositoryConfigValue ".config.solutions").trim("[]").split(",").trim("""")

function Create-DockerBuildTask
{
    [CmdletBinding()]
    [OutputType([String])]
    param(
        [string]$SolutionName,
        [string]$Config
    )

    return "{
            `"label`": `"$($SolutionName): Build Docker image $Config`",
            `"command`": `"./build.sh`",
            `"windows`": {
                `"command`": `".\\build.cmd`"
            },
            `"type`": `"shell`",
            `"args`": [
                `"$SolutionName`",
                `"$Config`"
            ],
            `"group`": {
                `"isDefault`": true,
                `"kind`": `"build`"
            },
            `"presentation`": {
                `"reveal`": `"always`"
            },
            `"problemMatcher`": `"`$msCompile`"
        }"
}

function Create-SolutionBuildTask
{
    [CmdletBinding()]
    [OutputType([String])]
    param(
        [string]$SolutionName,
        [string]$Config
    )

    return "{
            `"label`": `"$($SolutionName): Build Solution $Config`",
            `"command`": `"./build.sh`",
            `"windows`": {
                `"command`": `".\\build.cmd`"
            },
            `"type`": `"shell`",
            `"args`": [
                `"$SolutionName`",
                `"$Config`",
                `"False`"
            ],
            `"group`": {
                `"isDefault`": true,
                `"kind`": `"build`"
            },
            `"presentation`": {
                `"reveal`": `"always`"
            },
            `"problemMatcher`": `"`$msCompile`"
        }"
}

function Create-DockerRunTask
{
    [CmdletBinding()]
    [OutputType([String])]
    param(
        [string]$SolutionName,
        [string]$ContainerName,
        [int]$HttpPortNo,
        [int]$HttpsPortNo
    )

    $imageName = "$($SolutionName)_debug".ToLower()

    return "{
            `"label`": `"$($SolutionName): Run Docker container local (Debug, port $HttpPortNo/$HttpsPortNo)`",
            `"command`": `"docker`",
            `"type`": `"shell`",
            `"args`": [
                `"run`",
                `"--rm`",
                `"-d`",
                `"--name`",
                `"$ContainerName`",
                `"-p`",
                `"$($HttpsPortNo):443/tcp`",
                `"-p`",
                `"$($HttpPortNo):80/tcp`",
                `"$imageName:latest`"
            ],
            `"group`": `"none`",
            `"presentation`": {
                `"reveal`": `"always`"
            },
            `"dependsOn`":[
                `"$($SolutionName): Build Docker image Debug`"
            ],
            `"problemMatcher`": `"`$msCompile`"
        },
        {
            `"label`": `"$($SolutionName): Run Docker container local WaitAttach (Debug, port $HttpPortNo/$HttpsPortNo)`",
            `"command`": `"docker`",
            `"type`": `"shell`",
            `"args`": [
                `"run`",
                `"--rm`",
                `"-d`",
                `"--env-file`",
                `"./.vscode/localenv/ccdp.env`",
                `"-e`",
                `"\`"WaitAttach=1\`"`",
                `"--name`",
                `"$ContainerName`",
                `"-p`",
                `"$($HttpsPortNo):443/tcp`",
                `"-p`",
                `"$($HttpPortNo):80/tcp`",
                `"clusterconfigdp_debug:latest`"
            ],
            `"group`": `"none`",
            `"presentation`": {
                `"reveal`": `"always`"
            },
            `"dependsOn`":[
                `"$($SolutionName): Build Docker image Debug`"
            ],
            `"problemMatcher`": `"`$msCompile`"
        }"
}

function Create-DockerStopTask
{
    [CmdletBinding()]
    [OutputType([String])]
    param(
        [string]$SolutionName,
        [string]$ContainerName
    )
    return "{
            `"label`": `"$($SolutionName): Stop Docker container local (Debug)`",
            `"command`": `"docker`",
            `"type`": `"shell`",
            `"args`": [
                `"stop`",
                `"$ContainerName`"
            ],
            `"group`": `"none`",
            `"presentation`": {
                `"reveal`": `"always`"
            },
            `"problemMatcher`": `"`$msCompile`"
        }"
}

function Create-DockerRerunTask
{
    [CmdletBinding()]
    [OutputType([String])]
    param(
        [string]$SolutionName,
        [string]$ContainerName,
        [int]$HttpPortNo,
        [int]$HttpsPortNo
    )
    return "{
            `"label`": `"$($SolutionName): Rerun (Debug)`",
            `"group`": `"none`",
            `"dependsOn`":[
                `"$($SolutionName): Stop Docker container local (Debug)`",
                `"$($SolutionName): Run Docker container local (Debug, port $HttpPortNo/$HttpsPortNo)`"
            ],
            `"dependsOrder`": `"sequence`",
            `"presentation`": {
                `"reveal`": `"always`"
            },
            `"problemMatcher`": `"`$msCompile`"
        },
        {
            `"label`": `"CCDP: Rerun WaitAttach (Debug)`",
            `"group`": `"none`",
            `"dependsOn`":[
                `"$($SolutionName): Stop Docker container local (Debug)`",
                `"$($SolutionName): Run Docker container local WaitAttach (Debug, port $HttpPortNo/$HttpsPortNo)`"
            ],
            `"dependsOrder`": `"sequence`",
            `"presentation`": {
                `"reveal`": `"always`"
            },
            `"problemMatcher`": `"`$msCompile`"
        }"
}

function Create-BootstrapDevTask
{
    return "{
            `"label`": `"K8s: Bootstrap Dev Region Cluster`",
            `"command`": `"pwsh`",
            `"windows`": {
                `"command`": `"powershell`",
                `"args`": [
                    `"-ExecutionPolicy`",
                    `"Unrestricted`",
                    `"-NoProfile`",
                    `"-File`",
                    `"./build/deployment/bootstrapDevEnvironment.ps1`" 
                ]
            },
            `"type`": `"shell`",
            `"args`": [
                `"./build/deployment/bootstrapDevEnvironment.ps1`" 
            ],
            `"presentation`": {
                `"reveal`": `"always`"
            },
            `"problemMatcher`": `"`$msCompile`"
        }"
}

function Create-DevDeployTask
{
    [CmdletBinding()]
    [OutputType([String])]
    param(
        [string]$SolutionName
    )

    return "{
            `"label`": `"$($SolutionName): Deploy $SolutionName to Dev`",
            `"command`": `"pwsh`",
            `"windows`": {
                `"command`": `"powershell`",
                `"args`": [
                    `"-ExecutionPolicy`",
                    `"Unrestricted`",
                    `"-NoProfile`",
                    `"-Command`",
                    `"\`"./build/scripts/publishImage.ps1`" ,
                    `"-Role`",
                    `"$SolutionName`",
                    `"-Env`",
                    `"Dev`",
                    `"-Config`",
                    `"Release`",
                    `"-Actions`",
                    `"push,deploy\`"`"
                ]
            },
            `"type`": `"shell`",
            `"args`": [
                `"\`"./build/scripts/publishImage.ps1`" ,
                `"-Role`",
                `"$SolutionName`",
                `"-Env`",
                `"Dev`",
                `"-Config`",
                `"Release`",
                `"-Actions`",
                `"push,deploy\`"`"
            ],
            `"dependsOn`":[
                `"$($SolutionName): Build Docker image Release`"
            ],
            `"presentation`": {
                `"reveal`": `"always`"
            },
            `"problemMatcher`": `"`$msCompile`"
        }"
}

function Create-DevDeployAllTask
{
    return "{
            `"label`": `"Deploy All to Dev`",
            `"command`": `"pwsh`",
            `"windows`": {
                `"command`": `"powershell`",
                `"args`": [
                    `"-ExecutionPolicy`",
                    `"Unrestricted`",
                    `"-NoProfile`",
                    `"-Command`",
                    `"\`"./build/publishImage.ps1`" ,
                    `"-Role`",
                    `"all`",
                    `"-Env`",
                    `"Dev`",
                    `"-Config`",
                    `"Release`",
                    `"-Actions`",
                    `"push,deploy\`"`"
                ]
            },
            `"type`": `"shell`",
            `"args`": [
                `"\`"./build/publishImage.ps1`" ,
                `"-Role`",
                `"all`",
                `"-Env`",
                `"Dev`",
                `"-Config`",
                `"Release`",
                `"-Actions`",
                `"push,deploy\`"`"
            ],
            `"dependsOn`":[
                `"Build all Docker images Release`"
            ],
            `"presentation`": {
                `"reveal`": `"always`"
            },
            `"problemMatcher`": `"`$msCompile`"
        }"
}

function Create-VSCodeTasks
{
    $header = "
        {
            // See https://go.microsoft.com/fwlink/?LinkId=733558
            // for the documentation about the tasks.json format
            `"version`": `"2.0.0`",
            `"tasks`": ["

    $footer = "]}"

    $tasks = @()

    $AllSolutions = (Get-RepositoryConfigValue ".config.solutions").trim("[]").split(",").trim("""")

    foreach($sln in $AllSolutions)
    {
        $tasks += (Create-DockerBuildTask -Config Debug -SolutionName $sln)
        $tasks += (Create-DockerBuildTask -Config Release -SolutionName $sln)
    }

    $tasks += (Create-DockerBuildTask -Config Release -SolutionName all)

    foreach($sln in $AllSolutions)
    {
        $tasks += (Create-SolutionBuildTask -Config Debug -SolutionName $sln)
        $tasks += (Create-SolutionBuildTask -Config Release -SolutionName $sln)
    }

    $httpPortNo = 8080
    $httpsPortNo = 4430
    foreach($sln in $AllSolutions)
    {
        $containerName = "$(Get-RepositoryConfigValue ".solutions.$sln.ServiceName")_local".ToLower()
        $tasks += (Create-DockerRunTask -SolutionName $sln -ContainerName $containerName -HttpPortNo $httpPortNo -HttpsPortNo $httpsPortNo)
        $tasks += (Create-DockerStopTask -SolutionName $sln -ContainerName $containerName)
        $tasks += (Create-DockerRerunTask -SolutionName $sln -ContainerName $containerName -HttpPortNo $httpPortNo -HttpsPortNo $httpsPortNo)
        $httpPortNo++
        $httpsPortNo++
    }

    $tasks += (Create-BootstrapDevTask)

    foreach($sln in $AllSolutions)
    {
        if("$(Get-RepositoryConfigValue ".solutions.$sln.dockerBuild")_local".ToLower() -eq "true")
        {
            $tasks += (Create-DevDeployTask -SolutionName $sln)
        }
    }

    $tasks += (Create-DevDeployAllTask)

    return $header + ($tasks | Join-String Name -Separator ",") + $footer
}

# pretty print the json object
$tasksJson = Create-VSCodeTasks | ConvertFrom-Json | ConvertTo-Json

Write-Output $tasksJson