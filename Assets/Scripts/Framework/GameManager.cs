using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public enum GameState
    {
        Playing, // 游戏进行中
        Paused, // 游戏暂停
        GameOver // 游戏结束
    }

    [Header("管理器预制件")]
    public GameObject managersPrefab;

    private GameObject managersInstance;
    private GameState currentState;

    // 游戏开始时初始化状态
    void Start()
    {
        StartGame();
    }

    private void InstantiateAndInitializeManagers()
    {
        if (managersInstance != null)
        {
            DebugManager.LogWarning("Managers already instantiated!");
            return;
        }
        
        if (managersPrefab != null)
        {
            managersInstance = Instantiate(managersPrefab);
            managersInstance.name = "Managers";
            DontDestroyOnLoad(managersInstance);
            
            DebugManager.Log("Managers prefab instantiated");
        }
        else
        {
            // 如果没有分配预制件，尝试从Resources加载
            managersPrefab = Resources.Load<GameObject>("Prefabs/Managers");
            if (managersPrefab != null)
            {
                managersInstance = Instantiate(managersPrefab);
                managersInstance.name = "Managers";
                DontDestroyOnLoad(managersInstance);
                DebugManager.Log("Managers prefab loaded from Resources and instantiated");
            }
            else
            {
                DebugManager.LogError("Managers prefab not found in Resources!");
                return;
            }
        }

        // 初始化所有管理器
        InitializeAllManagers();
    }

    private void InitializeAllManagers()
    {
        if (managersInstance == null) return;

        // 获取并初始化所有管理器
        IManager[] managers = managersInstance.GetComponentsInChildren<IManager>();
        foreach (var manager in managers)
        {
            manager.Initialize();
        }

        DebugManager.Log($"All {managers.Length} managers initialized successfully");
    }

    public void SetGameState(GameState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1;
                UIManager.Instance.ClosePanel("PausePanel");
                UIManager.Instance.ClosePanel("GameOverPanel");
                
                // 在游戏开始时实例化管理器
                if (managersInstance == null)
                {
                    InstantiateAndInitializeManagers();
                }
                break;

            case GameState.Paused:
                Time.timeScale = 0;
                UIManager.Instance.OpenPanel("PausePanel");
                break;

            case GameState.GameOver:
                Time.timeScale = 0;
                UIManager.Instance.OpenPanel("GameOverPanel");
                break;
        }
    }

    // 销毁管理器实例（用于重新开始游戏等场景）
    public void DestroyManagers()
    {
        if (managersInstance != null)
        {
            Destroy(managersInstance);
            managersInstance = null;
            DebugManager.Log("Managers destroyed");
        }
    }

    #region 状态控制

    // 游戏开始
    public void StartGame()
    {
        SetGameState(GameState.Playing);
    }

    // 暂停游戏
    public void PauseGame()
    {
        if (currentState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
        }
    }

    // 恢复游戏
    public void ResumeGame()
    {
        if (currentState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }
    }

    // 游戏结束
    public void EndGame()
    {
        SetGameState(GameState.GameOver);
    }

    // 返回主菜单
    public void ReturnToMainMenu()
    {
        SetGameState(GameState.Playing);
        // 销毁管理器，因为主菜单可能不需要它们
        DestroyManagers();
        SceneLoader.Instance.LoadScene(GameScene.MainMenu);
    }

    // 重新开始游戏
    public void RestartGame()
    {
        DestroyManagers();
        SetGameState(GameState.Playing);
        // 这里可以添加其他重置逻辑
    }

    #endregion
}

// 管理器接口
public interface IManager
{
    void Initialize();
}