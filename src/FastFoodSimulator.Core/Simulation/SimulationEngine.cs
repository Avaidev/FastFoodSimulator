using FastFoodSimulator.Core.Abstractions;
using FastFoodSimulator.Core.Configuration;

namespace FastFoodSimulator.Core.Simulation;

/// <summary>Starts and stops simulation runs. Each run gets a fresh session and a clean board.</summary>
public sealed class SimulationEngine(ISimulationSessionFactory sessionFactory, IRestaurantBoardWriter board)
    : ISimulationEngine
{
    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private Task? _runTask;

    public event EventHandler<Exception>? Faulted;

    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _runTask is not null;
            }
        }
    }

    public void Start(SimulationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        lock (_gate)
        {
            if (_runTask is not null)
            {
                throw new InvalidOperationException("The simulation is already running.");
            }

            board.Reset();

            var cts = new CancellationTokenSource();
            var session = sessionFactory.Create(settings);
            var token = cts.Token;

            _cts = cts;
            // Task.Run: actors must not capture the UI synchronization context.
            _runTask = Task.Run(() => RunSessionAsync(session, cts, token));
        }
    }

    public async Task StopAsync()
    {
        CancellationTokenSource? cts;
        Task? runTask;

        lock (_gate)
        {
            cts = _cts;
            runTask = _runTask;
            _cts = null;
            _runTask = null;
        }

        if (cts is null || runTask is null)
        {
            return;
        }

        try
        {
            cts.Cancel();
            await runTask.ConfigureAwait(false);
        }
        finally
        {
            cts.Dispose();
        }
    }

    public void Dispose()
    {
        CancellationTokenSource? cts;

        lock (_gate)
        {
            cts = _cts;
            _cts = null;
            _runTask = null;
        }

        cts?.Cancel();
    }

    private async Task RunSessionAsync(ISimulationSession session, CancellationTokenSource cts, CancellationToken token)
    {
        try
        {
            await session.RunAsync(token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Normal stop.
        }
        catch (Exception ex)
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
            }

            Faulted?.Invoke(this, ex);
        }
    }
}
