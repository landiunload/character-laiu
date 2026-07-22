using System.Text.Json;
using Charlaiu.Web.Domain;
using Microsoft.JSInterop;

namespace Charlaiu.Web.Services;

/// <summary>
/// Хранилище анкет в localStorage браузера.
/// Данные никуда не отправляются и живут только на устройстве пользователя —
/// ключевое свойство приложения, унаследованное от первой версии.
///
/// Каждая анкета лежит под собственным ключом, а отдельный ключ-указатель
/// хранит их порядок. Так правка одного героя стоит одной сериализации его
/// анкеты, а не всей коллекции: раньше всё лежало одним массивом, и цена
/// нажатия клавиши росла линейно от общего объёма — 0,56 с при тридцати
/// заполненных анкетах против 20 мс при одной.
/// </summary>
public sealed class BrowserLocalStorageCharacterProfileRepository(IJSRuntime javascriptRuntime)
    : ICharacterProfileRepository
{
    /// <summary>Ключ со списком идентификаторов анкет в порядке добавления.</summary>
    private const string IndexStorageKey = "charlaiu.characterProfileIndex";

    /// <summary>Префикс ключа отдельной анкеты; дальше идёт её идентификатор.</summary>
    private const string ProfileStorageKeyPrefix = "charlaiu.characterProfile.";

    /// <summary>Ключ первой версии формата: все анкеты одним массивом.</summary>
    private const string LegacyAllProfilesStorageKey = "charlaiu.characterProfiles";

    /// <inheritdoc />
    public async Task<List<CharacterProfile>> LoadAllProfilesAsync()
    {
        var profileIdentifiers = await ReadIndexAsync();

        // Указателя нет — либо сохранений нет вовсе, либо они в старом формате
        if (profileIdentifiers.Count == 0)
        {
            return await MigrateFromLegacyFormatAsync();
        }

        var loadedProfiles = new List<CharacterProfile>(profileIdentifiers.Count);
        foreach (var profileIdentifier in profileIdentifiers)
        {
            if (await ReadProfileAsync(profileIdentifier) is { } loadedProfile)
            {
                loadedProfiles.Add(loadedProfile);
            }
        }

        return loadedProfiles;
    }

    /// <inheritdoc />
    public async Task SaveProfileAsync(CharacterProfile characterProfile)
    {
        await WriteProfileAsync(characterProfile);

        // Указатель трогаем, только когда состав действительно поменялся: на
        // горячем пути (правка существующей анкеты) это была бы лишняя запись
        var profileIdentifiers = await ReadIndexAsync();
        if (!profileIdentifiers.Contains(characterProfile.Identifier))
        {
            profileIdentifiers.Add(characterProfile.Identifier);
            await WriteIndexAsync(profileIdentifiers);
        }
    }

    /// <inheritdoc />
    public async Task RemoveProfileAsync(Guid profileIdentifier)
    {
        var profileIdentifiers = await ReadIndexAsync();
        if (profileIdentifiers.Remove(profileIdentifier))
        {
            await WriteIndexAsync(profileIdentifiers);
        }

        await javascriptRuntime.InvokeVoidAsync(
            "localStorage.removeItem", StorageKeyOf(profileIdentifier));
    }

    /// <summary>
    /// Переносит сохранения первой версии формата в новый и убирает старый ключ.
    /// Старый ключ удаляется последним: если запись оборвётся на середине,
    /// исходные данные останутся нетронутыми и перенос повторится при следующем
    /// запуске.
    /// </summary>
    private async Task<List<CharacterProfile>> MigrateFromLegacyFormatAsync()
    {
        var legacyJson = await javascriptRuntime.InvokeAsync<string?>(
            "localStorage.getItem", LegacyAllProfilesStorageKey);

        if (string.IsNullOrWhiteSpace(legacyJson))
        {
            return [];
        }

        List<CharacterProfile> legacyProfiles;
        try
        {
            legacyProfiles = JsonSerializer.Deserialize(
                legacyJson, CharlaiuJsonContext.Default.ListCharacterProfile) ?? [];
        }
        catch (JsonException)
        {
            // Повреждённые данные не должны ломать приложение. Старый ключ
            // остаётся нетронутым: разобрать его вручную ещё может быть можно
            return [];
        }

        foreach (var legacyProfile in legacyProfiles)
        {
            await WriteProfileAsync(legacyProfile);
        }

        await WriteIndexAsync(legacyProfiles.Select(profile => profile.Identifier).ToList());
        await javascriptRuntime.InvokeVoidAsync(
            "localStorage.removeItem", LegacyAllProfilesStorageKey);

        return legacyProfiles;
    }

    private static string StorageKeyOf(Guid profileIdentifier) =>
        ProfileStorageKeyPrefix + profileIdentifier;

    private async Task<List<Guid>> ReadIndexAsync()
    {
        var indexJson = await javascriptRuntime.InvokeAsync<string?>(
            "localStorage.getItem", IndexStorageKey);

        if (string.IsNullOrWhiteSpace(indexJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize(indexJson, CharlaiuJsonContext.Default.ListGuid) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private async Task WriteIndexAsync(List<Guid> profileIdentifiers)
    {
        var indexJson = JsonSerializer.Serialize(
            profileIdentifiers, CharlaiuJsonContext.Default.ListGuid);
        await javascriptRuntime.InvokeVoidAsync("localStorage.setItem", IndexStorageKey, indexJson);
    }

    private async Task<CharacterProfile?> ReadProfileAsync(Guid profileIdentifier)
    {
        var profileJson = await javascriptRuntime.InvokeAsync<string?>(
            "localStorage.getItem", StorageKeyOf(profileIdentifier));

        if (string.IsNullOrWhiteSpace(profileJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(profileJson, CharlaiuJsonContext.Default.CharacterProfile);
        }
        catch (JsonException)
        {
            // Одна повреждённая анкета не должна уносить с собой остальные
            return null;
        }
    }

    private async Task WriteProfileAsync(CharacterProfile characterProfile)
    {
        var profileJson = JsonSerializer.Serialize(
            characterProfile, CharlaiuJsonContext.Default.CharacterProfile);
        await javascriptRuntime.InvokeVoidAsync(
            "localStorage.setItem", StorageKeyOf(characterProfile.Identifier), profileJson);
    }
}
