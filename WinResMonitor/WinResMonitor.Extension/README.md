# WinResMonitor Browser Extension

Manifest V3 extension for Chrome and Edge. Tracks daily time per site, warns after 1 hour, and blocks for 24 hours after 1h 15m.

## Install (Developer Mode)

1. Go to `chrome://extensions` or `edge://extensions`
2. Enable **Developer mode**
3. Click **Load unpacked** → select this folder

## Lock Down via Group Policy (prevent removal)

### Chrome

1. Download the [Chrome ADM/ADMX templates](https://chromeenterprise.google/browser/download/)
2. Add to `Computer Configuration > Administrative Templates > Google > Google Chrome > Extensions`
3. Set **Configure the list of force-installed apps and extensions**:
   ```
   <extension-id>;https://clients2.google.com/service/update2/crx
   ```
   Replace `<extension-id>` with the ID shown on `chrome://extensions` after loading.

Or via registry (no ADMX needed):
```
reg add "HKLM\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist" /v 1 /t REG_SZ /d "<extension-id>;https://clients2.google.com/service/update2/crx" /f
```

### Edge
```
reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge\ExtensionInstallForcelist" /v 1 /t REG_SZ /d "<extension-id>;https://edge.microsoft.com/extensionwebstorebase/v1/crx" /f
```

### Prevent manual uninstall (both browsers)
```
reg add "HKLM\SOFTWARE\Policies\Google\Chrome\ExtensionInstallBlocklist" /v 1 /t REG_SZ /d "*" /f
reg add "HKLM\SOFTWARE\Policies\Google\Chrome\ExtensionInstallAllowlist" /v 1 /t REG_SZ /d "<extension-id>" /f
```

> For Edge replace `Google\Chrome` with `Microsoft\Edge`.

## How It Works

| Time Spent Today | Action |
|---|---|
| 0 – 59 min | Normal browsing |
| 60 – 74 min | Red warning banner injected into every page on that site |
| 75 min+ | Site redirected to block page for 24 hours |

Time is only counted when the tab is **active and the window is focused**. Whitelist sites (educational) are never tracked or blocked.

## Files

| File | Purpose |
|---|---|
| `manifest.json` | Extension manifest (MV3) |
| `background.js` | Service worker — time tracking, threshold logic, alarm tick |
| `content.js` | Injects warning banner into pages |
| `blocked.html` | Full-page block screen with countdown |
| `popup.html/js` | Extension popup — today's usage, site lists |
