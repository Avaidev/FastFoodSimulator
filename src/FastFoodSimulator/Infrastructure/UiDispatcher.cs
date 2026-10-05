using System.Windows.Threading;

namespace FastFoodSimulator.Infrastructure;

public sealed class UiDispatcher
{
    private readonly Dispatcher _dispatcher;

    public UiDispatcher(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public void Invoke(Action action)
    {
        if (_dispatcher.HasShutdownStarted)
        {
            return;
        }

        try
        {
            _dispatcher.Invoke(action);
        }
        catch (TaskCanceledException)
        {
        }
    }
}
