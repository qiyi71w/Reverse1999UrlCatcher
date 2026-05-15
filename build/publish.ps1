param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x64",
    [switch]$Zip
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$project = Join-Path $repoRoot "src\Reverse1999UrlCatcher.App\Reverse1999UrlCatcher.App.csproj"
$dist = Join-Path $repoRoot "dist\Reverse1999UrlCatcher"
$zipPath = Join-Path $repoRoot "dist\Reverse1999UrlCatcher-$Configuration-$Runtime.zip"

if (Test-Path -LiteralPath $dist) {
    Remove-Item -LiteralPath $dist -Recurse -Force
}

if (Test-Path -LiteralPath $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

dotnet publish $project `
    -c $Configuration `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -o $dist

$readmeSource = Join-Path $repoRoot "README.md"
if (Test-Path -LiteralPath $readmeSource) {
    Copy-Item -LiteralPath $readmeSource -Destination (Join-Path $dist "README.txt") -Force
}

$toolsRoot = Join-Path $repoRoot "tools"
if (Test-Path -LiteralPath $toolsRoot) {
    Copy-Item -LiteralPath $toolsRoot -Destination (Join-Path $dist "tools") -Recurse -Force
}

$requiredPaths = @(
    (Join-Path $dist "Reverse1999UrlCatcher.App.exe"),
    (Join-Path $dist "config\url_rules.json"),
    (Join-Path $dist "scripts\re1999_capture.py"),
    (Join-Path $dist "README.txt")
)

foreach ($path in $requiredPaths) {
    if (-not (Test-Path -LiteralPath $path)) {
        throw "Missing publish artifact: $path"
    }
}

$hashes = Get-ChildItem -LiteralPath $dist -File -Recurse | Get-FileHash -Algorithm SHA256
$hashes | ForEach-Object {
    $relative = $_.Path.Substring($dist.Length).TrimStart('\')
    "$($_.Hash)  $relative"
} | Set-Content -LiteralPath (Join-Path $dist "SHA256SUMS.txt")

if ($Zip) {
    Compress-Archive -Path (Join-Path $dist "*") -DestinationPath $zipPath -Force
    Write-Host "Published and zipped: $zipPath"
}
else {
    Write-Host "Published to $dist"
}
