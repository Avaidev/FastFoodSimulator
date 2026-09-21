using System.Windows.Threading;

namespace FastFoodSimulator.Wpf.Infrastructure;

public sealed class WpfUiDispatcher(Dispatcher dispatcher) : IUiDispatcher
{
    public void Post(Action action) => dispatcher.BeginInvoke(DispatcherPriority.Normal, action);
}
