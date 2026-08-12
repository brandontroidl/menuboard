# Publishes MenuBoard for deployment to the store PC.
#
# Usage (from the MenuBoard folder, in PowerShell):
#   .\publish.ps1                  # framework-dependent (store PC needs the .NET 8 Desktop Runtime)
#   .\publish.ps1 -SelfContained   # bundles the runtime (bigger, but nothing to install on the store PC)
#
# Output goes to .\publish\ - copy that whole folder to the store PC and run MenuBoard.exe.
# Note: menuboard.db, Images\ and settings.json live NEXT TO the exe, so back
# those up before replacing an existing install with a fresh publish.

param([switch]$SelfContained)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

if ($SelfContained) {
    dotnet publish "$root\src\MenuBoard" -c Release -r win-x64 --self-contained true -o "$root\publish"
} else {
    dotnet publish "$root\src\MenuBoard" -c Release -o "$root\publish"
}

Write-Host ""
Write-Host "Done. Deployment folder: $root\publish" -ForegroundColor Green
Write-Host "Copy the folder to the store PC and run MenuBoard.exe."
Write-Host "Turn on 'Start Menu Board automatically when Windows starts' in the admin App Settings."
