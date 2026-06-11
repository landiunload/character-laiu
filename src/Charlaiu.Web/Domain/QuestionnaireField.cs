namespace Charlaiu.Web.Domain;

/// <summary>
/// Поле анкеты: подпись, тип и введённые значения.
/// Свойства открыты для записи — это требование двусторонней привязки Blazor
/// и сериализации System.Text.Json.
/// </summary>
public sealed class QuestionnaireField
{
    /// <summary>Уникальный идентификатор поля.</summary>
    public Guid Identifier { get; set; } = Guid.CreateVersion7();

    /// <summary>Подпись поля. Пользователь может её переименовать.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Тип поля.</summary>
    public QuestionnaireFieldType FieldType { get; set; } = QuestionnaireFieldType.SingleLine;

    /// <summary>Основное значение (для одиночных полей и первой колонки парных).</summary>
    public string PrimaryValue { get; set; } = string.Empty;

    /// <summary>Второе значение — используется только парными полями.</summary>
    public string SecondaryValue { get; set; } = string.Empty;

    /// <summary>Признак, что поле содержит хоть какой-то текст.</summary>
    public bool HasAnyValue =>
        !string.IsNullOrWhiteSpace(PrimaryValue) || !string.IsNullOrWhiteSpace(SecondaryValue);

    /// <summary>Создаёт поле с указанной подписью и типом.</summary>
    public static QuestionnaireField Create(string label, QuestionnaireFieldType fieldType = QuestionnaireFieldType.SingleLine) =>
        new() { Label = label, FieldType = fieldType };

    /// <summary>Создаёт глубокую копию поля с новым идентификатором.</summary>
    public QuestionnaireField Clone() => new()
    {
        Label = Label,
        FieldType = FieldType,
        PrimaryValue = PrimaryValue,
        SecondaryValue = SecondaryValue
    };
}
