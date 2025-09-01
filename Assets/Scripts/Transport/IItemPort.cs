public interface IItemPort
{
    // 往外给货（从该端口输出）
    bool TryPull(ref ItemPayload payload);

    // 往里收货（向该端口输入）
    bool TryPush(in ItemPayload payload);

    // 是否还有可输出/可接收
    bool CanPull { get; }
    bool CanPush { get; }
}