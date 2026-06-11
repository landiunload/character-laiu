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
}
