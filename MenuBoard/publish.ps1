# Publishes MenuBoard for deployment to the store PC.
#
# Usage (from the MenuBoard folder, in PowerShell):
#   .\publish.ps1                     # self-contained (default): bundles the runtime,
#                                     # nothing to install on the store PC
#   .\publish.ps1 -FrameworkDependent # smaller output, but the store PC must have the
#                                     # .NET 10 Desktop Runtime (x64) installed
#
# Output goes to .\publish\ - copy that whole folder to a user-writable location
# on the store PC (e.g. C:\MenuBoard, not C:\Program Files) and run MenuBoard.exe.

param([switch]$FrameworkDependent)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$out = Join-Path $root "publish"

# Clean stale binaries first, KEEPING the store's data (database, images,
# settings). dotnet publish does not clean its output folder, and a folder
# that mixes framework-dependent and self-contained publishes breaks in a
# confusing way: the exe ignores the runtime files sitting next to it and
# asks you to "install .NET" even though everything is right there.
$keep = @("menuboard.db", "menuboard.db-shm", "menuboard.db-wal", "settings.json", "Images")
if (Test-Path $out) {
    Get-ChildItem -Path $out -Force | Where-Object { $keep -notcontains $_.Name } | Remove-Item -Recurse -Force
}

if ($FrameworkDependent) {
    dotnet publish "$root\src\MenuBoard" -c Release -o $out
} else {
    dotnet publish "$root\src\MenuBoard" -c Release -r win-x64 --self-contained true -o $out
}

Write-Host ""
Write-Host "Done. Deployment folder: $out" -ForegroundColor Green
Write-Host "Copy the folder to the store PC and run MenuBoard.exe."
Write-Host "Turn on 'Start Menu Board automatically when Windows starts' in the admin App Settings."
