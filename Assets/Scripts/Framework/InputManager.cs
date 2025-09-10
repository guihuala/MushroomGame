using System.Collections.Generic;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }

    [Header("通用输入设置")]
    public KeyCode eraseToggleKey = KeyCode.X;
    public KeyCode holdToErasePrimary = KeyCode.LeftAlt;
    public KeyCode holdToEraseSecondary = KeyCode.RightAlt;
    public KeyCode exitBuildKey = KeyCode.Escape;
    
    [Header("建造操作输入")]
    public KeyCode rotateKey = KeyCode.R;
    
    [Header("相机控制输入")]
    public string horizontalAxis = "Horizontal";
    public string verticalAxis = "Vertical";
    public string zoomAxis = "Mouse ScrollWheel";
    
    // ===== 右键辅助：按下/按住/抬起 =====
    public bool IsRightMouseDown() => GetMouseButtonDown(1);
    public bool IsRightMouseHeld() => GetMouseButton(1);
    public bool IsRightMouseUp() => GetMouseButtonUp(1);

    // 输入状态缓存
    private Dictionary<KeyCode, bool> _keyDownCache = new Dictionary<KeyCode, bool>();
    private Dictionary<KeyCode, bool> _keyUpCache = new Dictionary<KeyCode, bool>();
    private Dictionary<KeyCode, bool> _keyHeldCache = new Dictionary<KeyCode, bool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // 清空缓存
        _keyDownCache.Clear();
        _keyUpCache.Clear();
        _keyHeldCache.Clear();
    }

    // ===== 通用输入方法 =====
    public bool GetKeyDown(KeyCode keyCode)
    {
        if (!_keyDownCache.ContainsKey(keyCode))
            _keyDownCache[keyCode] = Input.GetKeyDown(keyCode);
        
        return _keyDownCache[keyCode];
    }

    public bool GetKeyUp(KeyCode keyCode)
    {
        if (!_keyUpCache.ContainsKey(keyCode))
            _keyUpCache[keyCode] = Input.GetKeyUp(keyCode);
        
        return _keyUpCache[keyCode];
    }

    public bool GetKey(KeyCode keyCode)
    {
        if (!_keyHeldCache.ContainsKey(keyCode))
            _keyHeldCache[keyCode] = Input.GetKey(keyCode);
        
        return _keyHeldCache[keyCode];
    }

    public float GetAxis(string axisName)
    {
        return Input.GetAxis(axisName);
    }

    public float GetAxisRaw(string axisName)
    {
        return Input.GetAxisRaw(axisName);
    }

    public bool GetMouseButton(int button)
    {
        return Input.GetMouseButton(button);
    }

    public bool GetMouseButtonDown(int button)
    {
        return Input.GetMouseButtonDown(button);
    }

    public bool GetMouseButtonUp(int button)
    {
        return Input.GetMouseButtonUp(button);
    }

    public Vector3 GetMousePosition()
    {
        return Input.mousePosition;
    }

    // ===== 特定功能输入检查 =====
    public bool IsEraseTogglePressed()
    {
        return GetKeyDown(eraseToggleKey);
    }

    public bool IsHoldEraseActive()
    {
        bool primary = holdToErasePrimary != KeyCode.None && GetKey(holdToErasePrimary);
        bool secondary = holdToEraseSecondary != KeyCode.None && GetKey(holdToEraseSecondary);
        return primary || secondary;
    }

    public bool IsExitBuildPressed()
    {
        return GetKeyDown(exitBuildKey);
    }

    public bool IsRotatePressed()
    {
        return GetKeyDown(rotateKey);
    }

    public Vector2 GetCameraMovementInput()
    {
        return new Vector2(
            GetAxis(horizontalAxis),
            GetAxis(verticalAxis)
        );
    }

    public float GetZoomInput()
    {
        return GetAxis(zoomAxis);
    }

    public bool IsCameraDragStarted()
    {
        return GetMouseButtonDown(2);
    }

    public bool IsCameraDragging()
    {
        return GetMouseButton(2);
    }

    public bool IsCameraDragEnded()
    {
        return GetMouseButtonUp(2);
    }

    public bool IsBuildActionPressed()
    {
        return GetMouseButtonDown(0);
    }

    public bool IsBuildActionHeld()
    {
        return GetMouseButton(0);
    }

    public bool IsBuildActionReleased()
    {
        return GetMouseButtonUp(0);
    }

    public bool IsBuildCancelled()
    {
        return GetMouseButtonDown(1);
    }

    // ===== 右键检测 =====
    public bool IsRightClick()
    {
        return GetMouseButtonDown(1); // 1 是鼠标右键
    }

    // 检查是否在UI上
    public bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null &&
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
}
