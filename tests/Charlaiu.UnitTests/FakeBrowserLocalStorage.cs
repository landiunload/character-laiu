using Microsoft.JSInterop;

namespace Charlaiu.UnitTests;

/// <summary>
/// Подделка localStorage: тот же набор операций, что зовёт хранилище анкет,
/// только в памяти. Позволяет проверять формат хранения и перенос сохранений
/// без браузера — ровно то, ради чего хранилище спрятано за интерфейсом.
/// </summary>
internal sealed class FakeBrowserLocalStorage : IJSRuntime
{
    private readonly Dictionary<string, string> _storedItems = new(StringComparer.Ordinal);

    /// <summary>
    /// Счётчик записей. По нему видно, что правка одной анкеты не разрастается
    /// в перезапись всего хранилища, — иначе вернулась бы та самая медленность.
    /// </summary>
    public int WriteOperationCount { get; private set; }

    /// <summary>Текущее содержимое хранилища.</summary>
    public IReadOnlyDictionary<string, string> StoredItems => _storedItems;

    /// <summary>Кладёт значение напрямую, минуя счётчик записей.</summary>
    public void Seed(string key, string value) => _storedItems[key] = value;

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
        ValueTask.FromResult(Execute<TValue>(identifier, args));

    /// <inheritdoc />
    public ValueTask<TValue> InvokeAsync<TValue>(
        string identifier, CancellationToken cancellationToken, object?[]? args) =>
        ValueTask.FromResult(Execute<TValue>(identifier, args));

    private TValue Execute<TValue>(string identifier, object?[]? arguments)
    {
        var callArguments = arguments ?? [];

        switch (identifier)
        {
            case "localStorage.getItem":
                _storedItems.TryGetValue((string)callArguments[0]!, out var storedValue);
                return (TValue)(object?)storedValue!;

            case "localStorage.setItem":
                _storedItems[(string)callArguments[0]!] = (string)callArguments[1]!;
                ++WriteOperationCount;
                return default!;

            case "localStorage.removeItem":
                _storedItems.Remove((string)callArguments[0]!);
                ++WriteOperationCount;
                return default!;

            default:
                // Молчаливое «ничего не делаю» скрыло бы забытый вызов
                throw new InvalidOperationException($"Неожиданный вызов JavaScript: {identifier}");
        }
    }
}
