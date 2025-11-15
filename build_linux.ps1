# build_linux.ps1
# Cross-build Linux x64 desktop binary

$ErrorActionPreference = "Stop"

$project    = "Sentry.CrashReporter/Sentry.CrashReporter.csproj"
$config     = "Release"
$framework  = "net9.0-desktop"
$rid        = "linux-x64"
$outRoot    = "artifacts"
$outDir     = Join-Path $outRoot "linux"

Write-Host "=== Building Linux x64 ($framework, $rid) ==="

if (Test-Path $outDir) {
    Remove-Item -Recurse -Force $outDir
}
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# 1. Clean for this TFM
dotnet clean $project -c $config -f $framework

# 2. Restore for SAME TFM + RID
dotnet restore $project -c $config -f $framework -r $rid

# Optional MSBuild task-host workaround
$env:DISABLEOUTOFPROCTASKHOST = "1"

# 3. Publish
dotnet publish $project `
    -c $config `
    -r $rid `
    -f $framework `
    --self-contained true `
    -o $outDir `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true


if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Linux build done."
Write-Host "Output folder: $outDir"
Get-ChildItem $outDir
