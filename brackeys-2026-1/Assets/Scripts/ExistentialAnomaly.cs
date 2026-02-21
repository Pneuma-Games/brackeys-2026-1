using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[DisallowMultipleComponent]
public class ExistentialAnomaly : MonoBehaviour
{
    [Header("Tags (must match project tags)")]
    public string entranceTag = "Entrance";
    public string exitTag = "Exit";
    public string playerTag = "Player";

    [Header("Effect Selection")]
    [Range(1, 3)]
    public int effectCount = 1;
    public bool rerandomizeOnActivate = true;
    public ForceEffectChoice forceEffect = ForceEffectChoice.None;

    [Header("Effect Count Rarity")]
    public int weight1Effect = 70;
    public int weight2Effects = 20;
    public int weight3Effects = 7;
    public int weight4Effects = 2;
    public int weight5Effects = 1;

    [Header("Gravity")]
    public float gravityIncreaseTarget = -30f;
    public float gravityDecreaseTarget = -2f;
    public float gravityLerpSpeed      = 0.5f;

    [Header("Player Scale")]
    public float playerShrinkTarget   = 0.4f;
    public float playerGrowTarget     = 2.5f;
    public float playerScaleLerpSpeed = 0.3f;

    [Header("Object Shake")]
    public float shakeAmount = 0.04f;
    public float shakeSpeed  = 20f;

    [Header("Object Avoidance")]
    public float avoidRadius = 4f;
    public float avoidSpeed  = 2f;

    [Header("Anomaly Object Growth")]
    public float anomalyGrowTarget = 2f;
    public float anomalyGrowSpeed  = 0.2f;

    [Header("Time Scale")]
    public float timeAccelTarget = 2.5f;
    public float timeSlowTarget  = 0.3f;
    public float timeLerpSpeed   = 0.3f;

    [Header("Post-Processing")]
    public float ppLerpSpeed = 0.5f;

    [Header("Desaturate")]
    public float desaturateRate = 5f;

    [Header("Music Variants (Placeholder)")]
    public string[] musicVariantKeys = { "MusicVariant_A", "MusicVariant_B", "MusicVariant_C" };

    [Header("Activation Delay")]
    public float activationDelayMin = 1f;
    public float activationDelayMax = 15f;

    public enum ForceEffectChoice
    {
        None = -1,
        GravityIncrease,
        GravityDecrease,
        PlayerShrink,
        PlayerGrow,
        ObjectShake,
        ObjectAvoid,
        AnomalyGrow,
        TimeAccelerate,
        TimeSlow,
        NoFriction,
        MusicVariant,
        ReverseControls,
        PostProcessingIntensify,
        PostProcessingVignette,
        PostProcessingChromatic,
        Desaturate,
        EchoShadow,
        HeartbeatCameraShake,
        SlowBlink,
    }

    private enum EffectType
    {
        GravityIncrease,
        GravityDecrease,
        PlayerShrink,
        PlayerGrow,
        ObjectShake,
        ObjectAvoid,
        AnomalyGrow,
        TimeAccelerate,
        TimeSlow,
        NoFriction,
        MusicVariant,
        ReverseControls,
        PostProcessingIntensify,
        PostProcessingVignette,
        PostProcessingChromatic,
        Desaturate,
        EchoShadow,
        HeartbeatCameraShake,
        SlowBlink,
    }

    private List<EffectType> activeEffects = new List<EffectType>();
    private List<Coroutine> runningCoroutines = new List<Coroutine>();

    private PlayerController playerController;
    private Rigidbody2D playerRb;
    private Transform playerTransform;
    private PhysicsMaterial2D noFrictionMat;
    private PhysicsMaterial2D originalMat;
    private Vector2 defaultGravity;
    private Vector3 playerOriginalScale;
    private List<AnomalousObject> peerObjects = new List<AnomalousObject>();
    private Dictionary<AnomalousObject, Vector3> peerOriginalScales = new Dictionary<AnomalousObject, Vector3>();
    private Dictionary<AnomalousObject, Vector3> peerOriginalPositions = new Dictionary<AnomalousObject, Vector3>();

    private float originalTimeScale = 1f;
    public bool effectsActive {get; private set;} = false;

    private UnityEngine.Rendering.Universal.Vignette vignetteEffect;
    private UnityEngine.Rendering.Universal.ChromaticAberration chromaticEffect;
    private UnityEngine.Rendering.Universal.ColorAdjustments colorAdjustments;
    private float originalVignetteIntensity;
    private float originalChromaticIntensity;
    private float originalSaturation;

    private RoomController roomController;
    private Volume postProcessVolume;
    private BlinkOverlay blinkOverlay;

    private Collider2D entranceTrigger;
    private Collider2D exitTrigger;

    void Awake()
    {
        roomController = FindAnyObjectByType<RoomController>();
        defaultGravity = Physics2D.gravity;
        noFrictionMat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
        ResolveTriggers();
        ResolvePlayer();
    }

    void Start()
    {
        peerObjects.Clear();
        foreach (var ao in FindObjectsByType<AnomalousObject>(FindObjectsSortMode.None))
        {
            peerObjects.Add(ao);
            peerOriginalScales[ao] = ao.transform.localScale;
            peerOriginalPositions[ao] = ao.transform.position;
        }

        postProcessVolume = FindFirstObjectByType<Volume>();
        if (postProcessVolume == null)
            Debug.LogWarning("[ExistentialAnomaly] No Volume found in scene.");

        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            postProcessVolume.profile.TryGet(out vignetteEffect);
            postProcessVolume.profile.TryGet(out chromaticEffect);
            postProcessVolume.profile.TryGet(out colorAdjustments);
            if (vignetteEffect   != null) originalVignetteIntensity  = vignetteEffect.intensity.value;
            if (chromaticEffect  != null) originalChromaticIntensity = chromaticEffect.intensity.value;
            if (colorAdjustments != null) originalSaturation         = colorAdjustments.saturation.value;
        }

        StartCoroutine(WatchTriggers());

        var overlayGo = GameObject.FindWithTag("BlinkOverlay");
        if (overlayGo != null)
            blinkOverlay = overlayGo.GetComponent<BlinkOverlay>();
        if (blinkOverlay == null)
            blinkOverlay = FindFirstObjectByType<BlinkOverlay>(FindObjectsInactive.Include);
        if (blinkOverlay != null)
            blinkOverlay.gameObject.SetActive(true);
        else
            Debug.LogWarning("[ExistentialAnomaly] BlinkOverlay not found in scene.");

        StartCoroutine(DeferredStart());
    }

    private IEnumerator DeferredStart()
    {
        float delay = Random.Range(activationDelayMin, activationDelayMax);
        Debug.Log($"[ExistentialAnomaly] Activation delayed by {delay:F1}s.");
        yield return new WaitForSecondsRealtime(delay);
        ResolvePlayer();
        Activate();
    }

    void OnDestroy()
    {
        RevertAll();
    }

    private void ResolveTriggers()
    {
        if (entranceTrigger == null)
        {
            var go = GameObject.FindWithTag(entranceTag);
            if (go != null) entranceTrigger = go.GetComponent<Collider2D>();
        }

        if (exitTrigger == null)
        {
            var go = GameObject.FindWithTag(exitTag);
            if (go != null) exitTrigger = go.GetComponent<Collider2D>();
        }
    }

    private void ResolvePlayer()
    {
        if (playerTransform != null) return;
        var go = GameObject.FindWithTag(playerTag);
        if (go == null)
        {
            var pc = FindFirstObjectByType<PlayerController>();
            if (pc != null) go = pc.gameObject;
        }
        if (go != null)
            CachePlayer(go);
    }

    private IEnumerator WatchTriggers()
    {
        while (true)
        {
            if (playerTransform == null) ResolvePlayer();
            if (entranceTrigger == null || exitTrigger == null) ResolveTriggers();

            if (playerTransform != null && effectsActive)
            {
                Vector2 samplePoint = playerTransform.position;

                if (entranceTrigger != null && entranceTrigger.OverlapPoint(samplePoint))
                {
                    OnPlayerUsedEntrance();
                    yield return new WaitForSeconds(0.5f);
                }
                else if (exitTrigger != null && exitTrigger.OverlapPoint(samplePoint))
                {
                    OnPlayerUsedExit();
                    yield return new WaitForSeconds(0.5f);
                }
            }

            yield return null;
        }
    }

    private void CachePlayer(GameObject playerGo)
    {
        playerTransform     = playerGo.transform;
        playerController    = playerGo.GetComponent<PlayerController>();
        playerRb            = playerGo.GetComponent<Rigidbody2D>();
        playerOriginalScale = playerTransform.localScale;
        if (playerRb != null)
            originalMat = playerRb.sharedMaterial;
    }

    public void Activate()
    {
        if (effectsActive) return;
        effectsActive = true;

        if (rerandomizeOnActivate || activeEffects.Count == 0)
            PickRandomEffects();

        foreach (var effect in activeEffects)
            RunEffect(effect);

        Debug.Log($"[ExistentialAnomaly] Activated with effects: {string.Join(", ", activeEffects)}");
    }

    public void RevertAll()
    {
        if (!effectsActive) return;
        effectsActive = false;

        foreach (var co in runningCoroutines)
            if (co != null) StopCoroutine(co);
        runningCoroutines.Clear();

        Physics2D.gravity  = defaultGravity;
        Time.timeScale     = originalTimeScale;

        if (playerTransform != null) playerTransform.localScale = playerOriginalScale;
        if (playerRb        != null) playerRb.sharedMaterial    = originalMat;
        if (playerController != null) playerController.SetControlsReversed(false);

        foreach (var ao in peerObjects)
        {
            if (ao == null) continue;
            if (peerOriginalScales.TryGetValue(ao, out var s))    ao.transform.localScale = s;
            if (peerOriginalPositions.TryGetValue(ao, out var p)) ao.transform.position   = p;
        }

        if (vignetteEffect   != null) vignetteEffect.intensity.value    = originalVignetteIntensity;
        if (chromaticEffect  != null) chromaticEffect.intensity.value   = originalChromaticIntensity;
        if (colorAdjustments != null) colorAdjustments.saturation.value = originalSaturation;

        Debug.Log("[ExistentialAnomaly] All effects reverted.");
    }

    private void OnPlayerUsedEntrance()
    {
        Debug.Log("[ExistentialAnomaly] Player exited through entrance.");
        RevertAll();
        roomController?.OnPlayerTryExit();
    }

    private void OnPlayerUsedExit()
    {
        Debug.Log("[ExistentialAnomaly] Player used exit – resetting anomaly.");
        RevertAll();
        StartCoroutine(DelayedActivate(0.1f));
    }

    private IEnumerator DelayedActivate(float delay)
    {
        yield return new WaitForSeconds(delay);
        Activate();
    }

    private void PickRandomEffects()
    {
        activeEffects.Clear();

        if (forceEffect != ForceEffectChoice.None)
        {
            if (System.Enum.TryParse(forceEffect.ToString(), out EffectType forced))
            {
                activeEffects.Add(forced);
                Debug.Log($"[ExistentialAnomaly] ForceEffect override: {forced}");
            }
            return;
        }

        int totalWeight = weight1Effect + weight2Effects + weight3Effects + weight4Effects + weight5Effects;
        int roll = Random.Range(0, totalWeight);
        int count;
        if      (roll < weight1Effect)                                                        count = 1;
        else if (roll < weight1Effect + weight2Effects)                                       count = 2;
        else if (roll < weight1Effect + weight2Effects + weight3Effects)                      count = 3;
        else if (roll < weight1Effect + weight2Effects + weight3Effects + weight4Effects)     count = 4;
        else                                                                                  count = 5;

        var pool = new List<EffectType>();
        foreach (EffectType e in System.Enum.GetValues(typeof(EffectType))) pool.Add(e);

        count = Mathf.Min(count, pool.Count);
        while (activeEffects.Count < count && pool.Count > 0)
        {
            int i = Random.Range(0, pool.Count);
            activeEffects.Add(pool[i]);
            pool.RemoveAt(i);
        }

        Debug.Log($"[ExistentialAnomaly] Rolled {count} effect(s).");
    }

    private void RunEffect(EffectType effect)
    {
        Coroutine co = null;
        switch (effect)
        {
            case EffectType.GravityIncrease:         co = StartCoroutine(LerpGravity(gravityIncreaseTarget));  break;
            case EffectType.GravityDecrease:         co = StartCoroutine(LerpGravity(gravityDecreaseTarget));  break;
            case EffectType.PlayerShrink:            co = StartCoroutine(LerpPlayerScale(playerShrinkTarget)); break;
            case EffectType.PlayerGrow:              co = StartCoroutine(LerpPlayerScale(playerGrowTarget));   break;
            case EffectType.ObjectShake:             co = StartCoroutine(ShakeObjects());                      break;
            case EffectType.ObjectAvoid:             co = StartCoroutine(ObjectsAvoidPlayer());                break;
            case EffectType.AnomalyGrow:             co = StartCoroutine(GrowAnomalyObjects());                break;
            case EffectType.TimeAccelerate:          co = StartCoroutine(LerpTimeScale(timeAccelTarget));      break;
            case EffectType.TimeSlow:                co = StartCoroutine(LerpTimeScale(timeSlowTarget));       break;
            case EffectType.NoFriction:              ApplyNoFriction();                                        break;
            case EffectType.MusicVariant:            TriggerMusicVariant();                                    break;
            case EffectType.ReverseControls:         ApplyReverseControls();                                   break;
            case EffectType.PostProcessingIntensify: co = StartCoroutine(IntensifyPostProcessing());           break;
            case EffectType.PostProcessingVignette:  co = StartCoroutine(IntensifyVignette());                 break;
            case EffectType.PostProcessingChromatic: co = StartCoroutine(IntensifyChromatic());                break;
            case EffectType.Desaturate:              co = StartCoroutine(DesaturateScene());                   break;
            case EffectType.EchoShadow:              co = StartCoroutine(EchoShadowLoop());                    break;
            case EffectType.HeartbeatCameraShake:    co = StartCoroutine(HeartbeatCameraShakeLoop());          break;
            case EffectType.SlowBlink:               co = StartCoroutine(SlowBlinkLoop());                     break;
        }
        if (co != null) runningCoroutines.Add(co);
    }

    private IEnumerator LerpGravity(float targetY)
    {
        Debug.Log($"[ExistentialAnomaly] Effect: Gravity tweening to {targetY}");
        float startY = Physics2D.gravity.y;
        float elapsed = 0f;
        float duration = Mathf.Max(Mathf.Abs(targetY - startY) / gravityLerpSpeed, 0.1f);

        while (elapsed < duration && effectsActive)
        {
            elapsed += Time.deltaTime;
            Physics2D.gravity = new Vector2(0f, Mathf.Lerp(startY, targetY, elapsed / duration));
            yield return null;
        }
        if (effectsActive) Physics2D.gravity = new Vector2(0f, targetY);
    }

    private IEnumerator LerpPlayerScale(float targetScale)
    {
        float waited = 0f;
        while (playerTransform == null && waited < 2f)
        {
            ResolvePlayer();
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (playerTransform == null) { Debug.LogWarning("[ExistentialAnomaly] LerpPlayerScale: player not found."); yield break; }

        if (playerOriginalScale == Vector3.zero) playerOriginalScale = playerTransform.localScale;
        if (playerOriginalScale == Vector3.zero) { Debug.LogWarning("[ExistentialAnomaly] LerpPlayerScale: scale is zero."); yield break; }

        Debug.Log($"[ExistentialAnomaly] Effect: Player scale → {targetScale}x");

        Vector3 startScale = playerTransform.localScale;
        Vector3 endScale   = playerOriginalScale * targetScale;
        float elapsed  = 0f;
        float duration = Mathf.Max(Mathf.Abs(targetScale - 1f) / playerScaleLerpSpeed, 0.5f);

        while (elapsed < duration && effectsActive)
        {
            elapsed += Time.deltaTime;
            playerTransform.localScale = Vector3.Lerp(startScale, endScale, elapsed / duration);
            yield return null;
        }
        if (effectsActive) playerTransform.localScale = endScale;
    }

    private IEnumerator ShakeObjects()
    {
        Debug.Log("[ExistentialAnomaly] Effect: Object shake started.");
        var origins = new Dictionary<AnomalousObject, Vector3>();
        foreach (var ao in peerObjects)
            if (ao != null) origins[ao] = ao.transform.position;

        while (effectsActive)
        {
            foreach (var ao in peerObjects)
            {
                if (ao == null) continue;
                if (!origins.ContainsKey(ao)) origins[ao] = ao.transform.position;
                ao.transform.position = origins[ao] + (Vector3)(Random.insideUnitCircle * shakeAmount);
            }
            yield return new WaitForSeconds(1f / shakeSpeed);
        }

        foreach (var ao in peerObjects)
            if (ao != null && origins.TryGetValue(ao, out var p)) ao.transform.position = p;
    }

    private IEnumerator ObjectsAvoidPlayer()
    {
        Debug.Log("[ExistentialAnomaly] Effect: Objects avoiding player.");
        while (effectsActive)
        {
            if (playerTransform != null)
            {
                foreach (var ao in peerObjects)
                {
                    if (ao == null) continue;
                    Vector3 toPlayer = ao.transform.position - playerTransform.position;
                    float dist = toPlayer.magnitude;
                    if (dist < avoidRadius && dist > 0.01f)
                        ao.transform.position += toPlayer.normalized * (avoidSpeed * Time.deltaTime);
                }
            }
            yield return null;
        }
    }

    private IEnumerator GrowAnomalyObjects()
    {
        Debug.Log($"[ExistentialAnomaly] Effect: Anomaly objects growing to {anomalyGrowTarget}x.");
        while (effectsActive)
        {
            foreach (var ao in peerObjects)
            {
                if (ao == null) continue;
                if (!peerOriginalScales.TryGetValue(ao, out var orig)) continue;
                ao.transform.localScale = Vector3.Lerp(ao.transform.localScale, orig * anomalyGrowTarget, anomalyGrowSpeed * Time.deltaTime);
            }
            yield return null;
        }
    }

    private IEnumerator LerpTimeScale(float target)
    {
        Debug.Log($"[ExistentialAnomaly] Effect: Time scale → {target}x.");
        float start    = Time.timeScale;
        float elapsed  = 0f;
        float duration = Mathf.Max(Mathf.Abs(target - start) / timeLerpSpeed, 0.1f);

        while (elapsed < duration && effectsActive)
        {
            elapsed += Time.unscaledDeltaTime;
            Time.timeScale       = Mathf.Lerp(start, target, elapsed / duration);
            Time.fixedDeltaTime  = 0.02f * Time.timeScale;
            yield return null;
        }
        if (effectsActive)
        {
            Time.timeScale      = target;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        }
    }

    private void ApplyNoFriction()
    {
        if (playerRb == null) return;
        originalMat = playerRb.sharedMaterial;
        playerRb.sharedMaterial = noFrictionMat;
        Debug.Log("[ExistentialAnomaly] Effect: No friction.");
    }

    private void TriggerMusicVariant()
    {
        if (musicVariantKeys.Length == 0) return;
        string key = musicVariantKeys[Random.Range(0, musicVariantKeys.Length)];
        Debug.Log($"[ExistentialAnomaly] Effect: Music variant → {key}");
    }

    private void ApplyReverseControls()
    {
        if (playerController == null) return;
        playerController.SetControlsReversed(true);
        Debug.Log("[ExistentialAnomaly] Effect: Controls reversed.");
    }

    private IEnumerator IntensifyPostProcessing()
    {
        Debug.Log("[ExistentialAnomaly] Effect: Post-processing intensifying.");
        var a = StartCoroutine(IntensifyVignette());
        var b = StartCoroutine(IntensifyChromatic());
        yield return a;
        yield return b;
    }

    private IEnumerator IntensifyVignette()
    {
        if (vignetteEffect == null) { Debug.LogWarning("[ExistentialAnomaly] Vignette override not found on Volume."); yield break; }
        Debug.Log("[ExistentialAnomaly] Effect: Vignette intensifying.");
        float elapsed  = 0f;
        float duration = Mathf.Max(1f / ppLerpSpeed, 0.1f);
        while (elapsed < duration && effectsActive)
        {
            elapsed += Time.deltaTime;
            vignetteEffect.intensity.value = Mathf.Lerp(originalVignetteIntensity, 0.7f, elapsed / duration);
            yield return null;
        }
        if (effectsActive) vignetteEffect.intensity.value = 0.7f;
    }

    private IEnumerator IntensifyChromatic()
    {
        if (chromaticEffect == null) { Debug.LogWarning("[ExistentialAnomaly] ChromaticAberration override not found on Volume."); yield break; }
        Debug.Log("[ExistentialAnomaly] Effect: Chromatic aberration intensifying.");
        float elapsed  = 0f;
        float duration = Mathf.Max(1f / ppLerpSpeed, 0.1f);
        while (elapsed < duration && effectsActive)
        {
            elapsed += Time.deltaTime;
            chromaticEffect.intensity.value = Mathf.Lerp(originalChromaticIntensity, 1f, elapsed / duration);
            yield return null;
        }
        if (effectsActive) chromaticEffect.intensity.value = 1f;
    }

    private IEnumerator DesaturateScene()
    {
        if (colorAdjustments == null) { Debug.LogWarning("[ExistentialAnomaly] ColorAdjustments override not found on Volume."); yield break; }
        Debug.Log("[ExistentialAnomaly] Effect: Desaturating scene.");
        while (effectsActive && colorAdjustments.saturation.value > -100f)
        {
            colorAdjustments.saturation.value = Mathf.MoveTowards(colorAdjustments.saturation.value, -100f, desaturateRate * Time.deltaTime);
            yield return null;
        }
        if (effectsActive) colorAdjustments.saturation.value = -100f;
    }

    private IEnumerator EchoShadowLoop()
    {
        float waited = 0f;
        while (playerTransform == null && waited < 2f)
        {
            ResolvePlayer();
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        if (playerTransform == null) { Debug.LogWarning("[ExistentialAnomaly] EchoShadow: player not found."); yield break; }

        SpriteRenderer playerSr = playerTransform.GetComponentInChildren<SpriteRenderer>();
        if (playerSr == null) { Debug.LogWarning("[ExistentialAnomaly] EchoShadow: no SpriteRenderer on player."); yield break; }

        Debug.Log("[ExistentialAnomaly] Effect: Echo shadow spawned.");

        var ghostGo = new GameObject("EchoShadow");
        var ghostSr = ghostGo.AddComponent<SpriteRenderer>();
        ghostSr.sprite         = playerSr.sprite;
        ghostSr.sortingLayerID = playerSr.sortingLayerID;
        ghostSr.sortingOrder   = playerSr.sortingOrder - 1;
        ghostSr.color          = new Color(0.6f, 0.6f, 1f, 0.35f);
        ghostGo.transform.position   = playerTransform.position;
        ghostGo.transform.localScale = playerSr.transform.localScale;

        float lag = 0.5f;
        var history = new Queue<(Vector3 pos, Vector3 scale, Sprite sprite, bool flipX)>();

        while (effectsActive)
        {
            if (playerSr == null) playerSr = playerTransform.GetComponentInChildren<SpriteRenderer>();

            if (playerSr != null)
                history.Enqueue((playerSr.transform.position, playerSr.transform.lossyScale, playerSr.sprite, playerSr.flipX));

            int maxHistory = Mathf.Max(1, Mathf.RoundToInt(lag / Mathf.Max(Time.deltaTime, 0.001f)));
            while (history.Count > maxHistory) history.Dequeue();

            if (history.Count > 0)
            {
                var oldest = history.Peek();
                ghostGo.transform.position  = oldest.pos;
                ghostGo.transform.localScale = oldest.scale;
                ghostSr.sprite  = oldest.sprite;
                ghostSr.flipX   = oldest.flipX;
            }

            yield return null;
        }

        Destroy(ghostGo);
        Debug.Log("[ExistentialAnomaly] Effect: Echo shadow destroyed.");
    }

    private IEnumerator HeartbeatCameraShakeLoop()
    {
        Debug.Log("[ExistentialAnomaly] Effect: Heartbeat camera shake started.");
        var cam = Camera.main;
        if (cam == null) yield break;

        while (effectsActive)
        {
            for (int beat = 0; beat < 2; beat++)
            {
                Vector3 origin = cam.transform.localPosition;
                float t = 0f;
                while (t < 0.12f && effectsActive)
                {
                    t += Time.deltaTime;
                    float intensity = Mathf.Sin(t / 0.12f * Mathf.PI) * 0.08f;
                    cam.transform.localPosition = origin + (Vector3)Random.insideUnitCircle * intensity;
                    yield return null;
                }
                cam.transform.localPosition = origin;
                yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(Random.Range(1.2f, 2.5f));
        }
    }

    private IEnumerator HUDDisappearLoop()
    {
        var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        while (effectsActive)
        {
            yield return new WaitForSeconds(Random.Range(3f, 6f));
            foreach (var c in canvases) c.enabled = false;
            Debug.Log("[ExistentialAnomaly] Effect: HUD hidden.");
            yield return new WaitForSeconds(Random.Range(1f, 3f));
            foreach (var c in canvases) if (c != null) c.enabled = true;
            Debug.Log("[ExistentialAnomaly] Effect: HUD restored.");
        }
        foreach (var c in canvases) if (c != null) c.enabled = true;
    }

    private IEnumerator SlowBlinkLoop()
    {
        if (blinkOverlay == null) { Debug.LogWarning("[ExistentialAnomaly] SlowBlink: no BlinkOverlay found."); yield break; }
        Debug.Log("[ExistentialAnomaly] Effect: Slow blink started.");
        while (effectsActive)
        {
            yield return new WaitForSecondsRealtime(Random.Range(5f, 10f));
            if (!effectsActive) break;
            yield return blinkOverlay.DoBlink(0.6f);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, 0.6f);
    }
}
