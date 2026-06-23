namespace YtPlaylist;

static class BinaryExtensions
{
    public static void Write(this BinaryWriter writer, int? value)
    {
        writer.Write(value.HasValue);
        if (value.HasValue)
        {
            writer.Write(value.Value);
        }
    }

    public static int? ReadInt32Nullable(this BinaryReader reader)
    {
        bool hasValue = reader.ReadBoolean();
        return hasValue ? reader.ReadInt32() : default;
    }
}