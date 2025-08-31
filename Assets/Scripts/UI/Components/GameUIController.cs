using System;
using UnityEngine;
using UnityEngine.UI;

public class GameUIController : MonoBehaviour
{
    public Button startButton;


    private void Awake()
    {
        
    }

    public void OnStartButtonClicked()
    {
        GameManager.Instance.StartGame();
    }
    
    public void OnPauseButtonClicked()
    {
        GameManager.Instance.PauseGame();
    }
    
    public void OnResumeButtonClicked()
    {
        GameManager.Instance.ResumeGame();
    }
    
    public void OnExitButtonClicked()
    {
        GameManager.Instance.EndGame();
    }
    
    public void OnReturnToMainMenuButtonClicked()
    {
        GameManager.Instance.ReturnToMainMenu();
    }
}