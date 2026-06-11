namespace Charlaiu.Web.Domain;

/// <summary>
/// Раздел анкеты: заголовок, авторская подсказка и набор полей.
/// Разделы и поля можно свободно добавлять, переименовывать и удалять —
/// остальное приложение работает с любой структурой (принцип открытости/закрытости).
/// </summary>
public sealed class QuestionnaireSection
{
    /// <summary>Уникальный идентификатор раздела.</summary>
    public Guid Identifier { get; set; } = Guid.CreateVersion7();

    /// <summary>Эмодзи-значок раздела.</summary>
    public string Icon { get; set; } = "star";

    /// <summary>Заголовок раздела.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Авторская подсказка, отображается под заголовком.</summary>
    public string Hint { get; set; } = string.Empty;

    /// <summary>Подписи колонок для парных полей (например, «Любит» и «Не любит»).</summary>
    public string? FirstColumnLabel { get; set; }

    /// <summary>Подпись второй колонки парных полей.</summary>
    public string? SecondColumnLabel { get; set; }

    /// <summary>Поля раздела.</summary>
    public List<QuestionnaireField> Fields { get; set; } = [];

    /// <summary>
    /// Добавляет новое пустое поле в конец раздела.
    /// В разделе с колонками новое поле тоже парное — иначе колонки разъедутся.
    /// </summary>
    public void AddField()
    {
        var newFieldType = FirstColumnLabel is not null
            ? QuestionnaireFieldType.PairedValues
            : QuestionnaireFieldType.MultiLine;

        Fields.Add(QuestionnaireField.Create("Новое поле", newFieldType));
    }

    /// <summary>Удаляет поле по идентификатору.</summary>
    public void RemoveField(Guid fieldIdentifier) =>
        Fields.RemoveAll(candidateField => candidateField.Identifier == fieldIdentifier);


    /// <summary>
    /// Сдвигает поле вверх или вниз по разделу.
    /// Смещение за границы списка молча игнорируется.
    /// </summary>
    public void MoveField(Guid fieldIdentifier, int positionOffset)
    {
        var currentIndex = Fields.FindIndex(candidateField => candidateField.Identifier == fieldIdentifier);
        var targetIndex = currentIndex + positionOffset;

        if (currentIndex < 0 || targetIndex < 0 || targetIndex >= Fields.Count)
        {
            return;
        }

        (Fields[currentIndex], Fields[targetIndex]) = (Fields[targetIndex], Fields[currentIndex]);
    }

    /// <summary>Создаёт глубокую копию раздела с новыми идентификаторами.</summary>
    public QuestionnaireSection Clone() => new()
    {
        Icon = Icon,
        Title = Title,
        Hint = Hint,
        FirstColumnLabel = FirstColumnLabel,
        SecondColumnLabel = SecondColumnLabel,
        Fields = Fields.Select(existingField => existingField.Clone()).ToList()
    };
}
