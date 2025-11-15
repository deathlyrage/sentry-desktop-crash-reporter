# build_winarm64.ps1
# Build Windows ARM64 self-contained, single-file exe (net9.0-desktop)

$ErrorActionPreference = "Stop"

$project    = "Sentry.CrashReporter/Sentry.CrashReporter.csproj"
$config     = "Release"
$rid        = "win-arm64"
$framework  = "net9.0-desktop"
$outRoot    = "artifacts"
$outDir     = Join-Path $outRoot "windows-arm64"

Write-Host "=== Building Windows ARM64 ($framework, $rid) ==="

if (Test-Path $outDir) {
    Remove-Item -Recurse -Force $outDir
}
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Same pattern as your win64 script
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
Write-Host "Windows ARM64 build done."
Write-Host "Output folder: $outDir"
Get-ChildItem $outDir
