#! /bin/bash

# solution file passed in
slnkey=$1

# show current os of build machine
cat /etc/os-release

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

# install jq if not already
jq_path=$(which jq)
if [ ! -f "$jq_path" ]; 
then   
    # Install system components
    apt-get update
    apt-get install -y jq
fi

# Restore solutionfile
ROOT_DIR="$(dirname $0)/../.."

for solutionfile in `find / -name $slnkey.sln`; do    
    echo "restore $solutionfile"
    dotnet restore $solutionfile
done

if [ $? -ne 0 ]; then
    echo "Failed to restore $slnkey.sln."
    exit 1   
fi

for buildTaskProjectfile in `find / -name TemplateBuildTasks.csproj`; do    
    echo "restore $buildTaskProjectfile"
    dotnet restore $buildTaskProjectfile
done

if [ $? -ne 0 ]; then
    echo "Failed to restore TemplateBuildTasks.csproj."
    exit 1   
fi

exit 0