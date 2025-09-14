using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public enum GameState
    {
        Playing, // 游戏进行中
        Paused, // 游戏暂停
        GameCleared 
    }

    [Header("管理器预制件")]
    public GameObject managersPrefab;

    private GameObject managersInstance;
    private GameState currentState;

    // 游戏开始时初始化状态
    void Start()
    {
        CheckFirstTimePlay(); // 检查是否第一次游玩
        StartGame();
    }

    private void CheckFirstTimePlay()
    {
        // 检查是否是第一次游玩
        if (PlayerPrefs.GetInt("FirstTimePlay", 0) == 0) // 0表示第一次游玩
        {
            // 显示新手指引面板
            UIManager.Instance.OpenPanel("GuidePanel");

            // 设置已游玩标记，避免下次显示
            PlayerPrefs.SetInt("FirstTimePlay", 1);
            PlayerPrefs.Save(); // 保存更改
        }
    }

    private void InstantiateAndInitializeManagers()
    {
        if (managersInstance != null)
        {
            return;
        }
        
        if (managersPrefab != null)
        {
            managersInstance = Instantiate(managersPrefab);
            managersInstance.name = "Managers";
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
                break;
            
            case GameState.GameCleared:
                Time.timeScale = 0;
                UIManager.Instance.OpenPanel("GameClearPanel");
                break;
        }
    }
    
    public void DestroyManagers()
    {
        if (managersInstance != null)
        {
            Destroy(managersInstance);
            managersInstance = null;
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
        SetGameState(GameState.GameCleared);
    }

    // 返回主菜单
    public void ReturnToMainMenu()
    {
        SetGameState(GameState.Playing);
        // 销毁管理器
        DestroyManagers();
        SceneLoader.Instance.LoadScene(GameScene.MainMenu);
    }

    // 重新开始游戏
    public void RestartGame()
    {
        DestroyManagers();
        SetGameState(GameState.Playing);
        
    }

    #endregion
}

// 管理器接口
public interface IManager
{
    void Initialize();
}