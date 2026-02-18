using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteraction : MonoBehaviour
{
    public float interactRange = 1.5f;
    public LayerMask interactableLayer;

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

        if (hit != null)
        {
            if (hit.TryGetComponent<AnomalousObject>(out var anomaly))
            {
                anomaly.FixAnomaly();
            }

            if (hit.CompareTag("Exit"))
            {
                FindAnyObjectByType<RoomController>().OnPlayerTryExit();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}