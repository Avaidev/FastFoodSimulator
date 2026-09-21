namespace FastFoodSimulator.Core.Board;

/// <summary>Immutable picture of the whole restaurant at one moment. Ready to be shown by any UI.</summary>
public sealed record RestaurantSnapshot(
    long Version,
    IReadOnlyList<int> OrderLine,
    int? OrderBeingTaken,
    int? OrderBeingCooked,
    IReadOnlyList<int> KitchenQueue,
    IReadOnlyList<int> ServingLine,
    IReadOnlyList<int> ReadyOnCounter)
{
    public static RestaurantSnapshot Empty { get; } =
        new(0, Array.Empty<int>(), null, null, Array.Empty<int>(), Array.Empty<int>(), Array.Empty<int>());

    /// <summary>The order number that was called out most recently and is still on the counter.</summary>
    public int? ReadyForPickup => ReadyOnCounter.Count > 0 ? ReadyOnCounter[ReadyOnCounter.Count - 1] : null;
}
