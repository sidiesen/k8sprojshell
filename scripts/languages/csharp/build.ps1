param (
    [Parameter(Mandatory=$false, Position=0)]
    [string]$Solution,

    [Parameter(Mandatory=$false, Position=1)]
    [string]$Config,

    [Parameter(Mandatory=$false, Position=2)]
    [string]$OutDir
)

$RootDir = Join-Path $PSScriptRoot "..\..\..\.."

$slnFile = Get-ChildItem -Path $RootDir\src\ -Name "*$Solution.sln" -Recurse

$outPath = Join-Path $OutDir (Get-Item $RootDir\src\$slnFile).BaseName
dotnet publish "$RootDir\src\$slnFile" -c $Config -o $outPath
if(! $?) { 
    Write-Error "Failed to publish src\$slnFile."
    Exit 1
}