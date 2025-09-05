public interface IItemPort
{
    // 往里收货
    bool TryReceive(in ItemPayload payload);

    // 往外给货，
    // 很多情况不直接用，
    // 使用到的情况：一个会“吸”的 Hub 每帧去邻居那里拿一个包；
    // 分拣器按需求从上游取。
    bool TryProvide(ref ItemPayload payload);

    // 是否还有可输出/可接收
    bool CanReceive { get; }
    bool CanProvide { get; }
}