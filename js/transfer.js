/**
 * Экспорт и импорт анкет.
 *
 * Единственная ответственность — преобразование персонажей
 * в файлы (JSON, Markdown) и обратно, включая проверку входных данных.
 */

import { uid } from './utils.js';

const FORMAT_MARKER = 'gde-on-leg/characters';
const FORMAT_VERSION = 1;

/** Допустимые типы полей — всё прочее при импорте отбрасывается */
const FIELD_TYPES = new Set(['text', 'textarea', 'pair']);

/**
 * Сериализует персонажей в JSON для скачивания.
 * @param {object[]} characters
 * @returns {string}
 */
export function toJSON(characters) {
  return JSON.stringify(
    { format: FORMAT_MARKER, version: FORMAT_VERSION, characters },
    null,
    2
  );
}

/**
 * Разбирает и проверяет импортируемый JSON.
 * Безопасность: структура пересобирается заново поле за полем,
 * все значения приводятся к строкам, лишние ключи отбрасываются,
 * идентификаторы выдаются новые (исключает конфликты и подмену данных).
 *
 * @param {string} raw — содержимое файла
 * @returns {object[]} список персонажей
 * @throws {Error} если файл не похож на экспорт анкет
 */
export function fromJSON(raw) {
  let data;
  try {
    data = JSON.parse(raw);
  } catch {
    throw new Error('Файл не является корректным JSON');
  }

  // Принимаем и полный экспорт, и одиночного персонажа, и просто массив
  let list;
  if (data && data.format === FORMAT_MARKER && Array.isArray(data.characters)) {
    list = data.characters;
  } else if (Array.isArray(data)) {
    list = data;
  } else if (data && Array.isArray(data.sections)) {
    list = [data];
  } else {
    throw new Error('В файле не найдено ни одной анкеты');
  }

  const characters = list.map(sanitizeCharacter).filter(Boolean);
  if (characters.length === 0) {
    throw new Error('В файле не найдено ни одной корректной анкеты');
  }
  console.info(`[Импорт] Прочитано персонажей: ${characters.length}`);
  return characters;
}

/** Пересобирает персонажа из недоверенных данных */
function sanitizeCharacter(input) {
  if (!input || typeof input !== 'object' || !Array.isArray(input.sections)) {
    return null;
  }
  return {
    id: uid(),
    name: str(input.name, 'Без имени', 200),
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString(),
    sections: input.sections.map(sanitizeSection).filter(Boolean),
  };
}

/** Пересобирает раздел из недоверенных данных */
function sanitizeSection(input) {
  if (!input || typeof input !== 'object' || !Array.isArray(input.fields)) {
    return null;
  }
  const columns = Array.isArray(input.columns) && input.columns.length === 2
    ? [str(input.columns[0], '', 50), str(input.columns[1], '', 50)]
    : null;
  return {
    id: uid(),
    icon: str(input.icon, '✦', 8),
    title: str(input.title, 'Раздел', 200),
    hint: str(input.hint, '', 500),
    columns,
    fields: input.fields.map(sanitizeField).filter(Boolean),
  };
}

/** Пересобирает поле из недоверенных данных */
function sanitizeField(input) {
  if (!input || typeof input !== 'object') return null;
  const type = FIELD_TYPES.has(input.type) ? input.type : 'text';
  const value = type === 'pair'
    ? [str(input.value?.[0], '', 20000), str(input.value?.[1], '', 20000)]
    : str(input.value, '', 20000);
  return {
    id: uid(),
    label: str(input.label, 'Поле', 300),
    type,
    hint: str(input.hint, '', 500),
    value,
  };
}

/** Приводит произвольное значение к строке с ограничением длины */
function str(value, fallback, maxLen) {
  if (typeof value !== 'string') return fallback;
  return value.slice(0, maxLen);
}

/**
 * Превращает анкету персонажа в документ Markdown —
 * в том же стиле, что исходная анкета «Где он лёг».
 * @param {object} character
 * @returns {string}
 */
export function toMarkdown(character) {
  const lines = [`# 📖 ${character.name || 'Анкета персонажа'}`, ''];

  for (const sec of character.sections) {
    lines.push('---', '', `## ${sec.icon} ${sec.title}`, '');

    if (sec.columns) {
      // Раздел-таблица («Любит / Не любит»)
      lines.push(`| | ${sec.columns[0]} | ${sec.columns[1]} |`);
      lines.push('|---|---|---|');
      for (const f of sec.fields) {
        const left = cell(f.value?.[0]);
        const right = cell(f.value?.[1]);
        lines.push(`| **${f.label}** | ${left} | ${right} |`);
      }
    } else {
      for (const f of sec.fields) {
        const value = String(f.value || '').trim();
        if (value.includes('\n')) {
          // Многострочный ответ выносим под заголовок поля
          lines.push(`- **${f.label}:**`);
          for (const row of value.split('\n')) lines.push(`  ${row}`);
        } else {
          lines.push(`- **${f.label}:** ${value}`);
        }
      }
    }

    if (sec.hint) {
      lines.push('', `> *${sec.hint}*`);
    }
    lines.push('');
  }

  return lines.join('\n');
}

/** Готовит значение для ячейки таблицы Markdown */
function cell(value) {
  return String(value || '').replaceAll('|', '\\|').replaceAll('\n', '<br>');
}
