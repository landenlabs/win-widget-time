# How to install WinWidgetTime

Every [GitHub release](https://github.com/landenlabs/win-widget-time/releases) includes these files:

| File | What it is |
|------|------------|
| `WinWidgetTime-<version>-x64.msix` | MSIX package for Intel/AMD 64-bit PCs **(recommended)** |
| `WinWidgetTime-<version>-arm64.msix` | MSIX package for ARM64 PCs (Snapdragon / Surface Pro X, etc.) |
| `WinWidgetTime.cer` | Public half of the certificate that signed the MSIX (self-signed releases only) |
| `install-msix.ps1` | Script that trusts the certificate, installs the MSIX, and starts the app |
| `uninstall-msix.ps1` | Script that removes the app and the certificate |
| `WinWidgetTime.zip` | Portable build: one self-contained `WinWidgetTime.exe`, no installer |

Both the MSIX and the zip are self-contained, so you don't need to install the .NET runtime for either one.

---

## Why MSIX is the recommended way

- It installs per user and shows up in the Start menu as **WinWidgetTime**.
- It upgrades in place when you install a newer release, and your settings are kept.
- It uninstalls cleanly from **Settings → Apps → Installed apps** and leaves no files behind.
- Windows checks the package signature, so a file that was tampered with won't install.

The zip is fine if you want a portable copy or can't install packages. You have to update it, add a Start menu shortcut, and delete it yourself.

---

## About the signing certificate

Windows **always** requires an MSIX package to be signed by a certificate the PC trusts. This applies on every Windows 10 and 11 PC, not just ones with extra security turned on. Certificates from commercial certificate authorities (CAs) are trusted automatically, but they cost money every year.

WinWidgetTime is a free project, so its release builds are signed with a **self-signed certificate** (`CN=LanDen Labs`) that the build creates. No commercial CA is paid. Windows doesn't trust that certificate yet, so before the MSIX can install, you add it **once per release** to the machine's **Trusted People** store. If you skip this step, the install fails with:

> `0x800B0109` – A certificate chain processed, but terminated in a root certificate which is not trusted by the trust provider.

Things to know:

- Adding the certificate needs administrator rights (one UAC prompt), because App Installer only checks the **Local Computer** store, not your user store.
- Put it in **Trusted People** only. Don't put it in *Trusted Root Certification Authorities*.
- The trust only covers packages signed by this certificate. It doesn't make Windows trust anything else.
- A new self-signed certificate is created for each release build. Each release you install adds its own certificate. `uninstall-msix.ps1` removes all of them.

### Windows security features that can still block the app

| Feature | What it does | Does the self-signed certificate help? |
|---------|--------------|----------------------------------------|
| **MSIX signature check** (App Installer / `Add-AppxPackage`) | Refuses to install packages whose signer isn't trusted. | **Yes.** Trusting the certificate is exactly what this check needs. |
| **Microsoft Defender SmartScreen** | Shows *"Windows protected your PC"* when you run a downloaded `.exe` that has no reputation yet (the zip build). | No. Click **More info → Run anyway**. |
| **Smart App Control** (Windows 11, under *Windows Security → App & browser control*) | When set to **On**, it blocks apps that Microsoft's cloud service doesn't recognize, unless they're signed by a CA in the Microsoft Trusted Root Program. | **No.** A self-signed certificate in your local store doesn't count. If Smart App Control blocks WinWidgetTime, you have to switch it **Off**. On many builds you can't turn it back on without resetting or reinstalling Windows, so decide carefully. |
| **App Control for Business (WDAC) / sideloading policy** on work PCs | IT policy can block sideloaded packages or unapproved apps. | No. Ask your IT department. |

To check Smart App Control, open **Settings → Privacy & security → Windows Security → App & browser control → Smart App Control settings**. Blocked files are logged in Event Viewer under **Applications and Services Logs → Microsoft → Windows → CodeIntegrity → Operational** (event ID 3077).

---

## Option 1: MSIX with the install script (recommended)

1. From the release, download the `.msix` for your CPU and `install-msix.ps1` into the same folder. If you're not sure of your CPU type, download both `.msix` files and the script picks the right one.
2. Open a normal (non-admin) PowerShell in that folder and run:

   ```powershell
   powershell -ExecutionPolicy Bypass -File .\install-msix.ps1
   ```

   The script:
   - picks the `.msix` that matches your CPU (or pass `-MsixPath <file>`),
   - reads the signing certificate from inside the package, so it always matches,
   - adds the certificate to `LocalMachine\TrustedPeople` (one UAC prompt, skipped if it's already trusted),
   - installs the package for your user, upgrading any older version,
   - starts WinWidgetTime (add `-NoLaunch` to skip this).

`-ExecutionPolicy Bypass` applies only to this one command. You need it because PowerShell blocks scripts downloaded from the internet by default.

## Option 2: MSIX installed manually

1. Download the `.msix` for your CPU and `WinWidgetTime.cer` from the **same** release.
2. Trust the certificate. You can do this one of two ways:
   - **Elevated PowerShell:**

     ```powershell
     Import-Certificate -FilePath .\WinWidgetTime.cer -CertStoreLocation Cert:\LocalMachine\TrustedPeople
     ```

   - **Windows interface:** Double-click `WinWidgetTime.cer`, then choose **Install Certificate… → Local Machine → Place all certificates in the following store → Browse → Trusted People → Finish**.
3. Double-click the `.msix` and click **Install**. Or run `Add-AppxPackage .\WinWidgetTime-<version>-x64.msix`.

## Option 3: Portable zip (no install)

1. Download `WinWidgetTime.zip`.
2. Right-click the zip, choose **Properties**, tick **Unblock**, and click **OK**. This stops SmartScreen from warning about every file you extract.
3. Extract it anywhere, for example `C:\opt\bin\winwidgets\`, and run `WinWidgetTime.exe`.
4. If SmartScreen still appears, click **More info → Run anyway**.

You don't need a certificate. Smart App Control (if **On**) may still block the unsigned exe, as described above.

## Option 4: Build from source

See [README.md](README.md#building-from-source). Run `build.bat <dest-dir>` to publish a portable build to `<dest-dir>\winwidgets\`.

---

## Uninstalling

**MSIX:** Run this from a normal PowerShell:

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall-msix.ps1            # remove the app and its certificate(s)
powershell -ExecutionPolicy Bypass -File .\uninstall-msix.ps1 -KeepApp   # remove only the certificate(s)
powershell -ExecutionPolicy Bypass -File .\uninstall-msix.ps1 -WhatIf    # preview, change nothing
```

The script removes the package for your user. It then deletes every **self-signed** `CN=LanDen Labs` certificate from the Trusted People and Root stores (machine and user), with one UAC prompt. Certificates issued by a CA are never touched.

You can also uninstall from **Settings → Apps → Installed apps → WinWidgetTime → Uninstall**. That removes the app but **not** the certificate. To remove the certificate yourself, open `certlm.msc` and go to **Trusted People → Certificates**, or run the script with `-KeepApp`.

**Zip:** Exit the widget from the tray menu and delete the folder. Settings are stored in `%AppData%\WinWidgetTime\`. Delete that folder too if you want them gone. If you turned on *Auto-start on login*, turn it off in Settings before deleting.
