using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public float interactRange = 1.5f;
    public LayerMask interactableLayer;

    [Header("Anti-Spam Settings")]
    public int maxStrikes = 3;
    public float strikeCooldown = 0.75f; // Time before another strike can be counted

    private int currentStrikes = 0;
    private float lastStrikeTime = -Mathf.Infinity;

    public void OnInteract(InputValue value)
    {
        if (value.isPressed)
        {
            PerformInteract();
        }
    }

    private void PerformInteract()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactRange, interactableLayer);

        bool validInteraction = false;

        if (hit != null)
        {
            var anomaly = hit.GetComponentInParent<AnomalousObject>();
            if (anomaly != null && anomaly.anomalyActive)
            {
                anomaly.FixAnomaly();
                validInteraction = true;
            }

            if (hit.CompareTag("Exit"))
            {
                FindAnyObjectByType<RoomController>().OnPlayerTryExit();
                validInteraction = true;
            }

            if (hit.CompareTag("Entrance"))
            {
                FindAnyObjectByType<RoomController>().OnPlayerAttemptEntranceExit();
                validInteraction = true;
            }
        }

        if (!validInteraction)
        {
            TryRegisterStrike();
        }
    }

    private void TryRegisterStrike()
    {
        if (Time.time - lastStrikeTime < strikeCooldown)
            return;

        lastStrikeTime = Time.time;

        currentStrikes++;
        Debug.Log($"Strike {currentStrikes}/{maxStrikes}");

        if (currentStrikes >= maxStrikes)
        {
            Debug.Log("Too many invalid interactions! Failing...");
            currentStrikes = 0;
            FindAnyObjectByType<RoomController>().FailOnThreeStrikes();
        }
    }

    public void ResetStrikes()
    {
        currentStrikes = 0;
        lastStrikeTime = -Mathf.Infinity;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}