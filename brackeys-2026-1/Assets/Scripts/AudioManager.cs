using FMOD.Studio;
using FMODUnity;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour {
    public static AudioManager Instance;

    #region ----- EVENT REGISTRY -----

    [System.Serializable]
    public class EventEntry {
        public string key;
        public EventReference eventRef;
    }

    [SerializeField] private List<EventEntry> eventList;

    private Dictionary<string, EventReference> eventMap;

    #endregion

    #region ----- ACTIVE 3D INSTANCES -----

    private Dictionary<string, EventInstance> activeInstances = new();

    #endregion

    #region ----- UNITY -----

    private void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        eventMap = new Dictionary<string, EventReference>();
        foreach (var entry in eventList) {
            eventMap[entry.key] = entry.eventRef;
        }

        RuntimeManager.LoadBank("Master");
        RuntimeManager.LoadBank("Master.strings");
    }

    private void OnApplicationFocus(bool hasFocus) {
        if (hasFocus)
            RuntimeManager.CoreSystem.mixerResume();
        else
            RuntimeManager.CoreSystem.mixerSuspend();
    }

    #endregion

    #region ----- ONE SHOT (2D / SIMPLE SFX) -----

    public void PlayEvent2D(string key) {
        if (eventMap.TryGetValue(key, out var eventRef)) {
            RuntimeManager.PlayOneShot(eventRef);
        }
        else {
            Debug.LogWarning($"Audio event not found: {key}");
        }
    }

    #endregion

    #region ----- 2D/3D EVENT WITH CONTROL -----

    public void StartEvent3D(string key, Vector3 position) {
        if (!eventMap.TryGetValue(key, out var eventRef)) {
            Debug.LogWarning($"Audio event not found: {key}");
            return;
        }

        if (activeInstances.ContainsKey(key))
            return;

        EventInstance instance = RuntimeManager.CreateInstance(eventRef);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(position));
        instance.start();

        activeInstances[key] = instance;
    }

    public void StartEvent2D(string key) {
        if (!eventMap.TryGetValue(key, out var eventRef)) {
            Debug.LogWarning($"Audio event not found: {key}");
            return;
        }

        if (activeInstances.ContainsKey(key))
            return;

        EventInstance instance = RuntimeManager.CreateInstance(eventRef);
        instance.start();

        activeInstances[key] = instance;
    }

    public void StopEvent(string key, bool fadeOut = true) {
        if (activeInstances.TryGetValue(key, out var instance)) {
            instance.stop(fadeOut
                ? FMOD.Studio.STOP_MODE.ALLOWFADEOUT
                : FMOD.Studio.STOP_MODE.IMMEDIATE);

            instance.release();
            activeInstances.Remove(key);
        }
    }

    #endregion

    #region --------MUSIC---------

    private EventInstance roomMusicInstance;
    private bool musicInitialized;
    private readonly string musicKey = "music_Room";

    public void StartRoomMusic() {

        if (musicInitialized) { return; }

        if (!eventMap.TryGetValue(musicKey, out var eventRef)) {
            Debug.LogWarning("Music event not found");
            return;
        }

        roomMusicInstance = RuntimeManager.CreateInstance(eventRef);
        roomMusicInstance.start();

        musicInitialized = true;
    }

    public void PauseRoomMusic() {
        if (roomMusicInstance.isValid())
            roomMusicInstance.setPaused(true);
    }

    public void ResumeRoomMusic() {
        if (roomMusicInstance.isValid())
            roomMusicInstance.setPaused(false);
    }
    public void ResetRoomMusicState() {
        if (!roomMusicInstance.isValid())
            return;

        roomMusicInstance.setParameterByName("MusicState", 0);
    }
    //public void SetMusicState(int value) {
    //    if (roomMusicInstance.isValid())
    //        roomMusicInstance.setParameterByName("MusicState", value);
    //}

    public void TriggerRoomMusicEvent() {
        if (!roomMusicInstance.isValid())
            return;

        int roll = Random.Range(0, 4);

        switch (roll) {
            case 0:
                // nothing changes
                break;

            case 1:
                roomMusicInstance.setParameterByName(
                    "MusicState",
                    Random.Range(1, 6)); // random normal stem muted
                break;

            case 2:
                roomMusicInstance.setParameterByName(
                    "MusicState",
                    Random.Range(6, 9)); //random distorted stem added
                break;

            case 3:
                roomMusicInstance.setParameterByName(
                    "MusicState",
                    Random.Range(10, 14)); // random normal stem detuned
                break;
        }
    }
    #endregion
}