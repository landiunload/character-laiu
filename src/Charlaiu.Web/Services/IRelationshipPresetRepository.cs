using Charlaiu.Web.Domain;

namespace Charlaiu.Web.Services;

/// <summary>Хранилище заготовок связей (название + цвет).</summary>
public interface IRelationshipPresetRepository
{
    /// <summary>Загружает заготовки; если их ещё нет — возвращает набор по умолчанию.</summary>
    Task<List<RelationshipPreset>> LoadAllPresetsAsync();

    /// <summary>Сохраняет все заготовки.</summary>
    Task SaveAllPresetsAsync(List<RelationshipPreset> relationshipPresets);
}
