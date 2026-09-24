using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Tests;

public class MaximumRatingsValidatorTests
{
    [Fact]
    public void EmptyRatings_NoErrors()
    {
        Assert.Empty(MaximumRatingsValidator.Errors(new MaximumRatings()));
    }

    [Fact]
    public void NegativeTemperatures_Allowed()
    {
        Assert.Empty(MaximumRatingsValidator.Errors(new MaximumRatings { TempMin = -60, TempMax = 25 }));
    }

    [Fact]
    public void NonPositiveLimit_ProducesError()
    {
        Assert.Contains(MaximumRatingsValidator.Errors(new MaximumRatings { UkeMax = 0 }), e => e.Contains("должно быть положительным"));
        Assert.Contains(MaximumRatingsValidator.Errors(new MaximumRatings { IkMax = -100 }), e => e.Contains("должно быть положительным"));
        Assert.Contains(MaximumRatingsValidator.Errors(new MaximumRatings { Rth = 0 }), e => e.Contains("должно быть положительным"));
    }

    [Fact]
    public void TempMinNotBelowTempMax_ProducesError()
    {
        Assert.Contains(MaximumRatingsValidator.Errors(new MaximumRatings { TempMin = 25, TempMax = 25 }), e => e.Contains("должна быть меньше"));
        Assert.Contains(MaximumRatingsValidator.Errors(new MaximumRatings { TempMin = 70, TempMax = 25 }), e => e.Contains("должна быть меньше"));
    }

    [Fact]
    public void PulseValues_RequirePulseDuration()
    {
        Assert.Contains(MaximumRatingsValidator.Errors(new MaximumRatings { IkPulseMax = 500 }), e => e.Contains("длительность импульса (pulseDuration, мкс) обязательна"));
        Assert.Contains(MaximumRatingsValidator.Errors(new MaximumRatings { PkPulseMax = 300 }), e => e.Contains("длительность импульса (pulseDuration, мкс) обязательна"));
        Assert.Empty(MaximumRatingsValidator.Errors(
            new MaximumRatings { IkPulseMax = 500, PkPulseMax = 300, PulseDuration = 100 }));
    }

    [Fact]
    public void PulseDurationWithoutPulseValues_ProducesError()
    {
        Assert.Contains(MaximumRatingsValidator.Errors(new MaximumRatings { PulseDuration = 100 }), e => e.Contains("импульсные ток/мощность не заданы"));
    }
}
