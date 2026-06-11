using System.Text.Json;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using Charlaiu.Web.Domain;

namespace Charlaiu.Web.Services;

/// <summary>Экспорт анкеты в JSON — резервная копия, пригодная для импорта обратно.</summary>
public sealed class JsonQuestionnaireExporter : IQuestionnaireExporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        // Кириллица в файле должна оставаться читаемой, а не превращаться в \uXXXX
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All)
    };

    /// <inheritdoc />
    public string FileExtension => "json";

    /// <inheritdoc />
    public string ContentType => "application/json";

    /// <inheritdoc />
    public string Export(CharacterProfile characterProfile) =>
        JsonSerializer.Serialize(characterProfile, SerializerOptions);
}
