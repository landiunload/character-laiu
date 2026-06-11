/**
 * Вспомогательные функции общего назначения.
 * Модуль не зависит от остальных частей приложения (принцип инверсии зависимостей).
 */

/**
 * Генерирует уникальный идентификатор.
 * Используем crypto.randomUUID, когда доступен, иначе — запасной вариант.
 * @returns {string}
 */
export function uid() {
  if (typeof crypto !== 'undefined' && typeof crypto.randomUUID === 'function') {
    return crypto.randomUUID();
  }
  // Запасной вариант для старых браузеров
  return 'id-' + Date.now().toString(36) + '-' + Math.random().toString(36).slice(2, 10);
}

/**
 * Откладывает вызов функции до паузы во входящих событиях.
 * Применяется для автосохранения, чтобы не писать в хранилище на каждый символ.
 * @param {Function} fn — функция для вызова
 * @param {number} ms — задержка в миллисекундах
 * @returns {Function}
 */
export function debounce(fn, ms = 400) {
  let timer = null;
  return function (...args) {
    clearTimeout(timer);
    timer = setTimeout(() => fn.apply(this, args), ms);
  };
}

/**
 * Скачивает текстовое содержимое как файл — без обращения к серверу.
 * @param {string} filename — имя файла
 * @param {string} content — содержимое
 * @param {string} mime — MIME-тип
 */
export function downloadFile(filename, content, mime = 'application/json') {
  const blob = new Blob([content], { type: mime + ';charset=utf-8' });
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = filename;
  link.click();
  // Освобождаем память после скачивания
  setTimeout(() => URL.revokeObjectURL(url), 1000);
}

/**
 * Превращает произвольную строку в безопасное имя файла.
 * @param {string} name
 * @returns {string}
 */
export function safeFilename(name) {
  const cleaned = String(name || '').trim().replace(/[^\p{L}\p{N}\- _]/gu, '').slice(0, 60);
  return cleaned || 'персонаж';
}
