# Builds MenuBoardSetup.exe - the one-click installer for the store PC.
#
# Requires NSIS (https://nsis.sourceforge.io) - install it, or run:
#   winget install NSIS.NSIS
#
# Usage (from the MenuBoard folder, in PowerShell):
#   .\build-installer.ps1
#
# Output: MenuBoardSetup.exe - give this single file to the store owner.
# Installing it: double-click -> Install -> done. The app installs per-user
# (no admin password), starts with Windows automatically, and gets a Start
# Menu entry. Reinstalling/upgrading keeps the store's menu data.

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$makensis = Get-Command makensis -ErrorAction SilentlyContinue
if (-not $makensis) {
    $candidates = @("$env:ProgramFiles\NSIS\makensis.exe", "${env:ProgramFiles(x86)}\NSIS\makensis.exe")
    $makensis = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
    if (-not $makensis) {
        Write-Error "NSIS not found. Install it first: winget install NSIS.NSIS"
    }
} else {
    $makensis = $makensis.Source
}

Write-Host "Publishing self-contained payload..." -ForegroundColor Cyan
if (Test-Path "$root\payload") { Remove-Item "$root\payload" -Recurse -Force }
dotnet publish "$root\src\MenuBoard" -c Release -r win-x64 --self-contained true -o "$root\payload"
Remove-Item "$root\payload\*.pdb" -ErrorAction SilentlyContinue

Write-Host "Compiling installer..." -ForegroundColor Cyan
& $makensis "$root\installer.nsi"

Write-Host ""
Write-Host "Done: $root\MenuBoardSetup.exe" -ForegroundColor Green
Write-Host "Give that single file to the store owner - double-click, Install, done."
