using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace YtPlaylist;

static class Utils
{
    public static T ThrowIfNull<T>([NotNull] this T? value, [CallerArgumentExpression(nameof(value))] string? expression = null)
        where T : class
    {
        if (value is null) throw new NullReferenceException($"{expression} is null");
        return value;
    }

    public static T ThrowIfNull<T>([NotNull] this T? value, [CallerArgumentExpression(nameof(value))] string? expression = null)
        where T : struct
    {
        if (value is null) throw new NullReferenceException($"{expression} is null");
        return value.Value;
    }
}
