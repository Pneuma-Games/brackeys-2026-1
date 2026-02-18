using UnityEngine;

public class AnomalousObject : MonoBehaviour
{
    [Header("Settings")]
    public Color anomalyColor = Color.red;
    public bool anomalyActive = false;

    private Color originalColor;
    private SpriteRenderer spriteRenderer;

    public void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    public void SetAnomaly(bool active)
    {
        anomalyActive = active;
        spriteRenderer.color = active ? anomalyColor : originalColor;
    }

    public void FixAnomaly()
    {
        if (anomalyActive)
        {
            Debug.Log($"{gameObject.name} fixed!");
            SetAnomaly(false);
            // VFX/SFX can be triggered here
        }
    }
}
