namespace voks.server.records;

public class WpfWorker : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ApplicationTermination _appTermination;
    private readonly ApartmentState STA = ApartmentState.STA;

    public WpfWorker(
        IServiceProvider serviceProvider,
        ApplicationTermination appTermination)
    {
        _services = serviceProvider;
        _appTermination = appTermination;
    }

    protected async override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var threadStart = new ParameterizedThreadStart(RunWpfApp);
        var thread = new Thread(threadStart);
        thread.SetApartmentState(STA);
        thread.Start(_services);

        await Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(50);
            }

        }, _appTermination.GetCancellationToken());
    }

    private void RunWpfApp(object? serviceProvider)
    {
        if (serviceProvider == null) _appTermination.Stop();

        var mainWin = new MainWindow((IServiceProvider)serviceProvider!);
        mainWin.Closing += MainWin_Closing;
        var wpfApp = new App();
        wpfApp.Run(mainWin);
    }

    private void MainWin_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _appTermination.Stop();
    }
}