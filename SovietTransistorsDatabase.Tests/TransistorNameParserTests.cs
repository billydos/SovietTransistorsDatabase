using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Tests;

public class TransistorNameParserTests
{
    public static TheoryData<string, char, char, bool, int, int, string, int?, int?> ValidNames => new()
    {
        { "КТ315Б", 'К', 'Т', false, 3, 15, "Б", null, null },
        { "2Т914А-1", '2', 'Т', false, 9, 14, "А", null, 1 },
        { "ГТ402Ж", 'Г', 'Т', false, 4, 2, "Ж", null, null },
        { "1Т402Ж", '1', 'Т', false, 4, 2, "Ж", null, null },
        { "КП303А", 'К', 'П', false, 3, 3, "А", null, null },
        { "КТ3102АМ", 'К', 'Т', false, 3, 102, "АМ", null, null },
        { "КТ815Г1", 'К', 'Т', false, 8, 15, "Г", 1, null },
        { "ГТС398А", 'Г', 'Т', true, 3, 98, "А", null, null },
        { "АТ312А-5", 'А', 'Т', false, 3, 12, "А", null, 5 },
    };

    [Theory]
    [MemberData(nameof(ValidNames))]
    public void TryParse_ValidDesignation_ParsesFields(
        string text, char material, char subclass, bool isAssembly, int feature, int number, string letters, int? modification, int? chipVariant)
    {
        bool ok = TransistorNameParser.TryParse(text, out var transistor, out var error);

        Assert.True(ok, error);
        Assert.Equal(material, transistor!.Material);
        Assert.Equal(subclass, transistor.Subclass);
        Assert.Equal(isAssembly, transistor.IsAssembly);
        Assert.Equal(feature, transistor.Feature);
        Assert.Equal(number, transistor.DevelopmentNumber);
        Assert.Equal(letters, transistor.Letters);
        Assert.Equal(modification, transistor.Modification);
        Assert.Equal(chipVariant, transistor.ChipVariant);
    }

    [Theory]
    [InlineData("КТ315Б", "КТ315Б")]
    [InlineData("кт315б", "КТ315Б")]
    [InlineData("  КТ315Б  ", "КТ315Б")]
    [InlineData("2Т914А–1", "2Т914А-1")]
    [InlineData("2Т914А−1", "2Т914А-1")]
    [InlineData("КП303А", "КП303А")]
    [InlineData("ГТ402Ж", "ГТ402Ж")]
    [InlineData("ГТС398А", "ГТС398А")]
    [InlineData("КТ815Г1", "КТ815Г1")]
    public void Parse_RoundTrip_NameEqualsNormalizedDesignation(string text, string expected)
    {
        var transistor = TransistorNameParser.Parse(text);

        Assert.Equal(expected, transistor.Name);
    }

    public static TheoryData<string, string> InvalidNames => new()
    {
        { "", "обозначение пустое" },
        { "   ", "обозначение пустое" },
        { "5Т315Б", "позиция 1: ожидался тип материала" },
        { "ХТ315Б", "позиция 1: ожидался тип материала" },
        { "KT315Б", "позиция 1: ожидался тип материала" },
        { "КМ315Б", "позиция 2: ожидался подкласс" },
        { "КТ015Б", "позиция 3: ожидался характерный эксплуатационный признак" },
        { "КТ3АБ", "порядковый номер разработки — от 2 до 3 цифр" },
        { "КТ12345Б", "от 2 до 3 цифр" },
        { "КТ1015Б", "номер разработки из трёх цифр не может начинаться с нуля" },
        { "КТ100Б", "номер разработки не может быть нулём" },
        { "КТ315", "отсутствует буква классификации по параметрам" },
        { "КТ315-2", "ожидалась буква классификации по параметрам" },
        { "КТ315БВГ", "не более двух букв классификации по параметрам" },
        { "КТ315Б0", "модификация — цифра от 1 до 9" },
        { "КТ315Б:", "неожидаемый символ" },
        { "КТ315Б-0", "бескорпусное исполнение — дефис и цифра от 1 до 6" },
        { "КТ315Б-7", "бескорпусное исполнение — дефис и цифра от 1 до 6" },
        { "КТ315Б-", "бескорпусное исполнение — дефис и цифра от 1 до 6" },
        { "КТ315Б-1В", "лишние символы после бескорпусного исполнения" },
    };

    [Theory]
    [MemberData(nameof(InvalidNames))]
    public void TryParse_InvalidDesignation_ReturnsFalseWithMessage(string text, string fragment)
    {
        bool ok = TransistorNameParser.TryParse(text, out var transistor, out var error);

        Assert.False(ok);
        Assert.Null(transistor);
        Assert.NotEmpty(error);
        Assert.Contains(fragment, error);
    }

    [Fact]
    public void TryParse_Null_ReturnsEmptyDesignationError()
    {
        bool ok = TransistorNameParser.TryParse(null, out var transistor, out var error);

        Assert.False(ok);
        Assert.Null(transistor);
        Assert.Equal("обозначение пустое", error);
    }

    [Fact]
    public void Parse_InvalidDesignation_ThrowsFormatException()
    {
        Assert.Throws<FormatException>(() => TransistorNameParser.Parse("ХТ315Б"));
    }
}
