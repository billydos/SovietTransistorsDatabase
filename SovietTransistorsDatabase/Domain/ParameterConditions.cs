namespace SovietTransistorsDatabase.Domain;

/// <summary>Условие измерения параметра. Температура среды в модель ключей не входит: она допустима у любого параметра.</summary>
public enum ConditionKey
{
    Uke,
    Ukb,
    Ueb,
    Ik,
    Ie,
    Ib,
    Freq,
    Rg,
    Rbe,
}

/// <summary>Справка по ключам условий: канонический порядок, обозначения, единицы и доступ к значениям параметра.</summary>
public static class ConditionKeys
{
    /// <summary>Канонический порядок ключей (сообщения валидатора, справка, DDL).</summary>
    public static readonly IReadOnlyList<ConditionKey> All = new ConditionKey[]
    {
        ConditionKey.Uke,
        ConditionKey.Ukb,
        ConditionKey.Ueb,
        ConditionKey.Ik,
        ConditionKey.Ie,
        ConditionKey.Ib,
        ConditionKey.Freq,
        ConditionKey.Rg,
        ConditionKey.Rbe,
    };

    /// <summary>Имя свойства <see cref="ElectricalParameter"/> и колонки БД (совпадает с именем элемента перечисления).</summary>
    public static string Name(ConditionKey key) => key.ToString();

    /// <summary>Имя ключа в jsonc: freq — единственное слово-исключение, остальные совпадают с именем.</summary>
    public static string JsoncKey(ConditionKey key) => key == ConditionKey.Freq ? "freq" : key.ToString();

    /// <summary>Обозначение условия для сообщений и справки (с ключом jsonc и единицей).</summary>
    public static string Label(ConditionKey key) => key switch
    {
        ConditionKey.Uke  => "Uкэ (Uke)",
        ConditionKey.Ukb  => "Uкб (Ukb)",
        ConditionKey.Ueb  => "Uэб (Ueb)",
        ConditionKey.Ik   => "Iк (Ik)",
        ConditionKey.Ie   => "Iэ (Ie)",
        ConditionKey.Ib   => "Iб (Ib)",
        ConditionKey.Freq => "частота (freq, МГц)",
        ConditionKey.Rg   => "Rг (Rg, Ом)",
        ConditionKey.Rbe  => "Rбэ (Rbe, Ом)",
        _ => throw new ArgumentOutOfRangeException(nameof(key)),
    };

    /// <summary>Название условия с единицей для комментариев DDL.</summary>
    public static string Description(ConditionKey key) => key switch
    {
        ConditionKey.Uke  => "напряжение коллектор-эмиттер, В",
        ConditionKey.Ukb  => "напряжение коллектор-база, В",
        ConditionKey.Ueb  => "напряжение эмиттер-база, В",
        ConditionKey.Ik   => "ток коллектора, мА",
        ConditionKey.Ie   => "ток эмиттера, мА",
        ConditionKey.Ib   => "ток базы, мА",
        ConditionKey.Freq => "частота, МГц",
        ConditionKey.Rg   => "сопротивление генератора, Ом",
        ConditionKey.Rbe  => "сопротивление в цепи база-эмиттер, Ом",
        _ => throw new ArgumentOutOfRangeException(nameof(key)),
    };

    /// <summary>Каноническая единица условия: В, мА, МГц или Ом.</summary>
    public static string Unit(ConditionKey key) => key switch
    {
        ConditionKey.Uke or ConditionKey.Ukb or ConditionKey.Ueb => "В",
        ConditionKey.Ik or ConditionKey.Ie or ConditionKey.Ib => "мА",
        ConditionKey.Freq => "МГц",
        ConditionKey.Rg or ConditionKey.Rbe => "Ом",
        _ => throw new ArgumentOutOfRangeException(nameof(key)),
    };

    /// <summary>Значение условия в параметре.</summary>
    public static double? ValueOf(ElectricalParameter parameter, ConditionKey key) => key switch
    {
        ConditionKey.Uke  => parameter.Uke,
        ConditionKey.Ukb  => parameter.Ukb,
        ConditionKey.Ueb  => parameter.Ueb,
        ConditionKey.Ik   => parameter.Ik,
        ConditionKey.Ie   => parameter.Ie,
        ConditionKey.Ib   => parameter.Ib,
        ConditionKey.Freq => parameter.Freq,
        ConditionKey.Rg   => parameter.Rg,
        ConditionKey.Rbe  => parameter.Rbe,
        _ => throw new ArgumentOutOfRangeException(nameof(key)),
    };

    /// <summary>Копия параметра с заданным значением условия (для тестов).</summary>
    public static ElectricalParameter WithValue(ElectricalParameter parameter, ConditionKey key, double value) => key switch
    {
        ConditionKey.Uke  => parameter with { Uke = value },
        ConditionKey.Ukb  => parameter with { Ukb = value },
        ConditionKey.Ueb  => parameter with { Ueb = value },
        ConditionKey.Ik   => parameter with { Ik = value },
        ConditionKey.Ie   => parameter with { Ie = value },
        ConditionKey.Ib   => parameter with { Ib = value },
        ConditionKey.Freq => parameter with { Freq = value },
        ConditionKey.Rg   => parameter with { Rg = value },
        ConditionKey.Rbe  => parameter with { Rbe = value },
        _ => throw new ArgumentOutOfRangeException(nameof(key)),
    };

    /// <summary>Сообщение о неположительном значении условия.</summary>
    public static string NonPositiveMessage(ConditionKey key) => key switch
    {
        ConditionKey.Uke  => "условие Uкэ должно быть положительным",
        ConditionKey.Ukb  => "условие Uкб должно быть положительным",
        ConditionKey.Ueb  => "условие Uэб должно быть положительным",
        ConditionKey.Ik   => "условие Iк должно быть положительным",
        ConditionKey.Ie   => "условие Iэ должно быть положительным",
        ConditionKey.Ib   => "условие Iб должно быть положительным",
        ConditionKey.Freq => "частота должна быть положительной",
        ConditionKey.Rg   => "сопротивление генератора должно быть положительным",
        ConditionKey.Rbe  => "сопротивление Rбэ должно быть положительным",
        _ => throw new ArgumentOutOfRangeException(nameof(key)),
    };
}

/// <summary>Один допустимый набор условий: обязательные и необязательные ключи, все остальные — запрещены.</summary>
public sealed class ConditionVariant
{
    public IReadOnlySet<ConditionKey> Required { get; }

    public IReadOnlySet<ConditionKey> Optional { get; }

    private ConditionVariant(IReadOnlySet<ConditionKey> required, IReadOnlySet<ConditionKey> optional)
    {
        Required = required;
        Optional = optional;
    }

    /// <summary>Начинает вариант с обязательных ключей; продолжается через <see cref="Optionally"/>.</summary>
    public static ConditionVariant Require(params ConditionKey[] required) =>
        new(new HashSet<ConditionKey>(required), new HashSet<ConditionKey>());

    /// <summary>Возвращает вариант с теми же обязательными ключами и дополнительными необязательными.</summary>
    public ConditionVariant Optionally(params ConditionKey[] optional) =>
        new(Required, new HashSet<ConditionKey>(optional));

    /// <summary>Вариант подходит, если все обязательные ключи заданы, а из остальных — только необязательные.</summary>
    public bool IsSatisfiedBy(ElectricalParameter parameter)
    {
        foreach (ConditionKey key in ConditionKeys.All)
        {
            bool set = ConditionKeys.ValueOf(parameter, key).HasValue;
            if (Required.Contains(key) != set && !Optional.Contains(key))
                return false;
        }
        return true;
    }
}

/// <summary>Спецификация условий параметра: набор взаимоисключающих вариантов, параметр корректен, если подходит хотя бы один.</summary>
public sealed class ConditionSpec
{
    public IReadOnlyList<ConditionVariant> Variants { get; }

    public ConditionSpec(params ConditionVariant[] variants)
    {
        if (variants.Length == 0)
            throw new ArgumentException("Спецификация условий должна содержать хотя бы один вариант.", nameof(variants));
        Variants = variants;
    }

    /// <summary>Параметр удовлетворяет спецификации, если подходит хотя бы один вариант.</summary>
    public bool IsSatisfiedBy(ElectricalParameter parameter) => Variants.Any(variant => variant.IsSatisfiedBy(parameter));

    /// <summary>Ключ допускается хотя бы одним вариантом — как обязательный или необязательный.</summary>
    public bool Allows(ConditionKey key) => Variants.Any(variant => variant.Required.Contains(key) || variant.Optional.Contains(key));

    /// <summary>Человекочитаемое описание вариантов, разделённых «либо».</summary>
    public string Describe() => string.Join(" либо ", Variants.Select(DescribeVariant));

    private static string DescribeVariant(ConditionVariant variant)
    {
        string text = string.Join(" + ", Ordered(variant.Required).Select(ConditionKeys.Label));
        if (variant.Optional.Count > 0)
            text += $" (опционально {string.Join(" + ", Ordered(variant.Optional).Select(ConditionKeys.Label))})";
        return text;
    }

    private static IEnumerable<ConditionKey> Ordered(IReadOnlySet<ConditionKey> keys) =>
        ConditionKeys.All.Where(keys.Contains);
}

/// <summary>Именованные спецификации условий, на которые ссылается каталог параметров.</summary>
public static class ConditionSpecs
{
    /// <summary>Пара Uкэ + Iк (ОЭ) либо Uкб + Iэ (ОБ).</summary>
    public static ConditionSpec PairUkeIkOrUkbIe { get; } = new(
        ConditionVariant.Require(ConditionKey.Uke, ConditionKey.Ik),
        ConditionVariant.Require(ConditionKey.Ukb, ConditionKey.Ie));

    /// <summary>Только один ток: Iк либо Iэ.</summary>
    public static ConditionSpec ExactlyOneCurrent { get; } = new(
        ConditionVariant.Require(ConditionKey.Ik),
        ConditionVariant.Require(ConditionKey.Ie));

    /// <summary>Только Uкб.</summary>
    public static ConditionSpec OnlyUkb { get; } = new(ConditionVariant.Require(ConditionKey.Ukb));

    /// <summary>Только Uэб.</summary>
    public static ConditionSpec OnlyUeb { get; } = new(ConditionVariant.Require(ConditionKey.Ueb));

    /// <summary>Только Uкэ.</summary>
    public static ConditionSpec OnlyUke { get; } = new(ConditionVariant.Require(ConditionKey.Uke));

    /// <summary>Uкэ и Iк.</summary>
    public static ConditionSpec UkeAndIk { get; } = new(ConditionVariant.Require(ConditionKey.Uke, ConditionKey.Ik));

    /// <summary>Uкб и Iэ (общая база).</summary>
    public static ConditionSpec UkbAndIe { get; } = new(ConditionVariant.Require(ConditionKey.Ukb, ConditionKey.Ie));

    /// <summary>Uкэ и сопротивление Rбэ.</summary>
    public static ConditionSpec UkeAndRbe { get; } = new(ConditionVariant.Require(ConditionKey.Uke, ConditionKey.Rbe));

    /// <summary>Iк и Iб.</summary>
    public static ConditionSpec IkAndIb { get; } = new(ConditionVariant.Require(ConditionKey.Ik, ConditionKey.Ib));

    /// <summary>Пара Uкэ + Iк либо Uкб + Iэ, обязательная частота и необязательный Rг (KShum).</summary>
    public static ConditionSpec PairPlusFrequency { get; } = new(
        ConditionVariant.Require(ConditionKey.Uke, ConditionKey.Ik, ConditionKey.Freq).Optionally(ConditionKey.Rg),
        ConditionVariant.Require(ConditionKey.Ukb, ConditionKey.Ie, ConditionKey.Freq).Optionally(ConditionKey.Rg));

    /// <summary>Обязательная частота; Uкэ + Iк задаются вместе или не задаются вовсе.</summary>
    public static ConditionSpec FrequencyPlusOptionalUkeIk { get; } = new(
        ConditionVariant.Require(ConditionKey.Freq),
        ConditionVariant.Require(ConditionKey.Freq, ConditionKey.Uke, ConditionKey.Ik));
}
