param(
    [ValidateSet('Install','Restore','Check')][string]$Mode = 'Install',
    [string]$GamePath = 'D:\Games\Steam\steamapps\common\Night Call',
    [string]$BackupPath
)
$ErrorActionPreference = 'Stop'
$GamePath = [IO.Path]::GetFullPath($GamePath).TrimEnd('\')
$packageRoot = $PSScriptRoot
$manifest = Get-Content -LiteralPath (Join-Path $packageRoot 'manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$allowed = @('Night Call_Data/resources.assets','Night Call_Data/sharedassets0.assets','Night Call_Data/sharedassets5.assets','Night Call_Data/Managed/Assembly-CSharp.dll','Night Call_Data/level1','Night Call_Data/level3','Night Call_Data/level7','Night Call_Data/sharedassets3.assets')
if (!(Test-Path -LiteralPath (Join-Path $GamePath 'Night Call.exe'))) {throw 'Night Call.exe not found in specified game folder.'}
if (@($manifest.files).Count -ne $allowed.Count -or @($manifest.files.path | Select-Object -Unique).Count -ne $allowed.Count) {throw 'Unexpected patch manifest'}
foreach ($row in $manifest.files) {
    if ($row.path -notin $allowed) {throw 'Unexpected file in patch manifest'}
}
function Hash([string]$Path) {
 $stream=[IO.File]::OpenRead($Path);$algorithm=[Security.Cryptography.SHA256]::Create()
 try {([BitConverter]::ToString($algorithm.ComputeHash($stream))).Replace('-','').ToLowerInvariant()} finally {$stream.Dispose();$algorithm.Dispose()}
}
function Target([string]$Relative) {
    $result = [IO.Path]::GetFullPath((Join-Path $GamePath $Relative))
    if (!$result.StartsWith($GamePath+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Target escapes game directory'}
    $result
}
$backupRoot = Join-Path $GamePath '.NightCallRussianBackups'
$latest = Join-Path $backupRoot 'LAST_BACKUP.txt'
$running = @(Get-Process -Name 'Night Call' -ErrorAction SilentlyContinue | Where-Object {$_.Path -eq (Join-Path $GamePath 'Night Call.exe')})
if ($running.Count -gt 0 -and $Mode -ne 'Check') {throw 'Close Night Call before installing or restoring the patch'}
if ($Mode -eq 'Restore') {
    if (!$BackupPath) {
        if (!(Test-Path -LiteralPath $latest)) {throw 'No recorded native-patch backup found'}
        $BackupPath = (Get-Content -LiteralPath $latest -Raw -Encoding UTF8).Trim()
    }
    $BackupPath = [IO.Path]::GetFullPath($BackupPath)
    if (!$BackupPath.StartsWith($backupRoot+'\',[StringComparison]::OrdinalIgnoreCase)) {throw 'Backup must be inside this game backup directory'}
    $stored = Get-Content -LiteralPath (Join-Path $BackupPath 'backup_manifest.json') -Raw -Encoding UTF8 | ConvertFrom-Json
    if (@($stored.files).Count -lt 1 -or @($stored.files).Count -gt $allowed.Count -or @($stored.files.path | Select-Object -Unique).Count -ne @($stored.files).Count) {throw 'Unexpected backup manifest'}
    foreach ($row in $stored.files) {
        if ($row.path -notin $allowed) {throw 'Unexpected backup file'}
        if ((Hash (Join-Path $BackupPath $row.path)) -ne $row.source_sha256) {throw 'Damaged backup file'}
        $current = Hash (Target $row.path)
        if ($current -ne $row.patched_sha256 -and $current -ne $row.source_sha256) {throw 'Game file changed after patch install; refusing to overwrite it during restore'}
    }
    foreach ($row in $stored.files) {Copy-Item -LiteralPath (Join-Path $BackupPath $row.path) -Destination (Target $row.path) -Force}
    foreach ($row in $stored.files) {if ((Hash (Target $row.path)) -ne $row.source_sha256) {throw 'Restore verification failed'}}
    Write-Output "Original files restored. Backup retained: $BackupPath"
    exit 0
}
$already = $true
foreach ($row in $manifest.files) {
    $payload = Join-Path $packageRoot ('payload/'+$row.path)
    if ((Hash $payload) -ne $row.patched_sha256) {throw "Patch payload checksum mismatch: $($row.path)"}
    $current = Hash (Target $row.path)
    if ($current -ne $row.patched_sha256) {$already = $false}
    if ($current -ne $row.source_sha256 -and $current -ne $row.patched_sha256) {throw "Different game build or edited file: $($row.path). No files were replaced. Если у вас уже стоит предыдущая версия перевода - сначала запустите Восстановить.cmd, затем Установить.cmd."}
}
if (Test-Path -LiteralPath (Join-Path $GamePath 'winhttp.dll')) {throw 'BepInEx loader winhttp.dll is present. Disable the old mod before using the native patch.'}
if ($Mode -eq 'Check') {Write-Output 'Game and patch checksums match. No files replaced.';exit 0}
if ($already) {Write-Output 'This native patch is already installed. No files replaced.';exit 0}
# Mixed partially installed files are not backed up as originals.
foreach ($row in $manifest.files) {
    if ((Hash (Target $row.path)) -ne $row.source_sha256) {throw 'Mixed original/patched state. Restore the recorded backup first.'}
}
$BackupPath = Join-Path $backupRoot ([DateTime]::UtcNow.ToString('yyyyMMddTHHmmssfffZ'))
foreach ($row in $manifest.files) {
    $dest = Join-Path $BackupPath $row.path
    New-Item -ItemType Directory -Force -Path (Split-Path $dest) | Out-Null
    Copy-Item -LiteralPath (Target $row.path) -Destination $dest
    if ((Hash $dest) -ne $row.source_sha256) {throw 'Backup verification failed; patch not applied'}
}
$manifest | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath (Join-Path $BackupPath 'backup_manifest.json') -Encoding UTF8
$BackupPath | Set-Content -LiteralPath $latest -Encoding UTF8
try {
    foreach ($row in $manifest.files) {
        Copy-Item -LiteralPath (Join-Path $packageRoot ('payload/'+$row.path)) -Destination (Target $row.path) -Force
        if ((Hash (Target $row.path)) -ne $row.patched_sha256) {throw 'Post-install file verification failed'}
    }
} catch {
    foreach ($row in $manifest.files) {Copy-Item -LiteralPath (Join-Path $BackupPath $row.path) -Destination (Target $row.path) -Force}
    throw
}
Write-Output "Native patch installed and SHA256 verified. Original backup: $BackupPath"
