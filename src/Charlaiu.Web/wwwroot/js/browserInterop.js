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
  }
};

// Применяем сохранённую тему сразу, до загрузки приложения — чтобы не мигал фон
document.documentElement.dataset.theme = localStorage.getItem("charlaiu.theme") || "light";
