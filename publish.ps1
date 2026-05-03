# Publish single-file self-contained CRM exe + install to %LOCALAPPDATA% + create desktop shortcut.
#
# Usage from PowerShell at project root:
#   powershell -ExecutionPolicy Bypass -File .\publish.ps1
#
# What it does:
#   1. dotnet publish in Release with single-file + self-contained options
#      (one ~70 MB exe, no .NET Runtime required on the target machine).
#   2. Copies the exe to %LOCALAPPDATA%\CrmApp\CrmApp.exe (per-user, no admin).
#   3. Creates a "CRM.lnk" shortcut on the current user's Desktop.

$ErrorActionPreference = 'Stop'

$projectFile  = 'src\CrmApp.WinForms\CrmApp.WinForms.csproj'
$publishDir   = 'publish'
$installDir   = Join-Path $env:LOCALAPPDATA 'CrmApp'
$exeName      = 'CrmApp.exe'
$shortcutName = 'CRM.lnk'

Write-Host '== Build single-file exe ==' -ForegroundColor Cyan
& dotnet publish $projectFile `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeAllContentForSelfExtract=true `
    -p:EnableCompressionInSingleFile=true `
    -p:DebugType=embedded `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    Write-Host 'Publish failed. See output above.' -ForegroundColor Red
    exit 1
}

# Copy exe to a stable location. %LOCALAPPDATA% is per-user; no admin rights needed.
Write-Host "`n== Install to $installDir ==" -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $installDir | Out-Null

$srcExe = Join-Path $publishDir $exeName
$dstExe = Join-Path $installDir $exeName

if (-not (Test-Path $srcExe)) {
    Write-Host "Not found after publish: $srcExe" -ForegroundColor Red
    exit 1
}

Copy-Item -Force $srcExe $dstExe
Write-Host "Copied: $dstExe"

# Create desktop shortcut via WScript.Shell COM.
Write-Host "`n== Desktop shortcut ==" -ForegroundColor Cyan
$desktop = [Environment]::GetFolderPath('Desktop')
$shortcutPath = Join-Path $desktop $shortcutName

$wshell = New-Object -ComObject WScript.Shell
$shortcut = $wshell.CreateShortcut($shortcutPath)
$shortcut.TargetPath       = $dstExe
$shortcut.WorkingDirectory = $installDir
$shortcut.Description      = 'CRM for small business'
$shortcut.IconLocation     = "$dstExe,0"
$shortcut.Save()
Write-Host "Created: $shortcutPath"

$sizeMB = [math]::Round((Get-Item $dstExe).Length / 1MB, 1)

Write-Host "`n== Done ==" -ForegroundColor Green
Write-Host "  exe:      $dstExe ($sizeMB MB)"
Write-Host "  shortcut: $shortcutPath"
Write-Host "  data:     $env:APPDATA\CrmApp\ (created on first launch)"
Write-Host ''
Write-Host 'Launch "CRM" from your desktop. No .NET Runtime required.'
