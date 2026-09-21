namespace FastFoodSimulator.Core.Abstractions;

public interface IOrderNumberGenerator
{
    /// <summary>Returns the next unique order number (thread-safe).</summary>
    int Next();
}
