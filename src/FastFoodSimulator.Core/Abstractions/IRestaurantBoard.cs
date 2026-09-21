using FastFoodSimulator.Core.Board;

namespace FastFoodSimulator.Core.Abstractions;

/// <summary>Read side of the board: used by the presentation layer.</summary>
public interface IRestaurantBoardReader
{
    RestaurantSnapshot Current { get; }

    /// <summary>Raised on the thread of the actor that changed the state. Handlers must not block.</summary>
    event EventHandler<RestaurantSnapshot>? Changed;
}

/// <summary>Write side of the board: actors report what they are doing.</summary>
public interface IRestaurantBoardWriter
{
    void Reset();

    void CustomerJoinedOrderLine(int customerId);

    void OrderTakingStarted(int customerId, int orderNumber);

    void OrderPlaced(int orderNumber);

    void CookingStarted(int orderNumber);

    void CookingFinished(int orderNumber);

    void CustomerJoinedServingLine(int orderNumber);

    void OrderCalledOut(int orderNumber);

    void OrderPickedUp(int orderNumber);
}

public interface IRestaurantBoard : IRestaurantBoardReader, IRestaurantBoardWriter
{
}
