@echo off
:: WinResMonitor — Browser Extension Lockdown
:: Run as Administrator after loading the extension in Chrome/Edge.
:: Usage: InstallExtension.bat <extension-id> [chrome|edge|both]

setlocal

set EXT_ID=%1
set BROWSER=%2
if "%EXT_ID%"=="" (
  echo.
  echo Usage: InstallExtension.bat ^<extension-id^> [chrome^|edge^|both]
  echo.
  echo The extension ID is shown on chrome://extensions after loading the extension.
  echo.
  pause
  exit /b 1
)

if "%BROWSER%"=="" set BROWSER=both

:: ── Chrome ──────────────────────────────────────────────────────────────────
if /i "%BROWSER%"=="chrome" goto :chrome
if /i "%BROWSER%"=="both"   goto :chrome
goto :edge

:chrome
echo Installing for Chrome...
reg add "HKLM\SOFTWARE\Policies\Google\Chrome\ExtensionInstallForcelist" /v 1 /t REG_SZ /d "%EXT_ID%;https://clients2.google.com/service/update2/crx" /f >nul
reg add "HKLM\SOFTWARE\Policies\Google\Chrome\ExtensionInstallBlocklist" /v 1 /t REG_SZ /d "*" /f >nul
reg add "HKLM\SOFTWARE\Policies\Google\Chrome\ExtensionInstallAllowlist" /v 1 /t REG_SZ /d "%EXT_ID%" /f >nul
echo   Chrome done.

:: ── Edge ────────────────────────────────────────────────────────────────────
:edge
if /i "%BROWSER%"=="chrome" goto :done
echo Installing for Edge...
reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge\ExtensionInstallForcelist" /v 1 /t REG_SZ /d "%EXT_ID%;https://edge.microsoft.com/extensionwebstorebase/v1/crx" /f >nul
reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge\ExtensionInstallBlocklist" /v 1 /t REG_SZ /d "*" /f >nul
reg add "HKLM\SOFTWARE\Policies\Microsoft\Edge\ExtensionInstallAllowlist" /v 1 /t REG_SZ /d "%EXT_ID%" /f >nul
echo   Edge done.

:done
echo.
echo Extension locked down. Restart the browser to apply.
echo.
pause
