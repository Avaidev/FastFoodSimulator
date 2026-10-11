using System.Windows.Input;
using FastFoodSimulator.Commands;
using FastFoodSimulator.Infrastructure;
using FastFoodSimulator.Models;
using FastFoodSimulator.Services;

namespace FastFoodSimulator.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private readonly ISimulationEngine _engine;
    private readonly UiDispatcher _ui;
    private long _lastVersion;
    private bool _isRunning;
    private int _orderLineCount;
    private int _waitingOrdersCount;
    private int _servingLineCount;

    public MainViewModel(ISimulationEngine engine, UiDispatcher ui)
    {
        _engine = engine;
        _ui = ui;

        Settings = new SettingsViewModel();
        Visualizer = new VisualizerViewModel();

        StartCommand = new RelayCommand(Start, () => !IsRunning && Settings.IsValid);
        StopCommand = new AsyncRelayCommand(StopAsync, () => IsRunning);

        _engine.SnapshotPublished += OnSnapshotPublished;
    }

    public SettingsViewModel Settings { get; }

    public VisualizerViewModel Visualizer { get; }

    public ICommand StartCommand { get; }

    public ICommand StopCommand { get; }

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (SetProperty(ref _isRunning, value))
            {
                OnPropertyChanged(nameof(IsSettingsEditable));
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    public bool IsSettingsEditable => !IsRunning;

    public int OrderLineCount
    {
        get => _orderLineCount;
        private set => SetProperty(ref _orderLineCount, value);
    }

    public int WaitingOrdersCount
    {
        get => _waitingOrdersCount;
        private set => SetProperty(ref _waitingOrdersCount, value);
    }

    public int ServingLineCount
    {
        get => _servingLineCount;
        private set => SetProperty(ref _servingLineCount, value);
    }

    public void Dispose()
    {
        _engine.SnapshotPublished -= OnSnapshotPublished;
    }

    private void Start()
    {
        _engine.Start(Settings.ToSettings());
    }

    private Task StopAsync() => _engine.StopAsync();

    private void OnSnapshotPublished(SimulationSnapshot snapshot) => _ui.Invoke(() => Apply(snapshot));

    private void Apply(SimulationSnapshot snapshot)
    {
        if (snapshot.Version <= _lastVersion)
        {
            return;
        }

        _lastVersion = snapshot.Version;

        IsRunning = snapshot.IsRunning;
        OrderLineCount = snapshot.OrderLine.Count;
        WaitingOrdersCount = snapshot.Carousel.Count;
        ServingLineCount = snapshot.ServingLine.Count;

        Visualizer.Update(snapshot);
    }
}
