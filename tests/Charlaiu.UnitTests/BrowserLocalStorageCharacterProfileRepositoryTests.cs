using System.Text.Json;
using Charlaiu.Web.Domain;
using Charlaiu.Web.Services;
using Xunit;

namespace Charlaiu.UnitTests;

/// <summary>
/// Тесты хранилища анкет: формат хранения и перенос сохранений первой версии.
/// Перенос трогает данные пользователя, а ошибка в нём необратима, поэтому
/// проверяется и удачный путь, и поведение на повреждённых данных.
/// </summary>
public sealed class BrowserLocalStorageCharacterProfileRepositoryTests
{
    private const string IndexStorageKey = "charlaiu.characterProfileIndex";
    private const string LegacyAllProfilesStorageKey = "charlaiu.characterProfiles";

    private readonly FakeBrowserLocalStorage _browserStorage = new();
    private readonly BrowserLocalStorageCharacterProfileRepository _repositoryUnderTest;

    public BrowserLocalStorageCharacterProfileRepositoryTests() =>
        _repositoryUnderTest = new BrowserLocalStorageCharacterProfileRepository(_browserStorage);

    private static string ProfileStorageKeyOf(CharacterProfile profile) =>
        "charlaiu.characterProfile." + profile.Identifier;

    [Fact]
    public async Task LoadAllProfilesAsync_ПустоеХранилище_ПустойСписок()
    {
        var loadedProfiles = await _repositoryUnderTest.LoadAllProfilesAsync();

        Assert.Empty(loadedProfiles);
    }

    [Fact]
    public async Task SaveProfileAsync_НоваяАнкета_ЛожитсяПодСвоимКлючомИВходитВУказатель()
    {
        var characterProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Мири");

        await _repositoryUnderTest.SaveProfileAsync(characterProfile);

        Assert.Contains(ProfileStorageKeyOf(characterProfile), _browserStorage.StoredItems.Keys);
        Assert.Contains(characterProfile.Identifier.ToString(), _browserStorage.StoredItems[IndexStorageKey]);
    }

    [Fact]
    public async Task SaveProfileAsync_ПовторноеСохранение_ПишетТолькоСамуАнкету()
    {
        // Смысл всей затеи: правка существующего героя не должна трогать
        // ни указатель, ни чужие анкеты. Иначе вернётся прежняя медленность.
        var firstProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Мири");
        var secondProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Лури");
        await _repositoryUnderTest.SaveProfileAsync(firstProfile);
        await _repositoryUnderTest.SaveProfileAsync(secondProfile);

        var writesBeforeEdit = _browserStorage.WriteOperationCount;
        firstProfile.Sections[0].Fields[0].PrimaryValue = "Мири";
        await _repositoryUnderTest.SaveProfileAsync(firstProfile);

        Assert.Equal(1, _browserStorage.WriteOperationCount - writesBeforeEdit);
    }

    [Fact]
    public async Task LoadAllProfilesAsync_СохранённыеАнкеты_ЧитаютсяВПорядкеДобавления()
    {
        var firstProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Мири");
        var secondProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Лури");
        await _repositoryUnderTest.SaveProfileAsync(firstProfile);
        await _repositoryUnderTest.SaveProfileAsync(secondProfile);

        var loadedProfiles = await _repositoryUnderTest.LoadAllProfilesAsync();

        Assert.Equal(["Мири", "Лури"], loadedProfiles.Select(profile => profile.DisplayName));
    }

    [Fact]
    public async Task RemoveProfileAsync_УбираетИАнкетуИЗаписьВУказателе()
    {
        var keptProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Мири");
        var removedProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Лишний");
        await _repositoryUnderTest.SaveProfileAsync(keptProfile);
        await _repositoryUnderTest.SaveProfileAsync(removedProfile);

        await _repositoryUnderTest.RemoveProfileAsync(removedProfile.Identifier);

        Assert.DoesNotContain(ProfileStorageKeyOf(removedProfile), _browserStorage.StoredItems.Keys);
        Assert.DoesNotContain(removedProfile.Identifier.ToString(), _browserStorage.StoredItems[IndexStorageKey]);
        var remainingProfile = Assert.Single(await _repositoryUnderTest.LoadAllProfilesAsync());
        Assert.Equal("Мири", remainingProfile.DisplayName);
    }

    [Fact]
    public async Task LoadAllProfilesAsync_ПовреждённаяОднаАнкета_ОстальныеВыживают()
    {
        var goodProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Мири");
        var brokenProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Битый");
        await _repositoryUnderTest.SaveProfileAsync(goodProfile);
        await _repositoryUnderTest.SaveProfileAsync(brokenProfile);
        _browserStorage.Seed(ProfileStorageKeyOf(brokenProfile), "{ это не json");

        var loadedProfiles = await _repositoryUnderTest.LoadAllProfilesAsync();

        var survivingProfile = Assert.Single(loadedProfiles);
        Assert.Equal("Мири", survivingProfile.DisplayName);
    }

    [Fact]
    public async Task LoadAllProfilesAsync_СтарыйФормат_ПереноситсяИСтарыйКлючУбирается()
    {
        var legacyProfiles = new List<CharacterProfile>
        {
            DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Мири"),
            DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Лури")
        };
        legacyProfiles[0].Sections[0].Fields[0].PrimaryValue = "Девочка с голубой лентой";
        _browserStorage.Seed(LegacyAllProfilesStorageKey, JsonSerializer.Serialize(legacyProfiles));

        var loadedProfiles = await _repositoryUnderTest.LoadAllProfilesAsync();

        Assert.Equal(["Мири", "Лури"], loadedProfiles.Select(profile => profile.DisplayName));
        Assert.Equal("Девочка с голубой лентой", loadedProfiles[0].Sections[0].Fields[0].PrimaryValue);
        Assert.DoesNotContain(LegacyAllProfilesStorageKey, _browserStorage.StoredItems.Keys);
        Assert.Contains(ProfileStorageKeyOf(legacyProfiles[0]), _browserStorage.StoredItems.Keys);
    }

    [Fact]
    public async Task LoadAllProfilesAsync_ПослеПереноса_ВторойЗапускЧитаетТеЖеДанные()
    {
        var legacyProfiles = new List<CharacterProfile>
        {
            DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Мири")
        };
        _browserStorage.Seed(LegacyAllProfilesStorageKey, JsonSerializer.Serialize(legacyProfiles));
        await _repositoryUnderTest.LoadAllProfilesAsync();

        // Второй запуск идёт уже по новому формату — старого ключа больше нет
        var reloadedProfiles = await _repositoryUnderTest.LoadAllProfilesAsync();

        var reloadedProfile = Assert.Single(reloadedProfiles);
        Assert.Equal("Мири", reloadedProfile.DisplayName);
        Assert.Equal(legacyProfiles[0].Identifier, reloadedProfile.Identifier);
    }

    [Fact]
    public async Task LoadAllProfilesAsync_ПовреждённыйСтарыйФормат_НеУдаляетИсходныеДанные()
    {
        // Если разобрать старое сохранение не вышло, единственное, чего делать
        // нельзя, — это стирать его: разобрать вручную ещё может быть можно
        _browserStorage.Seed(LegacyAllProfilesStorageKey, "{ это не json");

        var loadedProfiles = await _repositoryUnderTest.LoadAllProfilesAsync();

        Assert.Empty(loadedProfiles);
        Assert.Contains(LegacyAllProfilesStorageKey, _browserStorage.StoredItems.Keys);
    }
}
