using System.Windows;
using FastFoodSimulator.Infrastructure;
using FastFoodSimulator.Services;
using FastFoodSimulator.ViewModels;
using FastFoodSimulator.Views;

namespace FastFoodSimulator;

public partial class App : Application
{
    private SimulationEngine? _engine;
    private MainViewModel? _mainViewModel;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var ui = new UiDispatcher(Dispatcher);
        _engine = new SimulationEngine();
        _mainViewModel = new MainViewModel(_engine, ui);

        var window = new MainWindow { DataContext = _mainViewModel };
        MainWindow = window;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _mainViewModel?.Dispose();
        _engine?.Dispose();
        base.OnExit(e);
    }
}
