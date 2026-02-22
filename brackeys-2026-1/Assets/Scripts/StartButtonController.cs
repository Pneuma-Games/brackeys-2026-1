using UnityEngine;
using UnityEngine.SceneManagement;

public class StartButtonController : MonoBehaviour
{
    public string sceneName = "RoomScene";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void StartClick()
    {
        AudioManager.Instance.PlayEvent2D("ui_StartGame");
        AudioManager.Instance.StartRoomMusic();
        SceneManager.LoadScene(sceneName);
    }

    public void QuitClick()
    {
        Application.Quit();
    }
}
