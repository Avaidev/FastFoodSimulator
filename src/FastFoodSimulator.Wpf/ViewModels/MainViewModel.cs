using System.Collections.ObjectModel;
using FastFoodSimulator.Core.Abstractions;
using FastFoodSimulator.Core.Board;
using FastFoodSimulator.Wpf.Infrastructure;

namespace FastFoodSimulator.Wpf.ViewModels;

public sealed class MainViewModel : ViewModelBase, IDisposable
{
    private const string NoValue = "-";

    private readonly ISimulationEngine _engine;
    private readonly IRestaurantBoardReader _board;
    private readonly ISettingsValidator _validator;
    private readonly IUiDispatcher _dispatcher;

    private string _arrivalIntervalText = "1500";
    private string _cookingIntervalText = "2000";
    private string _warningMessage = string.Empty;
    private bool _isRunning;
    private long _appliedVersion = -1;

    private int _customersInLine;
    private string _orderBeingTaken = NoValue;
    private string _orderBeingCooked = NoValue;
    private int _kitchenQueueCount;
    private string _kitchenQueueText = string.Empty;
    private string _readyForPickup = NoValue;
    private int _servingLineCount;

    public MainViewModel(
        ISimulationEngine engine,
        IRestaurantBoardReader board,
        ISettingsValidator validator,
        IUiDispatcher dispatcher)
    {
        _engine = engine;
        _board = board;
        _validator = validator;
        _dispatcher = dispatcher;

        StartCommand = new RelayCommand(Start, () => !IsRunning);
        StopCommand = new AsyncRelayCommand(StopAsync, () => IsRunning);

        _board.Changed += OnBoardChanged;
        _engine.Faulted += OnEngineFaulted;

        Apply(_board.Current);
    }

    // ----- input area -----
    public string ArrivalIntervalText
    {
        get => _arrivalIntervalText;
        set => SetProperty(ref _arrivalIntervalText, value);
    }

    public string CookingIntervalText
    {
        get => _cookingIntervalText;
        set => SetProperty(ref _cookingIntervalText, value);
    }

    public string WarningMessage
    {
        get => _warningMessage;
        private set
        {
            if (SetProperty(ref _warningMessage, value))
            {
                OnPropertyChanged(nameof(HasWarning));
            }
        }
    }

    public bool HasWarning => !string.IsNullOrEmpty(WarningMessage);

    public bool IsRunning
    {
        get => _isRunning;
        private set
        {
            if (!SetProperty(ref _isRunning, value))
            {
                return;
            }

            OnPropertyChanged(nameof(IsNotRunning));
            StartCommand.RaiseCanExecuteChanged();
            StopCommand.RaiseCanExecuteChanged();
        }
    }

    public bool IsNotRunning => !IsRunning;

    public RelayCommand StartCommand { get; }

    public AsyncRelayCommand StopCommand { get; }

    // ----- 1. order line -----
    public int CustomersInLine
    {
        get => _customersInLine;
        private set => SetProperty(ref _customersInLine, value);
    }

    // ----- 2. cash desk -----
    public string OrderBeingTaken
    {
        get => _orderBeingTaken;
        private set => SetProperty(ref _orderBeingTaken, value);
    }

    // ----- 3. kitchen -----
    public string OrderBeingCooked
    {
        get => _orderBeingCooked;
        private set => SetProperty(ref _orderBeingCooked, value);
    }

    public int KitchenQueueCount
    {
        get => _kitchenQueueCount;
        private set => SetProperty(ref _kitchenQueueCount, value);
    }

    public string KitchenQueueText
    {
        get => _kitchenQueueText;
        private set => SetProperty(ref _kitchenQueueText, value);
    }

    // ----- 4. pickup -----
    public string ReadyForPickup
    {
        get => _readyForPickup;
        private set => SetProperty(ref _readyForPickup, value);
    }

    public int ServingLineCount
    {
        get => _servingLineCount;
        private set => SetProperty(ref _servingLineCount, value);
    }

    // ----- graphical map -----
    public ObservableCollection<ChipViewModel> OrderLineChips { get; } = new();

    public ObservableCollection<ChipViewModel> CashChips { get; } = new();

    public ObservableCollection<ChipViewModel> KitchenChips { get; } = new();

    public ObservableCollection<ChipViewModel> PickupChips { get; } = new();

    public void Dispose()
    {
        _board.Changed -= OnBoardChanged;
        _engine.Faulted -= OnEngineFaulted;
    }

    private void Start()
    {
        var validation = _validator.Validate(ArrivalIntervalText, CookingIntervalText);

        if (!validation.IsValid)
        {
            WarningMessage = string.Join(Environment.NewLine, validation.Errors);
            return;
        }

        WarningMessage = string.Empty;
        _engine.Start(validation.Settings);
        IsRunning = true;
    }

    private async Task StopAsync()
    {
        await _engine.StopAsync();
        IsRunning = false;
    }

    private void OnBoardChanged(object? sender, RestaurantSnapshot snapshot) =>
        _dispatcher.Post(() => Apply(snapshot));

    private void OnEngineFaulted(object? sender, Exception exception) =>
        _dispatcher.Post(() => _ = HandleFaultAsync(exception));

    private async Task HandleFaultAsync(Exception exception)
    {
        WarningMessage = $"Симуляция остановлена из-за ошибки: {exception.Message}";
        await _engine.StopAsync();
        IsRunning = false;
    }

    private void Apply(RestaurantSnapshot snapshot)
    {
        if (snapshot.Version <= _appliedVersion)
        {
            return; // stale notification that overtook a newer one
        }

        _appliedVersion = snapshot.Version;

        CustomersInLine = snapshot.OrderLine.Count;
        OrderBeingTaken = Format(snapshot.OrderBeingTaken);
        OrderBeingCooked = Format(snapshot.OrderBeingCooked);
        KitchenQueueCount = snapshot.KitchenQueue.Count;
        KitchenQueueText = string.Join(", ", snapshot.KitchenQueue);
        ReadyForPickup = Format(snapshot.ReadyForPickup);
        ServingLineCount = snapshot.ServingLine.Count;

        ChipCollectionSynchronizer.Sync(
            OrderLineChips,
            snapshot.OrderLine.Select(id => (id, false)).ToList());

        ChipCollectionSynchronizer.Sync(
            CashChips,
            snapshot.OrderBeingTaken is { } taking ? new List<(int, bool)> { (taking, true) } : new List<(int, bool)>());

        var kitchen = new List<(int, bool)>();
        if (snapshot.OrderBeingCooked is { } cooking)
        {
            kitchen.Add((cooking, true));
        }

        kitchen.AddRange(snapshot.KitchenQueue.Select(number => (number, false)));
        ChipCollectionSynchronizer.Sync(KitchenChips, kitchen);

        ChipCollectionSynchronizer.Sync(
            PickupChips,
            snapshot.ServingLine.Select(number => (number, snapshot.ReadyOnCounter.Contains(number))).ToList());
    }

    private static string Format(int? number) => number?.ToString() ?? NoValue;
}
