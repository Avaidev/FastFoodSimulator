using FastFoodSimulator.Core.Domain;

namespace FastFoodSimulator.Core.Abstractions;

/// <summary>The line in front of the pickup counter where customers wait for their order number.</summary>
public interface IServingLine
{
    /// <summary>The customer (who already holds an order) moves to the end of the serving line.</summary>
    void Join(Customer customer);
}
