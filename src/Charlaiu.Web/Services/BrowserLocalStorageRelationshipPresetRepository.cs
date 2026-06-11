using System.Text.Json;
using Charlaiu.Web.Domain;
using Microsoft.JSInterop;

namespace Charlaiu.Web.Services;

/// <summary>Хранилище заготовок связей в localStorage браузера.</summary>
public sealed class BrowserLocalStorageRelationshipPresetRepository(IJSRuntime javascriptRuntime)
    : IRelationshipPresetRepository
{
    private const string StorageKey = "charlaiu.relationshipPresets";

    /// <inheritdoc />
    public async Task<List<RelationshipPreset>> LoadAllPresetsAsync()
    {
        var storedJson = await javascriptRuntime.InvokeAsync<string?>("localStorage.getItem", StorageKey);

        if (string.IsNullOrWhiteSpace(storedJson))
        {
            return RelationshipPreset.CreateDefaultPresets();
        }

        try
        {
            var loadedPresets = JsonSerializer.Deserialize<List<RelationshipPreset>>(storedJson);
            return loadedPresets is { Count: > 0 } ? loadedPresets : RelationshipPreset.CreateDefaultPresets();
        }
        catch (JsonException)
        {
            return RelationshipPreset.CreateDefaultPresets();
        }
    }

    /// <inheritdoc />
    public async Task SaveAllPresetsAsync(List<RelationshipPreset> relationshipPresets)
    {
        var serializedPresets = JsonSerializer.Serialize(relationshipPresets);
        await javascriptRuntime.InvokeVoidAsync("localStorage.setItem", StorageKey, serializedPresets);
    }
}
