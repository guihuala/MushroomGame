using UnityEngine;

public class GameManager : Singleton<GameManager>
{
    public enum GameState
    {
        MainMenu,    // 主菜单
        Playing,     // 游戏进行中
        Paused,      // 游戏暂停
        GameOver     // 游戏结束
    }

    private GameState currentState;
    
    // 游戏开始时初始化状态
    void Start()
    {
        SetGameState(GameState.MainMenu);
    }
    
    public void SetGameState(GameState newState)
    {
        currentState = newState;

        switch (newState)
        {
            case GameState.MainMenu:
                Time.timeScale = 0;
                ShowMainMenu();
                break;

            case GameState.Playing:
                Time.timeScale = 1;
                HideAllMenus();
                break;

            case GameState.Paused:
                Time.timeScale = 0;
                ShowPauseMenu();
                break;

            case GameState.GameOver:
                Time.timeScale = 0;
                ShowGameOverMenu();
                break;
        }
    }
    
    // 显示主菜单
    private void ShowMainMenu()
    {
        
    }

    // 显示暂停菜单
    private void ShowPauseMenu()
    {
        
    }

    // 显示游戏结束菜单
    private void ShowGameOverMenu()
    {
        
    }

    // 隐藏所有菜单
    private void HideAllMenus()
    {
        
    }

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
        SetGameState(GameState.MainMenu);
    }
}
