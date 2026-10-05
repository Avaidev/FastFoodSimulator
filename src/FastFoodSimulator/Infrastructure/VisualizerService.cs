using System.Windows;
using FastFoodSimulator.Services;
using FastFoodSimulator.ViewModels;
using FastFoodSimulator.Views;

namespace FastFoodSimulator.Infrastructure;

public sealed class VisualizerService : IVisualizerService
{
    private readonly ISimulationEngine _engine;
    private readonly UiDispatcher _ui;
    private SimulationDetailWindow? _window;

    public VisualizerService(ISimulationEngine engine, UiDispatcher ui)
    {
        _engine = engine;
        _ui = ui;
    }

    public void Show()
    {
        if (_window is not null)
        {
            _window.Activate();
            return;
        }

        var viewModel = new SimulationDetailViewModel(_engine, _ui);
        _window = new SimulationDetailWindow
        {
            DataContext = viewModel,
            Owner = Application.Current.MainWindow
        };

        _window.Closed += (_, _) =>
        {
            viewModel.Dispose();
            _window = null;
        };

        _window.Show();
    }
}
