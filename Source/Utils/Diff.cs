using System.Collections;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Logger;

namespace YtPlaylist;

public class Diff
{
    public readonly struct Change(object? old, object? @new)
    {
        public readonly object? Old = old;
        public readonly object? New = @new;

        public override string ToString() => $"Change({Old ?? "null"} --> {New ?? "null"})";
    }

    readonly Dictionary<string, Change> _changes = [];

    public IReadOnlyDictionary<string, Change> Changes => _changes;

    class GenericValueComparer : IEqualityComparer<object?>
    {
        public static readonly GenericValueComparer Instance = new();

        public new bool Equals(object? x, object? y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is IEnumerable a && y is IEnumerable b
                && x is not string && y is not string)
            {
                return a.Cast<object?>().SequenceEqual(b.Cast<object?>(), Instance);
            }

            return x.Equals(y);
        }

        public int GetHashCode([DisallowNull] object? obj)
        {
            return obj.GetHashCode();
        }
    }

    [return: NotNullIfNotNull(nameof(newValue))]
    public T? Modify<T>(string key, T? oldValue, T? newValue)
    {
        if (_changes.TryGetValue(key, out Change old)) oldValue = (T?)old.Old;
        Change change = new(oldValue, newValue);

        if (GenericValueComparer.Instance.Equals(change.Old, change.New)) _changes.Remove(key);
        else _changes[key] = change;

        return newValue;
    }

    static void PrintValue(object? value)
    {
        switch (value)
        {
            case null:
                Console.Write("null");
                break;
            case string v:
                Console.Write('"');
                Console.Write(v
                    .Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\t", "\\\t")
                    .Replace("\n", "\\\n")
                    .Replace("\r", "\\\r")
                );
                Console.Write('"');
                break;
            case IEnumerable v:
                Console.Write('[');
                bool w = false;
                foreach (object? item in v)
                {
                    if (w) Console.Write(", ");
                    PrintValue(item);
                    w = true;
                }
                Console.Write(']');
                break;
            default:
                Console.Write(value);
                break;
        }
    }

    public void Print(int depth)
    {
        foreach ((string key, Change change) in _changes)
        {
            Console.Write(new string(' ', depth * 2));

            if (change.Old is null)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("+");
                Console.ResetColor();
                Console.Write(" ");
                Console.Write(key);
                Console.Write(" = ");
                PrintValue(change.New);
                Console.WriteLine();
                continue;
            }

            if (change.New is null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("-");
                Console.ResetColor();
                Console.Write(" ");
                Console.Write(key);
                Console.WriteLine();
                continue;
            }

            if (change.Old is IEnumerable oldEnumerable && change.New is IEnumerable newEnumerable
               && change.Old is not string && change.New is not string)
            {
                if (oldEnumerable.IsEmpty())
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("+");
                    Console.ResetColor();
                    Console.Write(" ");
                    Console.Write(key);
                    Console.Write(" = ");
                    PrintValue(change.New!);
                    Console.WriteLine();
                    continue;
                }
                else if (newEnumerable.IsEmpty())
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("-");
                    Console.ResetColor();
                    Console.Write(" ");
                    Console.Write(key);
                    Console.WriteLine();
                    continue;
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("x");
            Console.ResetColor();
            Console.Write(" ");
            Console.Write(key);
            Console.Write(" ");
            PrintValue(change.Old);
            Console.Write(" -> ");
            PrintValue(change.New);
            Console.WriteLine();
        }
    }

    public void Print()
    {
        using (Log.Auto())
        {
            Print(2);
        }
    }

    public void Clear() => _changes.Clear();
}

public class DiffList(int count)
{
    public readonly struct Change(bool isAdd, Diff? @new)
    {
        public readonly bool IsAdd = isAdd;
        public readonly Diff? New = @new;
    }

    int _count = count;
    readonly Dictionary<int, Change> _changes = [];

    public void Print(int depth)
    {
        foreach ((int index, Change change) in _changes)
        {
            Console.Write(new string(' ', depth * 2));

            if (change.New is null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write('-');
                Console.ResetColor();
                Console.Write(' ');
                Console.Write('[');
                Console.Write(index);
                Console.Write(']');
                Console.WriteLine();
                continue;
            }

            if (change.IsAdd)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write('+');
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write('x');
                Console.ResetColor();
            }

            Console.Write(' ');
            Console.Write('[');
            Console.Write(index);
            Console.Write(']');
            Console.Write(" = ");
            Console.WriteLine();
            change.New.Print(depth + 1);
        }
    }

    public Diff Modify(int index)
    {
        if (index < 0 || index >= _count) throw new IndexOutOfRangeException();

        if (_changes.TryGetValue(index, out Change change))
        {
            if (change.New is null) throw new UnreachableException();
            return change.New;
        }
        else
        {
            Diff res = new();
            _changes.Add(index, new Change(false, res));
            return res;
        }

    }

    public void Add()
    {
        _changes.Add(_count, new Change(true, new()));
        _count++;
    }

    public void RemoveAt(int index)
    {
        if (index < 0 || index >= _count) throw new IndexOutOfRangeException();

        _count--;
        _changes[index] = new Change(false, null);
    }
}
