namespace Charlaiu.Web.Domain;

/// <summary>
/// Заготовка связи: название и цвет настраиваются автором —
/// «Дружба», «Вражда» и любые собственные типы.
/// </summary>
public sealed class RelationshipPreset
{
    /// <summary>Название заготовки — оно же подпись связи на графе.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Цвет линий этого типа связи в формате #RRGGBB.</summary>
    public string ColorHex { get; set; } = "#888888";

    /// <summary>Заготовки по умолчанию для нового пользователя.</summary>
    public static List<RelationshipPreset> CreateDefaultPresets() =>
    [
        new() { Name = "Дружба", ColorHex = "#3a9d5d" },
        new() { Name = "Вражда", ColorHex = "#c0392b" },
        new() { Name = "Любовь", ColorHex = "#d4628f" },
        new() { Name = "Семья", ColorHex = "#4a78c2" },
        new() { Name = "Соперничество", ColorHex = "#b8772a" },
        new() { Name = "Другое", ColorHex = "#888888" }
    ];
}
