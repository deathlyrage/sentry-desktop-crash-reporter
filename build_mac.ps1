# build_mac.ps1
# Build macOS x64 and arm64 self-contained binaries (net9.0-desktop)

$ErrorActionPreference = "Stop"

$project    = "Sentry.CrashReporter/Sentry.CrashReporter.csproj"
$config     = "Release"
$framework  = "net9.0-desktop"
$outRoot    = "artifacts"

$ridX64     = "osx-x64"
$ridArm64   = "osx-arm64"

$outDirX64  = Join-Path $outRoot "mac-osx-x64"
$outDirArm  = Join-Path $outRoot "mac-osx-arm64"

Write-Host "=== Building macOS (x64 + arm64) - $framework ==="

# Clean output dirs
if (Test-Path $outDirX64) {
    Remove-Item -Recurse -Force $outDirX64
}
if (Test-Path $outDirArm) {
    Remove-Item -Recurse -Force $outDirArm
}

New-Item -ItemType Directory -Force -Path $outDirX64 | Out-Null
New-Item -ItemType Directory -Force -Path $outDirArm | Out-Null

# Restore once
dotnet restore $project

# Publish x64
Write-Host "=== Publishing macOS x64 ($ridX64) ==="
dotnet publish $project `
    -c $config `
    -r $ridX64 `
    -f $framework `
    --self-contained true `
    -o $outDirX64 `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish for $ridX64 failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

# Publish arm64
Write-Host "=== Publishing macOS arm64 ($ridArm64) ==="
dotnet publish $project `
    -c $config `
    -r $ridArm64 `
    -f $framework `
    --self-contained true `
    -o $outDirArm `
    /p:PublishSingleFile=true `
    /p:IncludeNativeLibrariesForSelfExtract=true

if ($LASTEXITCODE -ne 0) {
    Write-Error "dotnet publish for $ridArm64 failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "macOS builds done."
Write-Host "x64 output:   $outDirX64"
Write-Host "arm64 output: $outDirArm"
Get-ChildItem $outDirX64
Get-ChildItem $outDirArm
