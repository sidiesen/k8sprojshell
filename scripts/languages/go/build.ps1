param (
    [Parameter(Mandatory=$false, Position=0)]
    [string]$Solution,

    [Parameter(Mandatory=$false, Position=1)]
    [string]$Config,

    [Parameter(Mandatory=$false, Position=2)]
    [string]$OutDir
)

$RootDir = Join-Path $PSScriptRoot "..\..\..\.."

# TODO: build solution in Go