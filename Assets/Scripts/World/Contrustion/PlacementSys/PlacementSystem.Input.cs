using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class PlacementSystem
{
    #region 输入

    private void HandleBuildModeInput(Vector2Int cell, Vector3 worldPos)
    {
        if (_input.IsRotatePressed()) RotatePreview();

        // 右键退出建造模式
        if (_input.IsRightMouseDown() && !_input.IsPointerOverUI())
        {
            ExitBuildMode();
            SetEraseCursor(false);
            return;
        }

        // 左键开始放置：记录锚点，锁轴状态重置
        if (_input.IsBuildActionPressed() && !_input.IsPointerOverUI())
        {
            _isDragging = true;
            _dragAnchorCell = cell;
            _dragLastCell = cell;
            _lineAxis = LineAxis.Free;

            PlaceOne(cell, worldPos, _currentDir);
            return;
        }

        // 拖拽连续绘制（直线锁轴）
        if (_isDragging && _input.IsBuildActionHeld() && !_input.IsPointerOverUI())
        {
            if (cell != _dragLastCell)
            {
                // 首次移动时确定轴向
                if (_lineAxis == LineAxis.Free)
                {
                    var d = cell - _dragAnchorCell;
                    _lineAxis = (Mathf.Abs(d.x) >= Mathf.Abs(d.y)) ? LineAxis.Horizontal : LineAxis.Vertical;
                }

                // 将当前鼠标格投影到锁定轴上
                Vector2Int projected = (_lineAxis == LineAxis.Horizontal)
                    ? new Vector2Int(cell.x, _dragAnchorCell.y)
                    : new Vector2Int(_dragAnchorCell.x, cell.y);

                // 沿着“上一格 -> 投影格”逐格放置
                if (projected != _dragLastCell)
                {
                    StepAndPlaceAlongLockedAxis(_dragLastCell, projected);
                }
            }

            return;
        }

        // 松开结束
        if (_input.IsBuildActionReleased())
        {
            _isDragging = false;
            _dragLastBuilding = null;
            _lineAxis = LineAxis.Free;
        }
    }
    
    private void HandleDefaultModeInput(Vector2Int cell)
    {
        // 取消拆除检查
        if (_input.IsCancelErasePressed() && _isRightErasing)
        {
            CancelErase();
            return;
        }

        // 右键按下：进入拆除流程
        if (_input.IsRightMouseDown() && !_input.IsPointerOverUI())
        {
            _isRightErasing = true;
            _eraseAsBox = false;
            _eraseAnchorCell = cell;
            ClearPendingErase(); // 这会发送隐藏提示事件

            SetEraseCursor(true);
            return;
        }

        // 右键按住：标记待拆除建筑
        if (_isRightErasing && _input.IsRightMouseHeld() && !_input.IsPointerOverUI())
        {
            MsgCenter.SendMsgAct(MsgConst.ERASE_MODE_ENTER);
            
            if (!_eraseAsBox && cell != _eraseAnchorCell)
                _eraseAsBox = true;

            if (_eraseAsBox)
            {
                MarkAreaForErase(_eraseAnchorCell, cell);
            }
            else
            {
                MarkBuildingForErase(cell);
            }
            return;
        }

        // 右键抬起：执行或取消拆除
        if (_isRightErasing && _input.IsRightMouseUp())
        {
            MsgCenter.SendMsgAct(MsgConst.ERASE_MODE_EXIT);
            
            if (_isEraseCancelled)
            {
                ClearPendingErase(); // 这会发送隐藏提示事件
            }
            else if (_eraseAsBox)
            {
                ConfirmErase(); // 这会发送确认事件和隐藏提示事件
            }
            else
            {
                ConfirmErase(); // 这会发送确认事件和隐藏提示事件
            }

            _isRightErasing = false;
            _eraseAsBox = false;
            SetEraseCursor(false);
        }
    }

    private void MarkAreaForErase(Vector2Int start, Vector2Int end)
    {
        Vector2Int min = Vector2Int.Min(start, end);
        Vector2Int max = Vector2Int.Max(start, end);

        for (int x = min.x; x <= max.x; x++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                MarkBuildingForErase(cell);
            }
        }
    }
    
    private void MarkBuildingForErase(Vector2Int cell)
    {
        var building = grid.GetBuildingAt(cell);
        MarkBuildingForErase(building);
    }

    #endregion
}