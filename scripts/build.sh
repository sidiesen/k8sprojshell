#! /bin/bash

# solution file passed in
solutionfile=$1
config=$2
buildDockerImage=$3

if [ "$config" == "Release" ];
then
    # show current os of build machine
    echo "print /etc/os-release"
    cat /etc/os-release

    echo "print env"
    env
fi

# install pwsh if not already
pwsh_path=$(which pwsh)
if [ ! -f "$pwsh_path" ]; 
then   
    # Install system components
    apt-get update
    apt-get install -y curl gnupg apt-transport-https

    # Import the public repository GPG keys
    curl https://packages.microsoft.com/keys/microsoft.asc | apt-key add -
    # Register the Microsoft Product feed
    sh -c 'echo "deb [arch=amd64] https://packages.microsoft.com/repos/microsoft-debian-stretch-prod stretch main" > /etc/apt/sources.list.d/microsoft.list'

    # Update the list of products
    apt-get update

    # Install PowerShell
    apt-get install -y powershell
fi

pushd $PWD > /dev/null
ROOT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )/../.." > /dev/null 2>&1 && pwd )"
popd > /dev/null

if [ "$buildDockerImage" == "" ]; then
    buildDockerImage="True"
fi
export buildDockerImage=$buildDockerImage

pwsh $ROOT_DIR/build/scripts/build.ps1 -SolutionFile $solutionfile -Config $config

exit $?
