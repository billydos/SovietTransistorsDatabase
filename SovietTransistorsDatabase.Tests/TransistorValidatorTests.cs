using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Tests;

public class TransistorValidatorTests
{
    private static Transistor Valid() => new()
    {
        Material = 'К',
        Subclass = 'Т',
        Feature = 3,
        DevelopmentNumber = 315,
        Letters = "Б",
    };

    [Fact]
    public void ValidTransistor_NoErrors()
    {
        Assert.Empty(TransistorValidator.Errors(Valid()));
        Assert.Empty(TransistorValidator.Errors(Valid() with { Modification = 2, ChipVariant = 3 }));
    }

    [Theory]
    [InlineData('Х')]
    [InlineData('K')]
    [InlineData('5')]
    public void InvalidMaterial_ProducesError(char material)
    {
        Assert.Contains(TransistorValidator.Errors(Valid() with { Material = material }), e => e.Contains("материал"));
    }

    [Fact]
    public void InvalidSubclass_ProducesError()
    {
        Assert.Contains(TransistorValidator.Errors(Valid() with { Subclass = 'Б' }), e => e.Contains("подкласс"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void InvalidFeature_ProducesError(int feature)
    {
        Assert.Contains(TransistorValidator.Errors(Valid() with { Feature = feature }), e => e.Contains("признак"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    public void InvalidDevelopmentNumber_ProducesError(int number)
    {
        Assert.Contains(TransistorValidator.Errors(Valid() with { DevelopmentNumber = number }), e => e.Contains("номер разработки"));
    }

    [Theory]
    [InlineData("")]
    [InlineData("БВГ")]
    [InlineData("б")]
    public void InvalidLetters_ProduceError(string letters)
    {
        Assert.Contains(TransistorValidator.Errors(Valid() with { Letters = letters }), e => e.Contains("классификация"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(10)]
    public void InvalidModification_ProducesError(int modification)
    {
        Assert.Contains(TransistorValidator.Errors(Valid() with { Modification = modification }), e => e.Contains("модификация"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    public void InvalidChipVariant_ProducesError(int chipVariant)
    {
        Assert.Contains(TransistorValidator.Errors(Valid() with { ChipVariant = chipVariant }), e => e.Contains("бескорпусное"));
    }
}
