using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Charlaiu.Web.Domain;

namespace Charlaiu.Web.Services;

/// <summary>Экспорт анкеты в JSON — резервная копия, пригодная для импорта обратно.</summary>
public sealed class JsonQuestionnaireExporter : IQuestionnaireExporter
{
    // Отдельный экземпляр контекста: те же сгенерированные сериализаторы,
    // но с отступами и без экранирования кириллицы в \uXXXX
    private static readonly CharlaiuJsonContext ReadableJsonContext = new(new JsonSerializerOptions
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    });

    /// <inheritdoc />
    public string FileExtension => "json";

    /// <inheritdoc />
    public string ContentType => "application/json";

    /// <inheritdoc />
    public string Export(CharacterProfile characterProfile) =>
        JsonSerializer.Serialize(characterProfile, ReadableJsonContext.CharacterProfile);
}
