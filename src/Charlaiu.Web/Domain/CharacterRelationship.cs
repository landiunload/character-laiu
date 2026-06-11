namespace Charlaiu.Web.Domain;

/// <summary>
/// Связь между двумя персонажами: дружба, вражда, любовь — любая, какую придумает автор.
/// Связи хранятся отдельно от анкет, поэтому удаление героя не ломает остальных.
/// </summary>
public sealed class CharacterRelationship
{
    /// <summary>Уникальный идентификатор связи.</summary>
    public Guid Identifier { get; set; } = Guid.CreateVersion7();

    /// <summary>Первый участник связи.</summary>
    public Guid FirstCharacterIdentifier { get; set; }

    /// <summary>Второй участник связи.</summary>
    public Guid SecondCharacterIdentifier { get; set; }

    /// <summary>Подпись связи (например, «дружба с детства»).</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Цвет линии на графе в формате #RRGGBB.</summary>
    public string ColorHex { get; set; } = "#888888";

    /// <summary>
    /// Односторонняя связь: чувство направлено от первого персонажа ко второму
    /// (на графе рисуется стрелка). Двусторонняя связь — без стрелок.
    /// </summary>
    public bool IsOneDirectional { get; set; }
}
