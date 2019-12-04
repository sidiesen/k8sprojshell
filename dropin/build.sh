#!/bin/bash

solutionfile=$1
config=$2
buildDockerImage=$3

# install jq if not already
jq_path=$(which jq)
if [ ! -f "$jq_path" ]; 
then   
    # Install system components
    apt-get update
    apt-get install -y jq
fi

if [ -z "$solutionfile" ];
then
    solutionfile=$(jq -c -r .config.defaultSolution repoconfig.json)
fi
if [ -z "$config" ];
then
    config=$(jq -c -r .config.defaultConfig repoconfig.json)
fi

# Save working directory so that we can restore it back after building everything. This will make developers happy and then 
# switch to the folder this script resides in. Don't assume absolute paths because on the build host and on the dev host the locations may be different.
pushd $PWD > /dev/null

errorlogfile="./builderr.log"

current_dir="$( cd "$( dirname "${BASH_SOURCE[0]}" )" > /dev/null 2>&1 && pwd )"
bash -c "$current_dir/build/scripts/build.sh $solutionfile $config $buildDockerImage" 2>$errorlogfile
EX=$?

cat $errorlogfile

# Check exit code and exit with non-zero exit code so that build will fail.
if [ "$EX" != "0" ]; then
    echo "Failed to build $solutionfile."
fi

# Restore working directory of user so this works fine in dev box.
popd > /dev/null

# Exit with explicit 0 code so that build does not fail.
exit $EX
