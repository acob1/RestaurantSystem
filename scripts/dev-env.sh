# Source from the repository root: source scripts/dev-env.sh
# Uses workspace runtimes if installed; otherwise uses your system toolchain.
export PATH="$PWD/.tools/dotnet:$PWD/.tools/node/bin:$PATH"
export DOTNET_CLI_HOME="$PWD/.tools/dotnet-home"
export NUGET_PACKAGES="$PWD/.tools/nuget"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export N8N_ENCRYPTION_KEY="${N8N_ENCRYPTION_KEY:-local-development-key-never-use-hosted}"
