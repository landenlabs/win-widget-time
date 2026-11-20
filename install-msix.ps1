# Copyright (c) 2026 LanDen Labs - Dennis Lang
#
# Trust the WinWidgetTime MSIX signing certificate, install the MSIX, and launch the app.
#
# Put this script in the same folder as the downloaded .msix (and optional WinWidgetTime.cer), then:
#
#   powershell -ExecutionPolicy Bypass -File .\install-msix.ps1
#   powershell -ExecutionPolicy Bypass -File .\install-msix.ps1 -MsixPath .\WinWidgetTime-6.11.11-x64.msix
#
# Run it from a normal (non-admin) PowerShell. Only the certificate import is elevated (one UAC
# prompt); the package itself is installed for the current user, not for the admin account.

param(
    [string] $MsixPath = '',
    [string] $CerPath = '',
    [switch] $NoLaunch
)

$ErrorActionPreference = 'Stop'
$here = if ($PSScriptRoot) { $PSScriptRoot } else { (Get-Location).Path }

function Get-OsArch {
    $a = $env:PROCESSOR_ARCHITEW6432
    if (-not $a) { $a = $env:PROCESSOR_ARCHITECTURE }
    if ($a -eq 'ARM64') { 'arm64' } else { 'x64' }
}

function Test-IsAdmin {
    $id = [Security.Principal.WindowsIdentity]::GetCurrent()
    (New-Object Security.Principal.WindowsPrincipal $id).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

# 1. Locate the MSIX (newest matching this machine's architecture)
if (-not $MsixPath) {
    $arch = Get-OsArch
    $found = Get-ChildItem -Path $here -Filter "WinWidgetTime-*-$arch.msix" -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $found) { throw "No WinWidgetTime-*-$arch.msix found in $here. Pass -MsixPath." }
    $MsixPath = $found.FullName
}
$MsixPath = (Resolve-Path $MsixPath).Path
Write-Host "Package: $MsixPath"

# 2. Get the signing certificate - prefer the one embedded in the MSIX so it always matches
$cert = $null
try {
    $sig = Get-AuthenticodeSignature -FilePath $MsixPath
    if ($sig.SignerCertificate) { $cert = $sig.SignerCertificate }
} catch { }
if (-not $cert) {
    if (-not $CerPath) { $CerPath = Join-Path $here 'WinWidgetTime.cer' }
    if (-not (Test-Path $CerPath)) { throw "Could not read the MSIX signature and $CerPath was not found." }
    $cert = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2 (Resolve-Path $CerPath).Path
}
Write-Host "Signer:  $($cert.Subject)  [$($cert.Thumbprint)]"

# 3. Trust it (LocalMachine\TrustedPeople) unless it is already trusted
$trusted = (Test-Path "Cert:\LocalMachine\TrustedPeople\$($cert.Thumbprint)") -or
           (Test-Path "Cert:\LocalMachine\Root\$($cert.Thumbprint)") -or
           ($sig -and $sig.Status -eq 'Valid')
if ($trusted) {
    Write-Host 'Certificate already trusted.'
} else {
    $tmpCer = Join-Path $env:TEMP "WinWidgetTime-$($cert.Thumbprint).cer"
    [IO.File]::WriteAllBytes($tmpCer, $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert))
    $importCmd = "Import-Certificate -FilePath '$tmpCer' -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null"
    if (Test-IsAdmin) {
        Invoke-Expression $importCmd
    } else {
        Write-Host 'Requesting administrator rights to trust the certificate...'
        $enc = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes("`$ErrorActionPreference='Stop'; $importCmd"))
        $p = Start-Process powershell.exe -Verb RunAs -Wait -PassThru `
            -ArgumentList '-NoProfile', '-ExecutionPolicy', 'Bypass', '-EncodedCommand', $enc
        if ($p.ExitCode -ne 0) { throw "Certificate import failed (exit $($p.ExitCode))." }
    }
    Remove-Item $tmpCer -ErrorAction SilentlyContinue
    if (-not (Test-Path "Cert:\LocalMachine\TrustedPeople\$($cert.Thumbprint)")) {
        throw 'Certificate was not added to LocalMachine\TrustedPeople.'
    }
    Write-Host 'Certificate trusted (LocalMachine\TrustedPeople).'
}

# 4. Install (upgrades in place; closes a running copy if needed)
Write-Host 'Installing package...'
try {
    Add-AppxPackage -Path $MsixPath -ForceApplicationShutdown
} catch {
    Write-Host ''
    Write-Host "Install failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'If this is a managed/work PC, sideloading may be blocked by policy (ask IT).'
    Write-Host 'For details run:  Get-AppPackageLog -ActivityID <id from the message above>'
    exit 1
}

$pkg = Get-AppxPackage -Name 'LanDenLabs.WinWidgetTime' | Sort-Object Version -Descending | Select-Object -First 1
if (-not $pkg) { throw 'Package installed but not found via Get-AppxPackage.' }
Write-Host "Installed WinWidgetTime $($pkg.Version)" -ForegroundColor Green

# 5. Launch
if (-not $NoLaunch) {
    Start-Process "shell:AppsFolder\$($pkg.PackageFamilyName)!WinWidgetTime"
}
