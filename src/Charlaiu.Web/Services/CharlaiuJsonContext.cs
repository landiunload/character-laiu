using System.Text.Json.Serialization;
using Charlaiu.Web.Domain;

namespace Charlaiu.Web.Services;

/// <summary>
/// Сериализаторы, сгенерированные на этапе сборки.
/// Анкета целиком уходит в localStorage после каждого нажатия клавиши, поэтому
/// сериализация здесь — самый горячий путь приложения. Генератор избавляет его от
/// рефлексии: код читает поля напрямую, старт не тратится на разбор типов,
/// а компоновщик выкидывает из WebAssembly ненужные ветки System.Text.Json.
/// </summary>
[JsonSourceGenerationOptions(WriteIndented = false)]
[JsonSerializable(typeof(CharacterProfile))]
[JsonSerializable(typeof(List<CharacterProfile>))]
[JsonSerializable(typeof(List<CharacterRelationship>))]
[JsonSerializable(typeof(List<RelationshipPreset>))]
internal sealed partial class CharlaiuJsonContext : JsonSerializerContext;
