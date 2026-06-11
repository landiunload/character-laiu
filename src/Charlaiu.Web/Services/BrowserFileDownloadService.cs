using System.Text;
using Microsoft.JSInterop;

namespace Charlaiu.Web.Services;

/// <summary>Скачивание файлов средствами браузера через небольшую JavaScript-обёртку.</summary>
public sealed class BrowserFileDownloadService(IJSRuntime javascriptRuntime)
{
    /// <summary>Передаёт текстовое содержимое браузеру как скачиваемый файл.</summary>
    public async Task DownloadTextFileAsync(string fileName, string contentType, string textContent)
    {
        var contentAsBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(textContent));
        await javascriptRuntime.InvokeVoidAsync("charlaiuInterop.downloadFile", fileName, contentType, contentAsBase64);
    }
}
