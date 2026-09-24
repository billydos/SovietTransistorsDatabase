using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Tests;

public class ElectricalParameterValidatorTests
{
    private static ElectricalParameter P(ParameterKind kind, double? min = null, double? max = null,
        double? uke = null, double? ukb = null, double? ueb = null, double? ik = null, double? ie = null,
        double? ib = null, double? freq = null, double? rg = null, double? rbe = null) => new()
    {
        Kind = kind,
        ValueMin = min,
        ValueMax = max,
        Uke = uke,
        Ukb = ukb,
        Ueb = ueb,
        Ik = ik,
        Ie = ie,
        Ib = ib,
        Freq = freq,
        Rg = rg,
        Rbe = rbe,
    };

    private static IReadOnlyList<string> Errors(ParameterKind kind, double? min = null, double? max = null,
        double? uke = null, double? ukb = null, double? ueb = null, double? ik = null, double? ie = null,
        double? ib = null, double? freq = null, double? rg = null, double? rbe = null) =>
        ElectricalParameterValidator.Errors(P(kind, min, max, uke, ukb, ueb, ik, ie, ib, freq, rg, rbe));

    public static TheoryData<ConditionRule, ElectricalParameter> OneValidParameterPerRule => new()
    {
        { ConditionRule.PairUkeIkOrUkbIe, P(ParameterKind.H21E, min: 50, uke: 10, ik: 1) },
        { ConditionRule.PairUkeIkOrUkbIe, P(ParameterKind.CutoffFrequency, min: 5, ukb: 5, ie: 1) },
        { ConditionRule.ExactlyOneCurrent, P(ParameterKind.CutoffVoltage, min: 30, ik: 1) },
        { ConditionRule.ExactlyOneCurrent, P(ParameterKind.CollectorEmitterSaturation, max: 0.5, ie: 10) },
        { ConditionRule.OnlyUkb, P(ParameterKind.CollectorCutoffCurrent, max: 1, ukb: 10) },
        { ConditionRule.OnlyUeb, P(ParameterKind.EmitterCutoffCurrent, max: 0.5, ueb: 5) },
        { ConditionRule.OnlyUke, P(ParameterKind.CollectorEmitterCutoffCurrent, max: 20, uke: 15) },
        { ConditionRule.UkeAndRbe, P(ParameterKind.CollectorEmitterCutoffCurrentRbe, max: 5, uke: 10, rbe: 100) },
        { ConditionRule.UkeAndIk, P(ParameterKind.InputResistance, min: 200, uke: 5, ik: 1) },
        { ConditionRule.UkbAndIe, P(ParameterKind.H21B, min: 10, ukb: 5, ie: 10) },
        { ConditionRule.UkbAndIe, P(ParameterKind.FeedbackTimeConstant, max: 300, ukb: 5, ie: 1) },
        { ConditionRule.IkAndIb, P(ParameterKind.SwitchOnTime, max: 500, ik: 500, ib: 50) },
        { ConditionRule.IkAndIb, P(ParameterKind.SwitchOffTime, max: 1500, ik: 500, ib: 50) },
        { ConditionRule.PairPlusFrequency, P(ParameterKind.NoiseFigure, max: 4, uke: 5, ik: 1, freq: 1.8, rg: 500) },
        { ConditionRule.PairPlusFrequency, P(ParameterKind.NoiseFigure, max: 4, ukb: 5, ie: 1, freq: 1.8) },
        { ConditionRule.FrequencyPlusOptionalUkeIk, P(ParameterKind.OutputPower, min: 5, freq: 100) },
        { ConditionRule.FrequencyPlusOptionalUkeIk, P(ParameterKind.PowerGain, min: 10, uke: 10, ik: 100, freq: 100) },
        { ConditionRule.FrequencyPlusOptionalUkeIk, P(ParameterKind.CollectorEfficiency, min: 50, freq: 100) },
    };

    [Theory]
    [MemberData(nameof(OneValidParameterPerRule))]
    public void ValidParameter_EachConditionRule_NoErrors(ConditionRule rule, ElectricalParameter parameter)
    {
        Assert.Equal(rule, ElectricalParameterCatalog.Info(parameter.Kind).Rule);

        Assert.Empty(ElectricalParameterValidator.Errors(parameter));
    }

    [Fact]
    public void PairUkeIkOrUkbIe_RequiresExactlyOnePair()
    {
        Assert.Contains(Errors(ParameterKind.H21E, min: 50), e => e.Contains("ровно пара"));
        Assert.Contains(Errors(ParameterKind.H21E, min: 50, uke: 10), e => e.Contains("ровно пара"));
        Assert.Contains(Errors(ParameterKind.H21E, min: 50, uke: 10, ie: 1), e => e.Contains("ровно пара"));
        Assert.Contains(Errors(ParameterKind.H21E, min: 50, uke: 10, ik: 1, ukb: 5), e => e.Contains("ровно пара"));
    }

    [Fact]
    public void ExactlyOneCurrent_RequiresOneCurrentAndNoVoltage()
    {
        string fragment = "только ток коллектора (Ik) или только ток эмиттера (Ie)";
        Assert.Contains(Errors(ParameterKind.CutoffVoltage, min: 30), e => e.Contains(fragment));
        Assert.Contains(Errors(ParameterKind.CutoffVoltage, min: 30, ik: 1, ie: 1), e => e.Contains(fragment));
        Assert.Contains(Errors(ParameterKind.CutoffVoltage, min: 30, ik: 1, uke: 10), e => e.Contains(fragment));
    }

    [Fact]
    public void OnlyUkb_RejectsMissingOrExtraConditions()
    {
        string fragment = "только напряжение коллектор-база (Ukb)";
        Assert.Contains(Errors(ParameterKind.CollectorCutoffCurrent, max: 1), e => e.Contains(fragment));
        Assert.Contains(Errors(ParameterKind.CollectorCutoffCurrent, max: 1, ukb: 10, uke: 10), e => e.Contains(fragment));
        Assert.Contains(Errors(ParameterKind.CollectorCutoffCurrent, max: 1, ukb: 10, ik: 1), e => e.Contains(fragment));
    }

    [Fact]
    public void OnlyUeb_RejectsMissingOrExtraConditions()
    {
        string fragment = "только напряжение эмиттер-база (Ueb)";
        Assert.Contains(Errors(ParameterKind.EmitterCutoffCurrent, max: 1), e => e.Contains(fragment));
        Assert.Contains(Errors(ParameterKind.EmitterCutoffCurrent, max: 1, ueb: 5, uke: 5), e => e.Contains(fragment));
    }

    [Fact]
    public void OnlyUke_RejectsMissingOrExtraConditions()
    {
        string fragment = "только напряжение коллектор-эмиттер (Uke)";
        Assert.Contains(Errors(ParameterKind.CollectorEmitterCutoffCurrent, max: 20), e => e.Contains(fragment));
        Assert.Contains(Errors(ParameterKind.CollectorEmitterCutoffCurrent, max: 20, uke: 15, ie: 1), e => e.Contains(fragment));
    }

    [Fact]
    public void UkeAndRbe_RequiresBoth()
    {
        string fragment = "Uкэ (Uke) и сопротивление в цепи база-эмиттер (Rbe, Ом)";
        Assert.Contains(Errors(ParameterKind.CollectorEmitterCutoffCurrentRbe, max: 5, uke: 10), e => e.Contains(fragment));
        Assert.Contains(Errors(ParameterKind.CollectorEmitterCutoffCurrentRbe, max: 5, uke: 10, rbe: 100, ik: 1), e => e.Contains(fragment));
    }

    [Fact]
    public void UkeAndIk_RequiresBoth()
    {
        string fragment = "Uкэ (Uke) и Iк (Ik)";
        Assert.Contains(Errors(ParameterKind.InputResistance, min: 200, uke: 5), e => e.Contains(fragment));
        Assert.Contains(Errors(ParameterKind.InputResistance, min: 200, ik: 1, ukb: 5), e => e.Contains(fragment));
    }

    [Fact]
    public void UkbAndIe_RequiresBoth()
    {
        string fragment = "Uкб (Ukb) и Iэ (Ie)";
        Assert.Contains(Errors(ParameterKind.H21B, min: 10, ukb: 5), e => e.Contains(fragment));
        Assert.Contains(Errors(ParameterKind.H21B, min: 10, ie: 10, uke: 5), e => e.Contains(fragment));
    }

    [Fact]
    public void IkAndIb_RequiresBoth()
    {
        string fragment = "Iк (Ik) и Iб (Ib)";
        Assert.Contains(Errors(ParameterKind.SwitchOnTime, max: 500, ik: 500), e => e.Contains(fragment));
        Assert.Contains(Errors(ParameterKind.SwitchOffTime, max: 1500, ib: 50, uke: 5), e => e.Contains(fragment));
    }

    [Fact]
    public void PairPlusFrequency_RequiresPairAndFrequency()
    {
        Assert.Contains(Errors(ParameterKind.NoiseFigure, max: 4, uke: 5, ik: 1), e => e.Contains("обязательна частота измерения (freq, МГц)"));
        string pairFragment = "пара Uкэ + Iк (Uke, Ik) либо Uкб + Iэ (Ukb, Ie)";
        Assert.Contains(Errors(ParameterKind.NoiseFigure, max: 4, freq: 1.8), e => e.Contains(pairFragment));
        Assert.Contains(Errors(ParameterKind.NoiseFigure, max: 4, uke: 5, ie: 1, freq: 1.8), e => e.Contains(pairFragment));
    }

    [Fact]
    public void FrequencyPlusOptionalUkeIk_RequiresFrequencyAndPairedUkeIk()
    {
        Assert.Contains(Errors(ParameterKind.OutputPower, min: 5), e => e.Contains("обязательна частота (freq, МГц)"));
        Assert.Contains(Errors(ParameterKind.OutputPower, min: 5, uke: 10, freq: 100), e => e.Contains("задаются только вместе"));
        Assert.Contains(Errors(ParameterKind.OutputPower, min: 5, uke: 10, ik: 1, ie: 1, freq: 100), e => e.Contains("допускаются только Uke + Ik"));
    }

    [Fact]
    public void AtLeastParameter_RequiresMinAndForbidsMax()
    {
        Assert.Contains(Errors(ParameterKind.CutoffFrequency), e => e.Contains("обязательно значение «не менее» (min)"));
        Assert.Contains(Errors(ParameterKind.CutoffFrequency, min: 5, max: 10, uke: 10, ik: 1), e => e.Contains("верхняя граница (max) не допускается"));
    }

    [Fact]
    public void AtMostParameter_RequiresMaxAndForbidsMin()
    {
        Assert.Contains(Errors(ParameterKind.CollectorCutoffCurrent, ukb: 10), e => e.Contains("обязательно значение «не более» (max)"));
        Assert.Contains(Errors(ParameterKind.CollectorCutoffCurrent, min: 0.5, max: 1, ukb: 10), e => e.Contains("нижняя граница (min) не допускается"));
    }

    [Fact]
    public void AtLeastOrRangeParameter_RequiresMin_MaxOptional()
    {
        Assert.Contains(Errors(ParameterKind.H21E, uke: 10, ik: 1), e => e.Contains("обязательна нижняя граница (min)"));
        Assert.Empty(ElectricalParameterValidator.Errors(P(ParameterKind.H21E, min: 50, max: 200, uke: 10, ik: 1)));
    }

    [Fact]
    public void RangeWithMinAboveMax_ProducesError()
    {
        Assert.Contains(Errors(ParameterKind.H21E, min: 200, max: 50, uke: 10, ik: 1), e => e.Contains("нижняя граница больше верхней"));
    }

    [Fact]
    public void NonPositiveValues_ProduceErrors()
    {
        Assert.Contains(Errors(ParameterKind.H21E, min: -50, uke: 10, ik: 1), e => e.Contains("значение должно быть положительным"));
        Assert.Contains(Errors(ParameterKind.H21E, min: 50, max: 0, uke: 10, ik: 1), e => e.Contains("значение должно быть положительным"));
    }

    [Fact]
    public void EfficiencyAbove100_ProducesError()
    {
        Assert.Contains(Errors(ParameterKind.CollectorEfficiency, min: 101, freq: 100), e => e.Contains("КПД не может превышать 100%"));
    }

    [Fact]
    public void Frequency_OnlyAllowedForFrequencyKinds()
    {
        Assert.Contains(Errors(ParameterKind.H21E, min: 50, uke: 10, ik: 1, freq: 1), e => e.Contains("частота (freq) допускается только у частотных параметров"));
    }

    [Fact]
    public void Rg_OnlyAllowedForNoiseFigure()
    {
        Assert.Contains(Errors(ParameterKind.H21E, min: 50, uke: 10, ik: 1, rg: 500), e => e.Contains("сопротивление генератора (Rg) допускается только у KShum"));
    }

    [Fact]
    public void Rbe_OnlyAllowedForIkep()
    {
        Assert.Contains(Errors(ParameterKind.CollectorEmitterCutoffCurrent, max: 20, uke: 15, rbe: 100), e => e.Contains("допускается только у Ikep"));
    }

    [Fact]
    public void Ib_OnlyAllowedForSwitchingTimes()
    {
        Assert.Contains(Errors(ParameterKind.H21E, min: 50, uke: 10, ik: 1, ib: 10), e => e.Contains("ток базы (Ib) как условие допускается только у Ton/Toff"));
    }

    [Fact]
    public void NonPositiveConditions_ProduceErrors()
    {
        Assert.Contains(Errors(ParameterKind.H21E, min: 50, uke: 0, ik: 1), e => e.Contains("условие Uкэ должно быть положительным"));
        Assert.Contains(Errors(ParameterKind.SwitchOnTime, max: 500, ik: 0, ib: 50), e => e.Contains("условие Iк должно быть положительным"));
        Assert.Contains(Errors(ParameterKind.NoiseFigure, max: 4, uke: 5, ik: 1, freq: 0), e => e.Contains("частота должна быть положительной"));
        Assert.Contains(Errors(ParameterKind.NoiseFigure, max: 4, uke: 5, ik: 1, freq: 1.8, rg: -500), e => e.Contains("сопротивление генератора должно быть положительным"));
        Assert.Contains(Errors(ParameterKind.CollectorEmitterCutoffCurrentRbe, max: 5, uke: 10, rbe: 0), e => e.Contains("сопротивление Rбэ должно быть положительным"));
    }
}

public class ElectricalParameterCatalogTests
{
    [Fact]
    public void EveryConditionRule_IsUsedByCatalog()
    {
        var usedRules = ElectricalParameterCatalog.All.Values.Select(info => info.Rule).ToHashSet();

        Assert.Equal(Enum.GetValues<ConditionRule>().ToHashSet(), usedRules);
    }

    [Fact]
    public void Codes_AreUnique()
    {
        Assert.Equal(ElectricalParameterCatalog.All.Count, ElectricalParameterCatalog.ByCode.Count);
    }

    [Fact]
    public void TryGetByCode_ResolvesCodes()
    {
        Assert.True(ElectricalParameterCatalog.TryGetByCode("h21e", out var kind));
        Assert.Equal(ParameterKind.H21E, kind);
        Assert.False(ElectricalParameterCatalog.TryGetByCode("h21", out _));
    }

    [Fact]
    public void FrequencyCodesList_MatchesFrequencyKinds()
    {
        var codes = ElectricalParameterCatalog.FrequencyCodesList.Split(", ").ToHashSet();

        Assert.Equal(new HashSet<string> { "KShum", "KUr", "PVyh", "Kpd" }, codes);
    }
}
