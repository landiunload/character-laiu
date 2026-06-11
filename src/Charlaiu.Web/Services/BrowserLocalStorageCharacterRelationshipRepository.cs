using System.Text.Json;
using Charlaiu.Web.Domain;
using Microsoft.JSInterop;

namespace Charlaiu.Web.Services;

/// <summary>Хранилище связей персонажей в localStorage браузера.</summary>
public sealed class BrowserLocalStorageCharacterRelationshipRepository(IJSRuntime javascriptRuntime)
    : ICharacterRelationshipRepository
{
    private const string StorageKey = "charlaiu.characterRelationships";

    /// <inheritdoc />
    public async Task<List<CharacterRelationship>> LoadAllRelationshipsAsync()
    {
        var storedJson = await javascriptRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);

        if (string.IsNullOrWhiteSpace(storedJson))
        {
            return [];
        }

        try
        {
            return JsonSerializer.Deserialize<List<CharacterRelationship>>(storedJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public async Task SaveAllRelationshipsAsync(List<CharacterRelationship> characterRelationships)
    {
        var serializedRelationships = JsonSerializer.Serialize(characterRelationships);
        await javascriptRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, serializedRelationships);
    }
}
