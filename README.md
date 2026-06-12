# Readmd

Readmd is a small Windows Markdown viewer. It uses a Notepad-like window with tabs, opens each file in a new tab, renders Markdown read-only, and reads files with shared access so other programs can keep editing or replacing them.

The app icon is a document-and-Markdown mark stored as `src\Readmd\Assets\Readmd.ico`, with `src\Readmd\Assets\ReadmdIcon.svg` kept as the editable source design. Regenerate the `.ico` after design changes with:

```powershell
.\scripts\Generate-ReadmdIcon.ps1
```

## Build and Run

Requires the .NET 10 SDK.

```powershell
dotnet build
dotnet run --project src\Readmd -- .\README.md
```

## File Association

Publish the app, then register the published executable for `.md` Open With entries:

```powershell
dotnet publish src\Readmd -c Release -o .\artifacts\Readmd
.\scripts\Register-ReadmdFileAssociation.ps1 -ExePath .\artifacts\Readmd\Readmd.exe
```

Windows will then show Readmd in Explorer's Open With flow and in Settings > Apps > Default apps. Windows controls the final default-app choice, so the script registers the app without forcing a UserChoice hash.

To remove the registration:

```powershell
.\scripts\Unregister-ReadmdFileAssociation.ps1
```

## Markdown Support

Rendering is powered by Markdig advanced extensions, including tables, task lists, fenced code blocks, strikethrough, autolinks, footnotes, definition lists, and other common Markdown extensions. Mermaid code fences render in place through Mermaid loaded in the WebView.

## Tests

```powershell
dotnet test
```

The visual test launches the real WPF app with `tests\fixtures\visual.md`, captures the window, and writes `TestResults\readmd-visual.png`.
