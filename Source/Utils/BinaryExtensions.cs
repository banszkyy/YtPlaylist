namespace YtPlaylist;

static class BinaryExtensions
{
    public static void Write<T>(this BinaryWriter writer, T? value, Action<T> callback) where T : struct
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
        {
            callback(value.Value);
        }
    }

    public static void Write(this BinaryWriter writer, DateTimeOffset value)
    {
        writer.Write(value.Ticks);
        writer.Write(value.Offset);
    }
    public static void Write(this BinaryWriter writer, TimeSpan value) => writer.Write(value.Ticks);
    public static void Write(this BinaryWriter writer, int? value) => writer.Write(value, writer.Write);
    public static void WriteList<T>(this BinaryWriter writer, IReadOnlyCollection<T> values, Action<T> callback)
    {
        writer.Write(values.Count);
        foreach (T item in values)
        {
            callback(item);
        }
    }

    public static T? ReadNullable<T>(this BinaryReader reader, Func<T> callback)
    {
        bool hasValue = reader.ReadBoolean();
        return hasValue ? callback() : default;
    }

    public static int? ReadNullableInt32(this BinaryReader reader) => ReadNullable(reader, reader.ReadInt32);
    public static DateTimeOffset ReadDateTimeOffset(this BinaryReader reader) => new(reader.ReadInt64(), reader.ReadTimeSpan());
    public static TimeSpan ReadTimeSpan(this BinaryReader reader) => new(reader.ReadInt64());
    public static List<T> ReadList<T>(this BinaryReader reader, Func<T> callback)
    {
        int count = reader.ReadInt32();
        if (count < 0) throw new FormatException();
        List<T> res = [];
        for (int i = 0; i < count; i++)
        {
            res.Add(callback());
        }
        return res;
    }
}
