@echo off

set solutionfile=%1
set config=%2
set buildDockerImage=%3

rem get path to chocolatey, install if it is not present
set chocopath=""
for /f "delims=" %%i in ('%SystemRoot%\System32\where.exe chocolatey') do @set chocopath=%%i
if '%chocopath%'=='""' (
    @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -InputFormat None -ExecutionPolicy Bypass -Command "iex ((New-Object System.Net.WebClient).DownloadString('https://chocolatey.org/install.ps1'))" 
    SET "PATH=%PATH%;%ALLUSERSPROFILE%\chocolatey\bin"
)

rem get path to jq, install if it is not present
set jqpath=""
for /f "delims=" %%i in ('%SystemRoot%\System32\where.exe jq') do @set jqpath=%%i
if '%jqpath%'=='""' (
    rem ensure the default chocolatey source is enabled
    @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -Command "Start-Process cmd -ArgumentList ^"/C^",^"chocolatey^",^"source^",^"enable^",^"-n=chocolatey^" -Verb RunAs"
    @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -Command "Start-Process cmd -ArgumentList ^"/C^",^"chocolatey^",^"install^",^"jq^",^"-y^" -Verb RunAs"
)

rem get path to helm, install if it is not present
set helmpath=""
for /f "delims=" %%i in ('%SystemRoot%\System32\where.exe helm') do @set helmpath=%%i
if '%helmpath%'=='""' (
    rem ensure the default chocolatey source is enabled
    @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -Command "Start-Process cmd -ArgumentList ^"/C^",^"chocolatey^",^"source^",^"enable^",^"-n=chocolatey^" -Verb RunAs"
    @"%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe" -Command "Start-Process cmd -ArgumentList ^"/C^",^"chocolatey^",^"install^",^"kubernetes-helm^",^"-y^" -Verb RunAs"
)

if "%config%"=="" (
    FOR /F "delims=" %%i in ('jq -c -r .config.defaultConfig .\repoconfig.json') DO SET config=%%i
)

if "%solutionfile%"=="" (
    FOR /F "delims=" %%i in ('jq -c -r .config.defaultSolution .\repoconfig.json') DO SET solutionfile=%%i
)

if "%buildDockerImage%"=="" (
    SET buildDockerImage=True
)

rem Save working directory so that we can restore it back after building everything. This will make developers happy and then 
rem switch to the folder this script resides in. Don't assume absolute paths because on the build host and on the dev host the locations may be different.
pushd %~dp0

set errorlogfile="%~dp0\builderr.log"

powershell "%~dp0\build\build.ps1" -Config %config% -SolutionFile %solutionfile% 2>%errorlogfile%
set EX=%ERRORLEVEL%

type %errorlogfile%

rem Check exit code and exit with non-zero exit code so that build will fail.
if "%EX%" neq "0" (
    echo "Failed to build %solutionfile%."
	goto PopExit
)

:PopExit
rem Restore working directory of user so this works fine in dev box.
popd

rem Exit with explicit 0 code so that build does not fail.
exit %EX%