namespace FastFoodSimulator.ViewModels;

public sealed class WorkerStatusViewModel : ViewModelBase
{
    private string _value = "—";

    public WorkerStatusViewModel(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public string Value
    {
        get => _value;
        set => SetProperty(ref _value, value);
    }
}
