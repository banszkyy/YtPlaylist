using System.Diagnostics;

namespace YtPlaylist;

public enum ChangeType
{
    Create,
    Modify,
    Delete,
}

public static class Changes
{
    public static void Print<T>(IEnumerable<Change<T>> changes, Action<T>? print = null, Func<T, T, bool>? comparison = null) where T : notnull
    {
        comparison ??= static bool (T a, T b) => a.Equals(b);
        print ??= static void (T v) => Console.WriteLine(v);

        foreach (Change<T> change in changes.OrderBy(v => v.Type))
        {
            switch (change.Type)
            {
                case ChangeType.Create:
                    if (changes.Any(v => comparison(v.Value, change.Value) && v.Type is ChangeType.Delete)) continue;
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write(" + ");
                    Console.ResetColor();
                    break;
                case ChangeType.Modify:
                    if (changes.Any(v => comparison(v.Value, change.Value) && v.Type is ChangeType.Create or ChangeType.Delete)) continue;
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write(" x ");
                    Console.ResetColor();
                    break;
                case ChangeType.Delete:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write(" - ");
                    Console.ResetColor();
                    break;
                default:
                    throw new UnreachableException();
            }

            print(change.Value);
        }
    }
}

public readonly struct Change<T>(T value, ChangeType type)
{
    public T Value { get; } = value;
    public ChangeType Type { get; } = type;
}
