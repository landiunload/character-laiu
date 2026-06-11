namespace Charlaiu.Web.Domain;

/// <summary>Тип поля анкеты — определяет, каким элементом ввода поле отображается.</summary>
public enum QuestionnaireFieldType
{
    /// <summary>Однострочное текстовое поле.</summary>
    SingleLine = 0,

    /// <summary>Многострочное текстовое поле.</summary>
    MultiLine = 1,

    /// <summary>Парное поле: два значения в одной строке (например, «Любит / Не любит»).</summary>
    PairedValues = 2
}
