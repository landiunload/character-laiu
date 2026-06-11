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
}
