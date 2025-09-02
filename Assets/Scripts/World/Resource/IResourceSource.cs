public interface IResourceSource
{
    ItemDef YieldItem { get; }
    bool TryConsumeOnce(); // 消耗
}