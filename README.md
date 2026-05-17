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
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
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
- **Auto-start on login** — optional Windows startup via registry
- **Wallpaper embed mode** — render the widget at the wallpaper layer (below all windows)
- **Animated logo** — company logo plays on the About dialog (MP4 with PNG fallback)

---

## Requirements

- Windows 10 or Windows 11
- [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) — install once; no SDK required

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

### Option A — Download release zip

1. Go to [Releases](https://github.com/landenlabs/win-widget-time/releases)
2. Download `WinWidgetTime.zip`
3. Extract to any folder (e.g. `C:\opt\bin\winwidgets\`)
4. Run `WinWidgetTime.exe`

> The release zip contains a single self-contained `WinWidgetTime.exe` plus an `Assets\` folder.  
> You must have [.NET 8.0 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) installed.

### Option B — Build from source

```cmd
git clone https://github.com/landenlabs/win-widget-time.git
cd win-widget-time
install.bat
```

The `install.bat` script publishes the project and copies the output to `C:\opt\bin\winwidgets\`.

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
| Auto-start on login | Adds WinWidgetTime to Windows startup via registry |

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

- [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
- Windows (WPF requires a Windows build host)

### Build

```cmd
dotnet build WinWidgetTime.csproj -c Release
```

### Publish (FDD single-file, win-x64)

```cmd
dotnet publish WinWidgetTime.csproj -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true
```

Output: `bin\Release\net8.0-windows\win-x64\publish\`

This produces a **single `WinWidgetTime.exe`** (all managed assemblies bundled) plus the `Assets\` folder. Users need only the .NET 8 Desktop Runtime — no SDK required.

### Build and install via batch script

```cmd
install.bat
```

Kills any running instance, publishes, and copies all files to `C:\opt\bin\winwidgets\`.

---

## Project Structure

```
WinWidgetTime/
├── Models/
│   ├── AppSettings.cs       # Top-level settings (list of widgets, auto-start)
│   ├── PlaceEntry.cs        # Per-city data model
│   └── WidgetSettings.cs    # Per-widget data model (position, colors, places)
├── Services/
│   ├── AutoStartService.cs  # Windows registry startup
│   ├── DesktopService.cs    # Wallpaper embed / Win32 window helpers
│   ├── GeocodingService.cs  # OpenStreetMap city lookup
│   ├── MonitorService.cs    # Per-monitor position save/restore
│   ├── SettingsService.cs   # Load/save settings.json
│   └── TrayIconService.cs   # System tray icon
├── ViewModels/
│   └── TimeDisplayItem.cs   # Live time binding per city
├── Windows/
│   ├── AboutWindow.xaml     # About dialog
│   ├── ColorPickerWindow.xaml
│   ├── SettingsWindow.xaml  # Settings dialog (incl. screen-map position picker)
│   └── WidgetWindow.xaml    # Main widget overlay
├── Assets/
│   ├── landen_labs.mp4      # Animated logo (About dialog)
│   └── landenlabs.png       # Static logo fallback
└── install.bat              # Build and install script
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

## License

Apache 2.0 © [LanDen Labs](https://github.com/landenlabs) 2026
