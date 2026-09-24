using System.Globalization;
using System.Text;
using SovietTransistorsDatabase.Data;
using SovietTransistorsDatabase.Domain;
using SovietTransistorsDatabase.Import;

namespace SovietTransistorsDatabase;

internal static class Program
{
    private const string DefaultDatabaseFile = "transistors.db";

    public static int Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; }
        catch { }

        string? command = null;
        var rest = new List<string>();
        string? dbPath = null;
        bool dryRun = false;

        for (int i = 0; i < args.Length; i++)
        {
            string arg = args[i];
            if (arg == "--db")
            {
                if (i + 1 >= args.Length)
                {
                    Console.Error.WriteLine("Ошибка: после --db ожидается путь к файлу базы");
                    return 1;
                }
                dbPath = args[++i];
            }
            else if (arg.StartsWith("--db=", StringComparison.Ordinal))
            {
                dbPath = arg["--db=".Length..];
            }
            else if (arg == "--dry-run")
            {
                dryRun = true;
            }
            else if (command is null)
            {
                command = arg.ToLowerInvariant();
            }
            else
            {
                rest.Add(arg);
            }
        }

        if (command is null)
        {
            PrintHelp();
            return 1;
        }

        try
        {
            return command switch
            {
                "help" or "-h" or "--help" => PrintHelp(),
                "init" => CmdInit(dbPath),
                "parse" => CmdParse(rest),
                "add" => CmdAdd(rest, dbPath, dryRun),
                "import" => CmdImport(rest, dbPath, dryRun),
                "list" => CmdList(rest, dbPath),
                "info" => CmdInfo(rest, dbPath),
                "find" => CmdFind(rest, dbPath),
                "delete" => CmdDelete(rest, dbPath, dryRun),
                "count" => CmdCount(dbPath),
                _ => UnknownCommand(command),
            };
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException)
        {
            Console.Error.WriteLine("Ошибка: " + ex.Message);
            return 1;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("Непредвиденная ошибка: " + ex.Message);
            return 1;
        }
    }

    private static int CmdInit(string? dbPath)
    {
        string path = ResolveDatabasePath(dbPath);
        using ITransistorDatabase db = new SqliteDatabase(path);
        db.EnsureCreated();
        Console.WriteLine($"База данных готова: {Path.GetFullPath(path)}");
        return 0;
    }

    private static int CmdParse(List<string> names)
    {
        if (names.Count == 0)
        {
            Console.Error.WriteLine("Укажите обозначение, например: parse КТ315Б");
            return 1;
        }
        int failures = 0;
        foreach (string name in names)
        {
            if (TransistorNameParser.TryParse(name, out var transistor, out string error))
            {
                PrintTransistor(transistor!);
            }
            else
            {
                failures++;
                Console.Error.WriteLine($"{name}: {error}");
            }
        }
        return failures == 0 ? 0 : 1;
    }

    private static int CmdAdd(List<string> names, string? dbPath, bool dryRun)
    {
        if (names.Count == 0)
        {
            Console.Error.WriteLine("Укажите обозначение, например: add КТ315Б");
            return 1;
        }

        var valid = new List<Transistor>();
        int failures = 0;
        foreach (string name in names)
        {
            if (TransistorNameParser.TryParse(name, out var transistor, out string error))
            {
                valid.Add(transistor!);
            }
            else
            {
                failures++;
                Console.Error.WriteLine($"{name}: {error}");
            }
        }

        if (dryRun)
        {
            foreach (Transistor transistor in valid)
            {
                Console.WriteLine($"(проверка) {transistor.Name} — обозначение корректно");
            }
            return failures == 0 ? 0 : 1;
        }

        int added = 0;
        int duplicates = 0;
        using ITransistorDatabase db = OpenDatabase(dbPath);
        foreach (Transistor transistor in valid)
        {
            switch (db.Add(transistor))
            {
                case InsertOutcome.Added:
                    added++;
                    Console.WriteLine($"Добавлено: {transistor.Name}");
                    break;
                case InsertOutcome.DuplicateExists:
                    duplicates++;
                    Console.WriteLine($"Пропущено (уже есть): {transistor.Name}");
                    break;
            }
        }
        Console.WriteLine($"Итого: добавлено {added}, пропущено {duplicates}, ошибок разбора {failures}");
        return failures == 0 ? 0 : 1;
    }

    private static int CmdImport(List<string> files, string? dbPath, bool dryRun)
    {
        if (files.Count == 0)
        {
            Console.Error.WriteLine("Укажите путь к jsonc-файлу, например: import sample-data.jsonc");
            return 1;
        }
        if (files.Count > 1)
        {
            Console.Error.WriteLine("Ошибка: import принимает один файл");
            return 1;
        }
        string path = files[0];
        if (!File.Exists(path))
        {
            Console.Error.WriteLine($"Ошибка: файл не найден: {path}");
            return 1;
        }

        JsoncParseResult parsed = TransistorJsoncReader.ParseFile(path);
        foreach (JsoncIssue issue in parsed.Issues)
        {
            string source = issue.Source is null ? "" : $" ({issue.Source})";
            string prefix = issue.EntryIndex is int entryIndex ? $"Запись №{entryIndex}: " : "";
            Console.Error.WriteLine($"{prefix}{issue.Description}{source}");
        }
        Console.WriteLine($"Файл: {path}; корректных записей: {parsed.Entries.Count}, проблемных: {parsed.Issues.Count}");

        if (dryRun)
        {
            foreach (TransistorEntryData entry in parsed.Entries)
            {
                Console.WriteLine($"(проверка) {entry.Transistor.Name}{DescribeSections(entry)}");
            }
            return parsed.HasErrors ? 1 : 0;
        }

        int added = 0;
        int updated = 0;
        int skipped = 0;
        using ITransistorDatabase db = OpenDatabase(dbPath);
        foreach (TransistorEntryData entry in parsed.Entries)
        {
            TransistorDetails? details = entry.Attributes is null && entry.Manufacturers is null && entry.Parameters is null && entry.Ratings is null
                ? null
                : new TransistorDetails { Attributes = entry.Attributes, Manufacturers = entry.Manufacturers, Parameters = entry.Parameters, Ratings = entry.Ratings };
            switch (db.Save(entry.Transistor, details))
            {
                case UpsertOutcome.Added:
                    added++;
                    Console.WriteLine($"Добавлено: {entry.Transistor.Name}{DescribeSections(entry)}");
                    break;
                case UpsertOutcome.UpdatedExisting when details is null:
                    skipped++;
                    Console.WriteLine($"Пропущено (уже есть): {entry.Transistor.Name}");
                    break;
                case UpsertOutcome.UpdatedExisting:
                    updated++;
                    Console.WriteLine($"Обновлено: {entry.Transistor.Name}{DescribeSections(entry)}");
                    break;
            }
        }
        Console.WriteLine($"Итого: добавлено {added}, обновлено {updated}, пропущено {skipped}");
        return parsed.HasErrors ? 1 : 0;
    }

    private static string DescribeSections(TransistorEntryData entry)
    {
        var parts = new List<string>();
        if (entry.Attributes is not null) parts.Add("атрибуты");
        if (entry.Manufacturers is not null) parts.Add($"производителей {entry.Manufacturers.Count}");
        if (entry.Parameters is not null) parts.Add($"параметров {entry.Parameters.Count}");
        if (entry.Ratings is not null) parts.Add("предельные данные");
        return parts.Count == 0 ? "" : " (" + string.Join(", ", parts) + ")";
    }

    private static int CmdList(List<string> args, string? dbPath)
    {
        IReadOnlySet<string> allowed = new HashSet<string>
        {
            "--material", "--subclass", "--assembly", "--feature", "--number",
            "--letters", "--modification", "--chip", "--limit",
        };
        Dictionary<string, string> options = ParseOptions(args, allowed);

        var query = new TransistorQuery();
        if (options.TryGetValue("--material", out string? materialValue))
            query = query with { Material = ParseMaterialOption(materialValue) };
        if (options.TryGetValue("--subclass", out string? subclassValue))
            query = query with { Subclass = ParseSubclassOption(subclassValue) };
        if (options.TryGetValue("--assembly", out string? assemblyValue))
            query = query with { IsAssembly = ParseBoolOption("--assembly", assemblyValue) };
        if (options.TryGetValue("--feature", out string? featureValue))
            query = query with { Feature = ParseIntOption("--feature", featureValue, 1, 9) };
        if (options.TryGetValue("--number", out string? numberValue))
            query = query with { DevelopmentNumber = ParseIntOption("--number", numberValue, 1, 999) };
        if (options.TryGetValue("--letters", out string? lettersValue))
            query = query with { Letters = lettersValue.ToUpperInvariant() };
        if (options.TryGetValue("--modification", out string? modificationValue))
            query = query with { Modification = ParseIntOption("--modification", modificationValue, 1, 9) };
        if (options.TryGetValue("--chip", out string? chipValue))
            query = query with { ChipVariant = ParseIntOption("--chip", chipValue, 1, 6) };
        if (options.TryGetValue("--limit", out string? limitValue))
            query = query with { Limit = ParseIntOption("--limit", limitValue, 1, int.MaxValue) };

        using ITransistorDatabase db = OpenExistingDatabase(dbPath);
        IReadOnlyList<Transistor> rows = db.Query(query);
        PrintTable(rows);
        Console.WriteLine($"Записей: {rows.Count}");
        return 0;
    }

    private static int CmdInfo(List<string> names, string? dbPath)
    {
        if (names.Count != 1)
        {
            Console.Error.WriteLine("Укажите одно обозначение, например: info КТ315Б");
            return 1;
        }
        Transistor transistor = TransistorNameParser.Parse(names[0]);
        using ITransistorDatabase db = OpenExistingDatabase(dbPath);
        int? id = db.FindId(transistor);
        if (id is null)
        {
            Console.WriteLine($"Не найдено: {transistor.Name}");
            return 1;
        }

        PrintTransistor(transistor);
        TransistorAttributes? attributes = db.GetAttributes(id.Value);
        if (attributes is not null)
        {
            Console.WriteLine();
            Console.WriteLine("Атрибуты:");
            if (attributes.Structure is string structure) Console.WriteLine($"  Структура: {structure}");
            if (attributes.Technology is string technology) Console.WriteLine($"  Технология: {technology}");
            if (attributes.Package is string package) Console.WriteLine($"  Корпус: {package}");
            if (attributes.PackageMaterial is string material) Console.WriteLine($"  Материал корпуса: {material}");
            if (attributes.ColorMarking is string color) Console.WriteLine($"  Цветовая маркировка: {color}");
            if (attributes.Pinout is string pinout) Console.WriteLine($"  Цоколёвка: {pinout}");
            if (attributes.EsdSensitive is bool esd) Console.WriteLine($"  Повышенная чувствительность к статическому напряжению: {(esd ? "да" : "нет")}");
            if (attributes.MilitaryGrade is bool military) Console.WriteLine($"  Военное исполнение: {(military ? "да" : "нет")}");
            if (attributes.RadiationHardened is bool rad) Console.WriteLine($"  Радиационная стойкость: {(rad ? "да" : "нет")}");
            if (attributes.Tu is string tu) Console.WriteLine($"  ТУ: {tu}");
            if (attributes.YearFrom is int yearFrom)
            {
                Console.WriteLine(attributes.YearTo is int yearTo
                    ? $"  Годы выпуска: {yearFrom}–{yearTo}"
                    : $"  Выпускается с: {yearFrom}");
            }
            if (attributes.MassMax is double mass) Console.WriteLine($"  Масса: не более {ParameterText.Fmt(mass)} г");
            if (attributes.DatasheetUrl is string url) Console.WriteLine($"  Документация: {url}");
            if (attributes.Notes is string notes) Console.WriteLine($"  Примечание: {notes}");
        }

        IReadOnlyList<string> manufacturers = db.GetManufacturers(id.Value);
        if (manufacturers.Count > 0)
        {
            if (attributes is null)
            {
                Console.WriteLine();
                Console.WriteLine("Атрибуты:");
            }
            Console.WriteLine($"  Производители: {string.Join(", ", manufacturers)}");
        }

        IReadOnlyList<ElectricalParameter> parameters = db.GetParameters(id.Value);
        if (parameters.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Электрические параметры:");
            foreach (ElectricalParameter parameter in parameters)
            {
                ParameterInfo info = ElectricalParameterCatalog.Info(parameter.Kind);
                Console.WriteLine($"  {info.DisplayName} ({info.Code}): {ParameterText.Value(parameter)} {ParameterText.Conditions(parameter)}");
            }
        }

        MaximumRatings? ratings = db.GetRatings(id.Value);
        if (ratings is not null)
        {
            Console.WriteLine();
            Console.WriteLine("Предельные эксплуатационные данные:");
            if (ratings.UkeMax is double uke) Console.WriteLine($"  Постоянное напряжение коллектор-эмиттер: {ParameterText.Fmt(uke)} В");
            if (ratings.UkeoMax is double ukeo) Console.WriteLine($"  Постоянное напряжение коллектор-эмиттер при разомкнутой базе: {ParameterText.Fmt(ukeo)} В");
            if (ratings.UkbMax is double ukb) Console.WriteLine($"  Постоянное напряжение коллектор-база: {ParameterText.Fmt(ukb)} В");
            if (ratings.UbeMax is double ube) Console.WriteLine($"  Постоянное напряжение база-эмиттер: {ParameterText.Fmt(ube)} В");
            if (ratings.IkMax is double ik) Console.WriteLine($"  Постоянный ток коллектора: {ParameterText.Fmt(ik)} мА");
            if (ratings.IbMax is double ib) Console.WriteLine($"  Постоянный ток базы: {ParameterText.Fmt(ib)} мА");
            if (ratings.PkMax is double pk) Console.WriteLine($"  Постоянная рассеиваемая мощность коллектора: {ParameterText.Fmt(pk)} мВт");
            if (ratings.IkPulseMax is double ikp) Console.WriteLine($"  Импульсный ток коллектора: {ParameterText.Fmt(ikp)} мА при длительности {ParameterText.Fmt(ratings.PulseDuration!.Value)} мкс");
            if (ratings.PkPulseMax is double pkp) Console.WriteLine($"  Импульсная рассеиваемая мощность коллектора: {ParameterText.Fmt(pkp)} мВт при длительности {ParameterText.Fmt(ratings.PulseDuration!.Value)} мкс");
            if (ratings.TempMin is double tmin && ratings.TempMax is double tmax) Console.WriteLine($"  Температура среды: от {ParameterText.Fmt(tmin)} до {ParameterText.Fmt(tmax)} °C");
            if (ratings.TempJunctionMax is double tj) Console.WriteLine($"  Максимальная температура перехода: {ParameterText.Fmt(tj)} °C");
            if (ratings.Rth is double rth) Console.WriteLine($"  Тепловое сопротивление переход-корпус: {ParameterText.Fmt(rth)} °C/Вт");
        }
        return 0;
    }
    private static int CmdFind(List<string> names, string? dbPath)
    {
        if (names.Count != 1)
        {
            Console.Error.WriteLine("Укажите одно обозначение, например: find КТ315Б");
            return 1;
        }
        Transistor query = TransistorNameParser.Parse(names[0]);
        using ITransistorDatabase db = OpenExistingDatabase(dbPath);
        if (db.FindId(query) is not null)
        {
            PrintTransistor(query);
            return 0;
        }
        Console.WriteLine($"Не найдено: {query.Name}");
        IReadOnlyList<Transistor> equivalents = db.FindMaterialEquivalents(query);
        if (equivalents.Count > 0)
        {
            Console.WriteLine(
                $"Есть равнозначная по материалу запись: {string.Join(", ", equivalents.Select(e => e.Name))} " +
                "(символы Г/1, К/2, А/3, И/4 обозначают один материал, но записи раздельные)");
        }
        return 1;
    }

    private static int CmdDelete(List<string> names, string? dbPath, bool dryRun)
    {
        if (names.Count == 0)
        {
            Console.Error.WriteLine("Укажите обозначение, например: delete КТ315Б");
            return 1;
        }

        var valid = new List<Transistor>();
        int failures = 0;
        foreach (string name in names)
        {
            if (TransistorNameParser.TryParse(name, out var transistor, out string error))
            {
                valid.Add(transistor!);
            }
            else
            {
                failures++;
                Console.Error.WriteLine($"{name}: {error}");
            }
        }

        if (dryRun)
        {
            foreach (Transistor transistor in valid)
            {
                Console.WriteLine($"(проверка) будет удалён: {transistor.Name} (с параметрами и предельными данными)");
            }
            return failures == 0 ? 0 : 1;
        }

        int removed = 0;
        int notFound = 0;
        using ITransistorDatabase db = OpenExistingDatabase(dbPath);
        foreach (Transistor transistor in valid)
        {
            if (db.Delete(transistor))
            {
                removed++;
                Console.WriteLine($"Удалено: {transistor.Name}");
            }
            else
            {
                notFound++;
                Console.WriteLine($"Не найдено: {transistor.Name}");
            }
        }
        Console.WriteLine($"Итого: удалено {removed}, не найдено {notFound}, ошибок разбора {failures}");
        return failures == 0 && notFound == 0 ? 0 : 1;
    }

    private static int CmdCount(string? dbPath)
    {
        using ITransistorDatabase db = OpenExistingDatabase(dbPath);
        Console.WriteLine(db.CountAll());
        return 0;
    }

    /// <summary>База для команд записи (init/add/import): создаётся при необходимости, DDL выполняется один раз за запуск.</summary>
    private static ITransistorDatabase OpenDatabase(string? dbPath)
    {
        var database = new SqliteDatabase(ResolveDatabasePath(dbPath));
        database.EnsureCreated();
        return database;
    }

    /// <summary>База для команд чтения/delete: существующий файл, без выполнения DDL.</summary>
    private static ITransistorDatabase OpenExistingDatabase(string? dbPath)
    {
        string path = ResolveDatabasePath(dbPath);
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"база данных не найдена: {Path.GetFullPath(path)} (сначала выполните init или import)");
        }
        return new SqliteDatabase(path);
    }

    private static string ResolveDatabasePath(string? dbPath) =>
        dbPath
        ?? Environment.GetEnvironmentVariable("TRANSISTOR_DB")
        ?? DefaultDatabaseFile;

    private static Dictionary<string, string> ParseOptions(List<string> args, IReadOnlySet<string> allowed)
    {
        var options = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i < args.Count; i++)
        {
            string arg = args[i];
            string name;
            string value;
            int eq = arg.IndexOf('=');
            if (arg.StartsWith("--") && eq > 0)
            {
                name = arg[..eq];
                value = arg[(eq + 1)..];
            }
            else if (arg.StartsWith("--"))
            {
                name = arg;
                if (i + 1 >= args.Count)
                {
                    throw new ArgumentException($"для опции {arg} ожидается значение");
                }
                value = args[++i];
            }
            else
            {
                throw new ArgumentException($"неожидаемый аргумент «{arg}» (для фильтров используйте --опция=значение)");
            }
            if (!allowed.Contains(name))
            {
                throw new ArgumentException($"неизвестная опция {name}");
            }
            options[name] = value;
        }
        return options;
    }

    private static SemiconductorMaterial ParseMaterialOption(string value) =>
        value.Length == 1 && Materials.TryGetKind(value[0], out var kind)
            ? kind
            : throw new ArgumentException($"--material: ожидалось Г/1, К/2, А/3 или И/4, получено «{value}»");

    private static char ParseSubclassOption(string value) =>
        value.Length == 1 && (value[0] == 'Т' || value[0] == 'П')
            ? value[0]
            : throw new ArgumentException($"--subclass: ожидалось Т или П, получено «{value}»");

    private static bool ParseBoolOption(string name, string value) => value.ToLowerInvariant() switch
    {
        "true" or "да" => true,
        "false" or "нет" => false,
        _ => throw new ArgumentException($"{name}: ожидалось true/false, получено «{value}»"),
    };

    private static int ParseIntOption(string name, string value, int min, int max)
    {
        if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int result) || result < min || result > max)
        {
            throw new ArgumentException($"{name}: ожидалось целое от {min} до {max}, получено «{value}»");
        }
        return result;
    }

    private static void PrintTransistor(Transistor t)
    {
        Console.WriteLine($"Обозначение:      {t.Name}");
        Console.WriteLine($"Материал:         {t.Material} — {Materials.DisplayName(Materials.KindOf(t.Material))}");
        Console.WriteLine($"Подкласс:         {t.Subclass} — {(t.Subclass == 'Т' ? "биполярный" : "полевой")}");
        Console.WriteLine($"Сборка:           {(t.IsAssembly ? "да (С)" : "нет")}");
        Console.WriteLine($"Признак:          {t.Feature}");
        Console.WriteLine($"Номер разработки: {t.DevelopmentNumber:00}");
        Console.WriteLine($"Классификация:    {t.Letters}");
        Console.WriteLine($"Модификация:      {(t.Modification is int m ? m.ToString(CultureInfo.InvariantCulture) : "—")}");
        Console.WriteLine($"Бескорпусное:     {(t.ChipVariant is int c ? "-" + c.ToString(CultureInfo.InvariantCulture) : "—")}");
        Console.WriteLine();
    }

    private static void PrintTable(IReadOnlyList<Transistor> rows)
    {
        if (rows.Count == 0)
        {
            Console.WriteLine("Записей нет.");
            return;
        }

        string[] headers = { "Обозначение", "Материал", "Подкл.", "Сборка", "Признак", "№", "Буквы", "Мод.", "Бескорп." };
        string[][] table = rows.Select(r => new[]
        {
            r.Name,
            $"{r.Material} {Materials.ShortName(Materials.KindOf(r.Material))}",
            r.Subclass.ToString(),
            r.IsAssembly ? "С" : "—",
            r.Feature.ToString(CultureInfo.InvariantCulture),
            r.DevelopmentNumber.ToString("00", CultureInfo.InvariantCulture),
            r.Letters,
            r.Modification?.ToString(CultureInfo.InvariantCulture) ?? "—",
            r.ChipVariant is int c ? "-" + c.ToString(CultureInfo.InvariantCulture) : "—",
        }).ToArray();

        int[] widths = new int[headers.Length];
        for (int i = 0; i < headers.Length; i++)
        {
            widths[i] = Math.Max(headers[i].Length, table.Max(row => row[i].Length));
        }

        Console.WriteLine(string.Join("  ", headers.Select((h, i) => h.PadRight(widths[i]))));
        Console.WriteLine(string.Join("  ", widths.Select(w => new string('-', w))));
        foreach (string[] row in table)
        {
            Console.WriteLine(string.Join("  ", row.Select((v, i) => v.PadRight(widths[i]))));
        }
    }

    private static int UnknownCommand(string command)
    {
        Console.Error.WriteLine($"Неизвестная команда: {command}");
        PrintHelp();
        return 1;
    }

    /// <summary>Раздел справки о параметрах: строки собираются из каталога, а не дублируются вручную.</summary>
    private static string DescribeParameters()
    {
        var lines = new List<string> { "Электрические параметры (jsonc, ключ \"parameters\"), коды:" };
        foreach (ParameterInfo info in ElectricalParameterCatalog.All.Values.OrderBy(info => info.Kind))
        {
            string unit = info.Unit is null ? "раз" : info.Unit;
            string bounds = info.Direction switch
            {
                BoundDirection.AtLeast => "min",
                BoundDirection.AtMost => "max",
                BoundDirection.AtLeastOrRange => "min [max]",
                _ => throw new InvalidOperationException($"неизвестное правило границ: {info.Direction}"),
            };
            string line = $"  {info.Code} ({unit}) — {info.DisplayName}: {bounds}; условия: {info.Conditions.Describe()}";
            if (info.ValueCeiling is double ceiling)
                line += $" (не более {ParameterText.Fmt(ceiling)} {info.Unit})";
            lines.Add(line);
        }
        string conditionKeys = string.Join(", ", ConditionKeys.All
            .Select(key => $"{ConditionKeys.JsoncKey(key)} ({ConditionKeys.Unit(key)})"));
        lines.Add($"  min/max — значение, условия: {conditionKeys}, temp (°C).");
        return string.Join("\n", lines);
    }

    private static int PrintHelp()
    {
        Console.WriteLine("""
            Справочник советских транзисторов.

            Использование: SovietTransistorsDatabase <команда> [аргументы] [--db <путь>] [--dry-run]

            Команды:
              init                                 создать таблицы (команды записи создают базу автоматически)
              import <файл.jsonc>                  импорт: обозначения, атрибуты, параметры, предельные данные
              add <обозначение> [<обозначение>...]   добавить транзисторы по обозначению (через парсер)
              parse <обозначение>...                 разобрать обозначение без обращения к базе
              list [фильтры]                       вывести обозначения из базы
              info <обозначение>                   карточка: атрибуты, параметры, предельные данные
              find <обозначение>                   найти запись по точному обозначению (при отсутствии —
                                                   подсказка равнозначной по материалу: Г/1, К/2, А/3, И/4)
              delete <обозначение>...                удалить записи (каскадно с параметрами и предельными)
              count                                количество записей

            Опции:
              --db <путь>     путь к базе SQLite (по умолчанию ./transistors.db или переменная TRANSISTOR_DB)
              --dry-run       только проверка, без записи в базу
              Команды list/info/find/count/delete работают только с существующей базой (не создают её)

            Фильтры list (--опция=значение или --опция значение):
              --material=Г|1|К|2|А|3|И|4   --subclass=Т|П   --assembly=true|false
              --feature=1..9   --number=1..999   --letters=А|АМ
              --modification=1..9   --chip=1..6   --limit=N

            Примеры:
              SovietTransistorsDatabase add КТ315Б
              SovietTransistorsDatabase import sample-data.jsonc
              SovietTransistorsDatabase info КТ315Б
              SovietTransistorsDatabase list --material=К --subclass=П

            Формат обозначения: <материал><подкласс>[С]<признак><номер><буквы>[<модификация>][-<бескорп.>]
              материал:   Г/1 — германий, К/2 — кремний, А/3 — арсенид галлия, И/4 — индий
              подкласс:   Т — биполярный, П — полевой; С — сборка
              признак:    цифра 1–9
              номер:      01–999
              буквы:      1–2 заглавные русские буквы
              модификация: цифра 1–9
              бескорпусное исполнение: дефис и цифра 1–6
            """ + "\n\n" + DescribeParameters() + "\n" + """
            Атрибуты (jsonc, ключ "attributes"): structure (npn/pnp/n-fet...), technology, package,
              packageMaterial, colorMarking, pinout, esdSensitive/militaryGrade/radiationHardened (bool),
              tu, notes, yearFrom/yearTo (1949–2100), massMax (г), datasheetUrl;
              manufacturers — массив названий заводов (null — не менять, [] — очистить).
            Предельные данные (jsonc, ключ "ratings"): UkeMax/UkbMax/UbeMax/UkeoMax (В), IkMax/IbMax (мА),
              PkMax (мВт), IkPulseMax/PkPulseMax + pulseDuration (мкс), tempMin/tempMax/tempJunctionMax (°C),
              Rth (°C/Вт).
            """);
        return 0;
    }
}
