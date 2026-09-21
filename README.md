# Fast Food Simulator (WPF, .NET 9)

Simulation of a take-away restaurant: customers arrive at a constant rate, order at the cash desk,
wait in the serving line while the kitchen cooks, and pick the order up when the server calls its number.
No simulation library is used – only `Task`, `TaskCompletionSource`, `PeriodicTimer` and `Channel<T>`.

## Run

```
dotnet run --project src/FastFoodSimulator.Wpf
```
Requires Windows and the .NET 9 SDK (the WPF project targets `net9.0-windows`, the Core library `net9.0`).

## Who talks to whom

```
CustomerGenerator ─► [order line] ─► OrderTaker ─► [kitchen queue] ─► Cook ─► [service queue] ─► Server
 constant rate          FIFO         unique number      FIFO        fixed interval     FIFO      calls the number
                                     + receipt                       completes                   completes
                                        │                            PreparationPromise          PickupPromise
                                        ▼                                                            │
                                   ServingLine ◄─────────────────────────────────────────────────────┘
                     (every Customer awaits PickupPromise, then takes the order and leaves)
```

Order ticket = two promises (`Order.cs`):

| Promise | Completed by | Awaited by |
|---|---|---|
| `PreparationPromise` | Cook (order is ready) | Server (before calling the number) |
| `PickupPromise` | Server (number called out) | Customer (while in the serving line) |

## Architecture

```
src/
├─ FastFoodSimulator.Core            (net9.0, no UI dependencies)   [tests/ contains the verification program]
│  ├─ Domain/          Order, Customer
│  ├─ Configuration/   SimulationSettings
│  ├─ Abstractions/    ISimulationActor, IWorkQueue<T>, IServingLine, IOrderNumberGenerator,
│  │                   IRestaurantBoard(Reader/Writer), ISimulationEngine, ISimulationSession(Factory), ISettingsValidator
│  ├─ Board/           RestaurantBoard (thread-safe observable state), RestaurantSnapshot (immutable read model)
│  ├─ Simulation/
│  │  ├─ Actors/           CustomerGenerator, OrderTaker, Cook, Server, ServingLine (+ SimulationActorBase)
│  │  ├─ Infrastructure/   AsyncQueue<T> (Channel-based FIFO), SequentialOrderNumberGenerator
│  │  └─ SimulationSession, SimulationSessionFactory, SimulationEngine
│  └─ Validation/      SimulationSettingsValidator, SettingsValidationResult
└─ FastFoodSimulator.Wpf             (net9.0-windows, MVVM)
   ├─ Infrastructure/  ViewModelBase, RelayCommand, AsyncRelayCommand, IUiDispatcher, WpfUiDispatcher
   ├─ ViewModels/      MainViewModel, ChipViewModel, ChipCollectionSynchronizer
   ├─ Views/           MainWindow, StageColumn (reusable map column)
   ├─ Resources/       Colors.xaml, Styles.xaml
   └─ App.xaml(.cs)    composition root (manual dependency injection)
```

SOLID notes
* **S** – each actor does one job; validation, engine, state board and UI are separate classes.
* **O/L** – new actors are added by implementing `ISimulationActor` and registering them in `SimulationSessionFactory`.
* **I** – the board is split into a reader (UI) and a writer (actors) interface.
* **D** – actors, engine and view model depend on abstractions; only `App.OnStartup` creates concrete objects.

## User stories → implementation

| Story | Where |
|---|---|
| Enter arrival / cooking intervals | toolbar of `MainWindow`, `MainViewModel.ArrivalIntervalText / CookingIntervalText` |
| Customised warning for bad input | `SimulationSettingsValidator` → `MainViewModel.WarningMessage` → red banner |
| Start / Stop | `StartCommand`, `StopCommand` → `SimulationEngine.Start / StopAsync` |
| Order line, cash desk, kitchen, pickup text boxes | four cards in `MainWindow`, fed by `RestaurantSnapshot` |
| Graphical visualisation | "Интерактивная карта потоков ресторана": chips appear/move between the four `StageColumn`s |

Intervals accepted: integers from 100 to 60 000 ms. Extra durations live in `SimulationSettings` and only make each step
visible: order taking = min(800 ms, arrival/2), calling out = min(600 ms, cooking/2), pickup 500 ms. They are capped so the
cash desk and the server are never slower than the customers / the cook, i.e. the kitchen is the only bottleneck.

## Verification

`tests/FastFoodSimulator.Verification` is a console program that runs the real actors with several interval combinations
(the screenshot defaults, fast arrivals + slow kitchen, fast kitchen, stop/restart) and checks, from timestamped events and from
every snapshot the UI receives: FIFO order at every queue, consecutive unique order numbers, one order cooked at a time,
cook end ≤ call ≤ pickup, constant arrival/cooking rates, waiting list ordering, no lost or duplicated orders.

```
dotnet run --project tests/FastFoodSimulator.Verification
```
