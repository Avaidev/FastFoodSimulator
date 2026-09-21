namespace FastFoodSimulator.Wpf.Infrastructure;

/// <summary>Abstraction over "run this on the UI thread" so view models stay testable.</summary>
public interface IUiDispatcher
{
    void Post(Action action);
}
