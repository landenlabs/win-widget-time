# Microsoft Store submission: WinWidgetTime

Copy-paste material for Partner Center (https://partner.microsoft.com/dashboard). Fill in the
sections in the order Partner Center shows them.

## Product identity (Product management > Product identity)

Copy these three values into the repo Actions **Variables** (Settings > Secrets and variables >
Actions > Variables) so the release workflow builds a matching Store package:

| Partner Center field | Repo variable |
| --- | --- |
| Package/Identity/Name | `MSIX_IDENTITY_NAME` |
| Package/Identity/Publisher (`CN=...`) | `MSIX_PUBLISHER` |
| Package/Properties/PublisherDisplayName | `MSIX_PUBLISHER_DISPLAY_NAME` |

Then push a `v*` tag. The workflow's build artifact contains `dist-store/WinWidgetTime-<ver>-x64-store.msix`
and `-arm64-store.msix`. They are unsigned, which is what the Store expects. Upload both under
**Packages**.

## Properties

| Field | Value |
| --- | --- |
| Category | Utilities + tools (subcategory: none) |
| Privacy policy URL | https://landenlabs.github.io/win-widget-time/privacy.html |
| Website | https://landenlabs.github.io/win-widget-time/ |
| Support contact | https://github.com/landenlabs/win-widget-time/issues |
| Collects personal information? | No (see note below) |
| Product declarations | Leave "accesses, collects or transmits personal information" unchecked (see note below) |
| System requirements | Windows 10 version 1809 (build 17763) or later; x64 or ARM64; internet connection only for the optional city lookup |

**Note on personal information (decide before submitting).** The app never reads the device's
location, has no account and no telemetry, and LanDen Labs receives nothing. Its only network
request is the city lookup: when the user presses Enter or clicks **Look up** in Settings, the city
name the user typed is sent over HTTPS to OpenStreetMap Nominatim (`nominatim.openstreetmap.org`),
which returns map coordinates. The app converts them to a time zone on the PC and discards them. A
city typed for a world clock is not normally personal information, so "No" is the expected answer,
but because user-typed text does go to a third party, Microsoft requires a privacy policy URL either
way (it is set above and describes this request).

## Age ratings (IARC questionnaire)

Category: **Utility / productivity**. Expected rating: 3+ / Everyone.

- Violence, fear, sexuality, gambling, drugs, language: **No**.
- Users interact or communicate with each other (chat, sharing): **No**.
- Shares the user's current, physical location with other users: **No**. The app does not read
  the device's location; it only looks up the time zone of a city name the user types, and nothing
  is shared with other users.
- Digital purchases: **No**.
- Unrestricted internet / web browsing: **No**. The only connection is the city-name lookup to
  OpenStreetMap Nominatim; the About dialog's links open the user's own browser.

## Pricing and availability

Free. No in-app purchases. All markets.

## Restricted capability justification

Partner Center asks why the package declares `runFullTrust`. Suggested text:

> WinWidgetTime is a WPF desktop app packaged with the Desktop Bridge. It needs runFullTrust to run
> as a normal Win32 process and draw a transparent, always-on-desktop widget window (optionally
> embedded in the wallpaper layer) with a system tray icon. Its only network use is an optional,
> user-initiated city-name lookup over HTTPS to OpenStreetMap Nominatim to find a city's time zone.
> It collects no user data.

## Store listing (English, United States)

**Product name:** WinWidgetTime

**Short description** (up to 270 characters):

> A lightweight, transparent world clock widget that shows the live time for as many cities as you
> like, right on your desktop wallpaper. No ads, no account, no telemetry.

**Description:**

> WinWidgetTime puts a small, transparent world clock right on your desktop, so you can see the
> time anywhere at a glance.
>
> Add as many cities as you like. Type a city name and press Enter to look up its time zone, or type
> a time zone ID yourself, or use "Local" to follow your PC's own time zone. Each row gets its own
> label, color and date/time format, in 12 or 24 hour time, and updates every second.
>
> Make it yours: pick the background color, opacity and font size, show or hide the title bar, and
> drag a corner to resize. Run several widgets, place them on any monitor, and WinWidgetTime
> remembers where each one goes for each monitor layout. It can start automatically when you sign
> in.
>
> Private by design: WinWidgetTime has no account, ads, analytics or telemetry. The only time it
> uses the internet is when you look up a city: the city name you typed is sent to OpenStreetMap
> Nominatim to find its location, which the app turns into a time zone on your PC. Nothing else is
> sent.
>
> Free and open source under the Apache 2.0 license.

**What's new in this version:** (fill in per release)

**Product features** (one per line, up to 20):

```
World clock for any number of cities on your desktop
Live time, updated every second
City name lookup fills in the time zone
Per-city label, color and date/time format
12 or 24 hour time
Transparent widget that sits on your wallpaper
Custom background color, opacity and font size
Multiple widgets, multi-monitor aware
Optional start at sign-in
No ads, no account, no telemetry
```

**Search terms** (up to 7):

```
world clock
desktop clock
time zones
desktop widget
clock widget
time zone converter
multiple clocks
```

**Copyright and trademark info:** © 2026 LanDen Labs

**Additional license terms:** Apache License 2.0,
https://github.com/landenlabs/win-widget-time/blob/main/LICENSE

## Images

| Partner Center slot | File |
| --- | --- |
| Store logo, 1:1 (300 x 300) | `store-assets/tile-300x300.png` (**TODO: not created yet**, make it from `screens/icon.png`) |
| Screenshots (Desktop; at least 1, recommended 1366 x 768 or larger) | See note below |

The Store requires desktop screenshots of at least 1366 x 768. The images in `screens/` are cropped
to single windows and are too small. Capture full-screen shots with the widget on the desktop, for
example one with the widget on a wallpaper and one with Settings open.
