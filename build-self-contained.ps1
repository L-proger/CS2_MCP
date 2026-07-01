[CmdletBinding()]
param(
    [string]$Runtime = "win-x64",
    [string]$Configuration = "Release",
    [string]$OutputRoot = "",
    [switch]$NoSingleFile,
    [switch]$Clean
)

$ErrorActionPreference = "Stop"

$projectRoot = $PSScriptRoot
$projectPath = Join-Path $projectRoot "CS2_MCP.csproj"

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Project file was not found: $projectPath"
}

if ([string]::IsNullOrWhiteSpace($OutputRoot)) {
    $OutputRoot = Join-Path $projectRoot "artifacts\self-contained"
}

$outputPath = Join-Path $OutputRoot $Runtime

if ($Clean -and (Test-Path -LiteralPath $outputPath)) {
    Remove-Item -LiteralPath $outputPath -Recurse -Force
}

New-Item -ItemType Directory -Path $outputPath -Force | Out-Null

$publishArgs = @(
    "publish",
    $projectPath,
    "--configuration",
    $Configuration,
    "--runtime",
    $Runtime,
    "--self-contained",
    "true",
    "-p:UseAppHost=true",
    "-p:PublishTrimmed=false",
    "-p:DebugType=embedded",
    "-o",
    $outputPath
)

if (-not $NoSingleFile) {
    $publishArgs += @(
        "-p:PublishSingleFile=true",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true"
    )
}

Write-Host "Publishing self-contained app..."
Write-Host "Project: $projectPath"
Write-Host "Runtime: $Runtime"
Write-Host "Output:  $outputPath"

& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "Done."
$executableName = if ($Runtime.StartsWith("win-", [StringComparison]::OrdinalIgnoreCase)) {
    "CS2_MCP.exe"
} else {
    "CS2_MCP"
}
$executablePath = Join-Path $outputPath $executableName
Write-Host "Run stdio:"
Write-Host "  $executablePath"
Write-Host "Run HTTP:"
Write-Host "  $executablePath --transport http"
