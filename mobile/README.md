# chargeloop_mobile

Single-frontend Flutter WebView shell for **ChargeLoop** (Android / iOS).

It renders the live web UI (`backend/ChargeLoop.WebApi/wwwroot/index.html`) inside a full-screen `WebView` (`webview_flutter`), ensuring zero UI drift between web and mobile.

## Build Android APK

From the repository root:

```powershell
.\android-pack.ps1
```

This builds `dist/app-release.apk` pointing to `http://13.233.63.106/?app=1` (override with `-ServerUrl`).
