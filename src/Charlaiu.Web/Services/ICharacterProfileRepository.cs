using Charlaiu.Web.Domain;

namespace Charlaiu.Web.Services;

/// <summary>
/// Контракт хранилища анкет.
/// Интерфейс позволяет подменить реализацию (localStorage, файл, сервер)
/// без изменения компонентов — принцип инверсии зависимостей.
///
/// Запись идёт по одному герою. Раньше здесь был метод «сохранить всех», и он
/// заставлял платить за правку одного символа перезаписью всей коллекции:
/// замер показал 0,56 с на нажатие клавиши при тридцати заполненных анкетах.
/// </summary>
public interface ICharacterProfileRepository
{
    /// <summary>Загружает все сохранённые анкеты в порядке добавления.</summary>
    Task<List<CharacterProfile>> LoadAllProfilesAsync();

    /// <summary>
    /// Сохраняет одну анкету. Ранее неизвестная анкета добавляется в конец списка.
    /// </summary>
    Task SaveProfileAsync(CharacterProfile characterProfile);

    /// <summary>Удаляет анкету вместе с её местом в списке.</summary>
    Task RemoveProfileAsync(Guid profileIdentifier);
}
