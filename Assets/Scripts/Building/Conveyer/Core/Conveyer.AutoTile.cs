using UnityEngine;

public partial class Conveyer
{
    private int _lastAutoTileFrame = -1;

    public void AutoTile()
    {
        if (_lastAutoTileFrame == Time.frameCount) return;
        _lastAutoTileFrame = Time.frameCount;
        AutoTileSystem.RewireAround(grid, this, false);
        UpdateVisualDirection();
        FindBestOutputConnection();
        MarkPathDirty(); // 让可视化在下一帧重建
    }
}