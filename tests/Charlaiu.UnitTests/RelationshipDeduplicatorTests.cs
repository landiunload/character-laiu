using Charlaiu.Web.Domain;
using Xunit;

namespace Charlaiu.UnitTests;

/// <summary>Тесты объединения одинаковых связей.</summary>
public sealed class RelationshipDeduplicatorTests
{
    private static readonly Guid Miri = Guid.NewGuid();
    private static readonly Guid Villain = Guid.NewGuid();

    private static CharacterRelationship CreateRelationship(
        Guid first, Guid second, string label, bool oneDirectional) => new()
    {
        FirstCharacterIdentifier = first,
        SecondCharacterIdentifier = second,
        Label = label,
        IsOneDirectional = oneDirectional
    };

    [Fact]
    public void Deduplicate_ДвеВстречныеОдносторонние_СливаютсяВОднуДвустороннюю()
    {
        var relationships = new List<CharacterRelationship>
        {
            CreateRelationship(Miri, Villain, "Вражда", oneDirectional: true),
            CreateRelationship(Villain, Miri, "Вражда", oneDirectional: true)
        };

        var listWasChanged = RelationshipDeduplicator.Deduplicate(relationships);

        Assert.True(listWasChanged);
        var mergedRelationship = Assert.Single(relationships);
        Assert.False(mergedRelationship.IsOneDirectional);
    }

    [Fact]
    public void Deduplicate_ТочныйДубликатОдносторонней_Удаляется()
    {
        var relationships = new List<CharacterRelationship>
        {
            CreateRelationship(Miri, Villain, "Вражда", oneDirectional: true),
            CreateRelationship(Miri, Villain, "Вражда", oneDirectional: true)
        };

        Assert.True(RelationshipDeduplicator.Deduplicate(relationships));
        var remainingRelationship = Assert.Single(relationships);
        Assert.True(remainingRelationship.IsOneDirectional);
    }

    [Fact]
    public void Deduplicate_ДвусторонниеСРазнымПорядкомУчастников_СчитаютсяОдинаковыми()
    {
        var relationships = new List<CharacterRelationship>
        {
            CreateRelationship(Miri, Villain, "Дружба", oneDirectional: false),
            CreateRelationship(Villain, Miri, "Дружба", oneDirectional: false)
        };

        Assert.True(RelationshipDeduplicator.Deduplicate(relationships));
        Assert.Single(relationships);
    }

    [Fact]
    public void Deduplicate_РазныеТипыСвязей_НеОбъединяются()
    {
        var relationships = new List<CharacterRelationship>
        {
            CreateRelationship(Miri, Villain, "Вражда", oneDirectional: true),
            CreateRelationship(Villain, Miri, "Любовь", oneDirectional: true)
        };

        Assert.False(RelationshipDeduplicator.Deduplicate(relationships));
        Assert.Equal(2, relationships.Count);
    }

    [Fact]
    public void ClassifyNewRelationship_ВстречнаяОдносторонняя_ОбъединяетСуществующую()
    {
        var existingRelationship = CreateRelationship(Miri, Villain, "Вражда", oneDirectional: true);
        var newRelationship = CreateRelationship(Villain, Miri, "вражда", oneDirectional: true);

        var mergeOutcome = RelationshipDeduplicator.ClassifyNewRelationship([existingRelationship], newRelationship);

        Assert.Equal(RelationshipMergeOutcomeKind.MergedIntoBidirectional, mergeOutcome.Kind);
        Assert.Same(existingRelationship, mergeOutcome.ExistingRelationship);
    }

    [Fact]
    public void ClassifyNewRelationship_ТочныйДубликат_Отклоняется()
    {
        var existingRelationship = CreateRelationship(Miri, Villain, "Дружба", oneDirectional: false);
        var newRelationship = CreateRelationship(Villain, Miri, "Дружба", oneDirectional: false);

        var mergeOutcome = RelationshipDeduplicator.ClassifyNewRelationship([existingRelationship], newRelationship);

        Assert.Equal(RelationshipMergeOutcomeKind.Duplicate, mergeOutcome.Kind);
    }

    [Fact]
    public void ClassifyNewRelationship_НоваяСвязь_Добавляется()
    {
        var existingRelationship = CreateRelationship(Miri, Villain, "Дружба", oneDirectional: false);
        var newRelationship = CreateRelationship(Miri, Villain, "Соперничество", oneDirectional: true);

        var mergeOutcome = RelationshipDeduplicator.ClassifyNewRelationship([existingRelationship], newRelationship);

        Assert.Equal(RelationshipMergeOutcomeKind.AddAsNew, mergeOutcome.Kind);
    }
}
