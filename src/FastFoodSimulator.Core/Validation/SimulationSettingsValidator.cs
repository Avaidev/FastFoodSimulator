using System.Globalization;
using FastFoodSimulator.Core.Abstractions;
using FastFoodSimulator.Core.Configuration;

namespace FastFoodSimulator.Core.Validation;

/// <summary>Validates the two user-entered intervals and produces customised warning messages.</summary>
public sealed class SimulationSettingsValidator : ISettingsValidator
{
    public const int MinIntervalMs = 100;
    public const int MaxIntervalMs = 60_000;

    public SettingsValidationResult Validate(string? arrivalIntervalText, string? cookingIntervalText)
    {
        var errors = new List<string>();

        var arrival = Parse("прихода клиентов", arrivalIntervalText, errors);
        var cooking = Parse("готовки заказа", cookingIntervalText, errors);

        if (errors.Count > 0 || arrival is null || cooking is null)
        {
            return SettingsValidationResult.Failure(errors);
        }

        return SettingsValidationResult.Success(new SimulationSettings(
            TimeSpan.FromMilliseconds(arrival.Value),
            TimeSpan.FromMilliseconds(cooking.Value)));
    }

    private static int? Parse(string subject, string? text, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            errors.Add($"Интервал {subject} не задан. Введите целое число миллисекунд, например 1500.");
            return null;
        }

        var trimmed = text.Trim();

        if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            errors.Add($"Интервал {subject}: «{trimmed}» не является целым числом. Используйте только цифры, например 1500.");
            return null;
        }

        if (value < MinIntervalMs || value > MaxIntervalMs)
        {
            errors.Add($"Интервал {subject} должен быть от {MinIntervalMs} до {MaxIntervalMs} мс (введено: {value}).");
            return null;
        }

        return value;
    }
}
