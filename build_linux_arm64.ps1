# build_linux_arm64.ps1
# Build Linux ARM64 self-contained, single-file binary

$ErrorActionPreference = "Stop"

$project    = "Sentry.CrashReporter/Sentry.CrashReporter.csproj"
$config     = "Release"
$framework  = "net9.0-desktop"
$rid        = "linux-arm64"
$outRoot    = "artifacts"
$outDir     = Join-Path $outRoot "linux-arm64"

Write-Host "=== Building Linux ARM64 ($framework) ==="

if (Test-Path $outDir) {
    Remove-Item -Recurse -Force $outDir
}
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

dotnet restore $project

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
Write-Host "Linux ARM64 build done."
Write-Host "Output folder: $outDir"
Get-ChildItem $outDir
