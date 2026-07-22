using Charlaiu.Web.Domain;
using Xunit;

namespace Charlaiu.UnitTests;

/// <summary>
/// Тесты раздела анкеты: добавление/удаление/перемещение полей и копирование.
/// Раньше эти операции проверялись только через интерфейс, без модульного покрытия.
/// </summary>
public sealed class QuestionnaireSectionTests
{
    [Fact]
    public void AddField_РазделСКолонками_ДобавляетПарноеПоле()
    {
        // В разделе с колонками новое поле обязано быть парным, иначе колонки разъедутся
        var section = new QuestionnaireSection
        {
            FirstColumnLabel = "Любит",
            SecondColumnLabel = "Не любит"
        };

        section.AddField();

        var addedField = Assert.Single(section.Fields);
        Assert.Equal(QuestionnaireFieldType.PairedValues, addedField.FieldType);
    }

    [Fact]
    public void AddField_ОбычныйРаздел_ДобавляетМногострочноеПоле()
    {
        var section = new QuestionnaireSection { Title = "Внешность" };

        section.AddField();

        var addedField = Assert.Single(section.Fields);
        Assert.Equal(QuestionnaireFieldType.MultiLine, addedField.FieldType);
    }

    [Fact]
    public void RemoveField_СуществующееПоле_УдаляетТолькоЕго()
    {
        var section = new QuestionnaireSection();
        section.Fields.Add(QuestionnaireField.Create("Первое"));
        section.Fields.Add(QuestionnaireField.Create("Второе"));
        var removedIdentifier = section.Fields[0].Identifier;

        section.RemoveField(removedIdentifier);

        var remainingField = Assert.Single(section.Fields);
        Assert.Equal("Второе", remainingField.Label);
    }

    [Fact]
    public void MoveField_ВнизВПределахСписка_МеняетПорядок()
    {
        var section = new QuestionnaireSection();
        var first = QuestionnaireField.Create("Первое");
        var second = QuestionnaireField.Create("Второе");
        section.Fields.Add(first);
        section.Fields.Add(second);

        section.MoveField(first.Identifier, positionOffset: 1);

        Assert.Equal("Второе", section.Fields[0].Label);
        Assert.Equal("Первое", section.Fields[1].Label);
    }

    [Fact]
    public void MoveField_ЗаГраницуСписка_НичегоНеМеняет()
    {
        // Граничный случай: сдвиг первого поля вверх должен молча игнорироваться
        var section = new QuestionnaireSection();
        var first = QuestionnaireField.Create("Первое");
        var second = QuestionnaireField.Create("Второе");
        section.Fields.Add(first);
        section.Fields.Add(second);

        section.MoveField(first.Identifier, positionOffset: -1);

        Assert.Equal("Первое", section.Fields[0].Label);
        Assert.Equal("Второе", section.Fields[1].Label);
    }

    [Fact]
    public void Clone_КопияРаздела_ГлубокаяСНовымиИдентификаторамиПолей()
    {
        var section = new QuestionnaireSection { Title = "Внешность", Icon = "eye" };
        section.Fields.Add(QuestionnaireField.Create("Рост"));
        section.Fields[0].PrimaryValue = "Высокая";

        var clone = section.Clone();

        Assert.Equal(section.Title, clone.Title);
        Assert.NotEqual(section.Identifier, clone.Identifier);
        Assert.NotEqual(section.Fields[0].Identifier, clone.Fields[0].Identifier);

        // Копия независима: правка поля копии не задевает оригинал
        clone.Fields[0].PrimaryValue = "Низкая";
        Assert.Equal("Высокая", section.Fields[0].PrimaryValue);
    }
}
