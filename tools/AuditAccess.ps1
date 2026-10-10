<#
Guards against the publicized-DLL trap.

The assemblies in libs/ are publicized, so the compiler accepts direct access to members that
are private in the assembly the game actually ships. That compiles clean and then throws
FieldAccessException when the method is JIT'd - which, on a patch that runs during startup,
presents as the game failing to load a world rather than as anything resembling an access error.

This walks every field and method reference in the built DLL, resolves it against the shipped
assemblies, and reports any that are not public. Harmony *patching* a non-public method is fine
(it goes through reflection), so [HarmonyPatch] attribute arguments are strings or typeof and
never appear here as direct references - anything this flags is real.
#>
param(
  [string]$Dll     = "$PSScriptRoot\..\ValheimMod\bin\Release\ValheimMod.dll",
  [string]$Managed = "C:\Program Files (x86)\Steam\steamapps\common\Valheim\valheim_Data\Managed",
  [string]$Core    = "C:\Users\mpipk\AppData\Roaming\Thunderstore Mod Manager\DataFolder\Valheim\profiles\Cretila\BepInEx\core",
  [string]$Cecil   = "C:\Users\mpipk\AppData\Roaming\Thunderstore Mod Manager\DataFolder\Valheim\profiles\Cretila\BepInEx\core\Mono.Cecil.dll"
)

$ErrorActionPreference = 'Stop'
Add-Type -Path $Cecil

if (-not (Test-Path $Dll))     { throw "built DLL not found: $Dll" }
if (-not (Test-Path $Managed)) { throw "Managed folder not found: $Managed" }

# Resolve against the SHIPPED assemblies only. libs/ must stay off this path or the publicized
# copies would answer and every check would pass.
$resolver = New-Object Mono.Cecil.DefaultAssemblyResolver
foreach ($d in $resolver.GetSearchDirectories()) { $resolver.RemoveSearchDirectory($d) }
$resolver.AddSearchDirectory($Managed)
# BepInEx and Harmony live outside Managed. They are not publicized, so they carry no risk, but
# without them every call into them reports as unresolved and buries anything that matters.
if (Test-Path $Core) { $resolver.AddSearchDirectory($Core) }

$rp = New-Object Mono.Cecil.ReaderParameters
$rp.AssemblyResolver = $resolver
$asm = [Mono.Cecil.AssemblyDefinition]::ReadAssembly($Dll, $rp)

$findings = New-Object System.Collections.Generic.List[string]
$seen     = New-Object System.Collections.Generic.HashSet[string]

# Protected access from a subclass is legal and works at runtime - calling the BaseUnityPlugin
# constructor is the obvious case. Cecil only reports IsPublic, so without this the audit would
# flag every mod's own entry point and train us to ignore it.
function Inherits($type, $ancestorFullName) {
  $t = $type
  while ($null -ne $t) {
    if ($t.FullName -eq $ancestorFullName) { return $true }
    if ($null -eq $t.BaseType) { return $false }
    try { $t = $t.BaseType.Resolve() } catch { return $false }
  }
  return $false
}

function IsLegalAccess($def, $fromType) {
  if ($def.IsPublic) { return $true }
  if ($def.IsFamily -or $def.IsFamilyOrAssembly) {
    return (Inherits $fromType $def.DeclaringType.FullName)
  }
  return $false
}

function Note($kind, $where, $what) {
  $key = "$kind|$what"
  if ($seen.Add($key)) { $findings.Add("  [$kind] $what`n      in $where") }
}

foreach ($type in $asm.MainModule.GetTypes()) {
  foreach ($method in $type.Methods) {
    if (-not $method.HasBody) { continue }
    $where = "$($type.FullName)::$($method.Name)"

    foreach ($ins in $method.Body.Instructions) {
      $op = $ins.Operand
      if ($null -eq $op) { continue }

      # Skip references into this assembly itself - our own privates are our business
      if ($op -is [Mono.Cecil.FieldReference]) {
        if ($op.DeclaringType.Scope -eq $asm.MainModule) { continue }
        try { $def = $op.Resolve() } catch { $def = $null }
        if ($null -eq $def) { Note 'unresolved field' $where $op.FullName; continue }
        if (-not (IsLegalAccess $def $type)) { Note 'NON-PUBLIC FIELD' $where "$($def.DeclaringType.FullName)::$($def.Name)" }
      }
      elseif ($op -is [Mono.Cecil.MethodReference] -and -not ($op -is [Mono.Cecil.GenericInstanceMethod] -and $false)) {
        if ($op.DeclaringType.Scope -eq $asm.MainModule) { continue }
        try { $def = $op.Resolve() } catch { $def = $null }
        if ($null -eq $def) { Note 'unresolved method' $where $op.FullName; continue }
        if (-not (IsLegalAccess $def $type)) { Note 'NON-PUBLIC METHOD' $where "$($def.DeclaringType.FullName)::$($def.Name)" }
      }
    }
  }
}

$stamp = (Get-Item $Dll).LastWriteTime
Write-Host "audited $Dll (built $stamp)"
if ($findings.Count -eq 0) {
  Write-Host "CLEAN - every direct member access is public in the shipped assemblies"
} else {
  Write-Host "$($findings.Count) finding(s):"
  $findings | ForEach-Object { Write-Host $_ }
  exit 1
}
