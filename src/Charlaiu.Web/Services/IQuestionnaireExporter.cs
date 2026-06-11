using Charlaiu.Web.Domain;

namespace Charlaiu.Web.Services;

/// <summary>
/// Контракт экспортёра анкеты в текстовый формат.
/// Новый формат экспорта — это новая реализация интерфейса,
/// существующий код менять не нужно (принцип открытости/закрытости).
/// </summary>
public interface IQuestionnaireExporter
{
    /// <summary>Расширение файла без точки (например, «md»).</summary>
    string FileExtension { get; }

    /// <summary>MIME-тип содержимого.</summary>
    string ContentType { get; }

    /// <summary>Преобразует анкету в текст.</summary>
    string Export(CharacterProfile characterProfile);
}
