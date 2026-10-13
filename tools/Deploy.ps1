<#
Deploys the Release build to the live Thunderstore profile, but only if the access audit passes.

The audit exists to catch direct access to members that are non-public in the shipped assembly,
which compiles clean and throws at JIT. Running it and then copying anyway defeats the point -
that happened once, putting a DLL with a non-public StatusEffect.RemoveStartEffects call into the
live profile, where it would have thrown every frame from Update. The copy is gated now.
#>
param(
  [string]$Dll     = "$PSScriptRoot\..\ValheimMod\bin\Release\ValheimMod.dll",
  [string]$Plugins = "C:\Users\mpipk\AppData\Roaming\Thunderstore Mod Manager\DataFolder\Valheim\profiles\Cretila\BepInEx\plugins"
)

$ErrorActionPreference = 'Stop'

& "$PSScriptRoot\AuditAccess.ps1" -Dll $Dll
if ($LASTEXITCODE -ne 0) {
  Write-Host "DEPLOY ABORTED - audit failed. Fix the findings above before deploying."
  exit 1
}

$dest = Join-Path $Plugins (Split-Path $Dll -Leaf)
try {
  Copy-Item $Dll $dest -Force -ErrorAction Stop
  Write-Host "deployed -> $dest ($(([System.IO.FileInfo]$dest).LastWriteTime))"
} catch {
  # A running game keeps the DLL mapped, so this is the ordinary "close Valheim first" case
  Write-Host "DEPLOY BLOCKED - is Valheim running? The audited build is ready at $Dll"
  exit 2
}
