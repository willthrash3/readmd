using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Readmd.VisualTests;

public sealed class ScreenRenderTests
{
    [Fact]
    public async Task MainWindowRendersMarkdownOnScreen()
    {
        var root = FindRepositoryRoot();
        var appPath = Path.Combine(root, "src", "Readmd", "bin", "Debug", "net10.0-windows", "Readmd.exe");
        var fixturePath = Path.Combine(root, "tests", "fixtures", "visual.md");
        var outputDirectory = Path.Combine(root, "TestResults");
        Directory.CreateDirectory(outputDirectory);

        Assert.True(File.Exists(appPath), $"Expected app build output at {appPath}");
        Assert.True(File.Exists(fixturePath), $"Expected visual fixture at {fixturePath}");

        using var process = StartApp(appPath, fixturePath);
        try
        {
            var hwnd = await WaitForMainWindowAsync(process, TimeSpan.FromSeconds(20));
            Assert.NotEqual(IntPtr.Zero, hwnd);

            ShowWindow(hwnd, ShowWindowCommand.Restore);
            SetWindowPos(hwnd, new IntPtr(-1), 80, 80, 1100, 820, SetWindowPosFlags.ShowWindow);
            SetForegroundWindow(hwnd);
            await Task.Delay(TimeSpan.FromSeconds(4));

            var screenshotPath = Path.Combine(outputDirectory, "readmd-visual.png");
            using var bitmap = CaptureWindow(hwnd);
            bitmap.Save(screenshotPath, ImageFormat.Png);

            var darkMode = IsSystemDarkMode();
            var chromeThemePixels = darkMode
                ? CountDarkPixelsInAppChrome(bitmap)
                : CountLightPixelsInAppChrome(bitmap);
            Assert.True(chromeThemePixels > 8_000, $"Expected the {(darkMode ? "dark" : "light")} Readmd app chrome to be visible. Found {chromeThemePixels} matching pixels. Screenshot: {screenshotPath}");

            var contentTextPixels = darkMode
                ? CountLightPixelsBelowToolbar(bitmap)
                : CountDarkPixelsBelowToolbar(bitmap);
            Assert.True(contentTextPixels > 850, $"Expected rendered Markdown text below the toolbar. Found {contentTextPixels} contrasting pixels. Screenshot: {screenshotPath}");
        }
        finally
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
    }

    private static Process StartApp(string appPath, string fixturePath)
    {
        var info = new ProcessStartInfo(appPath, $"\"{fixturePath}\"")
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(appPath) ?? Environment.CurrentDirectory
        };
        info.Environment["READMD_INSTANCE_NAME"] = $"visual-{Guid.NewGuid():N}";
        var process = Process.Start(info);
        Assert.NotNull(process);
        return process;
    }

    private static async Task<IntPtr> WaitForMainWindowAsync(Process process, TimeSpan timeout)
    {
        var deadline = DateTimeOffset.UtcNow + timeout;
        while (DateTimeOffset.UtcNow < deadline)
        {
            process.Refresh();
            if (process.HasExited)
            {
                throw new InvalidOperationException($"Readmd exited before showing a window. Exit code: {process.ExitCode}");
            }

            if (process.MainWindowHandle != IntPtr.Zero)
            {
                return process.MainWindowHandle;
            }

            await Task.Delay(200);
        }

        return IntPtr.Zero;
    }

    private static Bitmap CaptureWindow(IntPtr hwnd)
    {
        Assert.True(GetWindowRect(hwnd, out var rect), "Could not locate Readmd window bounds.");
        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        Assert.True(width > 0 && height > 0, $"Invalid window size {width}x{height}.");

        var bitmap = new Bitmap(width, height);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.CopyFromScreen(rect.Left, rect.Top, 0, 0, new Size(width, height), CopyPixelOperation.SourceCopy);
        return bitmap;
    }

    private static int CountLightPixelsInAppChrome(Bitmap bitmap)
    {
        var count = 0;
        for (var y = 50; y < Math.Min(170, bitmap.Height); y += 2)
        {
            for (var x = 50; x < bitmap.Width - 30; x += 2)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.R > 220 && pixel.G > 225 && pixel.B > 225)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountDarkPixelsInAppChrome(Bitmap bitmap)
    {
        var count = 0;
        for (var y = 50; y < Math.Min(170, bitmap.Height); y += 2)
        {
            for (var x = 50; x < bitmap.Width - 30; x += 2)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.R < 80 && pixel.G < 80 && pixel.B < 80)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountDarkPixelsBelowToolbar(Bitmap bitmap)
    {
        var count = 0;
        for (var y = 120; y < bitmap.Height; y += 3)
        {
            for (var x = 30; x < bitmap.Width - 30; x += 3)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.R < 95 && pixel.G < 95 && pixel.B < 95)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static int CountLightPixelsBelowToolbar(Bitmap bitmap)
    {
        var count = 0;
        for (var y = 120; y < bitmap.Height; y += 3)
        {
            for (var x = 30; x < bitmap.Width - 30; x += 3)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.R > 155 && pixel.G > 155 && pixel.B > 155)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static bool IsSystemDarkMode()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        return key?.GetValue("AppsUseLightTheme") is int useLightTheme && useLightTheme == 0;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Readmd.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not find Readmd.sln above the test output directory.");
    }

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hwnd, out WindowRect rect);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hwnd, ShowWindowCommand command);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int cx, int cy, SetWindowPosFlags flags);

    private readonly struct WindowRect
    {
        public readonly int Left;
        public readonly int Top;
        public readonly int Right;
        public readonly int Bottom;
    }

    private enum ShowWindowCommand
    {
        Restore = 9
    }

    [Flags]
    private enum SetWindowPosFlags
    {
        ShowWindow = 0x0040
    }
}
