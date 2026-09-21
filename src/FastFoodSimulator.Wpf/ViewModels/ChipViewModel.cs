using FastFoodSimulator.Wpf.Infrastructure;

namespace FastFoodSimulator.Wpf.ViewModels;

/// <summary>A small badge (customer / order number) drawn on the restaurant map.</summary>
public sealed class ChipViewModel(int key) : ViewModelBase
{
    private bool _isActive;

    public int Key { get; } = key;

    public string Text => Key.ToString();

    /// <summary>Active chip = the one currently being served / cooked / called out.</summary>
    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }
}
