namespace SovietTransistorsDatabase.Domain;

public static class TransistorValidator
{
    public static IReadOnlyList<string> Errors(Transistor transistor)
    {
        var errors = new List<string>();
        if (!Materials.IsValidSymbol(transistor.Material))
            errors.Add($"материал «{transistor.Material}» должен быть Г/1, К/2, А/3 или И/4");
        if (transistor.Subclass is not ('Т' or 'П'))
            errors.Add($"подкласс «{transistor.Subclass}» должен быть Т или П");
        if (transistor.Feature is < 1 or > 9)
            errors.Add($"характерный эксплуатационный признак {transistor.Feature} должен быть от 1 до 9");
        if (transistor.DevelopmentNumber is < 1 or > 999)
            errors.Add($"порядковый номер разработки {transistor.DevelopmentNumber} должен быть от 1 до 999");
        if (!Cyrillic.IsUpperLetters(transistor.Letters))
            errors.Add($"классификация «{transistor.Letters}» должна быть одной или двумя заглавными русскими буквами");
        if (transistor.Modification is < 1 or > 9)
            errors.Add($"модификация {transistor.Modification} должна быть от 1 до 9");
        if (transistor.ChipVariant is < 1 or > 6)
            errors.Add($"бескорпусное исполнение {transistor.ChipVariant} должно быть от 1 до 6");
        return errors;
    }
}
