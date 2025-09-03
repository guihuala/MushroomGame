public interface IItemPort
{
    // 往里收货
    bool TryPull(ref ItemPayload payload);

    // 往外给货
    bool TryPush(in ItemPayload payload);

    // 是否还有可输出/可接收
    bool CanPush { get; }
    bool CanPull { get; }
}