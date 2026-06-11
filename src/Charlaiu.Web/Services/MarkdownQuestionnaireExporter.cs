using System.Text;
using Charlaiu.Web.Domain;

namespace Charlaiu.Web.Services;

/// <summary>
/// Экспорт анкеты в Markdown — готовый документ для вики, Notion или печати.
/// Пустые поля и разделы пропускаются.
/// </summary>
public sealed class MarkdownQuestionnaireExporter : IQuestionnaireExporter
{
    /// <inheritdoc />
    public string FileExtension => "md";

    /// <inheritdoc />
    public string ContentType => "text/markdown";

    /// <inheritdoc />
    public string Export(CharacterProfile characterProfile)
    {
        var markdownBuilder = new StringBuilder();
        markdownBuilder.AppendLine($"# {characterProfile.DisplayName}");
        markdownBuilder.AppendLine();

        foreach (var section in characterProfile.Sections)
        {
            var fieldsWithValues = section.Fields.Where(field => field.HasAnyValue).ToList();
            if (fieldsWithValues.Count == 0)
            {
                continue;
            }

            markdownBuilder.AppendLine($"## {section.Icon} {section.Title}");
            markdownBuilder.AppendLine();

            if (fieldsWithValues.Any(field => field.FieldType == QuestionnaireFieldType.PairedValues))
            {
                // Парные поля оформляем таблицей
                markdownBuilder.AppendLine($"| | {section.FirstColumnLabel ?? "Первое"} | {section.SecondColumnLabel ?? "Второе"} |");
                markdownBuilder.AppendLine("|---|---|---|");

                foreach (var pairedField in fieldsWithValues.Where(field => field.FieldType == QuestionnaireFieldType.PairedValues))
                {
                    markdownBuilder.AppendLine(
                        $"| **{pairedField.Label}** | {pairedField.PrimaryValue} | {pairedField.SecondaryValue} |");
                }

                markdownBuilder.AppendLine();
            }

            foreach (var regularField in fieldsWithValues.Where(field => field.FieldType != QuestionnaireFieldType.PairedValues))
            {
                markdownBuilder.AppendLine($"**{regularField.Label}:** {regularField.PrimaryValue}");
                markdownBuilder.AppendLine();
            }
        }

        return markdownBuilder.ToString();
    }
}
