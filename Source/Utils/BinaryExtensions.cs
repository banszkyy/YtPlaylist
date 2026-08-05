namespace YtPlaylist;

static class BinaryExtensions
{
    extension(BinaryWriter writer)
    {
        public void Write<T>(T? value, Action<T> callback) where T : struct
        {
            writer.Write(value.HasValue);
            if (value.HasValue)
            {
                callback(value.Value);
            }
        }

        public void Write(DateTimeOffset value)
        {
            writer.Write(value.Ticks);
            writer.Write(value.Offset);
        }
        public void Write(TimeSpan value) => writer.Write(value.Ticks);
        public void Write(int? value) => writer.Write(value, writer.Write);
        public void WriteList<T>(IReadOnlyCollection<T> values, Action<T> callback)
        {
            writer.Write(values.Count);
            foreach (T item in values)
            {
                callback(item);
            }
        }
    }

    extension(BinaryReader reader)
    {
        public T? ReadNullable<T>(Func<T> callback)
        {
            bool hasValue = reader.ReadBoolean();
            return hasValue ? callback() : default;
        }

        public int? ReadNullableInt32() => ReadNullable(reader, reader.ReadInt32);
        public DateTimeOffset ReadDateTimeOffset() => new(reader.ReadInt64(), reader.ReadTimeSpan());
        public TimeSpan ReadTimeSpan() => new(reader.ReadInt64());
        public List<T> ReadList<T>(Func<T> callback)
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
}
