using FastFoodSimulator.Infrastructure;
using FastFoodSimulator.Models;
using FastFoodSimulator.Services;

namespace FastFoodSimulator.ViewModels;

public sealed class SimulationDetailViewModel : ViewModelBase, IDisposable
{
    private const double Spacing = 38;
    private const int Columns = 7;
    private const int MaxUpperTokens = 28;
    private const int MaxLowerTokens = 14;

    private const double OrderLineX = 30;
    private const double KitchenX = 350;
    private const double PickupX = 670;
    private const double UpperY = 95;
    private const double LowerY = 300;

    private const string CustomerFill = "#93C5FD";
    private const string TakerFill = "#FDBA74";
    private const string TicketFill = "#FDE68A";
    private const string CookFill = "#FCA5A5";
    private const string ServingFill = "#86EFAC";
    private const string CounterFill = "#6EE7B7";
    private const string OverflowFill = "#D1D5DB";

    private readonly ISimulationEngine _engine;
    private readonly UiDispatcher _ui;
    private long _lastVersion;
    private IReadOnlyList<VisualToken> _tokens = Array.Empty<VisualToken>();

    public SimulationDetailViewModel(ISimulationEngine engine, UiDispatcher ui)
    {
        _engine = engine;
        _ui = ui;
        _engine.SnapshotPublished += OnSnapshotPublished;
    }

    public IReadOnlyList<VisualToken> Tokens
    {
        get => _tokens;
        private set => SetProperty(ref _tokens, value);
    }

    public void Dispose() => _engine.SnapshotPublished -= OnSnapshotPublished;

    private void OnSnapshotPublished(SimulationSnapshot snapshot) => _ui.Invoke(() => Apply(snapshot));

    private void Apply(SimulationSnapshot snapshot)
    {
        if (snapshot.Version <= _lastVersion)
        {
            return;
        }

        _lastVersion = snapshot.Version;

        var tokens = new List<VisualToken>();

        tokens.AddRange(Place(snapshot.OrderLine.Select(id => $"C{id}"), CustomerFill, Circle, OrderLineX, UpperY, MaxUpperTokens));
        tokens.AddRange(Place(Busy(snapshot.OrderTakers), TakerFill, Square, OrderLineX, LowerY, MaxLowerTokens));

        tokens.AddRange(Place(snapshot.Carousel.Select(id => $"#{id}"), TicketFill, Square, KitchenX, UpperY, MaxUpperTokens));
        tokens.AddRange(Place(Busy(snapshot.Cooks), CookFill, Square, KitchenX, LowerY, MaxLowerTokens));

        tokens.AddRange(Place(snapshot.ServingLine.Select(id => $"#{id}"), ServingFill, Circle, PickupX, UpperY, MaxUpperTokens));
        tokens.AddRange(Place(snapshot.PickupCounter.Select(id => $"#{id}"), CounterFill, Square, PickupX, LowerY, MaxLowerTokens));

        Tokens = tokens;
    }

    private static IEnumerable<string> Busy(IReadOnlyList<int> orders) =>
        orders.Where(id => id > 0).Select(id => $"#{id}");

    private static VisualToken Circle(double x, double y, string label, string fill) => new CircleToken(x, y, label, fill);

    private static VisualToken Square(double x, double y, string label, string fill) => new SquareToken(x, y, label, fill);

    private static IEnumerable<VisualToken> Place(
        IEnumerable<string> labels,
        string fill,
        Func<double, double, string, string, VisualToken> factory,
        double originX,
        double originY,
        int maxVisible)
    {
        var items = labels.ToList();
        var overflow = items.Count > maxVisible;
        var visible = overflow ? maxVisible - 1 : items.Count;

        for (var i = 0; i < visible; i++)
        {
            yield return factory(Slot(originX, i), Row(originY, i), items[i], fill);
        }

        if (overflow)
        {
            yield return new CircleToken(
                Slot(originX, visible),
                Row(originY, visible),
                $"+{items.Count - visible}",
                OverflowFill);
        }
    }

    private static double Slot(double originX, int index) => originX + (index % Columns) * Spacing;

    private static double Row(double originY, int index) => originY + (index / Columns) * Spacing;
}
