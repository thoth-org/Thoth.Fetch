#!/bin/bash

OS=${OS:-"unknown"}

echo $OSTYPE
if [ "$OS" != "Windows_NT" ]
then
  # Allows NETFramework like net45 to be built using dotnet core tooling with mono
  export FrameworkPathOverride=$(dirname $(which mono))/../lib/mono/4.5/
fi

set -eu
set -o pipefail

# liberated from https://stackoverflow.com/a/18443300/433393
realpath() {
  OURPWD=$PWD
  cd "$(dirname "$1")"
  LINK=$(readlink "$(basename "$1")")
  while [ "$LINK" ]; do
    cd "$(dirname "$LINK")"
    LINK=$(readlink "$(basename "$1")")
  done
  REALPATH="$PWD/$(basename "$1")"
  cd "$OURPWD"
  echo "$REALPATH"
}

TOOL_PATH=$(realpath ".fake")
FAKE="$TOOL_PATH"/fake

if ! [ -e "$FAKE" ]
then
  dotnet tool install fake-cli --tool-path "$TOOL_PATH" --version 5.16.1
fi
"$FAKE" "$@"
