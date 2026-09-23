namespace SovietTransistors.Domain;

/// <summary>Предельные эксплуатационные данные (единицы канонические: В, мА, мВт, мкс, °C, °C/Вт).</summary>
public sealed record MaximumRatings
{
    public double? UkeMax { get; init; }
    public double? UkbMax { get; init; }
    public double? UbeMax { get; init; }
    /// <summary>Постоянное напряжение коллектор-эмиттер при разомкнутой базе, В.</summary>
    public double? UkeoMax { get; init; }
    public double? IkMax { get; init; }
    public double? IbMax { get; init; }
    public double? PkMax { get; init; }
    /// <summary>Импульсный ток коллектора, мА (при PulseDuration).</summary>
    public double? IkPulseMax { get; init; }
    /// <summary>Импульсная рассеиваемая мощность, мВт (при PulseDuration).</summary>
    public double? PkPulseMax { get; init; }
    /// <summary>Длительность импульса, мкс; обязательна, если задан импульсный ток или мощность.</summary>
    public double? PulseDuration { get; init; }
    /// <summary>Минимальная температура среды, °C (может быть отрицательной).</summary>
    public double? TempMin { get; init; }
    /// <summary>Максимальная температура среды, °C.</summary>
    public double? TempMax { get; init; }
    /// <summary>Максимальная температура перехода, °C.</summary>
    public double? TempJunctionMax { get; init; }
    /// <summary>Тепловое сопротивление переход-корпус, °C/Вт.</summary>
    public double? Rth { get; init; }
}

public static class MaximumRatingsValidator
{
    public static IReadOnlyList<string> Errors(MaximumRatings ratings)
    {
        var errors = new List<string>();
        Add(ratings.UkeMax, "Uкэ макс", errors);
        Add(ratings.UkbMax, "Uкб макс", errors);
        Add(ratings.UbeMax, "Uбэ макс", errors);
        Add(ratings.UkeoMax, "Uкэо макс", errors);
        Add(ratings.IkMax, "Iк макс", errors);
        Add(ratings.IbMax, "Iб макс", errors);
        Add(ratings.PkMax, "Pк макс", errors);
        Add(ratings.IkPulseMax, "импульсный Iк макс", errors);
        Add(ratings.PkPulseMax, "импульсная Pк макс", errors);
        Add(ratings.PulseDuration, "длительность импульса", errors);
        Add(ratings.TempJunctionMax, "T перехода макс", errors);
        Add(ratings.Rth, "тепловое сопротивление", errors);

        if (ratings.TempMin is double tmin && ratings.TempMax is double tmax && tmin >= tmax)
            errors.Add($"Tмин ({ParameterText.Fmt(tmin)}) должна быть меньше Tмакс ({ParameterText.Fmt(tmax)})");

        bool hasPulseValues = ratings.IkPulseMax.HasValue || ratings.PkPulseMax.HasValue;
        if (hasPulseValues && ratings.PulseDuration is null)
            errors.Add("длительность импульса (pulseDuration, мкс) обязательна, если задан импульсный ток или мощность");
        if (!hasPulseValues && ratings.PulseDuration is not null)
            errors.Add("длительность импульса задана, но импульсные ток/мощность не заданы");
        return errors;
    }

    private static void Add(double? value, string name, List<string> errors)
    {
        if (value is <= 0) errors.Add($"{name}: значение должно быть положительным");
    }
}
