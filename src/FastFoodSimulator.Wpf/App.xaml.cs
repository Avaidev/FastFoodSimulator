using System.Windows;
using FastFoodSimulator.Core.Abstractions;
using FastFoodSimulator.Core.Board;
using FastFoodSimulator.Core.Simulation;
using FastFoodSimulator.Core.Validation;
using FastFoodSimulator.Wpf.Infrastructure;
using FastFoodSimulator.Wpf.ViewModels;
using FastFoodSimulator.Wpf.Views;

namespace FastFoodSimulator.Wpf;

public partial class App : Application
{
    private ISimulationEngine? _engine;
    private MainViewModel? _viewModel;

    /// <summary>Composition root: the only place that knows all concrete types.</summary>
    private void OnStartup(object sender, StartupEventArgs e)
    {
        var board = new RestaurantBoard();

        _engine = new SimulationEngine(new SimulationSessionFactory(board), board);
        _viewModel = new MainViewModel(
            _engine,
            board,
            new SimulationSettingsValidator(),
            new WpfUiDispatcher(Dispatcher));

        var window = new Views.MainWindow { DataContext = _viewModel };
        MainWindow = window;
        window.Show();
    }

    private void OnExit(object sender, ExitEventArgs e)
    {
        _viewModel?.Dispose();
        _engine?.Dispose();
    }
}
