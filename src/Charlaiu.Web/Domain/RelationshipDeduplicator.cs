namespace Charlaiu.Web.Domain;

/// <summary>
/// Объединение одинаковых связей: точные дубликаты схлопываются в одну запись,
/// а две встречные односторонние связи одного типа сливаются в одну двустороннюю.
/// </summary>
public static class RelationshipDeduplicator
{
    /// <summary>
    /// Удаляет дубликаты из списка. Возвращает true, если список изменился.
    /// </summary>
    public static bool Deduplicate(List<CharacterRelationship> relationships)
    {
        var listWasChanged = false;

        for (var currentIndex = 0; currentIndex < relationships.Count; currentIndex++)
        {
            var currentRelationship = relationships[currentIndex];

            for (var candidateIndex = relationships.Count - 1; candidateIndex > currentIndex; candidateIndex--)
            {
                var candidateRelationship = relationships[candidateIndex];

                if (AreExactDuplicates(currentRelationship, candidateRelationship))
                {
                    relationships.RemoveAt(candidateIndex);
                    listWasChanged = true;
                }
                else if (AreOpposingOneDirectional(currentRelationship, candidateRelationship))
                {
                    // Встречные односторонние связи одного типа — это одна двусторонняя
                    currentRelationship.IsOneDirectional = false;
                    relationships.RemoveAt(candidateIndex);
                    listWasChanged = true;
                }
            }
        }

        return listWasChanged;
    }

    /// <summary>
    /// Проверяет новую связь против существующих перед добавлением.
    /// Возвращает результат: добавлять, отклонить как дубликат или объединить со встречной.
    /// </summary>
    public static RelationshipMergeOutcome ClassifyNewRelationship(
        IEnumerable<CharacterRelationship> existingRelationships, CharacterRelationship newRelationship)
    {
        foreach (var existingRelationship in existingRelationships)
        {
            if (AreExactDuplicates(existingRelationship, newRelationship))
            {
                return RelationshipMergeOutcome.Duplicate(existingRelationship);
            }

            if (AreOpposingOneDirectional(existingRelationship, newRelationship))
            {
                return RelationshipMergeOutcome.MergeIntoBidirectional(existingRelationship);
            }
        }

        return RelationshipMergeOutcome.AddAsNew();
    }

    /// <summary>Одинаковые участники (с учётом направления), тип и направленность.</summary>
    private static bool AreExactDuplicates(CharacterRelationship first, CharacterRelationship second)
    {
        if (!LabelsMatch(first, second) || first.IsOneDirectional != second.IsOneDirectional)
        {
            return false;
        }

        var sameDirection =
            first.FirstCharacterIdentifier == second.FirstCharacterIdentifier &&
            first.SecondCharacterIdentifier == second.SecondCharacterIdentifier;

        var oppositeDirection =
            first.FirstCharacterIdentifier == second.SecondCharacterIdentifier &&
            first.SecondCharacterIdentifier == second.FirstCharacterIdentifier;

        // Для двусторонней связи направление записи не имеет значения
        return first.IsOneDirectional ? sameDirection : sameDirection || oppositeDirection;
    }

    /// <summary>Две односторонние связи одного типа, направленные навстречу друг другу.</summary>
    private static bool AreOpposingOneDirectional(CharacterRelationship first, CharacterRelationship second) =>
        first.IsOneDirectional && second.IsOneDirectional &&
        LabelsMatch(first, second) &&
        first.FirstCharacterIdentifier == second.SecondCharacterIdentifier &&
        first.SecondCharacterIdentifier == second.FirstCharacterIdentifier;

    // Сравнение по спанам: Deduplicate квадратичен по числу связей и зовёт это
    // на каждой паре, а Trim() на строке создавал бы новую строку каждый раз.
    private static bool LabelsMatch(CharacterRelationship first, CharacterRelationship second) =>
        first.Label.AsSpan().Trim().Equals(
            second.Label.AsSpan().Trim(), StringComparison.OrdinalIgnoreCase);
}

/// <summary>Результат проверки новой связи на дубликаты.</summary>
public sealed class RelationshipMergeOutcome
{
    /// <summary>Вид результата.</summary>
    public RelationshipMergeOutcomeKind Kind { get; private init; }

    /// <summary>Существующая связь, с которой произошло объединение или совпадение.</summary>
    public CharacterRelationship? ExistingRelationship { get; private init; }

    public static RelationshipMergeOutcome AddAsNew() =>
        new() { Kind = RelationshipMergeOutcomeKind.AddAsNew };

    public static RelationshipMergeOutcome Duplicate(CharacterRelationship existingRelationship) =>
        new() { Kind = RelationshipMergeOutcomeKind.Duplicate, ExistingRelationship = existingRelationship };

    public static RelationshipMergeOutcome MergeIntoBidirectional(CharacterRelationship existingRelationship) =>
        new() { Kind = RelationshipMergeOutcomeKind.MergedIntoBidirectional, ExistingRelationship = existingRelationship };
}

/// <summary>Варианты исхода добавления связи.</summary>
public enum RelationshipMergeOutcomeKind
{
    /// <summary>Такой связи ещё нет — добавить как новую.</summary>
    AddAsNew,

    /// <summary>Точно такая связь уже существует — добавлять нечего.</summary>
    Duplicate,

    /// <summary>Встречная односторонняя того же типа — существующая стала двусторонней.</summary>
    MergedIntoBidirectional
}
