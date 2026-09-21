using System.Diagnostics.CodeAnalysis;
using FastFoodSimulator.Core.Configuration;

namespace FastFoodSimulator.Core.Validation;

public sealed class SettingsValidationResult
{
    private SettingsValidationResult(SimulationSettings? settings, IReadOnlyList<string> errors)
    {
        Settings = settings;
        Errors = errors;
    }

    public SimulationSettings? Settings { get; }

    public IReadOnlyList<string> Errors { get; }

    [MemberNotNullWhen(true, nameof(Settings))]
    public bool IsValid => Settings is not null;

    public static SettingsValidationResult Success(SimulationSettings settings) =>
        new(settings, Array.Empty<string>());

    public static SettingsValidationResult Failure(IReadOnlyList<string> errors) => new(null, errors);
}
