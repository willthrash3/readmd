using System.Windows;
using Readmd.Infrastructure;

namespace Readmd;

public partial class App : Application
{
    private SingleInstanceCoordinator? _singleInstance;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstance = new SingleInstanceCoordinator(StartupOptions.Current.InstanceName);
        if (!_singleInstance.TryBecomePrimary())
        {
            SingleInstanceCoordinator.SendOpenRequest(StartupOptions.Current.InstanceName, e.Args);
            Shutdown();
            return;
        }

        var window = new MainWindow();
        MainWindow = window;

        _singleInstance.OpenRequested += paths =>
        {
            Dispatcher.Invoke(() =>
            {
                if (paths.Length > 0)
                {
                    window.OpenFiles(paths);
                }

                window.RestoreAndActivate();
            });
        };
        _singleInstance.StartListening();

        window.Show();
        if (e.Args.Length > 0)
        {
            window.OpenFiles(e.Args);
        }
        else
        {
            window.OpenBlankTab();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstance?.Dispose();
        base.OnExit(e);
    }
}
