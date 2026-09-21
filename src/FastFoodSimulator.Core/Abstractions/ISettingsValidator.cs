using FastFoodSimulator.Core.Validation;

namespace FastFoodSimulator.Core.Abstractions;

public interface ISettingsValidator
{
    SettingsValidationResult Validate(string? arrivalIntervalText, string? cookingIntervalText);
}
