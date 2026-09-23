using System.Text.Json;
using SovietTransistorsDatabase.Domain;

namespace SovietTransistorsDatabase.Import;

public sealed record JsoncIssue(int EntryIndex, string Description, string? Source);

/// <summary>Запись справочника: обозначение + необязательные атрибуты, параметры, предельные данные.</summary>
public sealed class TransistorEntryData
{
    public required Transistor Transistor { get; init; }
    public TransistorAttributes? Attributes { get; init; }
    public List<string>? Manufacturers { get; init; }
    public List<ElectricalParameter>? Parameters { get; init; }
    public MaximumRatings? Ratings { get; init; }
}

public sealed class JsoncParseResult
{
    public List<TransistorEntryData> Entries { get; } = new();
    public List<JsoncIssue> Issues { get; } = new();
    public bool HasErrors => Issues.Count > 0;
}

/// <summary>Чтение справочника из jsonc-файла (JSON с комментариями и висячими запятыми).</summary>
public static class TransistorJsoncReader
{
    private static readonly JsonDocumentOptions Options = new()
    {
        CommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private static readonly string[] DesignationFields =
    {
        "material", "subclass", "assembly", "feature", "number", "letters", "modification", "chip",
    };

    private static readonly string[] RequiredDesignationFields =
    {
        "material", "subclass", "feature", "number", "letters",
    };

    private static readonly string[] DetailFields = { "attributes", "parameters", "ratings" };

    private static readonly string[] ParameterFields =
    {
        "parameter", "min", "max", "Uke", "Ukb", "Ueb", "Ik", "Ie", "Ib", "freq", "Rg", "Rbe", "temp",
    };

    private static readonly string[] RatingFields =
    {
        "UkeMax", "UkbMax", "UbeMax", "UkeoMax", "IkMax", "IbMax", "PkMax",
        "IkPulseMax", "PkPulseMax", "pulseDuration", "tempMin", "tempMax", "tempJunctionMax", "Rth",
    };

    private static readonly string[] ExtraFields =
    {
        "structure", "technology", "package", "packageMaterial", "colorMarking", "pinout",
        "esdSensitive", "militaryGrade", "radiationHardened", "tu", "notes",
        "yearFrom", "yearTo", "massMax", "datasheetUrl",
    };

    public static JsoncParseResult ParseFile(string path)
    {
        return ParseText(File.ReadAllText(path));
    }

    public static JsoncParseResult ParseText(string text)
    {
        var result = new JsoncParseResult();

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(text, Options);
        }
        catch (JsonException ex)
        {
            throw new FormatException($"файл не является корректным JSONC: {ex.Message}");
        }

        using (document)
        {
            JsonElement root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                throw new FormatException("корневой элемент должен быть объектом вида { \"transistors\": [ … ] }");
            }
            foreach (JsonProperty property in root.EnumerateObject())
            {
                if (property.Name != "transistors")
                {
                    result.Issues.Add(new JsoncIssue(0, $"неизвестный ключ корневого объекта «{property.Name}» (допустим только \"transistors\")", null));
                }
            }
            if (!root.TryGetProperty("transistors", out JsonElement array))
            {
                throw new FormatException("отсутствует обязательный ключ \"transistors\"");
            }
            if (array.ValueKind != JsonValueKind.Array)
            {
                throw new FormatException("\"transistors\" должен быть массивом");
            }

            int index = 0;
            foreach (JsonElement entry in array.EnumerateArray())
            {
                index++;
                ReadEntry(entry, index, result);
            }
        }
        return result;
    }

    private static void ReadEntry(JsonElement entry, int index, JsoncParseResult result)
    {
        if (entry.ValueKind == JsonValueKind.String)
        {
            Transistor? fromString = TryReadName(entry.GetString()!, index, result);
            if (fromString is not null)
            {
                result.Entries.Add(new TransistorEntryData { Transistor = fromString });
            }
            return;
        }
        if (entry.ValueKind != JsonValueKind.Object)
        {
            result.Issues.Add(new JsoncIssue(index, $"запись должна быть строкой (обозначение) или объектом, получено: {DescribeKind(entry.ValueKind)}", null));
            return;
        }

        Transistor? transistor;
        if (entry.TryGetProperty("name", out JsonElement nameProperty))
        {
            string[] extra = entry.EnumerateObject().Select(p => p.Name)
                .Where(n => n != "name" && !DetailFields.Contains(n)).ToArray();
            if (extra.Length > 0)
            {
                result.Issues.Add(new JsoncIssue(index, $"при использовании \"name\" допустимы только {string.Join(", ", DetailFields)}, получено: {string.Join(", ", extra)}", null));
                return;
            }
            if (nameProperty.ValueKind != JsonValueKind.String)
            {
                result.Issues.Add(new JsoncIssue(index, "\"name\" должно быть строкой с обозначением транзистора", null));
                return;
            }
            transistor = TryReadName(nameProperty.GetString()!, index, result);
            if (transistor is null)
            {
                return;
            }
        }
        else
        {
            transistor = ReadDesignationFields(entry, index, result);
            if (transistor is null)
            {
                return;
            }
        }

        TransistorAttributes? attributes = null;
        List<string>? manufacturers = null;
        if (entry.TryGetProperty("attributes", out JsonElement attributesElement))
        {
            (attributes, manufacturers) = ReadAttributes(attributesElement, index, result);
        }

        List<ElectricalParameter>? parameters = ReadParameters(entry, index, result);
        MaximumRatings? ratings = ReadRatings(entry, index, result);

        result.Entries.Add(new TransistorEntryData
        {
            Transistor = transistor,
            Attributes = attributes,
            Manufacturers = manufacturers,
            Parameters = parameters,
            Ratings = ratings,
        });
    }

    private static Transistor? TryReadName(string name, int index, JsoncParseResult result)
    {
        if (TransistorNameParser.TryParse(name, out var parsed, out string error))
        {
            return parsed;
        }
        result.Issues.Add(new JsoncIssue(index, error, name));
        return null;
    }

    private static Transistor? ReadDesignationFields(JsonElement entry, int index, JsoncParseResult result)
    {
        var problems = new List<string>();

        foreach (JsonProperty property in entry.EnumerateObject())
        {
            if (!DesignationFields.Contains(property.Name) && !DetailFields.Contains(property.Name))
            {
                problems.Add($"неизвестное поле «{property.Name}» (допустимы: {string.Join(", ", DesignationFields.Concat(DetailFields))})");
            }
        }
        foreach (string required in RequiredDesignationFields)
        {
            if (!entry.TryGetProperty(required, out _))
            {
                problems.Add($"отсутствует обязательное поле \"{required}\"");
            }
        }

        char material = ReadCharField(entry, "material", Materials.IsValidSymbol, "Г|1, К|2, А|3 или И|4", problems);
        char subclass = ReadCharField(entry, "subclass", c => c is 'Т' or 'П', "Т или П", problems);
        bool assembly = ReadBoolField(entry, "assembly", problems);
        int feature = ReadIntField(entry, "feature", v => v is >= 1 and <= 9, "цифра от 1 до 9", problems);
        int number = ReadIntField(entry, "number", v => v is >= 1 and <= 999, "число от 1 до 999", problems);
        string letters = ReadLettersField(entry, problems);
        int? modification = ReadOptionalIntField(entry, "modification", v => v is >= 1 and <= 9, "цифра от 1 до 9", problems);
        int? chip = ReadOptionalIntField(entry, "chip", v => v is >= 1 and <= 6, "цифра от 1 до 6", problems);

        if (problems.Count > 0)
        {
            result.Issues.Add(new JsoncIssue(index, string.Join("; ", problems), null));
            return null;
        }

        return new Transistor
        {
            Material = material,
            Subclass = subclass,
            IsAssembly = assembly,
            Feature = feature,
            DevelopmentNumber = number,
            Letters = letters,
            Modification = modification,
            ChipVariant = chip,
        };
    }

    private static (TransistorAttributes?, List<string>?) ReadAttributes(JsonElement element, int index, JsoncParseResult result)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            result.Issues.Add(new JsoncIssue(index, "\"attributes\" должно быть объектом", null));
            return (null, null);
        }
        var problems = new List<string>();
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!ExtraFields.Contains(property.Name) && property.Name != "manufacturers")
            {
                problems.Add($"неизвестное поле «{property.Name}» (допустимы: {string.Join(", ", ExtraFields)}, manufacturers)");
            }
        }

        var attributes = new TransistorAttributes
        {
            Structure = ReadOptionalTrimmedString(element, "structure", problems),
            Technology = ReadOptionalTrimmedString(element, "technology", problems),
            Package = ReadOptionalTrimmedString(element, "package", problems),
            PackageMaterial = ReadOptionalTrimmedString(element, "packageMaterial", problems),
            ColorMarking = ReadOptionalTrimmedString(element, "colorMarking", problems),
            Pinout = ReadOptionalTrimmedString(element, "pinout", problems),
            EsdSensitive = ReadOptionalBool(element, "esdSensitive", problems),
            MilitaryGrade = ReadOptionalBool(element, "militaryGrade", problems),
            RadiationHardened = ReadOptionalBool(element, "radiationHardened", problems),
            Tu = ReadOptionalTrimmedString(element, "tu", problems),
            Notes = ReadOptionalTrimmedString(element, "notes", problems),
            YearFrom = ReadOptionalInt(element, "yearFrom", problems),
            YearTo = ReadOptionalInt(element, "yearTo", problems),
            MassMax = ReadOptionalNumber(element, "massMax", problems),
            DatasheetUrl = ReadOptionalTrimmedString(element, "datasheetUrl", problems),
        };

        List<string>? manufacturers = null;
        if (element.TryGetProperty("manufacturers", out JsonElement manufacturersElement))
        {
            if (manufacturersElement.ValueKind == JsonValueKind.Null)
            {
                // null — список производителей не меняется
            }
            else if (manufacturersElement.ValueKind != JsonValueKind.Array)
            {
                problems.Add("\"manufacturers\" должно быть массивом строк");
            }
            else
            {
                manufacturers = new List<string>();
                int number = 0;
                foreach (JsonElement item in manufacturersElement.EnumerateArray())
                {
                    number++;
                    if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
                    {
                        problems.Add($"\"manufacturers\" №{number}: ожидалось непустое название производителя");
                    }
                    else
                    {
                        manufacturers.Add(item.GetString()!.Trim());
                    }
                }
            }
        }

        problems.AddRange(TransistorAttributesValidator.Errors(attributes));
        if (problems.Count > 0)
        {
            result.Issues.Add(new JsoncIssue(index, $"атрибуты: {string.Join("; ", problems)}", null));
            return (null, null);
        }
        return (attributes, manufacturers);
    }

    private static List<ElectricalParameter>? ReadParameters(JsonElement entry, int index, JsoncParseResult result)
    {
        if (!entry.TryGetProperty("parameters", out JsonElement array)) return null;
        if (array.ValueKind != JsonValueKind.Array)
        {
            result.Issues.Add(new JsoncIssue(index, "\"parameters\" должно быть массивом объектов", null));
            return null;
        }
        var parameters = new List<ElectricalParameter>();
        int number = 0;
        bool hasErrors = false;
        foreach (JsonElement item in array.EnumerateArray())
        {
            number++;
            if (ReadParameterItem(item, index, number, result) is { } parameter)
            {
                parameters.Add(parameter);
            }
            else
            {
                hasErrors = true;
            }
        }
        // при ошибках секция не применяется целиком, чтобы не затереть корректные данные частичным списком
        return hasErrors ? null : parameters;
    }

    private static ElectricalParameter? ReadParameterItem(JsonElement item, int entryIndex, int parameterNumber, JsoncParseResult result)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            result.Issues.Add(new JsoncIssue(entryIndex, $"параметр №{parameterNumber}: должен быть объектом", null));
            return null;
        }

        var problems = new List<string>();
        foreach (JsonProperty property in item.EnumerateObject())
        {
            if (!ParameterFields.Contains(property.Name))
            {
                problems.Add($"неизвестное поле «{property.Name}» (допустимы: {string.Join(", ", ParameterFields)})");
            }
        }

        string? code = null;
        if (item.TryGetProperty("parameter", out JsonElement kindElement) && kindElement.ValueKind == JsonValueKind.String)
        {
            code = kindElement.GetString()!.Trim();
            if (!ElectricalParameterCatalog.TryGetByCode(code, out _))
            {
                problems.Add($"\"parameter\": неизвестный код «{code}» (допустимы: {ElectricalParameterCatalog.CodesList})");
                code = null;
            }
        }
        else
        {
            problems.Add($"обязательное поле \"parameter\" — код параметра ({ElectricalParameterCatalog.CodesList})");
        }

        double? min = ReadOptionalNumber(item, "min", problems);
        double? max = ReadOptionalNumber(item, "max", problems);
        double? uke = ReadOptionalNumber(item, "Uke", problems);
        double? ukb = ReadOptionalNumber(item, "Ukb", problems);
        double? ueb = ReadOptionalNumber(item, "Ueb", problems);
        double? ik = ReadOptionalNumber(item, "Ik", problems);
        double? ie = ReadOptionalNumber(item, "Ie", problems);
        double? ib = ReadOptionalNumber(item, "Ib", problems);
        double? f = ReadOptionalNumber(item, "freq", problems);
        double? rg = ReadOptionalNumber(item, "Rg", problems);
        double? rbe = ReadOptionalNumber(item, "Rbe", problems);
        double? temp = ReadOptionalNumber(item, "temp", problems);

        if (code is null || problems.Count > 0)
        {
            result.Issues.Add(new JsoncIssue(entryIndex, $"параметр №{parameterNumber}: {string.Join("; ", problems)}", null));
            return null;
        }

        ElectricalParameterCatalog.TryGetByCode(code, out ParameterKind kind);
        var parameter = new ElectricalParameter
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
            Freq = f,
            Rg = rg,
            Rbe = rbe,
            Temp = temp,
        };
        var errors = ElectricalParameterValidator.Errors(parameter);
        if (errors.Count > 0)
        {
            result.Issues.Add(new JsoncIssue(entryIndex, $"параметр №{parameterNumber} ({code}): {string.Join("; ", errors)}", null));
            return null;
        }
        return parameter;
    }

    private static MaximumRatings? ReadRatings(JsonElement entry, int index, JsoncParseResult result)
    {
        if (!entry.TryGetProperty("ratings", out JsonElement element)) return null;
        if (element.ValueKind != JsonValueKind.Object)
        {
            result.Issues.Add(new JsoncIssue(index, "\"ratings\" должно быть объектом", null));
            return null;
        }
        var problems = new List<string>();
        foreach (JsonProperty property in element.EnumerateObject())
        {
            if (!RatingFields.Contains(property.Name))
            {
                problems.Add($"неизвестное поле «{property.Name}» (допустимы: {string.Join(", ", RatingFields)})");
            }
        }
        var ratings = new MaximumRatings
        {
            UkeMax = ReadOptionalNumber(element, "UkeMax", problems),
            UkbMax = ReadOptionalNumber(element, "UkbMax", problems),
            UbeMax = ReadOptionalNumber(element, "UbeMax", problems),
            UkeoMax = ReadOptionalNumber(element, "UkeoMax", problems),
            IkMax = ReadOptionalNumber(element, "IkMax", problems),
            IbMax = ReadOptionalNumber(element, "IbMax", problems),
            PkMax = ReadOptionalNumber(element, "PkMax", problems),
            IkPulseMax = ReadOptionalNumber(element, "IkPulseMax", problems),
            PkPulseMax = ReadOptionalNumber(element, "PkPulseMax", problems),
            PulseDuration = ReadOptionalNumber(element, "pulseDuration", problems),
            TempMin = ReadOptionalNumber(element, "tempMin", problems),
            TempMax = ReadOptionalNumber(element, "tempMax", problems),
            TempJunctionMax = ReadOptionalNumber(element, "tempJunctionMax", problems),
            Rth = ReadOptionalNumber(element, "Rth", problems),
        };
        problems.AddRange(MaximumRatingsValidator.Errors(ratings));
        if (problems.Count > 0)
        {
            result.Issues.Add(new JsoncIssue(index, $"предельные данные: {string.Join("; ", problems)}", null));
            return null;
        }
        return ratings;
    }

    private static string? ReadOptionalTrimmedString(JsonElement element, string field, List<string> problems)
    {
        if (!element.TryGetProperty(field, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (value.ValueKind != JsonValueKind.String)
        {
            problems.Add($"\"{field}\" должно быть строкой");
            return null;
        }
        return value.GetString()!.Trim();
    }

    private static bool? ReadOptionalBool(JsonElement element, string field, List<string> problems)
    {
        if (!element.TryGetProperty(field, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (value.ValueKind == JsonValueKind.True) return true;
        if (value.ValueKind == JsonValueKind.False) return false;
        problems.Add($"\"{field}\" должно быть true или false");
        return null;
    }

    private static int? ReadOptionalInt(JsonElement element, string field, List<string> problems)
    {
        if (!element.TryGetProperty(field, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out int number))
        {
            problems.Add($"\"{field}\" должно быть целым числом");
            return null;
        }
        return number;
    }

    private static double? ReadOptionalNumber(JsonElement element, string field, List<string> problems)
    {
        if (!element.TryGetProperty(field, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double number))
        {
            problems.Add($"\"{field}\" должно быть числом");
            return null;
        }
        return number;
    }

    private static char ReadCharField(JsonElement entry, string field, Func<char, bool> isValid, string expected, List<string> problems)
    {
        if (!entry.TryGetProperty(field, out JsonElement element))
        {
            return '\0';
        }
        string? value = element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        if (value is null || value.Length != 1 || !isValid(value[0]))
        {
            problems.Add($"\"{field}\": ожидалось {expected}, получено «{value ?? "не строка"}»");
            return '\0';
        }
        return value[0];
    }

    private static bool ReadBoolField(JsonElement entry, string field, List<string> problems)
    {
        if (!entry.TryGetProperty(field, out JsonElement element) || element.ValueKind == JsonValueKind.Null)
        {
            return false;
        }
        if (element.ValueKind == JsonValueKind.True) return true;
        if (element.ValueKind == JsonValueKind.False) return false;
        problems.Add($"\"{field}\" должно быть true или false");
        return false;
    }

    private static int ReadIntField(JsonElement entry, string field, Func<int, bool> isValid, string expected, List<string> problems)
    {
        if (!entry.TryGetProperty(field, out JsonElement element))
        {
            return 0;
        }
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out int value))
        {
            problems.Add($"\"{field}\" должно быть целым числом ({expected})");
            return 0;
        }
        if (!isValid(value))
        {
            problems.Add($"\"{field}\": {expected}, получено {value}");
            return 0;
        }
        return value;
    }

    private static int? ReadOptionalIntField(JsonElement entry, string field, Func<int, bool> isValid, string expected, List<string> problems)
    {
        if (!entry.TryGetProperty(field, out JsonElement element) || element.ValueKind == JsonValueKind.Null)
        {
            return null;
        }
        if (element.ValueKind != JsonValueKind.Number || !element.TryGetInt32(out int value))
        {
            problems.Add($"\"{field}\" должно быть целым числом или null ({expected})");
            return null;
        }
        if (!isValid(value))
        {
            problems.Add($"\"{field}\": {expected}, получено {value}");
            return null;
        }
        return value;
    }

    private static string ReadLettersField(JsonElement entry, List<string> problems)
    {
        if (!entry.TryGetProperty("letters", out JsonElement element))
        {
            return "";
        }
        string? value = element.ValueKind == JsonValueKind.String ? element.GetString() : null;
        if (value is null || !Cyrillic.IsUpperLetters(value))
        {
            problems.Add($"\"letters\": одна или две заглавные русские буквы, получено «{value ?? "не строка"}»");
            return "";
        }
        return value;
    }

    private static string DescribeKind(JsonValueKind kind) => kind switch
    {
        JsonValueKind.Number => "число",
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        JsonValueKind.Array => "массив",
        _ => kind.ToString(),
    };
}
