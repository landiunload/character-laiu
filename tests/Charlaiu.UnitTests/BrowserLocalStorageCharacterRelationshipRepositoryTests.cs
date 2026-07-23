using Charlaiu.Web.Domain;
using Charlaiu.Web.Services;
using Xunit;

namespace Charlaiu.UnitTests;

/// <summary>
/// Тесты хранилища связей персонажей. В отличие от заготовок, пустое или
/// повреждённое хранилище здесь даёт пустой список, а не набор по умолчанию:
/// у нового автора связей просто нет. Проверяется формат хранения и то, что
/// негодные данные не роняют загрузку. Заглушка localStorage — общая с анкетами.
/// </summary>
public sealed class BrowserLocalStorageCharacterRelationshipRepositoryTests
{
    private const string StorageKey = "charlaiu.characterRelationships";

    private readonly FakeBrowserLocalStorage _browserStorage = new();
    private readonly BrowserLocalStorageCharacterRelationshipRepository _repositoryUnderTest;

    public BrowserLocalStorageCharacterRelationshipRepositoryTests() =>
        _repositoryUnderTest = new BrowserLocalStorageCharacterRelationshipRepository(_browserStorage);

    [Fact]
    public async Task LoadAllRelationshipsAsync_ПустоеХранилище_ПустойСписок()
    {
        var loadedRelationships = await _repositoryUnderTest.LoadAllRelationshipsAsync();

        Assert.Empty(loadedRelationships);
    }

    [Fact]
    public async Task LoadAllRelationshipsAsync_ПовреждённыйJson_ПустойСписок()
    {
        // Повреждение не должно ронять загрузку исключением
        _browserStorage.Seed(StorageKey, "{ это не json");

        var loadedRelationships = await _repositoryUnderTest.LoadAllRelationshipsAsync();

        Assert.Empty(loadedRelationships);
    }

    [Fact]
    public async Task SaveThenLoad_Связи_ЧитаютсяОбратноБезПотериПолей()
    {
        var relationship = new CharacterRelationship
        {
            FirstCharacterIdentifier = Guid.CreateVersion7(),
            SecondCharacterIdentifier = Guid.CreateVersion7(),
            Label = "дружба с детства",
            ColorHex = "#3a9d5d",
            IsOneDirectional = true
        };

        await _repositoryUnderTest.SaveAllRelationshipsAsync([relationship]);
        var loadedRelationships = await _repositoryUnderTest.LoadAllRelationshipsAsync();

        var loadedRelationship = Assert.Single(loadedRelationships);
        Assert.Equal(relationship.Identifier, loadedRelationship.Identifier);
        Assert.Equal(relationship.FirstCharacterIdentifier, loadedRelationship.FirstCharacterIdentifier);
        Assert.Equal(relationship.SecondCharacterIdentifier, loadedRelationship.SecondCharacterIdentifier);
        Assert.Equal("дружба с детства", loadedRelationship.Label);
        Assert.Equal("#3a9d5d", loadedRelationship.ColorHex);
        Assert.True(loadedRelationship.IsOneDirectional);
    }

    [Fact]
    public async Task SaveAllRelationshipsAsync_ПишетПодСвоимКлючом()
    {
        await _repositoryUnderTest.SaveAllRelationshipsAsync([]);

        Assert.Contains(StorageKey, _browserStorage.StoredItems.Keys);
    }
}
