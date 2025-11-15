# build_mac.ps1
# Build macOS x64 and arm64 self-contained binaries (net9.0-desktop)
# Then combine them using lipo_windows_amd64.exe in repo root.

$ErrorActionPreference = "Stop"

$project    = "Sentry.CrashReporter/Sentry.CrashReporter.csproj"
$config     = "Release"
$framework  = "net9.0-desktop"
$outRoot    = "artifacts"

$ridX64     = "osx-x64"
$ridArm64   = "osx-arm64"

$outDirX64  = Join-Path $outRoot "mac-osx-x64"
$outDirArm  = Join-Path $outRoot "mac-osx-arm64"
$outUniversal = Join-Path $outRoot "mac-universal"

# Lipo tool located in repo root
$lipo = Join-Path $PSScriptRoot "lipo_windows_amd64.exe"

Write-Host "=== Building macOS (x64 + arm64) - $framework ==="

# Clean output dirs
foreach ($dir in @($outDirX64, $outDirArm, $outUniversal)) {
    if (Test-Path $dir) { Remove-Item -Recurse -Force $dir }
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
}

dotnet restore $project

# --- x64 publish ---
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

# --- arm64 publish ---
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

# --- Universal binary merge ---
Write-Host ""
Write-Host "=== Creating macOS Universal Binary ==="

$lipoOutput = Join-Path $outUniversal "CrashReportClient"
$lipoArm    = Join-Path $outDirArm  "CrashReportClient"
$lipoX64    = Join-Path $outDirX64 "CrashReportClient"

if (!(Test-Path $lipo)) {
    Write-Error "lipo tool not found: $lipo"
    exit 1
}

& $lipo -output $lipoOutput -create $lipoArm $lipoX64

if ($LASTEXITCODE -ne 0) {
    Write-Error "lipo failed with exit code $LASTEXITCODE"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "macOS builds done."
Write-Host "x64 output:       $outDirX64"
Write-Host "arm64 output:     $outDirArm"
Write-Host "universal output: $outUniversal"

Get-ChildItem $outUniversal
