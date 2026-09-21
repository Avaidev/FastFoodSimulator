using System.Diagnostics;
using FastFoodSimulator.Core.Abstractions;
using FastFoodSimulator.Core.Board;
using FastFoodSimulator.Core.Configuration;
using FastFoodSimulator.Core.Simulation;

// Decorator: records every board call with a timestamp, then forwards to the real board.
sealed class Recorder(IRestaurantBoard inner, Stopwatch sw) : IRestaurantBoardWriter
{
    public readonly List<(long ms, string ev, int a)> Log = new();
    void L(string e, int a) { lock (Log) Log.Add((sw.ElapsedMilliseconds, e, a)); }
    public void Reset() { inner.Reset(); }
    public void CustomerJoinedOrderLine(int c) { L("arrive", c); inner.CustomerJoinedOrderLine(c); }
    public void OrderTakingStarted(int c, int o) { L("take", o); inner.OrderTakingStarted(c, o); }
    public void OrderPlaced(int o) { L("placed", o); inner.OrderPlaced(o); }
    public void CookingStarted(int o) { L("cookStart", o); inner.CookingStarted(o); }
    public void CookingFinished(int o) { L("cookEnd", o); inner.CookingFinished(o); }
    public void CustomerJoinedServingLine(int o) { L("line", o); inner.CustomerJoinedServingLine(o); }
    public void OrderCalledOut(int o) { L("call", o); inner.OrderCalledOut(o); }
    public void OrderPickedUp(int o) { L("pick", o); inner.OrderPickedUp(o); }
}

static class P
{
    static int fails;
    static void Check(bool ok, string msg) { Console.WriteLine((ok ? "  PASS  " : "  FAIL  ") + msg); if (!ok) fails++; }

    static async Task<int> Main()
    {
        await Scenario("app defaults from the screenshot", arrival: 1500, cook: 2000, runMs: 16000, expectKitchenBacklog: true);
        await Scenario("fast arrivals, slow kitchen", arrival: 300, cook: 1000, runMs: 9000, expectKitchenBacklog: true);
        await Scenario("kitchen slower than arrivals", arrival: 300, cook: 700, runMs: 7000, expectKitchenBacklog: true);
        await Scenario("kitchen faster than arrivals", arrival: 900, cook: 300, runMs: 7000);
        await Scenario("very fast kitchen, server is the bottleneck", arrival: 400, cook: 100, runMs: 6000);
        await StopRestart();
        Console.WriteLine(fails == 0 ? "\nALL CHECKS PASSED" : $"\n{fails} CHECK(S) FAILED");
        return fails;
    }

    static async Task Scenario(string name, int arrival, int cook, int runMs, bool expectKitchenBacklog = false)
    {
        Console.WriteLine($"\n=== {name}: arrival={arrival}ms cook={cook}ms ===");
        var sw = Stopwatch.StartNew();
        var board = new RestaurantBoard();
        var rec = new Recorder(board, sw);

        // Invariants checked on EVERY snapshot the UI would receive
        var snapshots = new List<RestaurantSnapshot>();
        board.Changed += (_, s) => { lock (snapshots) snapshots.Add(s); };

        var engine = new SimulationEngine(new SimulationSessionFactory(rec), board);
        var settings = new SimulationSettings(TimeSpan.FromMilliseconds(arrival), TimeSpan.FromMilliseconds(cook));
        engine.Start(settings);
        await Task.Delay(runMs);
        await engine.StopAsync();

        List<(long ms, string ev, int a)> log; lock (rec.Log) log = rec.Log.ToList();
        List<int> Seq(string e) => log.Where(x => x.ev == e).Select(x => x.a).ToList();
        List<long> Times(string e) => log.Where(x => x.ev == e).Select(x => x.ms).ToList();
        bool Increasing(List<int> l) => l.Zip(l.Skip(1), (p, n) => n > p).All(b => b);
        bool Consecutive(List<int> l) => l.Select((v, i) => v == i + 1).All(b => b);

        var arrive = Seq("arrive"); var take = Seq("take"); var cs = Seq("cookStart");
        var ce = Seq("cookEnd"); var call = Seq("call"); var pick = Seq("pick");
        Console.WriteLine($"  arrived={arrive.Count} taken={take.Count} cooked={ce.Count} called={call.Count} picked={pick.Count}");

        Check(Consecutive(arrive), "customers numbered 1,2,3... in arrival order");
        Check(Consecutive(take), "order numbers unique and consecutive (1,2,3...) – taken in customer order");
        Check(take.SequenceEqual(arrive.Take(take.Count)), "order taker serves customers FIFO (order N belongs to customer N)");
        Check(cs.SequenceEqual(take.Take(cs.Count)), "cook starts orders in exactly the order they were placed (FIFO kitchen)");
        Check(ce.SequenceEqual(cs.Take(ce.Count)), "cook finishes orders in the order started (one at a time)");
        Check(call.SequenceEqual(ce.Take(call.Count)), "server calls numbers in the order the cook finished them");
        Check(pick.OrderBy(x => x).SequenceEqual(pick.OrderBy(x => x)) && pick.Distinct().Count() == pick.Count, "no order picked up twice");
        Check(pick.All(p => call.Contains(p)), "customer only picks up after his number was called");

        // never two orders cooked at once
        bool overlap = false; int active = 0;
        foreach (var e in log.Where(x => x.ev is "cookStart" or "cookEnd").OrderBy(x => x.ms)) { active += e.ev == "cookStart" ? 1 : -1; if (active > 1) overlap = true; }
        Check(!overlap, "only one order is cooked at a time");

        // a call must never precede its cook end; pick must never precede call
        bool causal = true;
        foreach (var o in call) { var tEnd = log.First(x => x.ev == "cookEnd" && x.a == o).ms; var tCall = log.First(x => x.ev == "call" && x.a == o).ms; if (tCall < tEnd) causal = false; }
        foreach (var o in pick) { var tCall = log.First(x => x.ev == "call" && x.a == o).ms; var tPick = log.First(x => x.ev == "pick" && x.a == o).ms; if (tPick < tCall) causal = false; }
        Check(causal, "causality: cook end ≤ call ≤ pickup for every order");

        // constant rates
        var at = Times("arrive"); var gaps = at.Zip(at.Skip(1), (p, n) => n - p).ToList();
        Check(gaps.Count == 0 || gaps.All(g => Math.Abs(g - arrival) <= 60), $"arrivals at constant rate (gaps {gaps.Min()}..{gaps.Max()} ms, expected {arrival})");
        var cst = Times("cookStart"); var cet = Times("cookEnd");
        var dur = cst.Zip(cet, (s, e) => e - s).ToList();
        Check(dur.Count == 0 || dur.All(d => d >= cook - 5 && d <= cook + 60), $"each order cooks for the fixed interval (durations {dur.Min()}..{dur.Max()} ms, expected {cook})");

        // snapshot invariants (what the UI displays)
        bool kOrdered = true, noDup = true, cookNotQueued = true, ready = true, ver = true; long lastV = -1;
        lock (snapshots)
            foreach (var s in snapshots)
            {
                if (!Increasing(s.KitchenQueue.ToList())) kOrdered = false;
                if (s.KitchenQueue.Distinct().Count() != s.KitchenQueue.Count) noDup = false;
                if (s.OrderBeingCooked is { } c && s.KitchenQueue.Contains(c)) cookNotQueued = false;
                if (s.OrderBeingCooked is { } c2 && s.KitchenQueue.Any(q => q < c2)) kOrdered = false; // an older order waits while a newer one is cooked
                if (!s.ReadyOnCounter.All(r => s.ServingLine.Contains(r))) ready = false;
                if (s.Version <= lastV) ver = false; lastV = s.Version;
            }
        Check(kOrdered, "every UI snapshot: kitchen waiting list is in ascending order and older than nothing being cooked");
        Check(noDup, "every UI snapshot: no order twice in the waiting list");
        Check(cookNotQueued, "every UI snapshot: the order being cooked is not also in the waiting list");
        Check(ready, "every UI snapshot: an order on the counter always belongs to a customer in the serving line");
        Check(ver, "snapshot versions strictly increasing");

        int maxK; lock (snapshots) maxK = snapshots.Max(s => s.KitchenQueue.Count);
        Console.WriteLine($"  (max kitchen backlog observed: {maxK})");
        // Kitchen conservation: every placed order is exactly one of: waiting, cooking, finished
        var last = board.Current;
        int placed = Seq("placed").Count;
        int accounted = last.KitchenQueue.Count + (last.OrderBeingCooked.HasValue ? 1 : 0) + ce.Count;
        Check(placed == accounted, $"kitchen conservation: placed={placed} = waiting {last.KitchenQueue.Count} + cooking {(last.OrderBeingCooked.HasValue ? 1 : 0)} + finished {ce.Count}");
        Check(last.KitchenQueue.SequenceEqual(last.KitchenQueue.OrderBy(x => x)), "final waiting list is in FIFO order: " + string.Join(", ", last.KitchenQueue));
        if (expectKitchenBacklog)
        {
            Check(maxK >= 3, $"kitchen queue really builds up when cook is slower (max backlog {maxK})");
            int maxLine; lock (snapshots) maxLine = snapshots.Max(s2 => s2.OrderLine.Count);
            Check(maxLine <= 1, $"cash desk is not the bottleneck: order line stays short (max {maxLine})");
        }
        engine.Dispose();
    }

    static async Task StopRestart()
    {
        Console.WriteLine("\n=== stop / restart ===");
        var board = new RestaurantBoard();
        var engine = new SimulationEngine(new SimulationSessionFactory(board), board);
        var s = new SimulationSettings(TimeSpan.FromMilliseconds(200), TimeSpan.FromMilliseconds(500));
        engine.Start(s); await Task.Delay(2500);
        var sw = Stopwatch.StartNew(); await engine.StopAsync();
        Check(sw.ElapsedMilliseconds < 500, $"Stop returns promptly ({sw.ElapsedMilliseconds} ms)");
        Check(!engine.IsRunning, "engine reports not running after stop");
        engine.Start(s); await Task.Delay(300);
        var snap = board.Current;
        Check(snap.OrderLine.Count + snap.KitchenQueue.Count + snap.ServingLine.Count <= 3, "restart begins with a clean board (numbers restart from 1)");
        Check(snap.OrderBeingTaken is null or 1 || snap.KitchenQueue.Count == 0, "order numbering restarts");
        await engine.StopAsync();
        engine.Dispose();
    }
}
