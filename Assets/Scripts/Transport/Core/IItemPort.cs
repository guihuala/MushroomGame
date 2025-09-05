public interface IItemPort
{
    // 往里收货
    bool TryReceive(in ItemPayload payload);

    // 往外给货
    bool TryProvide(ref ItemPayload payload);

    // 是否还有可输出/可接收
    bool CanReceive { get; }
    bool CanProvide { get; }
}