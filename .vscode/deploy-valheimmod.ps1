$ErrorActionPreference = 'Stop'

$source = Join-Path $PSScriptRoot '..\ValheimMod\bin\Release\ValheimMod.dll'
$destination = Join-Path $env:APPDATA 'Thunderstore Mod Manager\DataFolder\Valheim\profiles\Cretila\BepInEx\plugins'

Copy-Item -LiteralPath $source -Destination $destination -Force
Write-Host "Copied ValheimMod.dll to $destination"
