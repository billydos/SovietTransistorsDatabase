using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Tests;

public class TransistorAttributesValidatorTests
{
    [Fact]
    public void EmptyAttributes_NoErrors()
    {
        Assert.Empty(TransistorAttributesValidator.Errors(new TransistorAttributes()));
    }

    [Fact]
    public void WhitespaceTextFields_ProduceErrors()
    {
        foreach (var attributes in new[]
        {
            new TransistorAttributes { Structure = " " },
            new TransistorAttributes { Technology = " " },
            new TransistorAttributes { Package = " " },
            new TransistorAttributes { PackageMaterial = " " },
            new TransistorAttributes { ColorMarking = " " },
            new TransistorAttributes { Pinout = " " },
            new TransistorAttributes { Tu = " " },
            new TransistorAttributes { Notes = " " },
            new TransistorAttributes { DatasheetUrl = " " },
        })
        {
            Assert.Contains(TransistorAttributesValidator.Errors(attributes), e => e.Contains("пустое значение"));
        }
    }

    [Fact]
    public void Years_InAllowedRange_NoErrors()
    {
        Assert.Empty(TransistorAttributesValidator.Errors(new TransistorAttributes { YearFrom = 1949 }));
        Assert.Empty(TransistorAttributesValidator.Errors(new TransistorAttributes { YearFrom = 1970, YearTo = 1985 }));
        Assert.Empty(TransistorAttributesValidator.Errors(new TransistorAttributes { YearTo = 2100 }));
    }

    [Fact]
    public void Years_OutOfRange_ProduceErrors()
    {
        Assert.Contains(TransistorAttributesValidator.Errors(new TransistorAttributes { YearFrom = 1948 }), e => e.Contains("год начала выпуска"));
        Assert.Contains(TransistorAttributesValidator.Errors(new TransistorAttributes { YearTo = 2101 }), e => e.Contains("год окончания выпуска"));
    }

    [Fact]
    public void YearFrom_MustBeBeforeYearTo()
    {
        Assert.Contains(TransistorAttributesValidator.Errors(new TransistorAttributes { YearFrom = 1970, YearTo = 1970 }), e => e.Contains("должен быть меньше года окончания"));
        Assert.Contains(TransistorAttributesValidator.Errors(new TransistorAttributes { YearFrom = 1980, YearTo = 1970 }), e => e.Contains("должен быть меньше года окончания"));
    }

    [Fact]
    public void MassMustBePositive()
    {
        Assert.Contains(TransistorAttributesValidator.Errors(new TransistorAttributes { MassMax = 0 }), e => e.Contains("масса"));
        Assert.Empty(TransistorAttributesValidator.Errors(new TransistorAttributes { MassMax = 1.2 }));
    }
}
