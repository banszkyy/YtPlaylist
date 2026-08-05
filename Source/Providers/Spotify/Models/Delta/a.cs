using System.Text.Json.Serialization;
using J = System.Text.Json.Serialization.JsonPropertyNameAttribute;

namespace YtPlaylist.Spotify;

public class DeltaRequest
{
    [J("deltas")] public required IReadOnlyList<Delta> Deltas { get; set; }
}

public class Delta
{
    [J("ops")] public required IReadOnlyList<DeltaOperation> Operations { get; set; }
    [J("info")] public required DeltaInfo Info { get; set; }
}

public class DeltaInfo
{
    [J("source")] public required OperationSource2 Source { get; set; }
}

public class OperationSource2
{
    [J("client")] public required string Client { get; set; }
}

public class DeltaOperation
{
    [J("kind")] public required string Kind { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("rem")] public RemoveOperation? Remove { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("add")] public AddOperation? Add { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("updateItemAttributes")] public UpdateItemAttributesOperation? UpdateItemAttributes { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull), J("updateListAttributes")] public UpdateListAttributesOperation? UpdateListAttributes { get; set; }
}

public class AddOperation
{
    [J("addFirst")] public required bool AddFirst { get; set; }
    [J("items")] public required IReadOnlyList<AddOperationItem> Items { get; set; }
}

public class AddOperationItem
{
    [J("attributes")] public required Dictionary<string, object> Attributes { get; set; }
    [J("uri")] public required string Uri { get; set; }
}

public class RemoveOperation
{
    [J("items")] public required IReadOnlyList<OperationItem> Items { get; set; }
    [J("itemsAsKey")] public required bool ItemsAsKey { get; set; }
}

public class UpdateItemAttributesOperation
{
    [J("item")] public required OperationItem Item { get; set; }
    [J("newAttributes")] public required UpdateAttributesOperationNewAttributes NewAttributes { get; set; }
}

public class UpdateListAttributesOperation
{
    [J("newAttributes")] public required UpdateAttributesOperationNewAttributes NewAttributes { get; set; }
}

public class UpdateAttributesOperationNewAttributes
{
    [J("values")] public required Dictionary<string, object> Values { get; set; }
}

public class OperationItem
{
    [J("uri")] public required string Uri { get; set; }

    public override string ToString() => $"<{Uri}>";
}

public class OperationsRequest
{
    [J("ops")] public required IReadOnlyList<OperationItem2> Operations { get; set; }
}

public class OperationItem2
{
    [J("kind")] public required string Kind { get; set; }
    [J("updateListAttributes")] public required UpdateListAttributes UpdateListAttributes { get; set; }
}

public class UpdateListAttributes
{
    [J("newAttributes")] public required NewAttributes NewAttributes { get; set; }
}

public class NewAttributes
{
    [J("values")] public required Values2 Values { get; set; }
}

public class Values2
{
    [J("name")] public required string Name { get; set; }

    public override string ToString() => Name;
}
