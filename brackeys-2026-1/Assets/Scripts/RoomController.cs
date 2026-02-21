using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RoomController : MonoBehaviour
{
    public Transform playerTransform;
    public Transform startPoint;
    public GameObject roomPrefab;
    public TMPro.TextMeshProUGUI roundCounterText;

    [Header("Locations")]
    public Transform roomEntrance;
    public Transform hallwayEntrance;

    [Header("Debug")]
    public bool forceExistentialAnomalyInNextRound = false; // For testing purposes, forces the next anomaly to be an existential anomaly when selected
    

    private List<AnomalousObject> objectsInRoom = new();
    private Queue<AnomalousObject> anomalyBag = new();

    private int maxAnomaliesPerRound = 2;
    public int maxRounds = 10;
    public int currentRound { get; private set; } = 0;

    enum GameState { Room, Hallway, GameOver}
    GameState currentGameState = GameState.Room;

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

        if (currentRound >= maxRounds)
        {
            Debug.Log("Max rounds reached. Game won!");
            // TO DO: Trigger game win here
            return;
        }

        foreach (var obj in objectsInRoom) obj.SetNormal();
        UpdateRoomCounter();

        int anomaliesThisRound = currentRound == 0 ? 0 : Mathf.Min(
            Random.value < 0.5f && currentRound != 1 ? 0 : Random.Range(1, maxAnomaliesPerRound + 1),
            anomalyBag.Count
        ); // Sorry this line is a mess, but it basically means: 50% chance for 0 anomalies (except for round 1), otherwise 1 to max anomalies, but never more than what's left in the bag
        Debug.Log($"Starting round {currentRound} with {anomaliesThisRound} anomalies. Anomaly bag has {anomalyBag.Count} objects left.");

        for (int i = 0; i < anomaliesThisRound; i++)
        {
            var obj = anomalyBag.Dequeue();
            if (forceExistentialAnomalyInNextRound)
            {
                obj.forceExistentialAnomaly = true;
                forceExistentialAnomalyInNextRound = false; // Reset the flag
            }
            int variantIndex = Random.Range(0, obj.GetAnomalyVariantCount()); // Random for now
            obj.SetAnomaly(variantIndex);
        }


        currentRound++;
        ResetPlayer();
    }

    void InitializeRoom()
    {
        objectsInRoom = roomPrefab.GetComponentsInChildren<AnomalousObject>().ToList();
    }

    public void OnPlayerTryExit()
    {
        bool anyAnomaliesLeft = objectsInRoom.Any(obj => obj.anomalyActive);
        
        if (currentGameState == GameState.Room)
        {
            currentGameState = GameState.Hallway;
            playerTransform.position = hallwayEntrance.position;
        }
        else if (currentGameState == GameState.Hallway)
        {
            currentGameState = GameState.Room;
            playerTransform.position = roomEntrance.position;
            OnPlayerEnterRoom();
        }
    }

    public void OnPlayerEnterRoom()
    {

        bool anyAnomaliesLeft = objectsInRoom.Any(obj => obj.anomalyActive);
        if (anyAnomaliesLeft)
        {
            Debug.Log("Failed! Anomaly still present.");
            ResetGame();
        }
        else
        {
            Debug.Log("Success! Moving to next round.");
            StartNextRound();
        }
    }

    // This means trying to exit through the entrance
    public void OnPlayerAttemptEntranceExit()
    {
        Debug.Log($"Player attempted to exit through the entrance! Game state: {currentGameState}");
        if (currentGameState == GameState.Hallway) return;

        bool anyAnomaliesLeft = objectsInRoom.Any(obj => obj.anomalyActive);
        ExistentialAnomaly existentialAnomaly = FindAnyObjectByType<ExistentialAnomaly>();
        if (existentialAnomaly && existentialAnomaly.effectsActive) // If existentail anomaly present, leave to ExistentialAnomaly.OnPlayerUsedEntrance/Exit
        {
            return;
        }
        Debug.Log("Failed! No existential anomaly present. Should have exited.");
        ResetGame();
        // Existential anomalies handle their own reset via ExistentialAnomaly.OnPlayerUsedEntrance/Exit
    }

    void ResetPlayer()
    {
        playerTransform.position = startPoint.position;
    }

    void ResetGame()
    {
        FillAnomalyBag();
        currentRound = 0;
        StartNextRound();
    }

    public void UpdateRoomCounter()
    {
        roundCounterText.text = $"{currentRound + 1}";
    }
}