using System.Globalization;

namespace SovietTransistorsDatabase.Domain;

public enum ParameterKind
{
    H21E,
    H21B,
    CutoffFrequency,
    CutoffVoltage,
    CollectorEmitterSaturation,
    BaseEmitterSaturation,
    CollectorCutoffCurrent,
    EmitterCutoffCurrent,
    CollectorEmitterCutoffCurrent,
    CollectorEmitterCutoffCurrentRbe,
    InputResistance,
    CollectorJunctionCapacitance,
    EmitterJunctionCapacitance,
    FeedbackTimeConstant,
    SwitchOnTime,
    SwitchOffTime,
    NoiseFigure,
    OutputPower,
    PowerGain,
    CollectorEfficiency,
}

/// <summary>«Не менее» (только min), «не более» (только max) или «не менее либо диапазон» (min обязателен, max опционален).</summary>
public enum BoundDirection { AtLeast, AtMost, AtLeastOrRange }

/// <summary>Параметр справочника: код jsonc, название, единица, правило границ, спецификация условий и потолок значения.</summary>
public sealed record ParameterInfo(
    ParameterKind Kind,
    string Code,
    string DisplayName,
    string? Unit,
    BoundDirection Direction,
    ConditionSpec Conditions,
    double? ValueCeiling = null);

public static class ElectricalParameterCatalog
{
    /// <summary>Словарь параметров: код, название, каноническая единица, правило границ, спецификация условий, потолок значения.</summary>
    public static readonly IReadOnlyDictionary<ParameterKind, ParameterInfo> All =
        new Dictionary<ParameterKind, ParameterInfo>
        {
            [ParameterKind.H21E] = new(ParameterKind.H21E, "h21e", "Статический коэффициент передачи тока (ОЭ)", null, BoundDirection.AtLeastOrRange, ConditionSpecs.PairUkeIkOrUkbIe),
            [ParameterKind.H21B] = new(ParameterKind.H21B, "h21b", "Коэффициент передачи тока (ОБ)", null, BoundDirection.AtLeastOrRange, ConditionSpecs.UkbAndIe),
            [ParameterKind.CutoffFrequency] = new(ParameterKind.CutoffFrequency, "FGran", "Граничная частота коэффициента передачи тока", "МГц", BoundDirection.AtLeast, ConditionSpecs.PairUkeIkOrUkbIe),
            [ParameterKind.CutoffVoltage] = new(ParameterKind.CutoffVoltage, "UGran", "Граничное напряжение", "В", BoundDirection.AtLeast, ConditionSpecs.ExactlyOneCurrent),
            [ParameterKind.CollectorEmitterSaturation] = new(ParameterKind.CollectorEmitterSaturation, "UkeNas", "Напряжение насыщения коллектор-эмиттер", "В", BoundDirection.AtMost, ConditionSpecs.ExactlyOneCurrent),
            [ParameterKind.BaseEmitterSaturation] = new(ParameterKind.BaseEmitterSaturation, "UbeNas", "Напряжение насыщения база-эмиттер", "В", BoundDirection.AtMost, ConditionSpecs.ExactlyOneCurrent),
            [ParameterKind.CollectorCutoffCurrent] = new(ParameterKind.CollectorCutoffCurrent, "Ikbo", "Обратный ток коллектора", "мкА", BoundDirection.AtMost, ConditionSpecs.OnlyUkb),
            [ParameterKind.EmitterCutoffCurrent] = new(ParameterKind.EmitterCutoffCurrent, "Iebo", "Обратный ток эмиттера", "мкА", BoundDirection.AtMost, ConditionSpecs.OnlyUeb),
            [ParameterKind.CollectorEmitterCutoffCurrent] = new(ParameterKind.CollectorEmitterCutoffCurrent, "Ikeo", "Обратный ток коллектор-эмиттер", "мкА", BoundDirection.AtMost, ConditionSpecs.OnlyUke),
            [ParameterKind.CollectorEmitterCutoffCurrentRbe] = new(ParameterKind.CollectorEmitterCutoffCurrentRbe, "Ikep", "Обратный ток коллектор-эмиттер при заданном Rбэ", "мкА", BoundDirection.AtMost, ConditionSpecs.UkeAndRbe),
            [ParameterKind.InputResistance] = new(ParameterKind.InputResistance, "h11", "Входное сопротивление", "Ом", BoundDirection.AtLeast, ConditionSpecs.UkeAndIk),
            [ParameterKind.CollectorJunctionCapacitance] = new(ParameterKind.CollectorJunctionCapacitance, "Ck", "Ёмкость коллекторного перехода", "пФ", BoundDirection.AtMost, ConditionSpecs.OnlyUkb),
            [ParameterKind.EmitterJunctionCapacitance] = new(ParameterKind.EmitterJunctionCapacitance, "Ce", "Ёмкость эмиттерного перехода", "пФ", BoundDirection.AtMost, ConditionSpecs.OnlyUeb),
            [ParameterKind.FeedbackTimeConstant] = new(ParameterKind.FeedbackTimeConstant, "Tauk", "Постоянная времени цепи обратной связи", "пс", BoundDirection.AtMost, ConditionSpecs.UkbAndIe),
            [ParameterKind.SwitchOnTime] = new(ParameterKind.SwitchOnTime, "Ton", "Время включения", "нс", BoundDirection.AtMost, ConditionSpecs.IkAndIb),
            [ParameterKind.SwitchOffTime] = new(ParameterKind.SwitchOffTime, "Toff", "Время выключения (рассасывания)", "нс", BoundDirection.AtMost, ConditionSpecs.IkAndIb),
            [ParameterKind.NoiseFigure] = new(ParameterKind.NoiseFigure, "KShum", "Коэффициент шума", "дБ", BoundDirection.AtMost, ConditionSpecs.PairPlusFrequency),
            [ParameterKind.OutputPower] = new(ParameterKind.OutputPower, "PVyh", "Выходная мощность", "Вт", BoundDirection.AtLeast, ConditionSpecs.FrequencyPlusOptionalUkeIk),
            [ParameterKind.PowerGain] = new(ParameterKind.PowerGain, "KUr", "Коэффициент усиления по мощности", "дБ", BoundDirection.AtLeast, ConditionSpecs.FrequencyPlusOptionalUkeIk),
            [ParameterKind.CollectorEfficiency] = new(ParameterKind.CollectorEfficiency, "Kpd", "КПД коллектора", "%", BoundDirection.AtLeast, ConditionSpecs.FrequencyPlusOptionalUkeIk, ValueCeiling: 100),
        };

    public static readonly IReadOnlyDictionary<string, ParameterKind> ByCode =
        All.Values.ToDictionary(info => info.Code, info => info.Kind);

    public static ParameterInfo Info(ParameterKind kind) => All[kind];

    public static bool TryGetByCode(string code, out ParameterKind kind) => ByCode.TryGetValue(code, out kind!);

    public static string CodesList => string.Join(", ", All.Values.OrderBy(info => info.Kind).Select(info => info.Code));
}

/// <summary>Электрический параметр при одном наборе условий (единицы канонические: В, мА, мкА, МГц, дБ/%, Ом, пс, нс, пФ, °C).</summary>
public sealed record ElectricalParameter
{
    public required ParameterKind Kind { get; init; }
    public double? ValueMin { get; init; }
    public double? ValueMax { get; init; }
    public double? Uke { get; init; }
    public double? Ukb { get; init; }
    public double? Ueb { get; init; }
    public double? Ik { get; init; }
    public double? Ie { get; init; }
    public double? Ib { get; init; }
    public double? Freq { get; init; }
    public double? Rg { get; init; }
    public double? Rbe { get; init; }
    public double? Temp { get; init; }
}

public static class ElectricalParameterValidator
{
    public static IReadOnlyList<string> Errors(ElectricalParameter parameter)
    {
        var errors = new List<string>();
        ParameterInfo info = ElectricalParameterCatalog.Info(parameter.Kind);

        switch (info.Direction)
        {
            case BoundDirection.AtLeast:
                if (parameter.ValueMin is null) errors.Add($"«{info.Code}»: обязательно значение «не менее» (min)");
                if (parameter.ValueMax is not null) errors.Add($"«{info.Code}»: верхняя граница (max) не допускается — параметр вида «не менее»");
                break;
            case BoundDirection.AtMost:
                if (parameter.ValueMax is null) errors.Add($"«{info.Code}»: обязательно значение «не более» (max)");
                if (parameter.ValueMin is not null) errors.Add($"«{info.Code}»: нижняя граница (min) не допускается — параметр вида «не более»");
                break;
            case BoundDirection.AtLeastOrRange:
                if (parameter.ValueMin is null) errors.Add($"«{info.Code}»: обязательна нижняя граница (min)");
                break;
        }
        if (parameter.ValueMin is double min && min <= 0) errors.Add($"«{info.Code}»: значение должно быть положительным");
        if (parameter.ValueMax is double max && max <= 0) errors.Add($"«{info.Code}»: значение должно быть положительным");
        if (parameter.ValueMin is double a && parameter.ValueMax is double b && a > b) errors.Add($"«{info.Code}»: нижняя граница больше верхней");
        if (info.ValueCeiling is double ceiling)
        {
            string unit = info.Unit is null ? "" : " " + info.Unit;
            string limit = $"«{info.Code}»: {info.DisplayName} не может превышать {ParameterText.Fmt(ceiling)}{unit}";
            if (parameter.ValueMin is double minCeiling && minCeiling > ceiling) errors.Add(limit);
            if (parameter.ValueMax is double maxCeiling && maxCeiling > ceiling) errors.Add(limit);
        }

        if (!info.Conditions.IsSatisfiedBy(parameter))
            errors.Add($"«{info.Code}»: условия — {info.Conditions.Describe()}; задано: {Describe(parameter)}");

        foreach (ConditionKey key in ConditionKeys.All)
            if (ConditionKeys.ValueOf(parameter, key) is <= 0)
                errors.Add($"«{info.Code}»: {ConditionKeys.NonPositiveMessage(key)}");
        return errors;
    }

    private static string Describe(ElectricalParameter parameter)
    {
        var parts = new List<string>();
        foreach (ConditionKey key in ConditionKeys.All)
            if (ConditionKeys.ValueOf(parameter, key).HasValue)
                parts.Add(ConditionKeys.Label(key));
        return parts.Count == 0 ? "ничего" : string.Join(", ", parts);
    }
}

public static class ParameterText
{
    public static string Value(ElectricalParameter parameter)
    {
        ParameterInfo info = ElectricalParameterCatalog.Info(parameter.Kind);
        string unit = info.Unit is null ? "" : " " + info.Unit;
        if (parameter.ValueMin is double min && parameter.ValueMax is double max)
            return $"{Fmt(min)}...{Fmt(max)}{unit}";
        if (parameter.ValueMin is double lower)
            return $"не менее {Fmt(lower)}{unit}";
        if (parameter.ValueMax is double upper)
            return $"не более {Fmt(upper)}{unit}";
        return "—";
    }

    public static string Conditions(ElectricalParameter parameter)
    {
        var parts = new List<string>();
        if (parameter.Uke is double uke) parts.Add($"Uкэ = {Fmt(uke)} В");
        if (parameter.Ukb is double ukb) parts.Add($"Uкб = {Fmt(ukb)} В");
        if (parameter.Ueb is double ueb) parts.Add($"Uэб = {Fmt(ueb)} В");
        if (parameter.Ik is double ik) parts.Add($"Iк = {Fmt(ik)} мА");
        if (parameter.Ie is double ie) parts.Add($"Iэ = {Fmt(ie)} мА");
        if (parameter.Ib is double ib) parts.Add($"Iб = {Fmt(ib)} мА");
        if (parameter.Freq is double f) parts.Add($"f = {Fmt(f)} МГц");
        if (parameter.Rg is double rg) parts.Add($"Rг = {Fmt(rg)} Ом");
        if (parameter.Rbe is double rbe) parts.Add($"Rбэ = {Fmt(rbe)} Ом");
        if (parameter.Temp is double temp) parts.Add($"T = {Fmt(temp)} °C");
        return parts.Count == 0 ? "без условий" : "при " + string.Join(", ", parts);
    }

    public static string Fmt(double value) =>
        value == Math.Floor(value) && Math.Abs(value) < 1e15
            ? ((long)value).ToString(CultureInfo.InvariantCulture)
            : value.ToString(CultureInfo.InvariantCulture);
}
