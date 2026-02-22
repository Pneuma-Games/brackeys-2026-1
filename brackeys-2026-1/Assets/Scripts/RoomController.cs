using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class RoomController : MonoBehaviour
{
    private static WaitForSeconds _waitForSeconds0_5 = new WaitForSeconds(0.5f);
    public Transform playerTransform;
    public Transform startPoint;
    public GameObject roomPrefab;
    public TMPro.TextMeshProUGUI roundCounterText;
    public ScreenFader screenFader;

    [Header("Locations")]
    public Transform roomEntrance;
    public Transform hallwayEntrance;

    [Header("Debug")]
    public bool forceExistentialAnomalyInNextRound = false; // For testing purposes, forces the next anomaly to be an existential anomaly when selected

    [Header("Settings")]

    [Range(0f, 0.5f)]
    [Tooltip("Chance that existential anomaly is chosen (0-0.5)")]
    public float existentialAnomalyChance = 0.3f;

    [Range(0f, 0.5f)]
    [Tooltip("Chance that reactive anomaly is chosen (0-0.5)")]
    public float reactiveAnomalyChance = 0.3f;

    [Range(0f, 1f)]
    [Tooltip("Chance that no anomalies spawn this round (0-1)")]
    public float chanceForNoAnomalies = 0.5f;
    

    private List<AnomalousObject> objectsInRoom = new();
    private Queue<AnomalousObject> anomalyBag = new();

    private int maxAnomaliesPerRound = 2;
    public int maxRounds = 10;
    public int currentRound { get; private set; } = 0;

    enum GameState { Room, Hallway, GameOver}
    GameState currentGameState = GameState.Room;

    private bool existentialAnomalyPresent = false;
    private bool usedEntranceAsExit = false;

    void Start()
    {
        InitializeRoom();
        FillAnomalyBag();
        StartNextRound();
    }
    void FillAnomalyBag()
    {
        var shuffled = objectsInRoom.OrderBy(x => Random.value).ToList();
        anomalyBag = new Queue<AnomalousObject>(shuffled);
        Debug.Log($"Anomaly bag filled with {anomalyBag.Count} objects.");
    }

    void StartNextRound()
    {
        existentialAnomalyPresent = false;
        usedEntranceAsExit = false;
        if (currentRound >= maxRounds)
        {
            Debug.Log("Max rounds reached. Game won!");
            // TO DO: Trigger game win here
            return;
        }

        foreach (var obj in objectsInRoom) obj.SetNormal();
        UpdateRoomCounter();

        int anomaliesThisRound = currentRound == 0 ? 0 : Mathf.Min(
            Random.value < chanceForNoAnomalies && currentRound != 1 ? 0 : Random.Range(1, maxAnomaliesPerRound + 1),
            anomalyBag.Count
        ); // Sorry this line is a mess, but it basically means: chanceForNoAnomalies chance for 0 anomalies (except for round 1), otherwise 1 to max anomalies, but never more than what's left in the bag
        Debug.Log($"Starting round {currentRound} with {anomaliesThisRound} anomalies. Anomaly bag has {anomalyBag.Count} objects left.");

        for (int i = 0; i < anomaliesThisRound; i++)
        {
            var obj = anomalyBag.Dequeue();
            if (forceExistentialAnomalyInNextRound)
            {
                obj.forceExistentialAnomaly = true;
                forceExistentialAnomalyInNextRound = false; // Reset the flag
            }

            int count = obj.GetAnomalyVariantCount();
            float roll = Random.value; // 0.0 to 1.0
            int variantIndex;

            if (roll < existentialAnomalyChance)
            {
                variantIndex = 0;
            }
            else if (roll < existentialAnomalyChance + reactiveAnomalyChance)
            {
                variantIndex = 1; // 1 is reactive by convetion
            }
            else
            {
                variantIndex = Random.Range(2, count); // These will be procedural ones
            }

            obj.SetAnomaly(variantIndex);
        }


        currentRound++;
        ResetPlayer();
    }

    void InitializeRoom()
    {
        objectsInRoom = roomPrefab.GetComponentsInChildren<AnomalousObject>().ToList();
    }

    public void OnPlayerTryExit() // This is just for hallway stuff
    {   
        StartCoroutine(HandleDoorTransition());
    }

    private IEnumerator HandleDoorTransition()
    {
        yield return screenFader.FadeOut();
        yield return _waitForSeconds0_5;

        if (currentGameState == GameState.Room)
        {
            currentGameState = GameState.Hallway;
            playerTransform.position = hallwayEntrance.position;
            Camera.main.transform.position = playerTransform.position;
        }
        else if (currentGameState == GameState.Hallway)
        {
            currentGameState = GameState.Room;
            playerTransform.position = roomEntrance.position;
            Camera.main.transform.position = playerTransform.position;
            OnPlayerEnterRoom();
        }
        yield return _waitForSeconds0_5;
        yield return screenFader.FadeIn();
    }

    public void OnPlayerEnterRoom()
    {
        bool anyAnomaliesLeft = objectsInRoom.Any(obj => obj.anomalyActive);
        ExistentialAnomaly[] existentialAnomalies = FindObjectsByType<ExistentialAnomaly>(FindObjectsSortMode.None);
        if (existentialAnomalyPresent && usedEntranceAsExit)
        {
            Debug.Log("Correctly used entrance in the presence of existential anomaly(s).");
            foreach (var ea in existentialAnomalies) ea.RevertAll();
            StartNextRound();
            return;
        }
        else if (existentialAnomalyPresent && !usedEntranceAsExit)
        {
            Debug.Log("Failed! Used exit in the presence of existential anomaly(s).");
            foreach (var ea in existentialAnomalies) ea.RevertAll();
            ResetGame();
            return;
        }
        // At this point we confirm no existential anomalies are active
        if (anyAnomaliesLeft) // Doesn't matter which exit method is used, fail either way
        {
            Debug.Log("Failed! Anomaly still present.");
            ResetGame();
            return;
        }
        else if (!anyAnomaliesLeft && usedEntranceAsExit)
        {
            Debug.Log("Failed! Used entrance even though no anomalies exist.");
            ResetGame();
            return;
        }
        else // no anomalies left and didn't use entrance as exit, success!
        {
            Debug.Log("Success! Moving to next round.");
            StartNextRound();
        }
    }

    // This means trying to exit through the entrance
    public void OnPlayerAttemptEntranceExit()
    {
        //Debug.Log($"Player attempted to exit through the entrance! Game state: {currentGameState}");
        if (currentGameState == GameState.Hallway) return;

        usedEntranceAsExit = true;

        bool anyAnomaliesLeft = objectsInRoom.Any(obj => obj.anomalyActive);
        ExistentialAnomaly existentialAnomaly = FindAnyObjectByType<ExistentialAnomaly>();
        if (existentialAnomaly && existentialAnomaly.effectsActive)
        {
            existentialAnomalyPresent = true;
            OnPlayerTryExit();
            return;
        }
        OnPlayerTryExit(); // All logic moved to this function
    }

    void ResetPlayer()
    {
        playerTransform.position = startPoint.position;
    }

    void ResetGame()
    {
        existentialAnomalyPresent = false;
        usedEntranceAsExit = false;
        FillAnomalyBag();
        currentRound = 0;
        StartNextRound();
    }

    public void UpdateRoomCounter()
    {
        roundCounterText.text = $"{currentRound + 1}";
    }
}