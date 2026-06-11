namespace Charlaiu.Web.Domain;

/// <summary>
/// Фабрика анкеты по умолчанию со структурой «Где он лёг».
/// Единственная ответственность класса — описывать, из каких разделов и полей
/// состоит новая анкета; остальное приложение от конкретной структуры не зависит.
/// </summary>
public static class DefaultQuestionnaireSchemaFactory
{
    private static QuestionnaireField SingleLineField(string label) =>
        QuestionnaireField.Create(label);

    private static QuestionnaireField MultiLineField(string label) =>
        QuestionnaireField.Create(label, QuestionnaireFieldType.MultiLine);

    private static QuestionnaireField PairedField(string label) =>
        QuestionnaireField.Create(label, QuestionnaireFieldType.PairedValues);

    /// <summary>Создаёт новую анкету персонажа со всеми одиннадцатью разделами.</summary>
    public static CharacterProfile CreateCharacterProfile(string displayName = "Новый герой") => new()
    {
        DisplayName = displayName,
        Sections =
        [
            new QuestionnaireSection
            {
                Icon = "⚓", Title = "Карточка",
                Fields =
                [
                    SingleLineField("Имя"),
                    SingleLineField("Прозвище"),
                    SingleLineField("Возраст"),
                    SingleLineField("Род занятий"),
                    SingleLineField("Квартал / дом"),
                    MultiLineField("Герой одним предложением")
                ]
            },
            new QuestionnaireSection
            {
                Icon = "👤", Title = "Внешность",
                Hint = "Одна деталь работает лучше десяти. У Мири — голубая лента. Что у этого героя?",
                Fields =
                [
                    SingleLineField("Рост и телосложение"),
                    MultiLineField("Лицо, особые приметы"),
                    MultiLineField("Одежда (повседневная / особая)"),
                    SingleLineField("Походка и жесты"),
                    SingleLineField("Деталь, по которой узнают издалека")
                ]
            },
            new QuestionnaireSection
            {
                Icon = "🎭", Title = "Характер",
                Hint = "Противоречие — сердце героя. Лури любит сестру, но не умеет это показать. А этот?",
                Fields =
                [
                    SingleLineField("Три слова о нём"),
                    SingleLineField("Со своими он"),
                    SingleLineField("С чужими он"),
                    SingleLineField("В стрессе"),
                    SingleLineField("Главная сила"),
                    SingleLineField("Главная слабость"),
                    MultiLineField("Внутреннее противоречие")
                ]
            },
            new QuestionnaireSection
            {
                Icon = "💭", Title = "Мечты и страхи",
                Fields =
                [
                    MultiLineField("Большая мечта (о которой молчит)"),
                    SingleLineField("Цель на ближайшее время"),
                    SingleLineField("Самый большой страх"),
                    SingleLineField("О чём жалеет"),
                    SingleLineField("Что потерял")
                ]
            },
            new QuestionnaireSection
            {
                Icon = "❤️", Title = "Любит / Не любит",
                FirstColumnLabel = "Любит", SecondColumnLabel = "Не любит",
                Fields =
                [
                    PairedField("Еда"),
                    PairedField("Погода"),
                    PairedField("Звуки, запахи"),
                    PairedField("Занятие"),
                    PairedField("В людях")
                ]
            },
            new QuestionnaireSection
            {
                Icon = "🗳", Title = "Взгляды и мир",
                Hint = "В нашем мире отношение к морю — это отношение к судьбе. Море кормит и забирает.",
                Fields =
                [
                    SingleLineField("Отношение к выборам Хранителя"),
                    SingleLineField("За кого голосовал бы (флот / земля / никто)"),
                    SingleLineField("Отношение к богатым и бедным"),
                    SingleLineField("Верит ли в легенды (Леамир, остров)"),
                    MultiLineField("Отношение к морю")
                ]
            },
            new QuestionnaireSection
            {
                Icon = "🗣", Title = "Речь",
                Fields =
                [
                    SingleLineField("Темп и громкость"),
                    SingleLineField("Любимое словечко или фраза"),
                    SingleLineField("О чём может говорить часами"),
                    SingleLineField("О чём молчит всегда")
                ]
            },
            new QuestionnaireSection
            {
                Icon = "👥", Title = "Связи",
                Fields =
                [
                    MultiLineField("Семья"),
                    SingleLineField("Друзья"),
                    SingleLineField("Кому доверяет"),
                    SingleLineField("Кого избегает"),
                    SingleLineField("Отношение к Мири"),
                    SingleLineField("Отношение к Лури"),
                    SingleLineField("Отношение к Феске")
                ]
            },
            new QuestionnaireSection
            {
                Icon = "🎬", Title = "Роль в истории",
                Hint = "«Хочет» и «нужно» — разные вещи. Лури хочет найти родителей. Нужно ей — вернуть сестру.",
                Fields =
                [
                    SingleLineField("Функция в сюжете"),
                    SingleLineField("Чего хочет (внешняя цель)"),
                    SingleLineField("Что ему нужно на самом деле (внутренняя потребность)"),
                    MultiLineField("Арка: каким входит → каким выходит"),
                    MultiLineField("Секрет (знает только автор)"),
                    SingleLineField("Чеховское ружьё, связанное с ним")
                ]
            },
            new QuestionnaireSection
            {
                Icon = "🌊", Title = "Пять вопросов на глубину",
                Fields =
                [
                    MultiLineField("Что он делает, когда никто не видит?"),
                    MultiLineField("Какое его самое раннее воспоминание?"),
                    MultiLineField("Что бы он сделал с большими деньгами?"),
                    MultiLineField("За что ему стыдно?"),
                    MultiLineField("Как он поведёт себя в шторм?")
                ]
            },
            new QuestionnaireSection
            {
                Icon = "📝", Title = "Заметки",
                Fields = [MultiLineField("Всё, что не влезло выше")]
            }
        ]
    };
}
