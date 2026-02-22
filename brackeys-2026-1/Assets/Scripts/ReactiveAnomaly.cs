using UnityEngine;
using System.Collections;

public class ReactiveAnomaly : MonoBehaviour
{
    public enum ReactiveType
    {
        TeleportOnce,
        ShakeWhileNearby,
        FlickerWhileNearby
    }

    [Header("Detection")]
    public string playerTag = "Player";

    [Header("Teleport Settings")]
    public Vector3 teleportDestination;

    [Header("Shake Settings")]
    public float shakeIntensity = 0.1f;
    public float shakeSpeed = 20f;

    private float flickerSpeed = 0.1f;
    bool isVisible = true;

    private ReactiveType selectedType;
    private bool hasTeleported = false;
    private bool playerInside = false;

    private Vector3 originalPosition;
    private SpriteRenderer spriteRenderer;
    private Coroutine flickerRoutine;

    void Start()
    {
        originalPosition = transform.position;
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Pick one random behavior
        selectedType = (ReactiveType)Random.Range(0, 3);
    }

    void Update()
    {
        if (selectedType == ReactiveType.ShakeWhileNearby && playerInside)
        {
            Shake();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = true;

        switch (selectedType)
        {
            case ReactiveType.TeleportOnce:
                if (!hasTeleported && teleportDestination != null)
                {
                    transform.position = teleportDestination;
                    hasTeleported = true;
                }
                break;

            case ReactiveType.ShakeWhileNearby:
                originalPosition = transform.position;
                break;

            case ReactiveType.FlickerWhileNearby:
                if (flickerRoutine == null)
                    flickerRoutine = StartCoroutine(Flicker());
                break;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = false;

        if (selectedType == ReactiveType.ShakeWhileNearby)
        {
            transform.position = originalPosition;
        }

        if (selectedType == ReactiveType.FlickerWhileNearby)
        {
            if (flickerRoutine != null)
            {
                StopCoroutine(flickerRoutine);
                flickerRoutine = null;
            }

            if (spriteRenderer != null)
                spriteRenderer.color = new Color(1, 1, 1, 1);
        }
    }

    void Shake()
    {
        float offsetX = Mathf.Sin(Time.time * shakeSpeed) * shakeIntensity;
        float offsetY = Mathf.Cos(Time.time * shakeSpeed) * shakeIntensity;

        transform.position = originalPosition + new Vector3(offsetX, offsetY, 0);
    }

    IEnumerator Flicker()
    {
        while (true)
        {

            if (spriteRenderer != null)
                spriteRenderer.color = isVisible ? new Color(1, 1, 1, 0) : new Color(1, 1, 1, 1); 

            isVisible = !isVisible;
            yield return new WaitForSeconds(flickerSpeed);
        }
    }
}