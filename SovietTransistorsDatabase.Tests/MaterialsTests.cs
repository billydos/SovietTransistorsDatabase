using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Tests;

public class MaterialsTests
{
    public static TheoryData<char, SemiconductorMaterial> EquivalentSymbols => new()
    {
        { 'Г', SemiconductorMaterial.Germanium },
        { '1', SemiconductorMaterial.Germanium },
        { 'К', SemiconductorMaterial.Silicon },
        { '2', SemiconductorMaterial.Silicon },
        { 'А', SemiconductorMaterial.GalliumArsenide },
        { '3', SemiconductorMaterial.GalliumArsenide },
        { 'И', SemiconductorMaterial.Indium },
        { '4', SemiconductorMaterial.Indium },
    };

    [Theory]
    [MemberData(nameof(EquivalentSymbols))]
    public void EquivalentSymbols_MapToSameKind(char symbol, SemiconductorMaterial expected)
    {
        Assert.True(Materials.IsValidSymbol(symbol));
        Assert.True(Materials.TryGetKind(symbol, out var kind));
        Assert.Equal(expected, kind);
        Assert.Equal(expected, Materials.KindOf(symbol));
    }

    [Fact]
    public void SymbolsOf_LetterAndDigitMapBackToSameKind()
    {
        foreach (SemiconductorMaterial kind in Enum.GetValues<SemiconductorMaterial>())
        {
            var (letter, digit) = Materials.SymbolsOf(kind);

            Assert.Equal(kind, Materials.KindOf(letter));
            Assert.Equal(kind, Materials.KindOf(digit));
        }
    }

    [Theory]
    [InlineData('5')]
    [InlineData('0')]
    [InlineData('В')]
    [InlineData('K')]
    public void InvalidSymbols_AreRejected(char symbol)
    {
        Assert.False(Materials.IsValidSymbol(symbol));
        Assert.False(Materials.TryGetKind(symbol, out _));
        Assert.Throws<ArgumentException>(() => Materials.KindOf(symbol));
    }

    [Fact]
    public void DisplayName_IncludesBothEquivalentSymbols()
    {
        Assert.Equal("германий", Materials.ShortName(SemiconductorMaterial.Germanium));
        Assert.Equal("кремний (К/2)", Materials.DisplayName(SemiconductorMaterial.Silicon));
    }
}

public class CyrillicTests
{
    [Theory]
    [InlineData('А')]
    [InlineData('Я')]
    [InlineData('Э')]
    [InlineData('Ё')]
    public void IsUpperLetter_AcceptsUppercaseRussian(char c)
    {
        Assert.True(Cyrillic.IsUpperLetter(c));
    }

    [Theory]
    [InlineData('а')]
    [InlineData('ё')]
    [InlineData('я')]
    [InlineData('A')]
    [InlineData('0')]
    [InlineData('-')]
    public void IsUpperLetter_RejectsEverythingElse(char c)
    {
        Assert.False(Cyrillic.IsUpperLetter(c));
    }

    [Theory]
    [InlineData("Б")]
    [InlineData("БВ")]
    [InlineData("Ё")]
    public void IsUpperLetters_AcceptsOneOrTwo(string s)
    {
        Assert.True(Cyrillic.IsUpperLetters(s));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("БВГ")]
    [InlineData("б")]
    [InlineData("A")]
    public void IsUpperLetters_RejectsOtherLengthsAndAlphabets(string? s)
    {
        Assert.False(Cyrillic.IsUpperLetters(s));
    }
}
