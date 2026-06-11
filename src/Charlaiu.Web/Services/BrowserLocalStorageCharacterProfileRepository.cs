using System.Text.Json;
using Charlaiu.Web.Domain;
using Microsoft.JSInterop;

namespace Charlaiu.Web.Services;

/// <summary>
/// Хранилище анкет в localStorage браузера.
/// Данные никуда не отправляются и живут только на устройстве пользователя —
/// ключевое свойство приложения, унаследованное от первой версии.
/// </summary>
public sealed class BrowserLocalStorageCharacterProfileRepository(IJSRuntime javascriptRuntime)
    : ICharacterProfileRepository
{
    private const string StorageKey = "charlaiu.characterProfiles";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = false
    };

    /// <inheritdoc />
    public async Task<List<CharacterProfile>> LoadAllProfilesAsync()
    {
        var storedJson = await javascriptRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);

        if (string.IsNullOrWhiteSpace(storedJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<CharacterProfile>>(storedJson, SerializerOptions) ?? [];
        }
        catch (JsonException)
        {
            // Повреждённые данные не должны ломать приложение — начинаем с чистого списка
            return [];
        }
    }

    /// <inheritdoc />
    public async Task SaveAllProfilesAsync(List<CharacterProfile> characterProfiles)
    {
        var serializedProfiles = JsonSerializer.Serialize(characterProfiles, SerializerOptions);
        await javascriptRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, serializedProfiles);
    }
}
