using System.Text.Json;
using Charlaiu.Web.Domain;
using Charlaiu.Web.Services;
using Xunit;

namespace Charlaiu.UnitTests;

/// <summary>
/// Тесты экспорта анкеты в JSON. Экспорт идёт через сериализаторы, сгенерированные
/// на этапе сборки, поэтому здесь же проверяется, что генератор охватил всю модель:
/// пропущенный тип превратился бы в потерянные при экспорте данные.
/// </summary>
public sealed class JsonQuestionnaireExporterTests
{
    private readonly JsonQuestionnaireExporter _exporterUnderTest = new();

    [Fact]
    public void Export_АнкетаЦеликом_ЧитаетсяОбратноБезПотерь()
    {
        // Подготовка
        var originalProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Мири");
        originalProfile.Category = "Главные";
        originalProfile.Sections[0].Fields[0].PrimaryValue = "Мири";

        var pairedSection = originalProfile.Sections.First(section => section.FirstColumnLabel is not null);
        pairedSection.Fields[0].PrimaryValue = "Жареная рыба";
        pairedSection.Fields[0].SecondaryValue = "Каша";

        // Действие
        var exportedJson = _exporterUnderTest.Export(originalProfile);
        var restoredProfile = JsonSerializer.Deserialize<CharacterProfile>(exportedJson);

        // Проверка
        Assert.NotNull(restoredProfile);
        Assert.Equal(originalProfile.DisplayName, restoredProfile.DisplayName);
        Assert.Equal(originalProfile.Category, restoredProfile.Category);
        Assert.Equal(originalProfile.Identifier, restoredProfile.Identifier);
        Assert.Equal(originalProfile.Sections.Count, restoredProfile.Sections.Count);

        var restoredPairedSection = restoredProfile.Sections.First(section => section.Identifier == pairedSection.Identifier);
        Assert.Equal(pairedSection.FirstColumnLabel, restoredPairedSection.FirstColumnLabel);
        Assert.Equal(pairedSection.SecondColumnLabel, restoredPairedSection.SecondColumnLabel);
        Assert.Equal("Жареная рыба", restoredPairedSection.Fields[0].PrimaryValue);
        Assert.Equal("Каша", restoredPairedSection.Fields[0].SecondaryValue);
        Assert.Equal(QuestionnaireFieldType.PairedValues, restoredPairedSection.Fields[0].FieldType);
    }

    [Fact]
    public void Export_Кириллица_ОстаётсяЧитаемой()
    {
        var characterProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Мири");

        var exportedJson = _exporterUnderTest.Export(characterProfile);

        Assert.Contains("Мири", exportedJson);
        Assert.DoesNotContain("\\u04", exportedJson);
    }

    [Fact]
    public void Export_ФорматФайла_СОтступами()
    {
        var characterProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile("Мири");

        var exportedJson = _exporterUnderTest.Export(characterProfile);

        Assert.Contains(Environment.NewLine, exportedJson);
        Assert.Equal("json", _exporterUnderTest.FileExtension);
        Assert.Equal("application/json", _exporterUnderTest.ContentType);
    }
}
