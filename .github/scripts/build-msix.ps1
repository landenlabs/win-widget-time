# Copyright (c) 2026 LanDen Labs - Dennis Lang
#
# Build a signed MSIX package for WinWidgetTime.
#
#   pwsh .github/scripts/build-msix.ps1 -Arch x64   -Version 6.11.11 -PfxPath cert.pfx -PfxPassword secret
#   pwsh .github/scripts/build-msix.ps1 -Arch arm64 -Version 6.11.11 -PfxPath cert.pfx -PfxPassword secret
#
# The certificate subject MUST equal -Publisher (the manifest Identity Publisher),
# otherwise signtool rejects the package.
#
# Microsoft Store build: pass -Store and omit -PfxPath. The package is left UNSIGNED (the Store
# re-signs it) and -IdentityName / -Publisher / -PublisherDisplayName MUST match
# Partner Center > Product management > Product identity, or the upload is rejected.
#
#   pwsh .github/scripts/build-msix.ps1 -Arch x64 -Version 6.11.11 -Store `
#        -IdentityName 12345LanDenLabs.WinWidgetTime -Publisher 'CN=XXXXXXXX-...' -PublisherDisplayName 'LanDen Labs'

param(
    [Parameter(Mandatory)] [ValidateSet('x64', 'arm64')] [string] $Arch,
    [Parameter(Mandatory)] [string] $Version,
    [string] $PfxPath = '',
    [string] $PfxPassword = '',
    [string] $Publisher = 'CN=LanDen Labs',
    [string] $IdentityName = 'LanDenLabs.WinWidgetTime',
    [string] $PublisherDisplayName = 'LanDen Labs',
    [switch] $Store,
    [string] $OutDir = 'dist',
    [string] $TimestampUrl = ''
)

$ErrorActionPreference = 'Stop'
if (-not $Store -and -not $PfxPath) { throw '-PfxPath is required unless -Store is given' }
$root = Resolve-Path (Join-Path $PSScriptRoot '..\..')
Push-Location $root
try {
    # MSIX needs a 4-part numeric version (each part 0..65535); "v6.06.20" -> "6.6.20.0".
    # Pre-release / build suffixes are dropped: "v6.11.17-beta" -> "6.11.17.0".
    $numeric = ($Version.TrimStart('v') -split '[-+]', 2)[0]
    $parts = @($numeric.Split('.') | ForEach-Object { [int]$_ })
    while ($parts.Count -lt 4) { $parts += 0 }
    $msixVersion = ($parts[0..3]) -join '.'

    # Locate Windows SDK tools (newest SDK first)
    $sdkBin = Get-ChildItem "${env:ProgramFiles(x86)}\Windows Kits\10\bin\10.*" -Directory |
        Sort-Object { [version]$_.Name } -Descending |
        Where-Object { Test-Path (Join-Path $_.FullName 'x64\makeappx.exe') } |
        Select-Object -First 1
    if (-not $sdkBin) { throw 'Windows SDK makeappx.exe not found' }
    $makeappx = Join-Path $sdkBin.FullName 'x64\makeappx.exe'
    $signtool = Join-Path $sdkBin.FullName 'x64\signtool.exe'

    # 1. Publish self-contained (not single-file: MSIX installs as a normal folder)
    $layout = Join-Path $root "msix-layout\$Arch"
    if (Test-Path $layout) { Remove-Item $layout -Recurse -Force }
    dotnet publish WinWidgetTime.csproj -c Release -r "win-$Arch" --self-contained true `
        -p:PublishSingleFile=false -o $layout
    if ($LASTEXITCODE) { throw "dotnet publish failed ($LASTEXITCODE)" }

    # 2. Package logos (kept in Images\ so they don't collide with the app's Assets\)
    Add-Type -AssemblyName System.Drawing
    $images = Join-Path $layout 'Images'
    New-Item -ItemType Directory -Force $images | Out-Null
    $src = [System.Drawing.Image]::FromFile((Join-Path $root 'screens\icon.png'))
    try {
        foreach ($logo in @(
            @{ Name = 'Square44x44Logo.png';   Size = 44 },
            @{ Name = 'Square150x150Logo.png'; Size = 150 },
            @{ Name = 'StoreLogo.png';         Size = 50 })) {
            $bmp = New-Object System.Drawing.Bitmap $logo.Size, $logo.Size
            $g = [System.Drawing.Graphics]::FromImage($bmp)
            $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
            $g.DrawImage($src, 0, 0, $logo.Size, $logo.Size)
            $g.Dispose()
            $bmp.Save((Join-Path $images $logo.Name), [System.Drawing.Imaging.ImageFormat]::Png)
            $bmp.Dispose()
        }
    }
    finally { $src.Dispose() }

    # 3. Manifest
    # The city lookup (OpenStreetMap Nominatim) is an outbound HTTPS call; a full-trust desktop app
    # needs no extra capability for that, so runFullTrust is the only one declared.
    $pubEsc  = [System.Security.SecurityElement]::Escape($Publisher)
    $idEsc   = [System.Security.SecurityElement]::Escape($IdentityName)
    $pubName = [System.Security.SecurityElement]::Escape($PublisherDisplayName)
    @"
<?xml version="1.0" encoding="utf-8"?>
<Package
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10"
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10"
  xmlns:uap5="http://schemas.microsoft.com/appx/manifest/uap/windows10/5"
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities"
  IgnorableNamespaces="uap uap5 rescap">
  <Identity Name="$idEsc" Publisher="$pubEsc" Version="$msixVersion" ProcessorArchitecture="$Arch" />
  <Properties>
    <DisplayName>WinWidgetTime</DisplayName>
    <PublisherDisplayName>$pubName</PublisherDisplayName>
    <Logo>Images\StoreLogo.png</Logo>
    <Description>Transparent world clock desktop widget showing the live time for the cities you choose</Description>
  </Properties>
  <Dependencies>
    <TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.17763.0" MaxVersionTested="10.0.26100.0" />
  </Dependencies>
  <Resources>
    <Resource Language="en-us" />
  </Resources>
  <Applications>
    <Application Id="WinWidgetTime" Executable="WinWidgetTime.exe" EntryPoint="Windows.FullTrustApplication">
      <uap:VisualElements
        DisplayName="WinWidgetTime"
        Description="World clock desktop widget"
        BackgroundColor="transparent"
        Square150x150Logo="Images\Square150x150Logo.png"
        Square44x44Logo="Images\Square44x44Logo.png" />
      <Extensions>
        <!-- Auto-start on login; toggled at runtime via AutoStartService (StartupTask API). -->
        <uap5:Extension Category="windows.startupTask" Executable="WinWidgetTime.exe" EntryPoint="Windows.FullTrustApplication">
          <uap5:StartupTask TaskId="WinWidgetTimeStartup" Enabled="false" DisplayName="WinWidgetTime" />
        </uap5:Extension>
      </Extensions>
    </Application>
  </Applications>
  <Capabilities>
    <rescap:Capability Name="runFullTrust" />
  </Capabilities>
</Package>
"@ | Set-Content -Path (Join-Path $layout 'AppxManifest.xml') -Encoding utf8

    # 4. Pack + sign
    New-Item -ItemType Directory -Force $OutDir | Out-Null
    $suffix = if ($Store) { '-store' } else { '' }
    $msix = Join-Path (Resolve-Path $OutDir) "WinWidgetTime-$($Version.TrimStart('v'))-$Arch$suffix.msix"
    & $makeappx pack /o /d $layout /p $msix
    if ($LASTEXITCODE) { throw "makeappx failed ($LASTEXITCODE)" }

    if ($Store) {
        Write-Host "Store package left unsigned (Partner Center signs it)"
    } else {
        $signArgs = @('sign', '/fd', 'SHA256', '/f', $PfxPath, '/p', $PfxPassword)
        if ($TimestampUrl) { $signArgs += @('/tr', $TimestampUrl, '/td', 'SHA256') }
        & $signtool @signArgs $msix
        if ($LASTEXITCODE) { throw "signtool failed ($LASTEXITCODE)" }
    }

    Write-Host "Built $msix (version $msixVersion)"
}
finally { Pop-Location }
