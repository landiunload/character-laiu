using Charlaiu.Web.Domain;

namespace Charlaiu.Web.Services;

/// <summary>
/// Контракт хранилища анкет.
/// Интерфейс позволяет подменить реализацию (localStorage, файл, сервер)
/// без изменения компонентов — принцип инверсии зависимостей.
/// </summary>
public interface ICharacterProfileRepository
{
    /// <summary>Загружает все сохранённые анкеты.</summary>
    Task<List<CharacterProfile>> LoadAllProfilesAsync();

    /// <summary>Сохраняет все анкеты целиком.</summary>
    Task SaveAllProfilesAsync(List<CharacterProfile> characterProfiles);
}
