using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil.Cil;

public class RoomController : MonoBehaviour
{
    public RoomData currentRoomData;
    public List<RoomData> allRooms;
    public Transform playerTransform;
    public Transform startPoint;
    private List<AnomalousObject> objectsInRoom = new();

    private GameObject activeRoomInstance;
    private List<AnomalousObject> activeAnomalies = new List<AnomalousObject>();

    void Start()
    {
        if (allRooms == null || allRooms.Count == 0)
        {
            Debug.LogError("No rooms assigned in RoomController!");
            return;
        }
        LoadRoom(allRooms[0]);
    }
    void LoadRoom(RoomData roomData)
    {
        if (activeRoomInstance != null) Destroy(activeRoomInstance);
        activeAnomalies.Clear();
        
        currentRoomData = roomData;
        if (currentRoomData == null)
        {
            Debug.LogError("No Room Data assigned!");
            return;
        }
        activeRoomInstance = Instantiate(currentRoomData.roomPrefab, transform);
        playerTransform.position = startPoint.position;
        
        InitializeRoom(currentRoomData);
    }
    void InitializeRoom(RoomData roomData)
    {
        objectsInRoom = activeRoomInstance.GetComponentsInChildren<AnomalousObject>().ToList();

        foreach (var obj in objectsInRoom) obj.SetAnomaly(false);
        Debug.Log("Active anomalies count: " + activeAnomalies.Count);
        if (activeAnomalies.Count == 0)
        {
            var randomObjects = objectsInRoom.OrderBy(x => Random.value).Take(roomData.anomalyCount);
            foreach (var obj in randomObjects) activeAnomalies.Add(obj);
        }
        Debug.Log("Active anomalies count after: " + activeAnomalies.Count);
        foreach (var obj in activeAnomalies)
        {
            Debug.Log("Setting anomaly active on: " + obj.gameObject.name);
            obj.SetAnomaly(true);
        }
    }

    public void OnPlayerTryExit()
    {
        bool anyAnomaliesLeft = objectsInRoom.Any(obj => obj.anomalyActive);

        if (anyAnomaliesLeft)
        {
            Debug.Log("Failed! Anomaly still present. Resetting...");
            ResetPlayer();
        }
        else
        {
            Debug.Log("Success! Proceeding to next level.");
            LoadNextFloor();
        }
    }

    void ResetPlayer()
    {
        playerTransform.position = startPoint.position;
        // This will re-randomize anomalies in the room
        InitializeRoom(currentRoomData); 
    }

    void LoadNextFloor()
    {
        int currentIndex = allRooms.IndexOf(currentRoomData);
        if (currentIndex < allRooms.Count - 1)
        {
            LoadRoom(allRooms[currentIndex + 1]);
        }
        else
        {
            Debug.Log("Game Won!");
            // Trigger win screen here
        }
    }


}