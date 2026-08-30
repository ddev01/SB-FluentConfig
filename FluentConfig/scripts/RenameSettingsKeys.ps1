# Throwaway helper for the owner: copy {old_slug}_settings / _data to {new_id}_settings / _data.
# Not shipped migration. Delete this script after use.
#
# The JSON file is a flat object of CPH persisted globals: { "first_chatters_settings": "{...}", ... }
#
#   .\RenameSettingsKeys.ps1 -OldSlug first_chatters -NewId alerts -GlobalsJsonPath globals.json
#
# Does not delete the old keys. Skips a destination that is already non-blank.

param(
    [Parameter(Mandatory = $true)]
    [string] $OldSlug,

    [Parameter(Mandatory = $true)]
    [string] $NewId,

    [Parameter(Mandatory = $true)]
    [string] $GlobalsJsonPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

if ($OldSlug -notmatch '^[a-z][a-z0-9_]*$') {
    throw "OldSlug must match [a-z][a-z0-9_]*"
}
if ($NewId -notmatch '^[a-z][a-z0-9_]*$') {
    throw "NewId must match [a-z][a-z0-9_]*"
}

$root = Get-Content -LiteralPath $GlobalsJsonPath -Raw | ConvertFrom-Json
$copied = 0
foreach ($suffix in @('settings', 'data')) {
    $srcName = "${OldSlug}_${suffix}"
    $dstName = "${NewId}_${suffix}"
    $srcVal = $root.$srcName
    $dstVal = $root.$dstName
    if ([string]::IsNullOrWhiteSpace($srcVal)) {
        Write-Host "skip $srcName (missing)"
        continue
    }
    if (-not [string]::IsNullOrWhiteSpace($dstVal)) {
        Write-Host "skip $dstName (already set)"
        continue
    }
    $root | Add-Member -NotePropertyName $dstName -NotePropertyValue $srcVal -Force
    Write-Host "copied $srcName -> $dstName"
    $copied++
}

$root | ConvertTo-Json -Compress:$false | Set-Content -LiteralPath $GlobalsJsonPath -Encoding UTF8
Write-Host "done ($copied key(s))"
