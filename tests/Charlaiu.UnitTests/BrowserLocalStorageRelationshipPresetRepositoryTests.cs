using Charlaiu.Web.Domain;
using Charlaiu.Web.Services;
using Xunit;

namespace Charlaiu.UnitTests;

/// <summary>
/// Тесты хранилища заготовок связей. Всё поведение держится на откате к
/// заготовкам по умолчанию, а его до сих пор не проверял ни один тест: пустое
/// хранилище, повреждённый JSON и — что не очевидно — сохранённый пустой список
/// одинаково возвращают набор по умолчанию, чтобы автор никогда не остался без
/// типов связей. Заглушка localStorage — та же, что у хранилища анкет.
/// </summary>
public sealed class BrowserLocalStorageRelationshipPresetRepositoryTests
{
    private const string StorageKey = "charlaiu.relationshipPresets";

    private readonly FakeBrowserLocalStorage _browserStorage = new();
    private readonly BrowserLocalStorageRelationshipPresetRepository _repositoryUnderTest;

    public BrowserLocalStorageRelationshipPresetRepositoryTests() =>
        _repositoryUnderTest = new BrowserLocalStorageRelationshipPresetRepository(_browserStorage);

    [Fact]
    public async Task LoadAllPresetsAsync_ПустоеХранилище_ВозвращаетЗаготовкиПоУмолчанию()
    {
        var loadedPresets = await _repositoryUnderTest.LoadAllPresetsAsync();

        Assert.Equal(RelationshipPreset.CreateDefaultPresets().Count, loadedPresets.Count);
        Assert.Contains(loadedPresets, preset => preset.Name == "Дружба");
    }

    [Fact]
    public async Task LoadAllPresetsAsync_ПовреждённыйJson_ВозвращаетЗаготовкиПоУмолчанию()
    {
        // Повреждение не должно оставить автора без типов связей
        _browserStorage.Seed(StorageKey, "{ это не json");

        var loadedPresets = await _repositoryUnderTest.LoadAllPresetsAsync();

        Assert.Equal(RelationshipPreset.CreateDefaultPresets().Count, loadedPresets.Count);
    }

    [Fact]
    public async Task LoadAllPresetsAsync_СохранёнПустойСписок_ВозвращаетЗаготовкиПоУмолчанию()
    {
        // Тонкая ветка `{ Count: > 0 }`: пустой сохранённый список — это не «автор
        // осознанно удалил все типы», а негодные данные; откатываемся к умолчанию.
        await _repositoryUnderTest.SaveAllPresetsAsync([]);

        var loadedPresets = await _repositoryUnderTest.LoadAllPresetsAsync();

        Assert.NotEmpty(loadedPresets);
        Assert.Equal(RelationshipPreset.CreateDefaultPresets().Count, loadedPresets.Count);
    }

    [Fact]
    public async Task SaveThenLoad_СобственныеЗаготовки_ЧитаютсяОбратно()
    {
        var customPresets = new List<RelationshipPreset>
        {
            new() { Name = "Наставничество", ColorHex = "#2f6f9f" },
            new() { Name = "Долг", ColorHex = "#7a4fb0" }
        };

        await _repositoryUnderTest.SaveAllPresetsAsync(customPresets);
        var loadedPresets = await _repositoryUnderTest.LoadAllPresetsAsync();

        Assert.Equal(
            customPresets.Select(preset => (preset.Name, preset.ColorHex)),
            loadedPresets.Select(preset => (preset.Name, preset.ColorHex)));
    }

    [Fact]
    public async Task SaveAllPresetsAsync_ПишетПодСвоимКлючом()
    {
        await _repositoryUnderTest.SaveAllPresetsAsync(RelationshipPreset.CreateDefaultPresets());

        Assert.Contains(StorageKey, _browserStorage.StoredItems.Keys);
    }
}
