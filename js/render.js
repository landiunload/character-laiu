/**
 * Отрисовка интерфейса.
 *
 * Единственная ответственность — построение DOM по данным персонажа.
 * Безопасность: весь пользовательский текст попадает в страницу только
 * через textContent и свойство value — разметка из данных никогда не выполняется.
 *
 * Модуль ничего не знает о хранилище: обо всех изменениях он сообщает
 * через переданные обработчики (принцип инверсии зависимостей).
 */

/**
 * @typedef {object} SheetHandlers
 * @property {Function} onChange        — изменился текст (нужно автосохранение)
 * @property {Function} onAddField      — добавить поле в раздел
 * @property {Function} onRemoveField   — удалить поле из раздела
 * @property {Function} onAddSection    — добавить раздел
 * @property {Function} onRemoveSection — удалить раздел
 */

/* ───────────────────────── Список персонажей ───────────────────────── */

/**
 * Рисует список персонажей в боковой панели.
 * @param {HTMLElement} container
 * @param {object[]} characters
 * @param {string|null} activeId
 * @param {(id: string) => void} onSelect
 */
export function renderCharacterList(container, characters, activeId, onSelect) {
  container.textContent = '';
  for (const ch of characters) {
    const item = el('button', 'character-item');
    if (ch.id === activeId) item.classList.add('character-item--active');
    item.type = 'button';

    const name = el('span', 'character-item__name');
    name.textContent = ch.name || 'Без имени';

    const meta = el('span', 'character-item__meta');
    meta.textContent = firstFilledField(ch) || 'анкета не заполнена';

    item.append(name, meta);
    item.addEventListener('click', () => onSelect(ch.id));
    container.append(item);
  }
}

/** Возвращает первое заполненное короткое поле — для подписи в списке */
function firstFilledField(character) {
  for (const sec of character.sections) {
    for (const f of sec.fields) {
      if (f.type !== 'pair' && typeof f.value === 'string') {
        const v = f.value.trim();
        if (v) return v.length > 40 ? v.slice(0, 40) + '…' : v;
      }
    }
  }
  return '';
}

/* ───────────────────────── Анкета ───────────────────────── */

/**
 * Рисует анкету персонажа целиком.
 * @param {HTMLElement} container
 * @param {object} character
 * @param {SheetHandlers} handlers
 */
export function renderSheet(container, character, handlers) {
  container.textContent = '';

  container.append(renderNameInput(character, handlers));

  for (const section of character.sections) {
    container.append(renderSection(character, section, handlers));
  }

  // Кнопка добавления нового раздела в конце анкеты
  const addSection = el('button', 'btn btn--dashed sheet__add-section');
  addSection.type = 'button';
  addSection.textContent = '+ Добавить раздел';
  addSection.addEventListener('click', () => handlers.onAddSection());
  container.append(addSection);
}

/** Поле имени персонажа — крупный заголовок анкеты */
function renderNameInput(character, handlers) {
  const wrap = el('div', 'sheet__name-wrap');
  const input = el('input', 'sheet__name');
  input.type = 'text';
  input.placeholder = 'Имя героя…';
  input.value = character.name === 'Новый герой' ? '' : character.name;
  input.setAttribute('aria-label', 'Имя персонажа');
  input.addEventListener('input', () => {
    character.name = input.value.trim() || 'Новый герой';
    handlers.onChange({ nameChanged: true });
  });
  wrap.append(input);
  return wrap;
}

/** Один раздел анкеты */
function renderSection(character, section, handlers) {
  const card = el('section', 'card');

  // Шапка раздела: иконка, редактируемый заголовок, удаление
  const head = el('header', 'card__head');

  const icon = el('span', 'card__icon');
  icon.textContent = section.icon;

  const title = el('input', 'card__title');
  title.type = 'text';
  title.value = section.title;
  title.setAttribute('aria-label', 'Название раздела');
  title.addEventListener('input', () => {
    section.title = title.value;
    handlers.onChange();
  });

  const removeBtn = iconButton('✕', 'Удалить раздел', () => {
    handlers.onRemoveSection(section.id);
  });

  head.append(icon, title, removeBtn);
  card.append(head);

  // Подпись колонок для разделов-таблиц («Любит / Не любит»)
  if (section.columns) {
    const cols = el('div', 'pair-columns');
    cols.append(el('span', 'pair-columns__spacer'));
    for (const col of section.columns) {
      const c = el('span', 'pair-columns__label');
      c.textContent = col;
      cols.append(c);
    }
    card.append(cols);
  }

  // Поля раздела
  const list = el('div', 'card__fields');
  for (const field of section.fields) {
    list.append(renderField(section, field, handlers));
  }
  card.append(list);

  // Кнопка добавления поля
  const addField = el('button', 'btn btn--dashed btn--small card__add-field');
  addField.type = 'button';
  addField.textContent = '+ поле';
  addField.addEventListener('click', () => handlers.onAddField(section.id));
  card.append(addField);

  // Авторская подсказка под разделом
  if (section.hint) {
    const hint = el('p', 'card__hint');
    hint.textContent = section.hint;
    card.append(hint);
  }

  return card;
}

/** Одно поле анкеты */
function renderField(section, field, handlers) {
  const row = el('div', 'field');
  if (field.type === 'pair') row.classList.add('field--pair');

  // Редактируемая подпись поля — растущая textarea, чтобы длинные подписи не обрезались
  const label = autoTextarea(field.label, 'Поле');
  label.className = 'field__label';
  label.setAttribute('aria-label', 'Название поля');
  label.addEventListener('input', () => {
    field.label = label.value;
    handlers.onChange();
  });
  row.append(label);

  // Значение поля — в зависимости от типа
  if (field.type === 'pair') {
    const pair = el('div', 'field__pair');
    for (const i of [0, 1]) {
      const input = autoTextarea(field.value[i], section.columns?.[i] || '');
      input.addEventListener('input', () => {
        field.value[i] = input.value;
        handlers.onChange();
      });
      pair.append(input);
    }
    row.append(pair);
  } else {
    const input = autoTextarea(field.value, '…');
    if (field.type === 'text') input.classList.add('field__input--single');
    input.addEventListener('input', () => {
      field.value = input.value;
      handlers.onChange();
    });
    row.append(input);
  }

  // Кнопка удаления поля
  row.append(iconButton('✕', 'Удалить поле', () => {
    handlers.onRemoveField(section.id, field.id);
  }));

  return row;
}

/**
 * Создаёт textarea, растущую под содержимое, — выглядит как строка,
 * но принимает сколько угодно текста.
 */
function autoTextarea(value, placeholder) {
  const input = el('textarea', 'field__input');
  input.rows = 1;
  input.placeholder = placeholder;
  input.value = value || '';
  const resize = () => {
    input.style.height = 'auto';
    input.style.height = input.scrollHeight + 'px';
  };
  input.addEventListener('input', resize);
  // Первичная подгонка высоты после вставки в документ
  requestAnimationFrame(resize);
  return input;
}

/* ───────────────────────── Мелкие помощники ───────────────────────── */

/** Создаёт элемент с классом */
function el(tag, className) {
  const node = document.createElement(tag);
  if (className) node.className = className;
  return node;
}

/** Маленькая круглая кнопка-иконка */
function iconButton(text, title, onClick) {
  const btn = el('button', 'icon-btn');
  btn.type = 'button';
  btn.textContent = text;
  btn.title = title;
  btn.setAttribute('aria-label', title);
  btn.addEventListener('click', onClick);
  return btn;
}
