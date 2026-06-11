/**
 * Точка входа и контроллер приложения.
 *
 * Связывает модули между собой: схему (schema), хранилище (repository),
 * отрисовку (render) и экспорт/импорт (transfer). Сами модули друг о друге
 * не знают — контроллер передаёт данные и обработчики (инверсия зависимостей).
 */

import { createCharacter, createField, createSection } from './schema.js';
import { CharacterRepository } from './repository.js';
import { renderCharacterList, renderSheet } from './render.js';
import { toJSON, fromJSON, toMarkdown } from './transfer.js';
import { debounce, downloadFile, safeFilename } from './utils.js';

class App {
  constructor() {
    this.repo = new CharacterRepository();
    this.state = this.repo.load();

    // Ссылки на элементы интерфейса
    this.dom = {
      list: document.getElementById('character-list'),
      sheet: document.getElementById('sheet'),
      empty: document.getElementById('empty-state'),
      topbar: document.querySelector('.topbar'),
      sidebar: document.getElementById('sidebar'),
      saveIndicator: document.getElementById('save-indicator'),
      importInput: document.getElementById('import-input'),
    };

    // Автосохранение с паузой, чтобы не писать в хранилище на каждый символ
    this.saveSoon = debounce(() => this.save(), 400);

    this.applyTheme(this.repo.loadTheme());
    this.bindToolbar();
    this.renderAll();

    // Страховка: если вкладку закрывают раньше, чем сработает отложенное
    // автосохранение, записываем состояние немедленно
    window.addEventListener('pagehide', () => this.save());

    console.info('[Приложение] Готово к работе');
  }

  /* ───────────── Состояние ───────────── */

  /** Текущий выбранный персонаж */
  get active() {
    return this.state.characters.find((c) => c.id === this.state.activeId) || null;
  }

  /** Сохраняет состояние и показывает индикатор */
  save() {
    const ch = this.active;
    if (ch) ch.updatedAt = new Date().toISOString();
    const ok = this.repo.save(this.state);
    this.flashIndicator(ok ? 'Сохранено' : 'Ошибка сохранения!');
  }

  /** Кратко показывает статус сохранения */
  flashIndicator(text) {
    this.dom.saveIndicator.textContent = text;
    clearTimeout(this._indicatorTimer);
    this._indicatorTimer = setTimeout(() => {
      this.dom.saveIndicator.textContent = '';
    }, 1500);
  }

  /* ───────────── Отрисовка ───────────── */

  /** Перерисовывает и список, и анкету */
  renderAll() {
    this.renderList();
    this.renderActiveSheet();
  }

  renderList() {
    renderCharacterList(this.dom.list, this.state.characters, this.state.activeId, (id) => {
      this.state.activeId = id;
      this.save();
      this.renderAll();
      // На узких экранах после выбора прячем панель
      this.dom.sidebar.classList.remove('sidebar--open');
    });
  }

  renderActiveSheet() {
    const ch = this.active;
    const hasCharacter = Boolean(ch);
    this.dom.empty.hidden = hasCharacter;
    // Без персонажа прячем кнопки действий, но кнопка меню остаётся доступной
    this.dom.topbar.classList.toggle('topbar--empty', !hasCharacter);
    this.dom.sheet.textContent = '';
    if (!hasCharacter) return;

    renderSheet(this.dom.sheet, ch, {
      // Изменение текста: сохраняем; имя дополнительно обновляет список
      onChange: (opts) => {
        this.saveSoon();
        if (opts && opts.nameChanged) this.renderList();
      },
      onAddField: (sectionId) => {
        const section = ch.sections.find((s) => s.id === sectionId);
        if (!section) return;
        section.fields.push(createField());
        this.afterStructureChange();
      },
      onRemoveField: (sectionId, fieldId) => {
        const section = ch.sections.find((s) => s.id === sectionId);
        if (!section) return;
        section.fields = section.fields.filter((f) => f.id !== fieldId);
        this.afterStructureChange();
      },
      onAddSection: () => {
        ch.sections.push(createSection());
        this.afterStructureChange();
        this.dom.sheet.lastElementChild?.scrollIntoView({ behavior: 'smooth' });
      },
      onRemoveSection: (sectionId) => {
        const section = ch.sections.find((s) => s.id === sectionId);
        if (!section) return;
        if (!confirm(`Удалить раздел «${section.title}» со всеми полями?`)) return;
        ch.sections = ch.sections.filter((s) => s.id !== sectionId);
        this.afterStructureChange();
      },
    });
  }

  /** Общие действия после изменения структуры анкеты */
  afterStructureChange() {
    this.save();
    this.renderActiveSheet();
  }

  /* ───────────── Действия панелей ───────────── */

  bindToolbar() {
    const on = (id, handler) => document.getElementById(id).addEventListener('click', handler);

    on('btn-add-character', () => this.addCharacter());
    on('btn-delete', () => this.deleteActive());
    on('btn-duplicate', () => this.duplicateActive());
    on('btn-export-json', () => this.exportActiveJSON());
    on('btn-export-md', () => this.exportActiveMarkdown());
    on('btn-export-all', () => this.exportAll());
    on('btn-import', () => this.dom.importInput.click());
    on('btn-theme', () => this.toggleTheme());
    on('btn-toggle-sidebar', () => this.dom.sidebar.classList.toggle('sidebar--open'));

    this.dom.importInput.addEventListener('change', () => this.importFromFile());
  }

  addCharacter() {
    const ch = createCharacter();
    this.state.characters.push(ch);
    this.state.activeId = ch.id;
    this.save();
    this.renderAll();
    // На узких экранах после создания закрываем панель, чтобы показать анкету
    this.dom.sidebar.classList.remove('sidebar--open');
    console.info('[Приложение] Создан новый персонаж');
    // Сразу ставим курсор в поле имени
    this.dom.sheet.querySelector('.sheet__name')?.focus();
  }

  deleteActive() {
    const ch = this.active;
    if (!ch) return;
    if (!confirm(`Удалить персонажа «${ch.name}»? Действие необратимо.`)) return;
    this.state.characters = this.state.characters.filter((c) => c.id !== ch.id);
    this.state.activeId = this.state.characters[0]?.id ?? null;
    this.save();
    this.renderAll();
    console.info(`[Приложение] Персонаж «${ch.name}» удалён`);
  }

  duplicateActive() {
    const ch = this.active;
    if (!ch) return;
    // Глубокая копия через проверенный импортом путь — даёт новые идентификаторы
    const [copy] = fromJSON(JSON.stringify([ch]));
    copy.name = ch.name + ' (копия)';
    this.state.characters.push(copy);
    this.state.activeId = copy.id;
    this.save();
    this.renderAll();
    console.info(`[Приложение] Создана копия персонажа «${ch.name}»`);
  }

  /* ───────────── Экспорт и импорт ───────────── */

  exportActiveJSON() {
    const ch = this.active;
    if (!ch) return;
    downloadFile(safeFilename(ch.name) + '.json', toJSON([ch]));
    console.info(`[Экспорт] Персонаж «${ch.name}» выгружен в JSON`);
  }

  exportActiveMarkdown() {
    const ch = this.active;
    if (!ch) return;
    downloadFile(safeFilename(ch.name) + '.md', toMarkdown(ch), 'text/markdown');
    console.info(`[Экспорт] Персонаж «${ch.name}» выгружен в Markdown`);
  }

  exportAll() {
    if (this.state.characters.length === 0) return;
    downloadFile('все-персонажи.json', toJSON(this.state.characters));
    console.info(`[Экспорт] Выгружено персонажей: ${this.state.characters.length}`);
  }

  async importFromFile() {
    const file = this.dom.importInput.files?.[0];
    this.dom.importInput.value = ''; // позволяем выбрать тот же файл повторно
    if (!file) return;

    try {
      const text = await file.text();
      const imported = fromJSON(text);
      this.state.characters.push(...imported);
      this.state.activeId = imported[0].id;
      this.save();
      this.renderAll();
      this.flashIndicator(`Импортировано: ${imported.length}`);
    } catch (err) {
      console.error('[Импорт] Ошибка:', err.message);
      alert('Не удалось импортировать файл: ' + err.message);
    }
  }

  /* ───────────── Тема оформления ───────────── */

  applyTheme(theme) {
    // По умолчанию следуем настройке системы; явный выбор пользователя важнее
    if (theme === 'dark' || theme === 'light') {
      document.documentElement.dataset.theme = theme;
    }
  }

  toggleTheme() {
    const current = document.documentElement.dataset.theme
      || (matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
    const next = current === 'dark' ? 'light' : 'dark';
    document.documentElement.dataset.theme = next;
    this.repo.saveTheme(next);
    console.info(`[Приложение] Тема переключена: ${next === 'dark' ? 'тёмная' : 'светлая'}`);
  }
}

// Запуск после построения DOM (скрипт подключён как module — DOM уже готов)
new App();
