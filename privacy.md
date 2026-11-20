# WinWidgetTime Privacy Policy

**Effective date:** November 20, 2026
**Publisher:** LanDen Labs

WinWidgetTime is a free Windows desktop widget that shows the current time for the cities you
choose. This policy explains what the app reads, what leaves your computer, and what is stored.

## Summary

- WinWidgetTime has **no accounts, no ads, no analytics, no telemetry, no tracking and no crash
  reporting**.
- It is **free**. There are no fees, subscriptions or in-app purchases.
- LanDen Labs **does not collect, receive or store any of your data**. There is no WinWidgetTime
  server.
- The app does **not** use your PC's location. It shows only the cities you type in.
- The only network request is the optional **city lookup** described below. It happens only when
  you ask for it.

## Information the app reads on your PC

To show the time, WinWidgetTime reads your PC's clock and time zone setting. To place the widget,
it also reads the size and layout of your monitors. Time-zone data is built into the app, so
showing the time for a city doesn't need the internet.

## Network requests the app makes

| When | Service contacted | What is sent |
| --- | --- | --- |
| In **Settings**, when you press **Enter** or click **Look up** on a city name | [OpenStreetMap Nominatim](https://nominatim.openstreetmap.org/) (`nominatim.openstreetmap.org`) | The city or place name you typed, plus the app name (`WinWidgetTime`) as the browser identifier that Nominatim's usage policy requires |

Nominatim sends back the place's map coordinates. The app turns those coordinates into a time zone
on your PC, saves the time zone, and **throws the coordinates away**.

If you enter `Local` as the city, the app uses your PC's own time zone and contacts no one. You can
also type a time zone ID by hand (for example `America/New_York`) and skip the lookup.

As with any internet request, OpenStreetMap can see your public IP address and the time of the
request. The [OpenStreetMap Foundation privacy policy](https://osmfoundation.org/wiki/Privacy_Policy)
applies to it. WinWidgetTime sends no name, account or identifier for you or your PC.

The app makes no other network connections. It doesn't check for updates. If you installed it from
the MSIX package, Windows handles updates when you install a newer release.

## Actions you choose to take

- The **GitHub** link in the **About** dialog opens the project page in your web browser.

## Information stored on your PC

WinWidgetTime saves your settings in `%AppData%\WinWidgetTime\settings.json`:

- each widget's position on each monitor layout, size, colors, opacity, font size and title-bar
  choice,
- your city list: the city name you typed, its time zone, the label shown on the widget, its color
  and its time format,
- whether *Auto-start on login* is on.

If you turn on *Auto-start on login* in the portable (zip) build, the app adds a `WinWidgetTime`
entry to your user's Windows startup (Run) registry key. In the MSIX build it uses the Windows
*Startup apps* setting instead.

When the app is installed from the MSIX package, Windows keeps this file inside the app's private
package folder (under `%LocalAppData%\Packages\`) instead.

The app keeps no log files or history. This data never leaves your PC. To delete it, uninstall the
app (uninstalling the MSIX removes its package folder), or delete the `%AppData%\WinWidgetTime`
folder.

## Children

WinWidgetTime is a general-purpose utility. It is not directed at children and collects no personal
information from anyone.

## License

WinWidgetTime is open source under the
[Apache License 2.0](https://github.com/landenlabs/win-widget-time/blob/main/LICENSE). You can read
all of its source code on [GitHub](https://github.com/landenlabs/win-widget-time) and confirm what
this policy describes.

## Changes to this policy

If the app's behavior changes in a way that affects privacy, this page will be updated and the
effective date above will change. The history of this page is public on
[GitHub](https://github.com/landenlabs/win-widget-time/commits/main/privacy.md).

## Contact

For questions about this policy, open an issue at
[github.com/landenlabs/win-widget-time/issues](https://github.com/landenlabs/win-widget-time/issues).
