using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewRoomData", menuName = "Room Data")]
public class RoomData : ScriptableObject
{
    public string roomName;
    public GameObject roomPrefab; // The physical layout
    public int anomalyCount = 1;  // How many objects should glitch?
}