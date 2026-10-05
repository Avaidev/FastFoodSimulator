namespace FastFoodSimulator.Models;

public sealed record LogEntry(DateTime Timestamp, int Step, string Message)
{
    public override string ToString()
    {
        var label = Step > 0 ? $"Step {Step}" : "System";
        return $"[{Timestamp:HH:mm:ss.fff}] {label,-6} | {Message}";
    }
}
