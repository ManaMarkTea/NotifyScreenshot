# NotifyScreenshot

NotifyScreenshot is a small Windows .NET helper that shows a local screenshot file as a Windows toast notification.

It is meant to be launched by another screenshot tool, script, or hotkey workflow after an image has been saved. Pass the saved image path to `NotifyScreenshot.exe`, and it immediately displays a toast containing the file name/path and an inline preview. Clicking the notification opens File Explorer with that screenshot selected; Alt-clicking opens the screenshot in its default image app instead.

## How It Works

1. The app starts as a small Windows executable with no visible user interface.
2. It reads the first command-line argument as the screenshot image path.
3. It builds a local Windows toast notification with:
   - the title `Screenshot`
   - the image path as notification text
   - the screenshot itself as an inline image preview
   - the image path stored in the toast activation arguments
4. The app exits after showing the notification.
5. If the toast is clicked later, Windows reactivates the app with the saved activation arguments.
6. The app reads the `imagePath` argument and either:
   - runs File Explorer with `/select`, opening the containing folder and highlighting the screenshot; or
   - when Alt is held, opens the screenshot in its default image app.

The notification behavior is implemented in [Program.cs](Program.cs). The project is intentionally built as a Windows executable instead of a console app, so it can run from screenshot tools without opening a terminal window.

## Requirements

- Windows 10 or Windows 11
- .NET SDK 10.0 or newer to build from source
- A local image file path to show in the toast

The project uses `Microsoft.Toolkit.Uwp.Notifications` to create and handle Windows toast notifications.

## Usage

Build or publish the app, then run it with the full path to a screenshot:

```powershell
.\NotifyScreenshot.exe "C:\Users\you\Pictures\Screenshots\example.png"
```

For best results, pass an absolute path to an existing image file. The app is designed for quick one-shot execution, so it will normally appear only as a notification and then exit.

## Using With Greenshot

NotifyScreenshot works well as a Greenshot external command. Greenshot captures the screenshot, writes it to an image file, and then passes that file path to this app so Windows can show a clickable preview notification.

First, publish NotifyScreenshot so you have a stable executable path:

```powershell
dotnet publish .\NotifyScreenshot.csproj -c Release -o .\publish-single
```

Then configure Greenshot:

1. Right-click the Greenshot tray icon and open `Preferences`.
2. Open the `Plugins` tab.
3. Select `External command Plugin` and click `Configure`.
4. Add a new external command with these values:
   - `Name`: `NotifyScreenshot`
   - `Command`: the full path to your published `NotifyScreenshot.exe`
   - `Arguments`: `"{0}"`
   - `Output format`: `png`
   - `Run in background`: enabled
5. Save the command.

After that, use it in one of these ways:

- Leave Greenshot's destination picker enabled, take a screenshot, and choose `External NotifyScreenshot` from the destination list.
- Or open Greenshot `Preferences`, go to the `Destination` tab, and add `External NotifyScreenshot` to the default destinations so it runs automatically after each capture.

The `"{0}"` argument is important: Greenshot replaces `{0}` with the screenshot file path, and the quotes keep paths with spaces working.

If the notification shows a temporary file path, adjust your Greenshot output settings so the screenshot is saved to your normal screenshot folder before or alongside the external command.

## Build

From the repository root:

```powershell
dotnet build .\NotifyScreenshot.csproj
```

## Publish

The project is configured for a self-contained Windows x64 single-file publish. That means the published app does not require a separate .NET runtime install on the target machine.

```powershell
dotnet publish .\NotifyScreenshot.csproj -c Release -o .\publish-single
```

The standalone executable will be created at:

```text
publish-single\NotifyScreenshot.exe
```

For a clean one-file output folder, publish to an empty folder or delete old publish output before republishing. Older builds of this project used WPF and may have left extra DLLs in the original `publish` folder.

## GitHub Releases

This repository includes a GitHub Actions workflow that can build and publish the standalone Windows executable automatically.

To create a release:

```powershell
git tag v1.0.0
git push origin v1.0.0
```

When GitHub receives a tag beginning with `v`, the workflow builds the app on Windows, publishes the self-contained single-file executable, and creates a GitHub Release with the `.exe` attached.

You can also run the release workflow manually from the repository's `Actions` tab and enter a tag such as `v1.0.0`.

## Project Structure

- `Program.cs` - startup logic, toast creation, and toast click handling
- `NotifyScreenshot.csproj` - target framework, icon, package, and publish settings
- `.github/workflows/release.yml` - GitHub Actions release build
- `screenshot_alt_macos_bigsur_icon_189765.ico` - application icon

## Notes

- This app does not take screenshots by itself. It only displays a notification for an image file that already exists.
- The first command-line argument should be the image path.
- Clicking the toast selects the image in File Explorer. Hold Alt while clicking to open the image directly.
- The app uses modern .NET, but it is still Windows-specific because Windows toast notifications and File Explorer selection are Windows features.
