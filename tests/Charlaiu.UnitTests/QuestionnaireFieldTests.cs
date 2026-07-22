using Charlaiu.Web.Domain;
using Xunit;

namespace Charlaiu.UnitTests;

/// <summary>Тесты поля анкеты: признак заполненности и глубокое копирование.</summary>
public sealed class QuestionnaireFieldTests
{
    [Fact]
    public void HasAnyValue_ПустоеПоле_Ложь()
    {
        var field = QuestionnaireField.Create("Имя");
        Assert.False(field.HasAnyValue);
    }

    [Fact]
    public void HasAnyValue_ТолькоПробелы_Ложь()
    {
        var field = QuestionnaireField.Create("Имя");
        field.PrimaryValue = "   ";
        field.SecondaryValue = "\t";
        Assert.False(field.HasAnyValue);
    }

    [Fact]
    public void HasAnyValue_ЗаполненоОсновноеЗначение_Истина()
    {
        var field = QuestionnaireField.Create("Имя");
        field.PrimaryValue = "Мири";
        Assert.True(field.HasAnyValue);
    }

    [Fact]
    public void HasAnyValue_ЗаполненоТольноВтороеЗначение_Истина()
    {
        // Парное поле может быть заполнено только во второй колонке
        var field = QuestionnaireField.Create("Любит / Не любит", QuestionnaireFieldType.PairedValues);
        field.SecondaryValue = "Громкие звуки";
        Assert.True(field.HasAnyValue);
    }

    [Fact]
    public void Clone_КопияПоля_НовыйИдентификаторИТеЖеЗначения()
    {
        var original = QuestionnaireField.Create("Черты", QuestionnaireFieldType.MultiLine);
        original.PrimaryValue = "Упрямая";
        original.SecondaryValue = "Добрая";

        var clone = original.Clone();

        Assert.NotEqual(original.Identifier, clone.Identifier);
        Assert.Equal(original.Label, clone.Label);
        Assert.Equal(original.FieldType, clone.FieldType);
        Assert.Equal(original.PrimaryValue, clone.PrimaryValue);
        Assert.Equal(original.SecondaryValue, clone.SecondaryValue);

        // Копия независима: правка не задевает оригинал
        clone.PrimaryValue = "Мягкая";
        Assert.Equal("Упрямая", original.PrimaryValue);
    }
}
