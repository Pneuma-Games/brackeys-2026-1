using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RoomController : MonoBehaviour
{
    public Transform playerTransform;
    public Transform startPoint;
    public GameObject roomPrefab;

    private List<AnomalousObject> objectsInRoom = new();
    private Queue<AnomalousObject> anomalyBag = new();

    private int maxAnomaliesPerRound = 2;
    private int currentRound = 0;

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
        foreach (var obj in objectsInRoom) obj.SetNormal();

        int anomaliesThisRound = currentRound == 0 ? 0 : Mathf.Min(
            Random.value < 0.2f && currentRound != 1 ? 0 : Random.Range(1, maxAnomaliesPerRound + 1),
            anomalyBag.Count
        ); // Sorry this line is a mess, but it basically means: 20% chance for 0 anomalies (except for round 1), otherwise 1 to max anomalies, but never more than what's left in the bag
        Debug.Log($"Starting round {currentRound} with {anomaliesThisRound} anomalies. Anomaly bag has {anomalyBag.Count} objects left.");

        for (int i = 0; i < anomaliesThisRound; i++)
        {
            var obj = anomalyBag.Dequeue();
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

        if (anyAnomaliesLeft)
        {
            Debug.Log("Failed! Anomaly still present.");
            FillAnomalyBag();
            currentRound = 0; // Resets the gsame entirely, so first round will be anomaly free again
            StartNextRound();
        }
        else
        {
            if (anomalyBag.Count <= 0)
            {
                Debug.Log("Game won!");
                return;
            }
            Debug.Log("Success! Moving to next round.");
            StartNextRound();
        }
    }

    void ResetPlayer()
    {
        playerTransform.position = startPoint.position;
    }
}