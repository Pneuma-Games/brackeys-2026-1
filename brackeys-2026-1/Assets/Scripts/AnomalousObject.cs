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
        if (anomalyPrefabs.Count > 0)
        {
            return anomalyPrefabs.Count + 4;
        }
        return anomalyPrefabs.Count;
    }

    public void SetAnomaly(int variantIndex)
    {
        int originalCount = anomalyPrefabs.Count;
        int maxIndex = GetAnomalyVariantCount();

        if (variantIndex < 0 || variantIndex >= maxIndex)
        {
            Debug.LogError("Invalid anomaly index.");
            return;
        }

        ClearCurrent();

        // Determine which prefab and what modifications to apply
        GameObject prefabToSpawn;
        bool isProcedural = variantIndex >= originalCount;
        int proceduralType = -1;

        if (isProcedural)
        {
            prefabToSpawn = normalPrefab;
            proceduralType = variantIndex - originalCount;
        }
        else
        {
            prefabToSpawn = anomalyPrefabs[variantIndex];
        }

        currentInstance = Instantiate(
            prefabToSpawn,
            transform.position,
            transform.rotation,
            transform
        );

        if (isProcedural)
        {
            ApplyProceduralEffect(currentInstance, proceduralType);
        }

        anomalyActive = true;
    }

    private void ApplyProceduralEffect(GameObject instance, int type)
    {
        switch (type)
        {
            case 0: // Slightly smaller
                instance.transform.localScale *= 0.8f;
                break;
            case 1: // Slightly larger
                instance.transform.localScale *= 1.2f;
                break;
            case 2: // Color shifted
                Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
                Color[] palette = new Color[]
                {
                    HexToColor("e5c2c0"), HexToColor("8fd5a6"), HexToColor("329f5b"),
                    HexToColor("0c8346"), HexToColor("0d5d56"), HexToColor("7353ba"),
                    HexToColor("2f195f"), HexToColor("0f1020"), HexToColor("332e3c"),
                    HexToColor("6f5e5c")
                };
                Color randomColor = palette[Random.Range(0, palette.Length)];
                
                foreach (Renderer r in renderers)
                {
                    r.material.color = randomColor; 
                }
                break;
            case 3: // Rotated 180 on Z
                instance.transform.Rotate(0, 0, 180);
                break;
        }
    }

    private Color HexToColor(string hex)
    {
        if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
            return color;
        return Color.gray; // Fallback
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
        if (anomalyActive)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.7f);
        }
    }
}
