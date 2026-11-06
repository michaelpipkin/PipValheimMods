<#
Turns the item dump into AutoPickupIgnorer/ItemList.md, the lookup table the mod's README
sends players to.

The dump comes from Pip's Mod's item list hotkey and lands in the live profile's BepInEx
folder. Only two of its ten columns matter here: players know an item by the name the game
shows them, and the config wants the prefab name, so the table is the bridge between the two.

Run it after a game update, commit the result, and the README's link is current again:

    tools\BuildItemList.ps1

Keep DumpRealItemsOnly on when producing the dump. With it off, ObjectDB's creature attack
prefabs and character customisation entries come through too, and none of those are items a
player can hold.
#>
param(
  [string]$Tsv = "$PSScriptRoot\..\BepInEx\live\PipsMod_ItemList.tsv",
  [string]$Out = "$PSScriptRoot\..\AutoPickupIgnorer\ItemList.md"
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Tsv)) {
  throw "item dump not found: $Tsv`nPress the item list hotkey in game, then run this again."
}

$rows = Import-Csv -Path $Tsv -Delimiter "`t"
if (-not $rows) { throw "no rows in $Tsv" }
foreach ($column in 'PrefabName', 'DisplayName') {
  if ($column -notin $rows[0].PSObject.Properties.Name) {
    throw "the dump has no $column column - was it written by a different version of the mod?"
  }
}

# Sorted by the name the player sees. The second key matters more than it looks: 40-odd display
# names cover several prefabs each, and sorting those together puts the alternatives side by side
# instead of scattering them through the table.
$sorted = $rows |
  Where-Object { $_.DisplayName -and $_.PrefabName } |
  Sort-Object @{ Expression = 'DisplayName' }, @{ Expression = 'PrefabName' }

# A handful of entries have no translation, so the game renders the raw token in brackets -
# [item_iceskates] and the like. They are unreleased or cut content. They sort above every real
# name, since '[' beats every letter, which would otherwise hand the top of the table to the one
# group of items nobody can look up by name. They go in a section of their own at the end.
$named = @($sorted | Where-Object { $_.DisplayName -notmatch '^\[.*\]$' })
$untranslated = @($sorted | Where-Object { $_.DisplayName -match '^\[.*\]$' })

# A pipe in a name would end the table cell early
function Escape-Cell([string]$text) { $text -replace '\|', '\|' }

$lines = [System.Collections.Generic.List[string]]::new()
$lines.Add('# Valheim item list')
$lines.Add('')
$lines.Add('Every item in the game, as the game itself reports them, for looking up the name to put')
$lines.Add('in `Pip.AutoPickupIgnorer.cfg`.')
$lines.Add('')
$lines.Add('**Display name** is what Valheim calls the item on screen. **Prefab name** is what the config')
$lines.Add('file wants. They are often different, and sometimes not obviously related.')
$lines.Add('')
$lines.Add('Some display names cover more than one prefab - several Black Metal Swords, several')
$lines.Add('Arbalests - and those are listed together. They are genuinely different items as far as the')
$lines.Add('game is concerned, so ignoring one does not ignore the others. If you are not sure which you')
$lines.Add('are picking up, add all of them.')
$lines.Add('')
$lines.Add("$($named.Count) items, read from Valheim on $(Get-Date -Format 'yyyy-MM-dd').")
$lines.Add('')
$lines.Add('| Display name | Prefab name |')
$lines.Add('| --- | --- |')
foreach ($row in $named) {
  # The prefab name is in backticks so it can be copied cleanly into the config
  $lines.Add('| ' + (Escape-Cell $row.DisplayName) + ' | `' + (Escape-Cell $row.PrefabName) + '` |')
}

if ($untranslated.Count) {
  $lines.Add('')
  $lines.Add('## Items with no name')
  $lines.Add('')
  $lines.Add('These exist in the game files but have no translated name, so Valheim shows the raw')
  $lines.Add('token in brackets. They are unreleased or cut content and you are unlikely to ever pick')
  $lines.Add('one up. Listed for completeness.')
  $lines.Add('')
  $lines.Add('| Shown as | Prefab name |')
  $lines.Add('| --- | --- |')
  foreach ($row in $untranslated) {
    $lines.Add('| ' + (Escape-Cell $row.DisplayName) + ' | `' + (Escape-Cell $row.PrefabName) + '` |')
  }
}

$outDir = Split-Path $Out -Parent
if (-not (Test-Path $outDir)) { throw "output folder not found: $outDir" }
Set-Content -Path $Out -Value $lines -Encoding UTF8

$duplicates = ($named | Group-Object DisplayName | Where-Object Count -gt 1).Count
Write-Host "wrote $Out"
Write-Host "  $($named.Count) named items, $duplicates display names covering more than one prefab"
Write-Host "  $($untranslated.Count) with no translated name, listed separately at the end"
