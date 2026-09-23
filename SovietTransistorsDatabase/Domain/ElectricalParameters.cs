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

public enum ConditionRule
{
    PairUkeIkOrUkbIe,
    ExactlyOneCurrent,
    OnlyUkb,
    OnlyUeb,
    UkeAndIk,
    UkbAndIe,
    OnlyUke,
    UkeAndRbe,
    IkAndIb,
    PairPlusFrequency,
    FrequencyPlusOptionalUkeIk,
}

public sealed record ParameterInfo(
    ParameterKind Kind,
    string Code,
    string DisplayName,
    string? Unit,
    BoundDirection Direction,
    ConditionRule Rule);

public static class ElectricalParameterCatalog
{
    /// <summary>Словарь параметров: код, название, каноническая единица, правило границы, правило условий.</summary>
    public static readonly IReadOnlyDictionary<ParameterKind, ParameterInfo> All =
        new Dictionary<ParameterKind, ParameterInfo>
        {
            [ParameterKind.H21E] = new(ParameterKind.H21E, "h21e", "Статический коэффициент передачи тока (ОЭ)", null, BoundDirection.AtLeastOrRange, ConditionRule.PairUkeIkOrUkbIe),
            [ParameterKind.H21B] = new(ParameterKind.H21B, "h21b", "Коэффициент передачи тока (ОБ)", null, BoundDirection.AtLeastOrRange, ConditionRule.UkbAndIe),
            [ParameterKind.CutoffFrequency] = new(ParameterKind.CutoffFrequency, "FGran", "Граничная частота коэффициента передачи тока", "МГц", BoundDirection.AtLeast, ConditionRule.PairUkeIkOrUkbIe),
            [ParameterKind.CutoffVoltage] = new(ParameterKind.CutoffVoltage, "UGran", "Граничное напряжение", "В", BoundDirection.AtLeast, ConditionRule.ExactlyOneCurrent),
            [ParameterKind.CollectorEmitterSaturation] = new(ParameterKind.CollectorEmitterSaturation, "UkeNas", "Напряжение насыщения коллектор-эмиттер", "В", BoundDirection.AtMost, ConditionRule.ExactlyOneCurrent),
            [ParameterKind.BaseEmitterSaturation] = new(ParameterKind.BaseEmitterSaturation, "UbeNas", "Напряжение насыщения база-эмиттер", "В", BoundDirection.AtMost, ConditionRule.ExactlyOneCurrent),
            [ParameterKind.CollectorCutoffCurrent] = new(ParameterKind.CollectorCutoffCurrent, "Ikbo", "Обратный ток коллектора", "мкА", BoundDirection.AtMost, ConditionRule.OnlyUkb),
            [ParameterKind.EmitterCutoffCurrent] = new(ParameterKind.EmitterCutoffCurrent, "Iebo", "Обратный ток эмиттера", "мкА", BoundDirection.AtMost, ConditionRule.OnlyUeb),
            [ParameterKind.CollectorEmitterCutoffCurrent] = new(ParameterKind.CollectorEmitterCutoffCurrent, "Ikeo", "Обратный ток коллектор-эмиттер", "мкА", BoundDirection.AtMost, ConditionRule.OnlyUke),
            [ParameterKind.CollectorEmitterCutoffCurrentRbe] = new(ParameterKind.CollectorEmitterCutoffCurrentRbe, "Ikep", "Обратный ток коллектор-эмиттер при заданном Rбэ", "мкА", BoundDirection.AtMost, ConditionRule.UkeAndRbe),
            [ParameterKind.InputResistance] = new(ParameterKind.InputResistance, "h11", "Входное сопротивление", "Ом", BoundDirection.AtLeast, ConditionRule.UkeAndIk),
            [ParameterKind.CollectorJunctionCapacitance] = new(ParameterKind.CollectorJunctionCapacitance, "Ck", "Ёмкость коллекторного перехода", "пФ", BoundDirection.AtMost, ConditionRule.OnlyUkb),
            [ParameterKind.EmitterJunctionCapacitance] = new(ParameterKind.EmitterJunctionCapacitance, "Ce", "Ёмкость эмиттерного перехода", "пФ", BoundDirection.AtMost, ConditionRule.OnlyUeb),
            [ParameterKind.FeedbackTimeConstant] = new(ParameterKind.FeedbackTimeConstant, "Tauk", "Постоянная времени цепи обратной связи", "пс", BoundDirection.AtMost, ConditionRule.UkbAndIe),
            [ParameterKind.SwitchOnTime] = new(ParameterKind.SwitchOnTime, "Ton", "Время включения", "нс", BoundDirection.AtMost, ConditionRule.IkAndIb),
            [ParameterKind.SwitchOffTime] = new(ParameterKind.SwitchOffTime, "Toff", "Время выключения (рассасывания)", "нс", BoundDirection.AtMost, ConditionRule.IkAndIb),
            [ParameterKind.NoiseFigure] = new(ParameterKind.NoiseFigure, "KShum", "Коэффициент шума", "дБ", BoundDirection.AtMost, ConditionRule.PairPlusFrequency),
            [ParameterKind.OutputPower] = new(ParameterKind.OutputPower, "PVyh", "Выходная мощность", "Вт", BoundDirection.AtLeast, ConditionRule.FrequencyPlusOptionalUkeIk),
            [ParameterKind.PowerGain] = new(ParameterKind.PowerGain, "KUr", "Коэффициент усиления по мощности", "дБ", BoundDirection.AtLeast, ConditionRule.FrequencyPlusOptionalUkeIk),
            [ParameterKind.CollectorEfficiency] = new(ParameterKind.CollectorEfficiency, "Kpd", "КПД коллектора", "%", BoundDirection.AtLeast, ConditionRule.FrequencyPlusOptionalUkeIk),
        };

    public static readonly IReadOnlyDictionary<string, ParameterKind> ByCode =
        All.Values.ToDictionary(info => info.Code, info => info.Kind);

    /// <summary>Параметры, у которых условие «частота» обязательно.</summary>
    public static readonly IReadOnlyCollection<ParameterKind> FrequencyKinds = new HashSet<ParameterKind>
    {
        ParameterKind.NoiseFigure,
        ParameterKind.OutputPower,
        ParameterKind.PowerGain,
        ParameterKind.CollectorEfficiency,
    };

    public static string FrequencyCodesList { get; } = string.Join(", ",
        FrequencyKinds.Select(kind => All[kind].Code).OrderBy(code => code, StringComparer.Ordinal));

    public static ParameterInfo Info(ParameterKind kind) => All[kind];

    public static bool TryGetByCode(string code, out ParameterKind kind) => ByCode.TryGetValue(code, out kind!);

    public static bool IsFrequencyKind(ParameterKind kind) => FrequencyKinds.Contains(kind);

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
        if (parameter.Kind == ParameterKind.CollectorEfficiency && parameter.ValueMin is double kpd && kpd > 100)
            errors.Add($"«{info.Code}»: КПД не может превышать 100%");

        switch (info.Rule)
        {
            case ConditionRule.PairUkeIkOrUkbIe:
                bool pairOk = (parameter.Uke.HasValue && parameter.Ik.HasValue && !parameter.Ukb.HasValue && !parameter.Ueb.HasValue && !parameter.Ie.HasValue)
                           || (parameter.Ukb.HasValue && parameter.Ie.HasValue && !parameter.Uke.HasValue && !parameter.Ueb.HasValue && !parameter.Ik.HasValue);
                if (!pairOk)
                    errors.Add($"«{info.Code}»: условия — ровно пара Uкэ + Iк (Uke, Ik, включение с ОЭ) либо Uкб + Iэ (Ukb, Ie, ОБ); задано: {Describe(parameter)}");
                break;
            case ConditionRule.ExactlyOneCurrent:
                int currents = (parameter.Ik.HasValue ? 1 : 0) + (parameter.Ie.HasValue ? 1 : 0);
                bool hasVoltage = parameter.Uke.HasValue || parameter.Ukb.HasValue || parameter.Ueb.HasValue;
                if (currents != 1 || hasVoltage)
                    errors.Add($"«{info.Code}»: условие — только ток коллектора (Ik) или только ток эмиттера (Ie); задано: {Describe(parameter)}");
                break;
            case ConditionRule.OnlyUkb:
                if (!parameter.Ukb.HasValue || parameter.Uke.HasValue || parameter.Ueb.HasValue || parameter.Ik.HasValue || parameter.Ie.HasValue)
                    errors.Add($"«{info.Code}»: условие — только напряжение коллектор-база (Ukb); задано: {Describe(parameter)}");
                break;
            case ConditionRule.OnlyUeb:
                if (!parameter.Ueb.HasValue || parameter.Uke.HasValue || parameter.Ukb.HasValue || parameter.Ik.HasValue || parameter.Ie.HasValue)
                    errors.Add($"«{info.Code}»: условие — только напряжение эмиттер-база (Ueb); задано: {Describe(parameter)}");
                break;
            case ConditionRule.UkeAndIk:
                if (!parameter.Uke.HasValue || !parameter.Ik.HasValue || parameter.Ukb.HasValue || parameter.Ueb.HasValue || parameter.Ie.HasValue)
                    errors.Add($"«{info.Code}»: условия — Uкэ (Uke) и Iк (Ik); задано: {Describe(parameter)}");
                break;
            case ConditionRule.UkbAndIe:
                if (!parameter.Ukb.HasValue || !parameter.Ie.HasValue || parameter.Uke.HasValue || parameter.Ueb.HasValue || parameter.Ik.HasValue)
                    errors.Add($"«{info.Code}»: условия — Uкб (Ukb) и Iэ (Ie), схема с общей базой; задано: {Describe(parameter)}");
                break;
            case ConditionRule.OnlyUke:
                if (!parameter.Uke.HasValue || parameter.Ukb.HasValue || parameter.Ueb.HasValue || parameter.Ik.HasValue || parameter.Ie.HasValue)
                    errors.Add($"«{info.Code}»: условие — только напряжение коллектор-эмиттер (Uke); задано: {Describe(parameter)}");
                break;
            case ConditionRule.UkeAndRbe:
                if (!parameter.Uke.HasValue || !parameter.Rbe.HasValue || parameter.Ukb.HasValue || parameter.Ueb.HasValue || parameter.Ik.HasValue || parameter.Ie.HasValue)
                    errors.Add($"«{info.Code}»: условия — Uкэ (Uke) и сопротивление в цепи база-эмиттер (Rbe, Ом); задано: {Describe(parameter)}");
                break;
            case ConditionRule.IkAndIb:
                if (!parameter.Ik.HasValue || !parameter.Ib.HasValue || parameter.Uke.HasValue || parameter.Ukb.HasValue || parameter.Ueb.HasValue || parameter.Ie.HasValue)
                    errors.Add($"«{info.Code}»: условия — Iк (Ik) и Iб (Ib); задано: {Describe(parameter)}");
                break;
            case ConditionRule.PairPlusFrequency:
                bool noisePairOk = (parameter.Uke.HasValue && parameter.Ik.HasValue && !parameter.Ukb.HasValue && !parameter.Ueb.HasValue && !parameter.Ie.HasValue)
                                || (parameter.Ukb.HasValue && parameter.Ie.HasValue && !parameter.Uke.HasValue && !parameter.Ueb.HasValue && !parameter.Ik.HasValue);
                if (!noisePairOk)
                    errors.Add($"«{info.Code}»: условия — пара Uкэ + Iк (Uke, Ik) либо Uкб + Iэ (Ukb, Ie) и обязательная частота (freq, МГц); задано: {Describe(parameter)}");
                if (parameter.Freq is null)
                    errors.Add($"«{info.Code}»: обязательна частота измерения (freq, МГц)");
                break;
            case ConditionRule.FrequencyPlusOptionalUkeIk:
                if (parameter.Freq is null)
                    errors.Add($"«{info.Code}»: обязательна частота (freq, МГц)");
                bool bothOrNone = (parameter.Uke.HasValue && parameter.Ik.HasValue) || (!parameter.Uke.HasValue && !parameter.Ik.HasValue);
                if (!bothOrNone)
                    errors.Add($"«{info.Code}»: Uкэ (Uke) и Iк (Ik) задаются только вместе; задано: {Describe(parameter)}");
                if (parameter.Ukb.HasValue || parameter.Ueb.HasValue || parameter.Ie.HasValue)
                    errors.Add($"«{info.Code}»: допускаются только Uke + Ik (вместе) и частота; задано: {Describe(parameter)}");
                break;
        }

        if (parameter.Freq.HasValue && !ElectricalParameterCatalog.IsFrequencyKind(parameter.Kind))
            errors.Add($"«{info.Code}»: частота (freq) допускается только у частотных параметров ({ElectricalParameterCatalog.FrequencyCodesList})");
        if (parameter.Rg.HasValue && parameter.Kind != ParameterKind.NoiseFigure)
            errors.Add($"«{info.Code}»: сопротивление генератора (Rg) допускается только у KShum");
        if (parameter.Rbe.HasValue && parameter.Kind != ParameterKind.CollectorEmitterCutoffCurrentRbe)
            errors.Add($"«{info.Code}»: сопротивление Rбэ (Rbe) допускается только у Ikep");
        if (parameter.Ib.HasValue && parameter.Kind is not (ParameterKind.SwitchOnTime or ParameterKind.SwitchOffTime))
            errors.Add($"«{info.Code}»: ток базы (Ib) как условие допускается только у Ton/Toff");

        if (IsNonPositive(parameter.Uke)) errors.Add($"«{info.Code}»: условие Uкэ должно быть положительным");
        if (IsNonPositive(parameter.Ukb)) errors.Add($"«{info.Code}»: условие Uкб должно быть положительным");
        if (IsNonPositive(parameter.Ueb)) errors.Add($"«{info.Code}»: условие Uэб должно быть положительным");
        if (IsNonPositive(parameter.Ik)) errors.Add($"«{info.Code}»: условие Iк должно быть положительным");
        if (IsNonPositive(parameter.Ie)) errors.Add($"«{info.Code}»: условие Iэ должно быть положительным");
        if (IsNonPositive(parameter.Ib)) errors.Add($"«{info.Code}»: условие Iб должно быть положительным");
        if (IsNonPositive(parameter.Freq)) errors.Add($"«{info.Code}»: частота должна быть положительной");
        if (IsNonPositive(parameter.Rg)) errors.Add($"«{info.Code}»: сопротивление генератора должно быть положительным");
        if (IsNonPositive(parameter.Rbe)) errors.Add($"«{info.Code}»: сопротивление Rбэ должно быть положительным");
        return errors;
    }

    private static bool IsNonPositive(double? value) => value is <= 0;

    private static string Describe(ElectricalParameter p)
    {
        var parts = new List<string>();
        if (p.Uke.HasValue) parts.Add("Uкэ");
        if (p.Ukb.HasValue) parts.Add("Uкб");
        if (p.Ueb.HasValue) parts.Add("Uэб");
        if (p.Ik.HasValue) parts.Add("Iк");
        if (p.Ie.HasValue) parts.Add("Iэ");
        if (p.Ib.HasValue) parts.Add("Iб");
        if (p.Freq.HasValue) parts.Add("f");
        if (p.Rg.HasValue) parts.Add("Rг");
        if (p.Rbe.HasValue) parts.Add("Rбэ");
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
            return $"{Fmt(min)}…{Fmt(max)}{unit}";
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
