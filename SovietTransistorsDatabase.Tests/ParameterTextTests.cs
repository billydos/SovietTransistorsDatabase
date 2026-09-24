using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Tests;

public class ParameterTextTests
{
    [Theory]
    [InlineData(5, "5")]
    [InlineData(1.5, "1.5")]
    [InlineData(0.001, "0.001")]
    [InlineData(-60, "-60")]
    public void Fmt_UsesInvariantCulture(double value, string expected)
    {
        Assert.Equal(expected, ParameterText.Fmt(value));
    }

    [Fact]
    public void Value_MinOnly_WithoutUnit()
    {
        var parameter = new ElectricalParameter { Kind = ParameterKind.H21E, ValueMin = 50, Uke = 10, Ik = 1 };

        Assert.Equal("не менее 50", ParameterText.Value(parameter));
    }

    [Fact]
    public void Value_Range_BothBounds()
    {
        var parameter = new ElectricalParameter { Kind = ParameterKind.H21E, ValueMin = 20, ValueMax = 200, Uke = 10, Ik = 1 };

        Assert.Equal("20...200", ParameterText.Value(parameter));
    }

    [Fact]
    public void Value_MaxOnly_WithUnit()
    {
        var parameter = new ElectricalParameter { Kind = ParameterKind.CollectorCutoffCurrent, ValueMax = 1.2, Ukb = 10 };

        Assert.Equal("не более 1.2 мкА", ParameterText.Value(parameter));
    }

    [Fact]
    public void Conditions_WithoutConditions()
    {
        Assert.Equal("без условий",
            ParameterText.Conditions(new ElectricalParameter { Kind = ParameterKind.H21E, ValueMin = 50 }));
    }

    [Fact]
    public void Conditions_AllSpecified_InCanonicalOrder()
    {
        var parameter = new ElectricalParameter
        {
            Kind = ParameterKind.NoiseFigure,
            ValueMax = 4,
            Uke = 5,
            Ik = 1,
            Freq = 1.8,
            Rg = 500,
            Temp = 25,
        };

        Assert.Equal("при Uкэ = 5 В, Iк = 1 мА, f = 1.8 МГц, Rг = 500 Ом, T = 25 °C",
            ParameterText.Conditions(parameter));
    }
}
