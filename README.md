<table border="0">
  <tr>
    <td>
      17-May-2026<br>
      Windows<br>
      <a href="https://landenlabs.com/index.html">Home</a>
    </td>
    <td>
      <a href="https://landenlabs.com/index.html">
        <img src="screens/landenlabs.webp" width="300" alt="Logo">
      </a>
    </td>
  </tr>
</table>

# WinWidgetTime

[![Build and Package](https://github.com/landenlabs/win-widget-time/actions/workflows/build.yml/badge.svg)](https://github.com/landenlabs/win-widget-time/actions/workflows/build.yml)
![Platform](https://img.shields.io/badge/platform-Windows%2010%20%2F%2011-blue)
![.NET](https://img.shields.io/badge/.NET-10.0-purple)
![License](https://img.shields.io/badge/license-Apache%202.0-green)

A lightweight, transparent **World Clock desktop widget** for Windows 10 and 11. Displays live time for multiple cities as a semi-transparent overlay directly on your desktop wallpaper.

**By [LanDen Labs](https://github.com/landenlabs) (2026)**

---

## Screenshots

**Example on Desktop**

![Widget on desktop](screens/on-desktop.png)

**Settings dialog**

![Settings dialog](screens/settings.png)

**About dialog**

![About dialog](screens/about.png)

---

## Features

- **Multi-city clock** — display time for any number of cities simultaneously
- **Transparent overlay** — sits directly on the desktop wallpaper, no taskbar clutter
- **Live updates** — time refreshes every second
- **Per-city colors** — assign a custom color to each city row
- **City lookup** — search by city name via OpenStreetMap Nominatim (auto-populates timezone)
- **Drag to reposition** — click and drag the widget anywhere on the desktop *(Windows 11)*
- **Screen-map position picker** — drag a scaled widget marker across a miniature monitor map inside Settings to reposition the widget *(Windows 10 & 11 — see [Windows 10 notes](#windows-10-notes))*
- **Multi-monitor aware** — position saved per monitor layout; snaps to a safe position if the saved location is off-screen
- **Drag to reorder** — reorder cities in the settings list via drag handle
- **12 or 24-hour format** — configurable date and time display formats per city
- **Background color & opacity** — live-preview color and transparency in the widget while Settings is open
- **Show / hide title bar** — toggle the widget header on or off
- **Auto-start on login** — optional launch with Windows (a *Startup apps* entry when installed from the MSIX)
- **Wallpaper embed mode** — render the widget at the wallpaper layer (below all windows)
- **Animated logo** — company logo plays on the About dialog (MP4 with PNG fallback)

---

## Requirements

- Windows 10 or Windows 11 (x64 or ARM64)
- No .NET install needed for the release builds — the MSIX and zip are self-contained
- Framework-dependent builds from source need the [.NET 10.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)

---

## Windows 10 Notes

### Drag limitation

On **Windows 10**, the desktop widget cannot be dragged directly on screen. This is caused by a Windows 10 incompatibility between WPF's `AllowsTransparency` and `WindowStyle="None"` — the combination that transparent widgets require. The drag operation silently fails.

**Windows 11** does not have this limitation; direct drag works normally.

### Workaround — Screen-map position picker

Open **Settings** (hover the widget and click ⚙, or right-click → Settings) and scroll to the **Widget Position** panel at the bottom:

```
Widget Position ─────────────────────────────── X: 120  Y: 200
┌──────────────────────────────────────────────────────────────┐
│  ┌────────────────────────────┐  ┌─────────────────────┐    │
│  │  Primary                   │  │  2560×1440          │    │
│  │        ▓▓▓▓▓               │  └─────────────────────┘    │
│  └────────────────────────────┘                              │
└──────────────────────────────────────────────────────────────┘
  Drag the blue marker to reposition the widget — it moves live.
```

- The canvas shows **all connected monitors** scaled to fit
- The **blue marker** represents the widget at its current position
- Drag the marker to the desired location — **the widget moves live** as you drag
- Click **Save** to keep the new position, or **Cancel** to restore it
- The X / Y coordinates update in real-time as you drag

This approach works on Windows 10 because the Settings dialog is a normal opaque window that does not require transparency.

---

## Installation

See **[how-to-install.md](how-to-install.md)** for full details — every install option, why the MSIX is signed with a self-signed certificate, which Windows security features (SmartScreen, Smart App Control) can still block the app, and how to uninstall.

### Option A — MSIX installer (recommended)

1. From [Releases](https://github.com/landenlabs/win-widget-time/releases) download `WinWidgetTime-<version>-x64.msix` (or `-arm64.msix`) and `install-msix.ps1` into the same folder
2. Run it from a normal (non-admin) PowerShell:
   `powershell -ExecutionPolicy Bypass -File .\install-msix.ps1`
   It picks the `.msix` matching your CPU, trusts its self-signed certificate (one UAC prompt), installs, and launches the app. It shows up in the Start menu as **WinWidgetTime**.

> Windows only installs an MSIX whose signing certificate is trusted, so the self-signed certificate must be added to **Local Machine → Trusted People** first — the script does this for you.
> To remove the app and the certificate later: `powershell -ExecutionPolicy Bypass -File .\uninstall-msix.ps1`

### Option B — Portable release zip

1. Download `WinWidgetTime.zip` from [Releases](https://github.com/landenlabs/win-widget-time/releases)
2. Right-click the zip → **Properties** → tick **Unblock** → **OK**
3. Extract to any folder (e.g. `C:\opt\bin\winwidgets\`) and run `WinWidgetTime.exe`

> The zip contains a single self-contained `WinWidgetTime.exe` (assets bundled inside) — no .NET runtime needed. The exe is unsigned, so SmartScreen may warn (**More info → Run anyway**).

### Option C — Build from source

```cmd
git clone https://github.com/landenlabs/win-widget-time.git
cd win-widget-time
build.bat C:\opt\bin
```

`build.bat <dest-dir>` publishes a self-contained single-file exe to `<dest-dir>\winwidgets\` and writes a `<dest-dir>\WinWidgetTime.bat` launcher. With no argument it uses the `BINDIR` environment variable.

---

## Usage

### Widget controls

| Action | Result |
|--------|--------|
| **Hover** | Reveals ⚙ Settings and ? About buttons |
| **Drag** | Repositions the widget *(Windows 11 only — use Settings on Windows 10)* |
| **Right-click** | Opens context menu (Settings / About / Exit) |

### Context menu

```
⚙  Settings
?  About
───────────
✕  Remove Widget
───────────
✕  Exit
```

---

## Settings

Open Settings via the hover button or right-click menu.

### Places list (left panel)

- **Add / Delete** cities using the buttons below the list
- **Drag** the grip handle (⠿) to reorder
- **Click** a city to edit it in the right panel

### Edit panel (right panel)

| Field | Description |
|-------|-------------|
| City / Place | City name — press Enter or click **Look up** to auto-fill timezone |
| Time Zone | IANA timezone ID (e.g. `America/New_York`) — auto-filled by lookup |
| Display Label | The label shown in the widget for this city |
| Row Color | Per-city color swatch — click to open the color picker |
| Time Format | Per-city date/time format string (e.g. `ddd MMM dd  hh:mm:ss tt`) |

### Widget Appearance (bottom panel)

| Setting | Description |
|---------|-------------|
| Font Size | Slider 10–36 px — updates live |
| Opacity | Background opacity 0–100% — updates live |
| Background | Background color swatch — click to change |
| Show title bar | Toggle the widget header on/off |
| Auto-start on login | Launch the widget when Windows starts (registry Run key for the zip; Task Manager › Startup apps entry for the MSIX) |

### Widget Position (bottom of Settings — Windows 10 workaround)

A miniature map of your monitor layout. Drag the **blue marker** to move the widget anywhere on any screen. The widget repositions live as you drag. Coordinates are shown in the header. Changes are applied on **Save** and reverted on **Cancel**.

### Format tokens (click ? next to Time Format)

| Token | Meaning | Example |
|-------|---------|---------|
| `ddd` | 3-char day | Mon |
| `MMM` | 3-char month | Jan |
| `dd` | 2-digit day | 07 |
| `HH` / `hh` | 24 / 12-hour | 14 / 02 |
| `mm` | Minutes | 30 |
| `ss` | Seconds | 05 |
| `tt` | AM/PM | PM |

Settings are saved to `%APPDATA%\WinWidgetTime\settings.json`.

---

## Building from Source

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Windows (WPF requires a Windows build host)

### Build

```cmd
dotnet build WinWidgetTime.csproj -c Release
```

### Publish (FDD single-file, win-x64)

```cmd
dotnet publish WinWidgetTime.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Output: `bin\Release\net10.0-windows10.0.17763.0\win-x64\publish\`

This produces a **single `WinWidgetTime.exe`** (all managed assemblies bundled) plus the `Assets\` folder. Users need only the .NET 10 Desktop Runtime — no SDK required.

### Build and install via batch script

```cmd
build.bat <dest-dir>
```

Kills any running instance, publishes a self-contained, compressed single-file exe (the same build as the release zip), copies it to `<dest-dir>\winwidgets\`, and writes a `<dest-dir>\WinWidgetTime.bat` launcher. `<dest-dir>` defaults to the `BINDIR` environment variable.

### Build the MSIX locally

```powershell
powershell -ExecutionPolicy Bypass -File .github\scripts\build-msix.ps1 -Arch x64 -Version 6.11.20 -PfxPath cert.pfx -PfxPassword secret
```

The certificate subject must equal the package Publisher (default `CN=LanDen Labs`). The package is written to `dist\`. Requires the Windows 10/11 SDK (`makeappx.exe`, `signtool.exe`).

### Releases

Pushing a `v*` tag runs [.github/workflows/build.yml](.github/workflows/build.yml). It publishes the portable zip and signed x64 + ARM64 MSIX packages, and attaches them to a GitHub release together with `install-msix.ps1`, `uninstall-msix.ps1` and (for self-signed builds) `WinWidgetTime.cer`. If the repo secrets `MSIX_CERT_BASE64` (base64 `.pfx`) and `MSIX_CERT_PASSWORD` are set, that certificate signs the packages; otherwise a self-signed `CN=LanDen Labs` certificate is generated for the build.

---

## Project Structure

```
WinWidgetTime/
├── Models/
│   ├── AppSettings.cs          # Top-level settings (list of widgets, auto-start)
│   ├── PlaceEntry.cs           # Per-city data model
│   └── WidgetSettings.cs       # Per-widget data model (position, colors, places)
├── Services/
│   ├── AutoStartService.cs     # Run on login (Run key, or MSIX StartupTask)
│   ├── DesktopService.cs       # Wallpaper embed / Win32 window helpers
│   ├── GeocodingService.cs     # OpenStreetMap city lookup
│   ├── MonitorService.cs       # Per-monitor position save/restore
│   ├── SettingsService.cs      # Load/save settings.json
│   └── TrayIconService.cs      # System tray icon
├── ViewModels/
│   └── TimeDisplayItem.cs      # Live time binding per city
├── Windows/
│   ├── AboutWindow.xaml        # About dialog
│   ├── ColorPickerWindow.xaml  # Color picker dialog
│   ├── SettingsWindow.xaml     # Settings dialog (incl. screen-map position picker)
│   └── WidgetWindow.xaml       # Main widget overlay
├── Assets/
│   ├── landen_labs.mp4         # Animated logo (About dialog)
│   └── landenlabs.png          # Static logo fallback
├── time.png                    # Tray icon image
├── .github/
│   ├── workflows/build.yml     # Release build: zip + signed MSIX (x64, arm64)
│   └── scripts/build-msix.ps1  # MSIX packaging and signing
├── screens/icon.png            # Source image for the MSIX logos
├── build.bat                   # Build and install portable exe locally
├── install-msix.ps1            # Trust certificate + install MSIX (end users)
├── uninstall-msix.ps1          # Remove MSIX + its self-signed certificate(s)
└── how-to-install.md           # Install options, certificate and security notes
```

---

## Credits

| Component | Source |
|-----------|--------|
| City geocoding | [OpenStreetMap Nominatim](https://nominatim.openstreetmap.org/) |
| Timezone lookup | [GeoTimeZone](https://github.com/mattjohnsonpint/GeoTimeZone) |
| Timezone conversion | [TimeZoneConverter](https://github.com/mattjohnsonpint/TimeZoneConverter) |
| Timezone data | [IANA TZDB](https://www.iana.org/time-zones) |

---

## Privacy

No accounts, ads, analytics, telemetry or tracking. See [privacy.md](privacy.md) for exactly what the app reads, stores and (if anything) sends.

---

## License

Apache 2.0 © [LanDen Labs](https://github.com/landenlabs) 2026
