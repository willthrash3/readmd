# Readmd

Readmd is a small Windows Markdown viewer for opening `.md` files without taking ownership of them. It keeps the app read-only, renders each file in its own tab, and reads files with shared access so editors, sync tools, and build processes can continue modifying or replacing the file while Readmd is open.

## Features

- Tabbed Markdown viewing with `Ctrl+O` to open files and `Ctrl+W` to close the current tab.
- Read-only rendering through Markdig advanced extensions.
- Mermaid diagram support for fenced `mermaid` code blocks.
- Shared file reads with `FileShare.ReadWrite | FileShare.Delete`.
- Single-instance handoff so opening another Markdown file activates the existing Readmd window and adds a tab.
- Per-user Windows file association scripts for `.md` Open With entries.
- Multi-size Windows app icon generated from the project icon design.

## Requirements

- Windows with WPF support.
- .NET 10 SDK for building and testing.
- Microsoft Edge WebView2 Runtime for viewing rendered documents.
- PowerShell 7 or Windows PowerShell for the helper scripts.

## Quick Start

Build the solution:

```powershell
dotnet build
```

Run Readmd with a file:

```powershell
dotnet run --project src\Readmd -- .\README.md
```

Open more than one file at startup:

```powershell
dotnet run --project src\Readmd -- .\README.md .\tests\fixtures\visual.md
```

If no file is supplied, Readmd opens an empty read-only tab.

## Project Layout

```text
src\Readmd\
  App.xaml(.cs)                  WPF startup and single-instance handoff.
  MainWindow.xaml(.cs)           Window chrome, tabs, commands, and WebView loading.
  Documents\                     File loading and rendered-document state.
  Infrastructure\                Startup options and named-pipe single-instance support.
  Rendering\MarkdownRenderer.cs  Markdig pipeline and HTML/CSS shell.
  Assets\                        App icon assets.

scripts\
  Generate-ReadmdIcon.ps1        Regenerates the `.ico` and preview PNG.
  Register-ReadmdFileAssociation.ps1
  Unregister-ReadmdFileAssociation.ps1

tests\
  Readmd.Tests\                  Renderer and asset unit tests.
  Readmd.VisualTests\            Real WPF window screenshot test.
  fixtures\visual.md             Visual test input document.
```

## Markdown Support

Rendering is powered by Markdig advanced extensions. The app supports common Markdown features such as:

- headings, paragraphs, links, images, and horizontal rules
- fenced code blocks and inline code
- tables
- task lists
- strikethrough
- block quotes
- autolinks
- footnotes
- definition lists
- Mermaid diagrams through fenced `mermaid` code blocks

Raw HTML from Markdown files is disabled before rendering. Relative file references, such as local images, are resolved by adding a `<base>` tag for the opened document's directory.

## Security Model

Readmd is a viewer, not an editor or script host. The renderer disables raw HTML and wraps the document in a restrictive Content Security Policy. Images can load from local files, data URLs, and HTTPS. Mermaid is loaded from jsDelivr and initialized inside the WebView when present.

If WebView2 cannot initialize, the tab shows a rendered error document instead of crashing the app.

## Single-Instance Behavior

Readmd uses a per-instance mutex and named pipe. By default, all launches use the same instance name and forward new file paths to the first window.

Tests can isolate themselves with:

```powershell
$env:READMD_INSTANCE_NAME = "visual-test"
```

Use a different `READMD_INSTANCE_NAME` when you intentionally want separate Readmd windows for automation or manual testing.

## File Association

Publish the app:

```powershell
dotnet publish src\Readmd -c Release -o .\artifacts\Readmd
```

Register the published executable for `.md` Open With entries:

```powershell
.\scripts\Register-ReadmdFileAssociation.ps1 -ExePath .\artifacts\Readmd\Readmd.exe
```

The registration writes per-user keys under `HKCU`, so it does not require administrator access. Windows will show Readmd in Explorer's Open With flow and in Settings > Apps > Default apps. Windows controls the final default-app choice, so the script registers Readmd without forcing a UserChoice hash.

Remove the registration:

```powershell
.\scripts\Unregister-ReadmdFileAssociation.ps1
```

## Icon Generation

The checked-in app icon is `src\Readmd\Assets\Readmd.ico`. Regenerate it after icon design changes:

```powershell
.\scripts\Generate-ReadmdIcon.ps1
```

The script writes the ICO file and a preview image at `TestResults\readmd-icon-256.png` by default.

## Tests

Run all tests:

```powershell
dotnet test
```

Run without rebuilding after a successful build:

```powershell
dotnet test --no-build
```

The visual test launches the real WPF app with `tests\fixtures\visual.md`, captures the window, and writes `TestResults\readmd-visual.png`. It requires access to the interactive Windows desktop. If screenshot capture fails in a restricted shell, rerun it from an interactive session.

## Troubleshooting

`WebView2 failed to load.`

Install or repair the Microsoft Edge WebView2 Runtime, then restart Readmd.

`dotnet` cannot find `net10.0-windows`.

Install the .NET 10 SDK and verify it is visible with:

```powershell
dotnet --list-sdks
```

Readmd does not appear in Open With.

Publish the app first, rerun the registration script with the published `Readmd.exe`, then reopen Explorer or check Windows Settings > Apps > Default apps.

The visual test captures the wrong window.

Make sure the test is running in an unlocked interactive desktop session. The test now verifies both the Readmd chrome and rendered Markdown content, so background captures should fail instead of passing silently.
