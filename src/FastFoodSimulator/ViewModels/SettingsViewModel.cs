using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using FastFoodSimulator.Models;

namespace FastFoodSimulator.ViewModels;

public sealed class SettingsViewModel : ViewModelBase, IDataErrorInfo
{
    private string _customerArrivalInterval = "1000";
    private string _orderTakingInterval = "2500";
    private string _orderFulfillmentInterval = "3000";
    private string _orderTakerCount = "1";
    private string _cookCount = "2";

    public string CustomerArrivalInterval
    {
        get => _customerArrivalInterval;
        set => Update(ref _customerArrivalInterval, value);
    }

    public string OrderTakingInterval
    {
        get => _orderTakingInterval;
        set => Update(ref _orderTakingInterval, value);
    }

    public string OrderFulfillmentInterval
    {
        get => _orderFulfillmentInterval;
        set => Update(ref _orderFulfillmentInterval, value);
    }

    public string OrderTakerCount
    {
        get => _orderTakerCount;
        set => Update(ref _orderTakerCount, value);
    }

    public string CookCount
    {
        get => _cookCount;
        set => Update(ref _cookCount, value);
    }

    public string Error => string.Empty;

    public bool IsValid => WarningMessage.Length == 0;

    public string WarningMessage => string.Join(
        Environment.NewLine,
        new[]
        {
            this[nameof(CustomerArrivalInterval)],
            this[nameof(OrderTakingInterval)],
            this[nameof(OrderFulfillmentInterval)],
            this[nameof(OrderTakerCount)],
            this[nameof(CookCount)]
        }.Where(message => message.Length > 0));

    public string this[string columnName] => columnName switch
    {
        nameof(CustomerArrivalInterval) => ValidateInterval(CustomerArrivalInterval, "Customer Arrival Interval"),
        nameof(OrderTakingInterval) => ValidateInterval(OrderTakingInterval, "Order Taking Interval"),
        nameof(OrderFulfillmentInterval) => ValidateInterval(OrderFulfillmentInterval, "Order Fulfillment Interval"),
        nameof(OrderTakerCount) => ValidateCount(OrderTakerCount, "Number of Order Takers"),
        nameof(CookCount) => ValidateCount(CookCount, "Number of Cooks"),
        _ => string.Empty
    };

    public SimulationSettings ToSettings()
    {
        if (!IsValid)
        {
            throw new InvalidOperationException("Settings contain invalid values.");
        }

        return new SimulationSettings(
            Parse(CustomerArrivalInterval),
            Parse(OrderTakingInterval),
            Parse(OrderFulfillmentInterval),
            Parse(OrderTakerCount),
            Parse(CookCount));
    }

    private void Update(ref string field, string value, [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
        {
            return;
        }

        OnPropertyChanged(nameof(WarningMessage));
        OnPropertyChanged(nameof(IsValid));
        CommandManager.InvalidateRequerySuggested();
    }

    private static string ValidateInterval(string text, string label) =>
        IsInRange(text, SettingsLimits.MinIntervalMs, SettingsLimits.MaxIntervalMs)
            ? string.Empty
            : $"{label} must be a whole number between {SettingsLimits.MinIntervalMs} and {SettingsLimits.MaxIntervalMs} ms.";

    private static string ValidateCount(string text, string label) =>
        IsInRange(text, SettingsLimits.MinWorkers, SettingsLimits.MaxWorkers)
            ? string.Empty
            : $"{label} must be a whole number between {SettingsLimits.MinWorkers} and {SettingsLimits.MaxWorkers}.";

    private static bool IsInRange(string? text, int min, int max) =>
        int.TryParse(text?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var value)
        && value >= min
        && value <= max;

    private static int Parse(string text) => int.Parse(text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture);
}
