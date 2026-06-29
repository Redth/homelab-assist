<#
.SYNOPSIS
  homelab-secret (Windows) — store/retrieve homelab credentials in the native Windows
  Credential Vault, so tokens/passwords never sit in plaintext files or shell history.

.DESCRIPTION
  Uses the Windows Runtime PasswordVault (built into Windows 10/11 desktop — no module to
  install). Secrets are namespaced under the resource "homelab-assist"; the KEY you choose
  should match the environment variable its consumer expects (e.g. NPM_PASSWORD).

  If PasswordVault is unavailable in your context, install the cross-platform
  Microsoft.PowerShell.SecretManagement module as an alternative (see the homelab-secrets skill).

.EXAMPLE
  .\homelab-secret.ps1 set NPM_PASSWORD
  .\homelab-secret.ps1 get NPM_PASSWORD
  .\homelab-secret.ps1 run NPM_PASSWORD -- npm-mcp.cmd
  .\homelab-secret.ps1 has NPM_PASSWORD
  .\homelab-secret.ps1 delete NPM_PASSWORD
#>
param(
  [Parameter(Position = 0)] [string]$Command,
  [Parameter(Position = 1)] [string]$Key,
  [Parameter(Position = 2, ValueFromRemainingArguments = $true)] [string[]]$Rest
)

$ErrorActionPreference = 'Stop'
$Service = 'homelab-assist'

function Get-Vault {
  [void][Windows.Security.Credentials.PasswordVault, Windows.Security.Credentials, ContentType = WindowsRuntime]
  return [Windows.Security.Credentials.PasswordVault]::new()
}

function Set-Secret([string]$k, [string]$v) {
  $vault = Get-Vault
  try { $vault.Remove($vault.Retrieve($Service, $k)) } catch {}
  $vault.Add([Windows.Security.Credentials.PasswordCredential]::new($Service, $k, $v))
}

function Get-Secret([string]$k) {
  $vault = Get-Vault
  $cred = $vault.Retrieve($Service, $k)
  $cred.RetrievePassword()
  return $cred.Password
}

function Test-Secret([string]$k) {
  try { Get-Vault | Out-Null; (Get-Vault).Retrieve($Service, $k) | Out-Null; return $true }
  catch { return $false }
}

function Remove-Secret([string]$k) {
  $vault = Get-Vault
  try { $vault.Remove($vault.Retrieve($Service, $k)) } catch {}
}

switch ($Command) {
  'set' {
    if (-not $Key) { Write-Error 'usage: homelab-secret.ps1 set <KEY>'; exit 2 }
    if ($Rest -and $Rest.Count -ge 1) { $val = $Rest[0] }
    else {
      $sec = Read-Host -AsSecureString "Value for $Key (hidden)"
      $bstr = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($sec)
      $val = [Runtime.InteropServices.Marshal]::PtrToStringBSTR($bstr)
      [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($bstr)
    }
    Set-Secret $Key $val
    Write-Host "stored $Key in $Service." -ForegroundColor Green
  }
  'get'    { if (-not $Key) { Write-Error 'usage: get <KEY>'; exit 2 }; Get-Secret $Key }
  'has'    { if (-not $Key) { Write-Error 'usage: has <KEY>'; exit 2 }; if (Test-Secret $Key) { exit 0 } else { exit 1 } }
  'delete' { if (-not $Key) { Write-Error 'usage: delete <KEY>'; exit 2 }; Remove-Secret $Key; Write-Host "deleted $Key." }
  'rm'     { if (-not $Key) { Write-Error 'usage: rm <KEY>'; exit 2 }; Remove-Secret $Key; Write-Host "deleted $Key." }
  'run' {
    # run KEY1,KEY2 -- cmd args...
    if (-not $Key) { Write-Error 'usage: run <KEY[,KEY..]> -- <cmd...>'; exit 2 }
    $args2 = @($Rest)
    if ($args2.Count -ge 1 -and $args2[0] -eq '--') { $args2 = $args2[1..($args2.Count - 1)] }
    if ($args2.Count -lt 1) { Write-Error 'run: missing command after --'; exit 2 }
    foreach ($k in ($Key -split ',')) {
      if (-not (Test-Secret $k)) { Write-Error "'$k' not found (set it with: homelab-secret.ps1 set $k)"; exit 1 }
      Set-Item -Path "Env:$k" -Value (Get-Secret $k)
    }
    & $args2[0] @($args2[1..($args2.Count - 1)])
  }
  default { Write-Host 'homelab-secret.ps1 — commands: set get run has delete (see -? for help)'; exit 2 }
}
