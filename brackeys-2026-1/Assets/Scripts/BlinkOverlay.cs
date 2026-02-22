using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BlinkOverlay : MonoBehaviour
{
    private Image overlay;

    void Awake()
    {
        gameObject.SetActive(true);
        overlay = GetComponent<Image>();
        SetAlpha(0f);

        var canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Camera.main;
            canvas.planeDistance = 0.5f;
            canvas.sortingOrder = 999;
        }
        else
        {
            Debug.LogWarning("[BlinkOverlay] No parent Canvas found.");
        }
    }

    public IEnumerator DoBlink(float duration)
    {
        float half = duration * 0.5f;
        yield return Fade(0f, 1f, half);
        yield return Fade(1f, 0f, half);
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(from, to, elapsed / duration));
            yield return null;
        }
        SetAlpha(to);
    }

    private void SetAlpha(float a)
    {
        if (overlay == null) return;
        Color c = overlay.color;
        c.a = a;
        overlay.color = c;
    }
}
