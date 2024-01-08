using Robust.Shared.Serialization;
using Robust.Shared.Serialization;

namespace Content.Shared.MassMedia.Systems;

[Serializable, NetSerializable]
public struct NewsArticle
{
    [ViewVariables(VVAccess.ReadWrite)]
    public string Name;

    [ViewVariables(VVAccess.ReadWrite)]
    public string Content;

    [ViewVariables(VVAccess.ReadWrite)]
    public string? Author;

    [ViewVariables]
    public ICollection<(NetEntity, uint)>? AuthorStationRecordKeyIds;

    [ViewVariables]
    public TimeSpan ShareTime;
}

[ByRefEvent]
public record struct NewsArticlePublishedEvent(NewsArticle Article);

[ByRefEvent]
public record struct NewsArticleDeletedEvent;
