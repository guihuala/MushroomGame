using UnityEngine;


public interface IBeltNode {
    // 栅格位置与朝向（单位向量，Up/Right/Down/Left）
    Vector2Int Cell { get; }
    Vector2Int InDir { get; }
    Vector2Int OutDir { get; }

    // 供调度器调用的两步：先移动可视，再尝试交接
    void StepMove(float dt);
    void StepTransfer();
}
