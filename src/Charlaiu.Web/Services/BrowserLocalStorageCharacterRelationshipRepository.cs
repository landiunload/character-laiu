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
            return JsonSerializer.Deserialize(storedJson, CharlaiuJsonContext.Default.ListCharacterRelationship) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <inheritdoc />
    public async Task SaveAllRelationshipsAsync(List<CharacterRelationship> characterRelationships)
    {
        var serializedRelationships = JsonSerializer.Serialize(characterRelationships, CharlaiuJsonContext.Default.ListCharacterRelationship);
        await javascriptRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, serializedRelationships);
    }
}
