/**
 * Репозиторий персонажей поверх localStorage.
 *
 * Единственная ответственность — чтение и запись данных.
 * Остальное приложение не знает, где именно лежат данные:
 * при желании localStorage можно заменить на сервер, поменяв только этот модуль
 * (принципы единственной ответственности и инверсии зависимостей).
 */

const STORAGE_KEY = 'gde-on-leg.characters.v1';
const THEME_KEY = 'gde-on-leg.theme';

export class CharacterRepository {
  /**
   * @param {Storage} storage — хранилище с интерфейсом localStorage
   *        (подменяется в тестах — принцип подстановки Лисков)
   */
  constructor(storage = window.localStorage) {
    this._storage = storage;
  }

  /**
   * Загружает состояние приложения.
   * @returns {{characters: object[], activeId: string|null}}
   */
  load() {
    try {
      const raw = this._storage.getItem(STORAGE_KEY);
      if (!raw) {
        console.info('[Хранилище] Сохранённых данных нет — начинаем с чистого листа');
        return { characters: [], activeId: null };
      }
      const data = JSON.parse(raw);
      if (!data || !Array.isArray(data.characters)) {
        throw new Error('неожиданная структура данных');
      }
      console.info(`[Хранилище] Загружено персонажей: ${data.characters.length}`);
      return { characters: data.characters, activeId: data.activeId ?? null };
    } catch (err) {
      console.error('[Хранилище] Не удалось прочитать данные:', err.message);
      return { characters: [], activeId: null };
    }
  }

  /**
   * Сохраняет состояние приложения.
   * @param {{characters: object[], activeId: string|null}} state
   * @returns {boolean} получилось ли сохранить
   */
  save(state) {
    try {
      this._storage.setItem(STORAGE_KEY, JSON.stringify(state));
      return true;
    } catch (err) {
      // Чаще всего сюда попадаем при переполнении хранилища
      console.error('[Хранилище] Не удалось сохранить данные:', err.message);
      return false;
    }
  }

  /** @returns {string|null} сохранённая тема оформления */
  loadTheme() {
    try {
      return this._storage.getItem(THEME_KEY);
    } catch {
      return null;
    }
  }

  /** Запоминает тему оформления */
  saveTheme(theme) {
    try {
      this._storage.setItem(THEME_KEY, theme);
    } catch (err) {
      console.error('[Хранилище] Не удалось сохранить тему:', err.message);
    }
  }
}
