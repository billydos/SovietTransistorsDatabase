using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Tests;

public class TransistorAttributesTests
{
    [Fact]
    public void FieldNames_PinExpectedFields()
    {
        Assert.Equal(new[]
        {
            "Structure", "Technology", "Package", "PackageMaterial", "ColorMarking", "Pinout",
            "EsdSensitive", "MilitaryGrade", "RadiationHardened", "Tu", "Notes",
            "YearFrom", "YearTo", "MassMax", "DatasheetUrl"
        }, TransistorAttributes.FieldNames);
    }

    [Fact]
    public void HasAnyValue_AllFieldsEmpty_IsFalse()
    {
        Assert.False(TransistorAttributes.HasAnyValue(new TransistorAttributes()));
    }

    [Fact]
    public void HasAnyValue_AnySingleFieldSet_IsTrue()
    {
        foreach (var attributes in new[]
        {
            new TransistorAttributes { Structure = "npn" },
            new TransistorAttributes { Technology = "планарная" },
            new TransistorAttributes { Package = "TO-92" },
            new TransistorAttributes { PackageMaterial = "металл" },
            new TransistorAttributes { ColorMarking = "красная точка" },
            new TransistorAttributes { Pinout = "КБЭ" },
            new TransistorAttributes { EsdSensitive = true },
            new TransistorAttributes { MilitaryGrade = false },
            new TransistorAttributes { RadiationHardened = true },
            new TransistorAttributes { Tu = "ТУ 11.365.001-71" },
            new TransistorAttributes { Notes = "примечание" },
            new TransistorAttributes { YearFrom = 1970 },
            new TransistorAttributes { YearTo = 1985 },
            new TransistorAttributes { MassMax = 1.2 },
            new TransistorAttributes { DatasheetUrl = "https://example.com" },
        })
        {
            Assert.True(TransistorAttributes.HasAnyValue(attributes));
        }
    }
}