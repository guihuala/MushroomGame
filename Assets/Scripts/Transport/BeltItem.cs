public class BeltItem
{
    public ItemPayload payload;
    public float pos;

    public readonly long id;
    private static long _nextId;

    public BeltItem(ItemPayload payload)
    {
        this.payload = payload;
        pos = 0f;
        id = ++_nextId;
    }
}