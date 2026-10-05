namespace FastFoodSimulator.ViewModels;

public abstract record VisualToken(double X, double Y, string Label, string Fill);

public sealed record CircleToken(double X, double Y, string Label, string Fill) : VisualToken(X, Y, Label, Fill);

public sealed record SquareToken(double X, double Y, string Label, string Fill) : VisualToken(X, Y, Label, Fill);
