// Небольшая обёртка над возможностями браузера, недоступными из C# напрямую
window.charlaiuInterop = {
  /**
   * Скачивает файл: содержимое приходит из C# в виде base64.
   * @param {string} fileName     имя сохраняемого файла
   * @param {string} contentType  MIME-тип содержимого
   * @param {string} contentAsBase64 содержимое в base64 (UTF-8)
   */
  downloadFile(fileName, contentType, contentAsBase64) {
    const binaryString = atob(contentAsBase64);
    const contentBytes = Uint8Array.from(binaryString, character => character.charCodeAt(0));
    const downloadUrl = URL.createObjectURL(new Blob([contentBytes], { type: contentType }));

    const downloadLink = document.createElement("a");
    downloadLink.href = downloadUrl;
    downloadLink.download = fileName;
    downloadLink.click();

    URL.revokeObjectURL(downloadUrl);
  },

  /** Применяет тему оформления к документу и запоминает выбор. */
  applyTheme(themeName) {
    document.documentElement.dataset.theme = themeName;
    localStorage.setItem("charlaiu.theme", themeName);
  },

  /** Возвращает сохранённую тему оформления (или пустую строку). */
  readSavedTheme() {
    return localStorage.getItem("charlaiu.theme") || "";
  },

  /**
   * Ставит контекстное меню под курсор. Строгая CSP запрещает инлайновые
   * атрибуты style, поэтому позиция задаётся через CSSOM после рендера Blazor.
   * Меню не выходит за края окна.
   */
  positionContextMenu(clientX, clientY, attemptsLeft = 10) {
    const menuElement = document.querySelector(".node-context-menu");
    if (!menuElement) {
      if (attemptsLeft > 0) {
        requestAnimationFrame(() =>
          window.charlaiuInterop.positionContextMenu(clientX, clientY, attemptsLeft - 1));
      }
      return;
    }

    const menuRect = menuElement.getBoundingClientRect();
    const clampedX = Math.min(clientX, window.innerWidth - menuRect.width - 8);
    const clampedY = Math.min(clientY, window.innerHeight - menuRect.height - 8);
    menuElement.style.left = Math.max(8, clampedX) + "px";
    menuElement.style.top = Math.max(8, clampedY) + "px";
    menuElement.style.visibility = "visible";
  }
};

// Применяем сохранённую тему сразу, до загрузки приложения — чтобы не мигал фон
document.documentElement.dataset.theme = localStorage.getItem("charlaiu.theme") || "light";
