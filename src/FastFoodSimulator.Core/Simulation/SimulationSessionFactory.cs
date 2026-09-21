using FastFoodSimulator.Core.Abstractions;
using FastFoodSimulator.Core.Configuration;
using FastFoodSimulator.Core.Domain;
using FastFoodSimulator.Core.Simulation.Actors;
using FastFoodSimulator.Core.Simulation.Infrastructure;

namespace FastFoodSimulator.Core.Simulation;

/// <summary>Composition of one restaurant: which queue connects which actors.</summary>
public sealed class SimulationSessionFactory(IRestaurantBoardWriter board) : ISimulationSessionFactory
{
    public ISimulationSession Create(SimulationSettings settings)
    {
        // customers -> [order line] -> order taker -> [kitchen carousel] -> cook -> [service queue] -> server -> customer
        var orderLine = new AsyncQueue<Customer>();
        var kitchenQueue = new AsyncQueue<Order>();
        var serviceQueue = new AsyncQueue<Order>();

        var servingLine = new ServingLine(settings, board);

        ISimulationActor[] actors =
        [
            new CustomerGenerator(settings, orderLine, board),
            new OrderTaker(settings, orderLine, kitchenQueue, servingLine, new SequentialOrderNumberGenerator(), board),
            new Cook(settings, kitchenQueue, serviceQueue, board),
            new Server(settings, serviceQueue, board),
            servingLine,
        ];

        return new SimulationSession(actors);
    }
}
