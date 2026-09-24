using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Tests;

public class ElectricalParameterValidatorTests
{
    private static readonly IReadOnlyDictionary<ConditionKey, double> SampleValues = new Dictionary<ConditionKey, double>
    {
        [ConditionKey.Uke] = 10,
        [ConditionKey.Ukb] = 5,
        [ConditionKey.Ueb] = 5,
        [ConditionKey.Ik] = 1,
        [ConditionKey.Ie] = 10,
        [ConditionKey.Ib] = 50,
        [ConditionKey.Freq] = 1.8,
        [ConditionKey.Rg] = 500,
        [ConditionKey.Rbe] = 100,
    };

    public static TheoryData<ParameterKind, int> KindVariantPairs
    {
        get
        {
            var data = new TheoryData<ParameterKind, int>();
            foreach (ParameterInfo info in ElectricalParameterCatalog.All.Values)
                for (int index = 0; index < info.Conditions.Variants.Count; index++)
                    data.Add(info.Kind, index);
            return data;
        }
    }

    /// <summary>Параметр с границей по правилу вида (min/max) и условиями из обязательных ключей варианта (без одного ключа).</summary>
    private static ElectricalParameter FromVariant(ParameterKind kind, ConditionVariant variant, ConditionKey? except = null)
    {
        ElectricalParameter parameter = WithBounds(kind, new ElectricalParameter { Kind = kind });
        foreach (ConditionKey key in variant.Required)
            if (key != except)
                parameter = ConditionKeys.WithValue(parameter, key, SampleValues[key]);
        return parameter;
    }

    private static ElectricalParameter WithBounds(ParameterKind kind, ElectricalParameter parameter)
    {
        ParameterInfo info = ElectricalParameterCatalog.Info(kind);
        return info.Direction switch
        {
            BoundDirection.AtLeast => parameter with { ValueMin = 50 },
            BoundDirection.AtMost => parameter with { ValueMax = 1 },
            BoundDirection.AtLeastOrRange => parameter with { ValueMin = 50 },
            _ => parameter,
        };
    }

    private static bool SatisfiedByOtherVariant(ConditionSpec spec, ElectricalParameter parameter, int exceptIndex) =>
        spec.Variants.Where((_, index) => index != exceptIndex).Any(variant => variant.IsSatisfiedBy(parameter));

    [Theory]
    [MemberData(nameof(KindVariantPairs))]
    public void RequiredKeysOnly_NoErrors(ParameterKind kind, int variantIndex)
    {
        ConditionVariant variant = ElectricalParameterCatalog.Info(kind).Conditions.Variants[variantIndex];

        Assert.Empty(ElectricalParameterValidator.Errors(FromVariant(kind, variant)));
    }

    [Theory]
    [MemberData(nameof(KindVariantPairs))]
    public void MissingRequiredKey_ProducesConditionError(ParameterKind kind, int variantIndex)
    {
        ConditionSpec spec = ElectricalParameterCatalog.Info(kind).Conditions;
        ConditionVariant variant = spec.Variants[variantIndex];

        foreach (ConditionKey required in variant.Required)
        {
            ElectricalParameter parameter = FromVariant(kind, variant, except: required);
            IReadOnlyList<string> errors = ElectricalParameterValidator.Errors(parameter);
            if (!SatisfiedByOtherVariant(spec, parameter, variantIndex))
                Assert.Contains(errors, error => error.Contains("условия —"));
        }
    }

    [Theory]
    [MemberData(nameof(KindVariantPairs))]
    public void ForbiddenKey_ProducesConditionError(ParameterKind kind, int variantIndex)
    {
        ConditionSpec spec = ElectricalParameterCatalog.Info(kind).Conditions;
        ConditionVariant variant = spec.Variants[variantIndex];

        foreach (ConditionKey forbidden in ConditionKeys.All
                     .Where(key => !variant.Required.Contains(key) && !variant.Optional.Contains(key)))
        {
            ElectricalParameter parameter = ConditionKeys.WithValue(FromVariant(kind, variant), forbidden, SampleValues[forbidden]);
            IReadOnlyList<string> errors = ElectricalParameterValidator.Errors(parameter);
            if (!SatisfiedByOtherVariant(spec, parameter, variantIndex))
                Assert.Contains(errors, error => error.Contains("условия —"));
        }
    }

    [Theory]
    [MemberData(nameof(KindVariantPairs))]
    public void OptionalKey_AddedToRequiredSet_NoErrors(ParameterKind kind, int variantIndex)
    {
        ConditionVariant variant = ElectricalParameterCatalog.Info(kind).Conditions.Variants[variantIndex];

        foreach (ConditionKey optional in variant.Optional)
        {
            ElectricalParameter parameter = ConditionKeys.WithValue(FromVariant(kind, variant), optional, SampleValues[optional]);
            Assert.Empty(ElectricalParameterValidator.Errors(parameter));
        }
    }

    [Theory]
    [MemberData(nameof(KindVariantPairs))]
    public void Temp_IsAllowedWithAnyConditions(ParameterKind kind, int variantIndex)
    {
        ConditionVariant variant = ElectricalParameterCatalog.Info(kind).Conditions.Variants[variantIndex];

        ElectricalParameter parameter = FromVariant(kind, variant) with { Temp = 25 };
        Assert.Empty(ElectricalParameterValidator.Errors(parameter));
    }

    [Fact]
    public void ConditionViolation_ProducesSingleMessage()
    {
        var errors = ElectricalParameterValidator.Errors(new ElectricalParameter
        {
            Kind = ParameterKind.H21E,
            ValueMin = 50,
            Ib = 10,
        });

        Assert.Equal(1, errors.Count(error => error.Contains("условия —")));
        Assert.Contains(errors, error => error.Contains("задано: Iб (Ib)"));
    }

    [Fact]
    public void Describe_JoinsVariantsWithEither()
    {
        Assert.Equal("Uкэ (Uke) + Iк (Ik) либо Uкб (Ukb) + Iэ (Ie)", ConditionSpecs.PairUkeIkOrUkbIe.Describe());
        Assert.Equal("Iк (Ik) либо Iэ (Ie)", ConditionSpecs.ExactlyOneCurrent.Describe());
        Assert.Equal("Uкэ (Uke)", ConditionSpecs.OnlyUke.Describe());
    }

    [Fact]
    public void AtLeastParameter_RequiresMinAndForbidsMax()
    {
        Assert.Contains(ElectricalParameterValidator.Errors(new ElectricalParameter { Kind = ParameterKind.CutoffFrequency }),
            e => e.Contains("обязательно значение «не менее» (min)"));
        Assert.Contains(ElectricalParameterValidator.Errors(new ElectricalParameter
        {
            Kind = ParameterKind.CutoffFrequency,
            ValueMin = 5,
            ValueMax = 10,
            Uke = 10,
            Ik = 1,
        }), e => e.Contains("верхняя граница (max) не допускается"));
    }

    [Fact]
    public void AtMostParameter_RequiresMaxAndForbidsMin()
    {
        Assert.Contains(ElectricalParameterValidator.Errors(new ElectricalParameter { Kind = ParameterKind.CollectorCutoffCurrent, Ukb = 10 }),
            e => e.Contains("обязательно значение «не более» (max)"));
        Assert.Contains(ElectricalParameterValidator.Errors(new ElectricalParameter
        {
            Kind = ParameterKind.CollectorCutoffCurrent,
            ValueMin = 0.5,
            ValueMax = 1,
            Ukb = 10,
        }), e => e.Contains("нижняя граница (min) не допускается"));
    }

    [Fact]
    public void AtLeastOrRangeParameter_RequiresMin_MaxOptional()
    {
        Assert.Contains(ElectricalParameterValidator.Errors(new ElectricalParameter { Kind = ParameterKind.H21E, Uke = 10, Ik = 1 }),
            e => e.Contains("обязательна нижняя граница (min)"));
        Assert.Empty(ElectricalParameterValidator.Errors(new ElectricalParameter
        {
            Kind = ParameterKind.H21E,
            ValueMin = 50,
            ValueMax = 200,
            Uke = 10,
            Ik = 1,
        }));
    }

    [Fact]
    public void RangeWithMinAboveMax_ProducesError()
    {
        Assert.Contains(ElectricalParameterValidator.Errors(new ElectricalParameter
        {
            Kind = ParameterKind.H21E,
            ValueMin = 200,
            ValueMax = 50,
            Uke = 10,
            Ik = 1,
        }), e => e.Contains("нижняя граница больше верхней"));
    }

    [Fact]
    public void NonPositiveValues_ProduceErrors()
    {
        Assert.Contains(ElectricalParameterValidator.Errors(new ElectricalParameter
        {
            Kind = ParameterKind.H21E,
            ValueMin = -50,
            Uke = 10,
            Ik = 1,
        }), e => e.Contains("значение должно быть положительным"));
        Assert.Contains(ElectricalParameterValidator.Errors(new ElectricalParameter
        {
            Kind = ParameterKind.H21E,
            ValueMin = 50,
            ValueMax = 0,
            Uke = 10,
            Ik = 1,
        }), e => e.Contains("значение должно быть положительным"));
    }

    [Fact]
    public void NonPositiveConditions_ProduceErrorsForEachKey()
    {
        foreach (ConditionKey key in ConditionKeys.All)
        {
            foreach (ParameterInfo info in ElectricalParameterCatalog.All.Values)
            {
                ConditionVariant? variant = info.Conditions.Variants.FirstOrDefault(v => v.Required.Contains(key) || v.Optional.Contains(key));
                if (variant is null) continue;

                ElectricalParameter parameter = ConditionKeys.WithValue(FromVariant(info.Kind, variant), key, 0);
                Assert.Contains(ElectricalParameterValidator.Errors(parameter),
                    e => e.Contains(ConditionKeys.NonPositiveMessage(key)));
            }
        }
    }

    [Fact]
    public void EfficiencyAboveCeiling_ProducesError()
    {
        Assert.Contains(ElectricalParameterValidator.Errors(new ElectricalParameter
        {
            Kind = ParameterKind.CollectorEfficiency,
            ValueMin = 101,
            Freq = 100,
        }), e => e.Contains("не может превышать 100 %"));
    }

    [Fact]
    public void EfficiencyWithinCeiling_NoErrors()
    {
        Assert.Empty(ElectricalParameterValidator.Errors(new ElectricalParameter
        {
            Kind = ParameterKind.CollectorEfficiency,
            ValueMin = 100,
            Freq = 100,
        }));
    }
}

public class ElectricalParameterCatalogTests
{
    [Fact]
    public void EveryParameter_HasConditionSpecWithVariant()
    {
        foreach (ParameterInfo info in ElectricalParameterCatalog.All.Values)
        {
            Assert.NotNull(info.Conditions);
            Assert.NotEmpty(info.Conditions.Variants);
        }
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
    public void FrequencyKinds_RequireFrequencyInEveryVariant()
    {
        var frequencyKinds = new[] { ParameterKind.NoiseFigure, ParameterKind.OutputPower, ParameterKind.PowerGain, ParameterKind.CollectorEfficiency };

        foreach (ParameterKind kind in frequencyKinds)
            Assert.All(ElectricalParameterCatalog.Info(kind).Conditions.Variants,
                variant => Assert.Contains(ConditionKey.Freq, variant.Required));
    }

    [Fact]
    public void OnlyUkeAndRbe_AreAllowedForIkep()
    {
        ConditionSpec spec = ElectricalParameterCatalog.Info(ParameterKind.CollectorEmitterCutoffCurrentRbe).Conditions;

        Assert.True(spec.Allows(ConditionKey.Uke));
        Assert.True(spec.Allows(ConditionKey.Rbe));
        Assert.DoesNotContain(ConditionKey.Uke, spec.Variants.SelectMany(variant => variant.Optional));
    }
}
