using UnityEngine;
using System.Collections.Generic;

public class AnomalousObject : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject normalPrefab;
    public List<GameObject> anomalyPrefabs;

    private GameObject currentInstance;
    public bool anomalyActive { get; private set; }

    public void Awake()
    {
        SetNormal();
    }

    public int GetAnomalyVariantCount()
    {
        return anomalyPrefabs.Count;
    }

    public void SetAnomaly(int variantIndex)
    {
        if (variantIndex < 0 || variantIndex >= anomalyPrefabs.Count)
        {
            Debug.LogError("Invalid anomaly index.");
            return;
        }

        ClearCurrent();

        currentInstance = Instantiate(
            anomalyPrefabs[variantIndex],
            transform.position,
            transform.rotation,
            transform
        );

        anomalyActive = true;
    }

    public void SetNormal()
    {
        ClearCurrent();

        currentInstance = Instantiate(
            normalPrefab,
            transform.position,
            transform.rotation,
            transform
        );

        anomalyActive = false;
    }

    private void ClearCurrent()
    {
        if (currentInstance != null)
            Destroy(currentInstance);
    }

    public void FixAnomaly()
    {
        Debug.Log($"Attempting to fix {gameObject.name}...");
        if (anomalyActive)
        {
            Debug.Log($"{gameObject.name} fixed!");
            SetNormal();
            // VFX/SFX can be triggered here
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
    }
}
