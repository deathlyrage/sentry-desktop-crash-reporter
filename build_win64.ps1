# build_win64.ps1
# Build Windows x64 self-contained, single-file exe (net9.0)

$ErrorActionPreference = "Stop"

$project    = "Sentry.CrashReporter/Sentry.CrashReporter.csproj"
$config     = "Release"
$rid        = "win-x64"
$framework  = "net9.0-desktop"          # <-- changed from net9.0-desktop
$outRoot    = "artifacts"
$outDir     = Join-Path $outRoot "windows"

Write-Host "=== Building Windows x64 ($framework) ==="

if (Test-Path $outDir) {
    Remove-Item -Recurse -Force $outDir
}
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

# Optional: clean/restore
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
Write-Host "Windows build done."
Write-Host "Output folder: $outDir"
Get-ChildItem $outDir
