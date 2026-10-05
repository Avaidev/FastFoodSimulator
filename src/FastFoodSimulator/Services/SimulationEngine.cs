using System.Collections.Concurrent;
using FastFoodSimulator.Collections;
using FastFoodSimulator.Models;

namespace FastFoodSimulator.Services;

public sealed class SimulationEngine : ISimulationEngine
{
    private readonly object _snapshotGate = new();
    private readonly ConcurrentDictionary<int, byte> _pickupCounter = new();
    private readonly ConcurrentDictionary<int, Task> _customerTasks = new();

    private WorkQueue<Customer> _orderLine = new();
    private WorkQueue<Order> _carousel = new();
    private WorkQueue<Order> _serverIntake = new();
    private ServingLine _servingLine = new();
    private int[] _takerOrders = [];
    private int[] _cookOrders = [];

    private CancellationTokenSource? _cts;
    private Task _workers = Task.CompletedTask;
    private long _version;
    private int _customerSequence;
    private int _orderSequence;
    private volatile bool _isRunning;

    public event Action<SimulationSnapshot>? SnapshotPublished;

    public event Action<LogEntry>? LogWritten;

    public bool IsRunning => _isRunning;

    public void Start(SimulationSettings settings)
    {
        if (_isRunning)
        {
            return;
        }

        ResetState(settings);
        _cts = new CancellationTokenSource();
        var token = _cts.Token;
        _isRunning = true;

        var workers = new List<Task>
        {
            RunWorker(() => RunCustomerArrivalsAsync(settings, token))
        };

        for (var i = 1; i <= settings.OrderTakerCount; i++)
        {
            var index = i;
            workers.Add(RunWorker(() => RunOrderTakerAsync(index, settings, token)));
        }

        for (var i = 1; i <= settings.CookCount; i++)
        {
            var index = i;
            workers.Add(RunWorker(() => RunCookAsync(index, settings, token)));
        }

        workers.Add(RunWorker(() => RunServerAsync(token)));

        _workers = Task.WhenAll(workers);

        Log(0, "Simulation started");
        Publish();
    }

    public async Task StopAsync()
    {
        var cts = _cts;
        if (cts is null)
        {
            return;
        }

        cts.Cancel();
        await _workers;
        await Task.WhenAll(_customerTasks.Values);

        cts.Dispose();
        _cts = null;
        _isRunning = false;

        Log(0, "Simulation stopped");
        Publish();
    }

    public void Dispose()
    {
        try
        {
            _cts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private static Task RunWorker(Func<Task> work) => Task.Run(async () =>
    {
        try
        {
            await work();
        }
        catch (OperationCanceledException)
        {
        }
    });

    private void ResetState(SimulationSettings settings)
    {
        _orderLine = new WorkQueue<Customer>();
        _carousel = new WorkQueue<Order>();
        _serverIntake = new WorkQueue<Order>();
        _servingLine = new ServingLine();
        _pickupCounter.Clear();
        _customerTasks.Clear();
        _takerOrders = new int[settings.OrderTakerCount];
        _cookOrders = new int[settings.CookCount];
        _customerSequence = 0;
        _orderSequence = 0;
    }

    private async Task RunCustomerArrivalsAsync(SimulationSettings settings, CancellationToken ct)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(settings.CustomerArrivalIntervalMs));

        while (await timer.WaitForNextTickAsync(ct))
        {
            var customer = new Customer(Interlocked.Increment(ref _customerSequence));
            _orderLine.Enqueue(customer);

            var task = RunWorker(() => RunCustomerAsync(customer, settings, ct));
            _customerTasks[customer.Id] = task;
            _ = task.ContinueWith(_ => _customerTasks.TryRemove(customer.Id, out Task? _), TaskScheduler.Default);

            Log(0, $"Customer C{customer.Id} arrived and joined the order line");
            Publish();
        }
    }

    private async Task RunCustomerAsync(Customer customer, SimulationSettings settings, CancellationToken ct)
    {
        var order = await customer.Receipt.Task.WaitAsync(ct);

        _servingLine.Enqueue(order);
        Log(2, $"Customer C{customer.Id} paid and moved to the end of the serving line (order #{order.Id})");
        Publish();
        customer.InServingLine.TrySetResult();

        await order.CustomerPromise.Task.WaitAsync(ct);
        await Task.Delay(settings.PickupDurationMs, ct);

        _pickupCounter.TryRemove(order.Id, out _);
        _servingLine.Remove(order);
        order.State = OrderState.PickedUp;
        Log(9, $"Customer C{customer.Id} picked up order #{order.Id} and went to the dining area");
        Publish();
    }

    private async Task RunOrderTakerAsync(int index, SimulationSettings settings, CancellationToken ct)
    {
        while (true)
        {
            var customer = await _orderLine.DequeueAsync(ct);
            var order = new Order(Interlocked.Increment(ref _orderSequence), customer.Id);

            _takerOrders[index - 1] = order.Id;
            Publish();

            await Task.Delay(settings.OrderTakingIntervalMs, ct);

            Log(1, $"Order Taker {index} created ticket #{order.Id} for customer C{customer.Id} and gave the receipt");
            customer.Receipt.TrySetResult(order);

            await customer.InServingLine.Task.WaitAsync(ct);

            order.State = OrderState.OnCarousel;
            _carousel.Enqueue(order);
            Log(3, $"Order Taker {index} placed ticket #{order.Id} on the kitchen carousel");

            _takerOrders[index - 1] = 0;
            Publish();
        }
    }

    private async Task RunCookAsync(int index, SimulationSettings settings, CancellationToken ct)
    {
        while (true)
        {
            var order = await _carousel.DequeueAsync(ct);

            order.State = OrderState.Preparing;
            _serverIntake.Enqueue(order);
            _cookOrders[index - 1] = order.Id;
            Log(4, $"Cook {index} removed ticket #{order.Id} from the carousel");
            Log(5, $"Cook {index} is preparing order #{order.Id}");
            Publish();

            await Task.Delay(settings.OrderFulfillmentIntervalMs, ct);

            order.State = OrderState.Prepared;
            _cookOrders[index - 1] = 0;
            Log(6, $"Cook {index} finished order #{order.Id} and handed it with the ticket to the server");
            order.KitchenPromise.TrySetResult();
            Publish();
        }
    }

    private async Task RunServerAsync(CancellationToken ct)
    {
        while (true)
        {
            var order = await _serverIntake.DequeueAsync(ct);
            await order.KitchenPromise.Task.WaitAsync(ct);

            Log(7, $"Server took order #{order.Id} and its ticket from the cook");

            order.State = OrderState.ReadyForPickup;
            _pickupCounter[order.Id] = 0;
            Log(8, $"Server called out order #{order.Id} and placed it on the counter");
            Publish();

            order.CustomerPromise.TrySetResult();
        }
    }

    private void Log(int step, string message) =>
        LogWritten?.Invoke(new LogEntry(DateTime.Now, step, message));

    private void Publish()
    {
        var handler = SnapshotPublished;
        if (handler is null)
        {
            return;
        }

        handler(CreateSnapshot());
    }

    private SimulationSnapshot CreateSnapshot()
    {
        lock (_snapshotGate)
        {
            return new SimulationSnapshot(
                ++_version,
                _isRunning,
                _orderLine.ToArray().Select(c => c.Id).ToArray(),
                (int[])_takerOrders.Clone(),
                _carousel.ToArray().Select(o => o.Id).ToArray(),
                (int[])_cookOrders.Clone(),
                _servingLine.ToArray().Select(o => o.Id).ToArray(),
                _pickupCounter.Keys.OrderBy(id => id).ToArray());
        }
    }
}
