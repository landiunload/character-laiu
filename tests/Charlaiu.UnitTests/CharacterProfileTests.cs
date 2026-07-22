using Charlaiu.Web.Domain;
using Xunit;

namespace Charlaiu.UnitTests;

/// <summary>Тесты модели анкеты персонажа.</summary>
public sealed class CharacterProfileTests
{
    [Fact]
    public void CreateCharacterProfile_СхемаПоУмолчанию_СодержитОдиннадцатьРазделов()
    {
        var createdProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile();

        Assert.Equal(11, createdProfile.Sections.Count);
        Assert.Contains(createdProfile.Sections, section => section.Title == "Пять вопросов на глубину");
    }

    [Fact]
    public void Clone_КопияГероя_НезависимаОтОригинала()
    {
        // Подготовка
        var originalProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Мири");
        originalProfile.Sections[0].Fields[0].PrimaryValue = "Мири";

        // Действие
        var clonedProfile = originalProfile.Clone();
        clonedProfile.Sections[0].Fields[0].PrimaryValue = "Лури";

        // Проверка: изменение копии не задело оригинал, идентификаторы новые
        Assert.Equal("Мири", originalProfile.Sections[0].Fields[0].PrimaryValue);
        Assert.NotEqual(originalProfile.Identifier, clonedProfile.Identifier);
        Assert.Equal("Мири (копия)", clonedProfile.DisplayName);
    }

    [Fact]
    public void RemoveSection_СуществующийРаздел_УдаляетсяТолькоОн()
    {
        var characterProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile();
        var sectionsCountBeforeRemoval = characterProfile.Sections.Count;

        characterProfile.RemoveSection(characterProfile.Sections[0].Identifier);

        Assert.Equal(sectionsCountBeforeRemoval - 1, characterProfile.Sections.Count);
    }

    [Fact]
    public void AddSection_СКолонками_СоздаётПарныйРазделСПолем()
    {
        var characterProfile = new CharacterProfile();

        characterProfile.AddSection(withPairedColumns: true);

        var addedSection = Assert.Single(characterProfile.Sections);
        Assert.NotNull(addedSection.FirstColumnLabel);
        Assert.NotNull(addedSection.SecondColumnLabel);
        var addedField = Assert.Single(addedSection.Fields);
        Assert.Equal(QuestionnaireFieldType.PairedValues, addedField.FieldType);
    }

    [Fact]
    public void AddSection_Обычный_СоздаётРазделСМногострочнымПолем()
    {
        var characterProfile = new CharacterProfile();

        characterProfile.AddSection();

        var addedSection = Assert.Single(characterProfile.Sections);
        Assert.Null(addedSection.FirstColumnLabel);
        var addedField = Assert.Single(addedSection.Fields);
        Assert.Equal(QuestionnaireFieldType.MultiLine, addedField.FieldType);
    }

    [Fact]
    public void MoveSection_ЗаГраницуСписка_НичегоНеМеняет()
    {
        // Граничный случай: сдвиг первого раздела вверх должен молча игнорироваться
        var characterProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile();
        var firstSectionIdentifier = characterProfile.Sections[0].Identifier;

        characterProfile.MoveSection(firstSectionIdentifier, positionOffset: -1);

        Assert.Equal(firstSectionIdentifier, characterProfile.Sections[0].Identifier);
    }

    [Fact]
    public void MoveSection_ВнизВПределахСписка_МеняетПорядок()
    {
        var characterProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile();
        var firstSectionIdentifier = characterProfile.Sections[0].Identifier;
        var secondSectionIdentifier = characterProfile.Sections[1].Identifier;

        characterProfile.MoveSection(firstSectionIdentifier, positionOffset: 1);

        Assert.Equal(secondSectionIdentifier, characterProfile.Sections[0].Identifier);
        Assert.Equal(firstSectionIdentifier, characterProfile.Sections[1].Identifier);
    }
}
