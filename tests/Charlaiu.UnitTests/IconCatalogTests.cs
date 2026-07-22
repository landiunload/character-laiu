using Charlaiu.Web.Components;
using Charlaiu.Web.Domain;
using Xunit;

namespace Charlaiu.UnitTests;

/// <summary>
/// Тесты каталога иконок. Проверяются не столько сами функции, сколько инварианты,
/// нарушение которых видно лишь в браузере: <c>ApplicationIcon</c> берёт контур
/// по индексатору, поэтому имя, которого нет в каталоге, роняет отрисовку раздела.
/// </summary>
public sealed class IconCatalogTests
{
    [Fact]
    public void ResolveIconName_ИзвестноеИмя_ВозвращаетсяКакЕсть()
    {
        Assert.Equal("person", IconCatalog.ResolveIconName("person"));
    }

    [Fact]
    public void ResolveIconName_ЭмодзиИзСтарыхСохранений_ЗаменяетсяИконкой()
    {
        Assert.Equal("group", IconCatalog.ResolveIconName("👥"));
    }

    [Fact]
    public void ResolveIconName_НеизвестноеЗначение_ПревращаетсяВЗвезду()
    {
        Assert.Equal("star", IconCatalog.ResolveIconName("совершенно неизвестная иконка"));
        Assert.Equal("star", IconCatalog.ResolveIconName(string.Empty));
    }

    [Fact]
    public void ResolveIconName_ЛюбойРезультат_ЕстьВКаталоге()
    {
        // Ключевой инвариант: что бы ни лежало в сохранении, ApplicationIcon
        // найдёт контур по возвращённому имени и не упадёт по KeyNotFoundException
        string[] storedValuesFromTheWild = ["person", "👥", "", "star", "🚂", "неизвестно", "Person"];

        foreach (var storedValue in storedValuesFromTheWild)
        {
            Assert.True(
                IconCatalog.IconPathsByName.ContainsKey(IconCatalog.ResolveIconName(storedValue)),
                $"Для сохранённого значения «{storedValue}» вернулось имя вне каталога.");
        }
    }

    /// <summary>
    /// Таблица продублирована намеренно: она фиксирует ожидаемое соответствие,
    /// поэтому ловит и опечатку в имени иконки, и потерю самой строки замены —
    /// в обоих случаях старое сохранение молча теряет значок раздела.
    /// «✦» отображается в «star» по существу, а не по умолчанию.
    /// </summary>
    [Theory]
    [InlineData("⚓", "description")]
    [InlineData("👤", "person")]
    [InlineData("🎭", "mood")]
    [InlineData("💭", "cloud")]
    [InlineData("❤️", "favorite")]
    [InlineData("🗳", "public")]
    [InlineData("🗣", "mic")]
    [InlineData("👥", "group")]
    [InlineData("🎬", "flag")]
    [InlineData("🌊", "sailing")]
    [InlineData("📝", "notes")]
    [InlineData("✦", "star")]
    [InlineData("🚂", "bolt")]
    public void ЗаменыЭмодзи_ДаютОжидаемуюИконкуИзКаталога(string legacyEmoji, string expectedIconName)
    {
        var resolvedIconName = IconCatalog.ResolveIconName(legacyEmoji);

        Assert.Equal(expectedIconName, resolvedIconName);
        Assert.True(
            IconCatalog.IconPathsByName.ContainsKey(resolvedIconName),
            $"Эмодзи «{legacyEmoji}» отображается на отсутствующую иконку «{resolvedIconName}».");
    }

    [Fact]
    public void ИконкиСхемыПоУмолчанию_ВсеЕстьВКаталоге()
    {
        // Иначе новая анкета молча покажет звёзды вместо задуманных значков
        var defaultProfile = DefaultQuestionnaireSchemaFactory.CreateCharacterProfile();

        foreach (var section in defaultProfile.Sections)
        {
            Assert.True(
                IconCatalog.IconPathsByName.ContainsKey(section.Icon),
                $"Раздел «{section.Title}» просит иконку «{section.Icon}», которой нет в каталоге.");
        }
    }

    [Fact]
    public void КонтурыИконок_НиОдинНеПустой()
    {
        foreach (var (iconName, svgPath) in IconCatalog.IconPathsByName)
        {
            Assert.False(string.IsNullOrWhiteSpace(svgPath), $"У иконки «{iconName}» пустой контур.");
        }
    }
}
