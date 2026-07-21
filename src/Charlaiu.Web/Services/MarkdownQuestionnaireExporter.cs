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
        markdownBuilder.Append("# ").AppendLine(characterProfile.DisplayName);
        markdownBuilder.AppendLine();

        foreach (var section in characterProfile.Sections)
        {
            // Один проход по полям вместо четырёх (ToList + Any + два Where):
            // ни промежуточного списка, ни итераторов LINQ на каждый раздел
            var hasPairedValues = false;
            var hasAnyValue = false;

            foreach (var field in section.Fields)
            {
                if (!field.HasAnyValue) { continue; }
                hasAnyValue = true;
                hasPairedValues |= field.FieldType == QuestionnaireFieldType.PairedValues;
            }

            if (!hasAnyValue)
            {
                continue;
            }

            markdownBuilder.Append("## ").Append(section.Icon).Append(' ').AppendLine(section.Title);
            markdownBuilder.AppendLine();

            if (hasPairedValues)
            {
                // Парные поля оформляем таблицей
                markdownBuilder
                    .Append("| | ").Append(section.FirstColumnLabel ?? "Первое")
                    .Append(" | ").Append(section.SecondColumnLabel ?? "Второе")
                    .AppendLine(" |");
                markdownBuilder.AppendLine("|---|---|---|");

                foreach (var field in section.Fields)
                {
                    if (!field.HasAnyValue || field.FieldType != QuestionnaireFieldType.PairedValues) { continue; }

                    markdownBuilder
                        .Append("| **").Append(field.Label)
                        .Append("** | ").Append(field.PrimaryValue)
                        .Append(" | ").Append(field.SecondaryValue)
                        .AppendLine(" |");
                }

                markdownBuilder.AppendLine();
            }

            foreach (var field in section.Fields)
            {
                if (!field.HasAnyValue || field.FieldType == QuestionnaireFieldType.PairedValues) { continue; }

                markdownBuilder.Append("**").Append(field.Label).Append(":** ").AppendLine(field.PrimaryValue);
                markdownBuilder.AppendLine();
            }
        }

        return markdownBuilder.ToString();
    }
}
