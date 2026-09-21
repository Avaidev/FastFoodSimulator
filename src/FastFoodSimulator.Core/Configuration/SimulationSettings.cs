namespace FastFoodSimulator.Core.Configuration;

/// <summary>
/// Parameters of one simulation run. The first two intervals are entered by the user.
/// The remaining durations only make the individual steps visible; they are capped relative to the user's
/// intervals so that the cash desk and the server can never become the bottleneck instead of the kitchen.
/// </summary>
public sealed record SimulationSettings(TimeSpan CustomerArrivalInterval, TimeSpan CookingInterval)
{
    private static readonly TimeSpan MaxOrderTaking = TimeSpan.FromMilliseconds(800);
    private static readonly TimeSpan MaxAnnouncement = TimeSpan.FromMilliseconds(600);
    private static readonly TimeSpan MaxPickup = TimeSpan.FromMilliseconds(500);

    /// <summary>How long the Order Taker needs for one order: at most 800 ms and always faster than customers arrive.</summary>
    public TimeSpan OrderTakingDuration { get; init; } = Min(MaxOrderTaking, CustomerArrivalInterval / 2);

    /// <summary>How long the Server needs to call out an order: at most 600 ms and always faster than the Cook.</summary>
    public TimeSpan AnnouncementDuration { get; init; } = Min(MaxAnnouncement, CookingInterval / 2);

    /// <summary>How long a Customer needs to take the order from the counter (customers do this in parallel).</summary>
    public TimeSpan PickupDuration { get; init; } = MaxPickup;

    private static TimeSpan Min(TimeSpan a, TimeSpan b) => a <= b ? a : b;
}
