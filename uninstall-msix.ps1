# Copyright (c) 2026 LanDen Labs - Dennis Lang
#
# Uninstall the WinWidgetTime MSIX and remove the self-signed signing certificate(s) that
# install-msix.ps1 (or a manual Import-Certificate) added to the machine certificate stores.
#
#   powershell -ExecutionPolicy Bypass -File .\uninstall-msix.ps1              # remove app + certificate
#   powershell -ExecutionPolicy Bypass -File .\uninstall-msix.ps1 -KeepApp     # remove certificate only
#   powershell -ExecutionPolicy Bypass -File .\uninstall-msix.ps1 -WhatIf      # show what would be removed
#
# Run it from a normal (non-admin) PowerShell. The app is removed for the current user; only the
# certificate removal is elevated (one UAC prompt).
#
# Self-signed releases generate a new certificate per build, so every release you trusted left its
# own certificate behind. This script removes ALL of them: certificates whose Subject equals the
# package Publisher (default 'CN=LanDen Labs') AND that are self-signed (Issuer == Subject).
# CA-issued certificates are never touched.
#
# All LanDen Labs widgets (Perf, Battery, Time, Timer) share the same publisher certificate subject.
# If another package from the same publisher is still installed, the certificates are KEPT (removing
# them would break that app's trust). Pass -Force to remove them anyway.

[CmdletBinding(SupportsShouldProcess)]
param(
    [string] $Publisher = '',
    [switch] $KeepApp,
    [switch] $Force
)

$ErrorActionPreference = 'Stop'
$packageName = 'LanDenLabs.WinWidgetTime'

# Load Appx up front with WhatIf off, otherwise -WhatIf prints a line for every alias it defines
$wi = $WhatIfPreference; $WhatIfPreference = $false
Import-Module Appx -ErrorAction SilentlyContinue
$WhatIfPreference = $wi

function Test-IsAdmin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    (New-Object Security.Principal.WindowsPrincipal $id).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# 1. Work out the publisher (certificate subject) - from the installed package if present
$pkgs = @(Get-AppxPackage -Name $packageName -ErrorAction SilentlyContinue)
if (-not $Publisher) {
    $Publisher = if ($pkgs.Count -gt 0) { $pkgs[0].Publisher } else { 'CN=LanDen Labs' }
}
Write-Host "Publisher: $Publisher"

# 2. Remove the app (current user)
if ($KeepApp) {
    Write-Host 'Keeping installed app (-KeepApp).'
} elseif ($pkgs.Count -eq 0) {
    Write-Host 'WinWidgetTime package is not installed for this user.'
} else {
    foreach ($p in $pkgs) {
        if ($PSCmdlet.ShouldProcess($p.PackageFullName, 'Remove-AppxPackage')) {
            Get-Process -Name WinWidgetTime -ErrorAction SilentlyContinue | Stop-Process -Force -ErrorAction SilentlyContinue
            Remove-AppxPackage -Package $p.PackageFullName
            Write-Host "Removed package $($p.PackageFullName)"
        }
    }
}

# 3. Keep the certificate while any other package signed by the same publisher still needs it
$others = @(Get-AppxPackage -ErrorAction SilentlyContinue |
    Where-Object { $_.Publisher -eq $Publisher -and $_.Name -ne $packageName })
if ($others.Count -gt 0 -and -not $Force) {
    Write-Host "Keeping '$Publisher' certificate(s) - still used by:"
    $others | ForEach-Object { Write-Host "  $($_.Name)" }
    Write-Host 'Run with -Force to remove them anyway.'
    exit 0
}

# 4. Find matching self-signed certificates in the stores an install could have used
$stores = 'Cert:\LocalMachine\TrustedPeople', 'Cert:\LocalMachine\Root',
          'Cert:\CurrentUser\TrustedPeople', 'Cert:\CurrentUser\Root'
$found = foreach ($s in $stores) {
    Get-ChildItem -Path $s -ErrorAction SilentlyContinue |
        Where-Object { $_.Subject -eq $Publisher -and $_.Issuer -eq $_.Subject } |
        ForEach-Object { [pscustomobject]@{ Store = $s; Thumbprint = $_.Thumbprint; NotAfter = $_.NotAfter } }
}
$found = @($found)
if ($found.Count -eq 0) {
    Write-Host "No self-signed '$Publisher' certificates found. Nothing to remove."
    exit 0
}
$found | ForEach-Object { Write-Host "Found:   $($_.Store)\$($_.Thumbprint)  (expires $($_.NotAfter.ToString('yyyy-MM-dd')))" }

if (-not $PSCmdlet.ShouldProcess("$($found.Count) certificate(s)", 'Remove')) { exit 0 }

# 5. Remove them. CurrentUser stores need no elevation; LocalMachine stores do.
$userCerts    = @($found | Where-Object { $_.Store -like 'Cert:\CurrentUser\*' })
$machineCerts = @($found | Where-Object { $_.Store -like 'Cert:\LocalMachine\*' })

foreach ($c in $userCerts) {
    Remove-Item -Path "$($c.Store)\$($c.Thumbprint)" -Force
}

if ($machineCerts.Count -gt 0) {
    $paths = ($machineCerts | ForEach-Object { "'$($_.Store)\$($_.Thumbprint)'" }) -join ','
    $removeCmd = "`$ErrorActionPreference='Stop'; foreach (`$p in @($paths)) { Remove-Item -Path `$p -Force }"
    if (Test-IsAdmin) {
        Invoke-Expression $removeCmd
    } else {
        Write-Host 'Requesting administrator rights to remove the certificate...'
        $enc = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($removeCmd))
        $p = Start-Process powershell.exe -Verb RunAs -Wait -PassThru `
            -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-EncodedCommand', $enc
        if ($p.ExitCode -ne 0) { throw "Certificate removal failed (exit $($p.ExitCode))." }
    }
}

# 6. Verify
$left = @($found | Where-Object { Test-Path "$($_.Store)\$($_.Thumbprint)" })
if ($left.Count -gt 0) {
    $left | ForEach-Object { Write-Host "Still present: $($_.Store)\$($_.Thumbprint)" -ForegroundColor Red }
    exit 1
}
Write-Host "Removed $($found.Count) certificate(s)." -ForegroundColor Green
