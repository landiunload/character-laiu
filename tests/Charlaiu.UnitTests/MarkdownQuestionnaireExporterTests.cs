using Charlaiu.Web.Domain;
using Charlaiu.Web.Services;
using Xunit;

namespace Charlaiu.UnitTests;

/// <summary>Тесты экспорта анкеты в Markdown.</summary>
public sealed class MarkdownQuestionnaireExporterTests
{
    private readonly MarkdownQuestionnaireExporter _exporterUnderTest = new();

    [Fact]
    public void Export_ЗаполненноеПоле_ПопадаетВДокумент()
    {
        // Подготовка
        var characterProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Мири");
        characterProfile.Sections[0].Fields[0].PrimaryValue = "Мири";

        // Действие
        var exportedMarkdown = _exporterUnderTest.Export(characterProfile);

        // Проверка
        Assert.Contains("# Мири", exportedMarkdown);
        Assert.Contains("**Имя:** Мири", exportedMarkdown);
    }

    [Fact]
    public void Export_ПустыеРазделы_НеПопадаютВДокумент()
    {
        var emptyProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Пустой");

        var exportedMarkdown = _exporterUnderTest.Export(emptyProfile);

        Assert.DoesNotContain("##", exportedMarkdown);
    }

    [Fact]
    public void Export_ПарноеПоле_ОформляетсяТаблицей()
    {
        // Подготовка
        var characterProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Феска");
        var likesSection = characterProfile.Sections.First(section => section.Title == "Любит / Не любит");
        likesSection.Fields[0].PrimaryValue = "Жареная рыба";
        likesSection.Fields[0].SecondaryValue = "Каша";

        // Действие
        var exportedMarkdown = _exporterUnderTest.Export(characterProfile);

        // Проверка
        Assert.Contains("| **Еда** | Жареная рыба | Каша |", exportedMarkdown);
        Assert.Contains("| | Любит | Не любит |", exportedMarkdown);
    }

    [Fact]
    public void Export_ПарныеИОбычныеПоляВОдномРазделе_ИдутТаблицейИСпискомОтдельно()
    {
        // Подготовка: раздел с колонками, где одно поле парное, а второе — обычное
        var characterProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Феска");
        var mixedSection = characterProfile.Sections.First(section => section.Title == "Любит / Не любит");
        mixedSection.Fields[0].PrimaryValue = "Жареная рыба";
        mixedSection.Fields[0].SecondaryValue = "Каша";

        var regularField = QuestionnaireField.Create("Заметка", QuestionnaireFieldType.MultiLine);
        regularField.PrimaryValue = "Ест только по утрам";
        mixedSection.Fields.Add(regularField);

        // Действие
        var exportedMarkdown = _exporterUnderTest.Export(characterProfile);

        // Проверка: таблица собрана только из парных полей, обычное вынесено под неё
        Assert.Contains("| **Еда** | Жареная рыба | Каша |", exportedMarkdown);
        Assert.Contains("**Заметка:** Ест только по утрам", exportedMarkdown);
        Assert.DoesNotContain("| **Заметка**", exportedMarkdown);

        var tableRowPosition = exportedMarkdown.IndexOf("| **Еда**", StringComparison.Ordinal);
        var regularFieldPosition = exportedMarkdown.IndexOf("**Заметка:**", StringComparison.Ordinal);
        Assert.True(tableRowPosition < regularFieldPosition);
    }

    [Fact]
    public void Export_ПолеБезЗначения_ПропускаетсяНоРазделОстаётся()
    {
        // Подготовка: в разделе заполнено одно поле из нескольких
        var characterProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Лури");
        var firstSection = characterProfile.Sections[0];
        firstSection.Fields[0].PrimaryValue = "Лури";

        // Действие
        var exportedMarkdown = _exporterUnderTest.Export(characterProfile);

        // Проверка
        Assert.Contains("**Имя:** Лури", exportedMarkdown);
        foreach (var emptyField in firstSection.Fields.Skip(1))
        {
            Assert.DoesNotContain($"**{emptyField.Label}:**", exportedMarkdown);
        }
    }
}
