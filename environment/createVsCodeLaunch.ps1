$RootDir = Join-Path $PSScriptRoot "..\.."
$BuildDir = Join-Path $PSScriptRoot ".."

Import-Module $BuildDir/deployment/jsonValues.psm1

$solutionSet = (Get-RepositoryConfigValue ".config.solutions").trim("[]").split(",").trim("""")

function Create-LaunchConfig
{
    [CmdletBinding()]
    [OutputType([String])]
    param(
        [string]$SolutionName,
        [string]$ContainerName
    )

    return "{
        `"name`": `"${SolutionName}: Attach Debugger local`",
        `"type`": `"coreclr`",
        `"request`": `"attach`",
        `"preLaunchTask`": `"`",
        `"processName`" : `"dotnet`",
        `"pipeTransport`": {
            `"debuggerPath`": `"/vsdbg/vsdbg`",
            `"pipeProgram`": `"docker`",
            `"pipeCwd`": `"`${workspaceRoot}`",
            `"quoteArgs`": false,
            `"pipeArgs`": [
                `"exec -i $ContainerName`"
            ]
        }
    }"
}

function Create-VSCodeLaunchConfigs
{
    $header = "
    {
        // Use IntelliSense to learn about possible attributes.
        // Hover to view descriptions of existing attributes.
        // For more information, visit: https://go.microsoft.com/fwlink/?linkid=830387
        `"version`": `"0.2.0`",
        `"configurations`": ["

    $footer = "    ]
}"

    $configs = @()

    $AllSolutions = (Get-RepositoryConfigValue ".config.solutions").trim("[]").split(",").trim("""")

    foreach($sln in $AllSolutions)
    {
        $containerName = "$(Get-RepositoryConfigValue ".solutions.$sln.ServiceName")_local".ToLower()
        $configs += (Create-LaunchConfig -SolutionName $sln -ContainerName $containerName)
    }

    return $header + ($configs -join ",") + $footer
}

# pretty print the json object
$launchConfigsJson = Create-VSCodeLaunchConfigs | ConvertFrom-Json | ConvertTo-Json -depth 100

Write-Output $launchConfigsJson