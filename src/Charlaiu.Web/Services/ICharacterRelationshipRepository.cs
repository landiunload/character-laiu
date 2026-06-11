using Charlaiu.Web.Domain;

namespace Charlaiu.Web.Services;

/// <summary>Контракт хранилища связей между персонажами.</summary>
public interface ICharacterRelationshipRepository
{
    /// <summary>Загружает все сохранённые связи.</summary>
    Task<List<CharacterRelationship>> LoadAllRelationshipsAsync();

    /// <summary>Сохраняет все связи целиком.</summary>
    Task SaveAllRelationshipsAsync(List<CharacterRelationship> characterRelationships);
}
