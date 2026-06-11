namespace Charlaiu.Web.Domain;

/// <summary>
/// Анкета персонажа — корневая модель приложения.
/// Состоит из разделов с полями; структура полностью настраивается пользователем.
/// </summary>
public sealed class CharacterProfile
{
    /// <summary>Уникальный идентификатор персонажа.</summary>
    public Guid Identifier { get; set; } = Guid.CreateVersion7();

    /// <summary>Имя персонажа, отображается в списке героев.</summary>
    public string DisplayName { get; set; } = "Новый герой";

    /// <summary>Момент создания анкеты в формате UTC.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Момент последнего изменения анкеты в формате UTC.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>Разделы анкеты.</summary>
    public List<QuestionnaireSection> Sections { get; set; } = [];

    /// <summary>Отмечает анкету изменённой — обновляет время последнего изменения.</summary>
    public void MarkAsUpdated() => UpdatedAtUtc = DateTimeOffset.UtcNow;

    /// <summary>Добавляет новый раздел в конец анкеты.</summary>
    public void AddSection()
    {
        var createdSection = new QuestionnaireSection { Title = "Новый раздел" };
        createdSection.AddField();
        Sections.Add(createdSection);
    }

    /// <summary>Удаляет раздел по идентификатору.</summary>
    public void RemoveSection(Guid sectionIdentifier) =>
        Sections.RemoveAll(candidateSection => candidateSection.Identifier == sectionIdentifier);


    /// <summary>
    /// Сдвигает раздел вверх или вниз по списку.
    /// Смещение за границы списка молча игнорируется — кнопки можно нажимать без проверок.
    /// </summary>
    public void MoveSection(Guid sectionIdentifier, int positionOffset)
    {
        var currentIndex = Sections.FindIndex(candidateSection => candidateSection.Identifier == sectionIdentifier);
        var targetIndex = currentIndex + positionOffset;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= Sections.Count)
        {
            return;
        }

        (Sections[currentIndex], Sections[targetIndex]) = (Sections[targetIndex], Sections[currentIndex]);
    }

    /// <summary>Создаёт глубокую копию анкеты с новыми идентификаторами.</summary>
    public CharacterProfile Clone() => new()
    {
        DisplayName = DisplayName + " (копия)",
        Sections = Sections.Select(existingSection => existingSection.Clone()).ToList()
    };
}
