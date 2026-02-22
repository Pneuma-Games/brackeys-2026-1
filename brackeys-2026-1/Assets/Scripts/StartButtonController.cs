using UnityEngine;
using UnityEngine.SceneManagement;
using FMODUnity;

public class StartButtonController : MonoBehaviour
{
    public string sceneName = "RoomScene";
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void StartClick()
    {
        //FMODUnity.RuntimeManager.StudioSystem.flushCommands();
        AudioManager.Instance.PlayEvent2D("ui_StartGame");
        SceneManager.LoadScene(sceneName);
    }
}
