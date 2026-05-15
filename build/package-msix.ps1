param(
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command "makeappx.exe" -ErrorAction SilentlyContinue)) {
    throw "makeappx.exe was not found. Install Windows SDK packaging tools before creating MSIX packages."
}

throw "MSIX packaging manifest is not part of the MVP yet. Use build\publish.ps1 for the first zip-style release."
