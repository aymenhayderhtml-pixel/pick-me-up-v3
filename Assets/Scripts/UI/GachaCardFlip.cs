using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TalismanState
{
    Sealed,
    Shattering,
    Awakened
}

/// <summary>
/// Controls the Xianxia-style "Heavenly Breakthrough" talisman seal-shatter and card flip.
/// Works out-of-the-box by dynamically creating a Talisman parchment seal over any card prefab.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class GachaCardFlip : MonoBehaviour
{
    public TalismanState CurrentState { get; private set; } = TalismanState.Sealed;

    // Event hooks for SummonUI to bind sound/particles
    public static Action<Transform, int> OnSealShattered;
    public static Action<int> OnAuraHum;

    private int _rarity = 3;
    private GameObject _sealOverlay;
    private Image _glowAura;
    private bool _isInitialized = false;

    /// <summary>
    /// Programmatically builds the Xianxia sealed talisman visual layout over the existing card prefab.
    /// This guarantees immediate compatibility without requiring asset or hierarchy modifications in Unity.
    /// </summary>
    public void Initialize(int rarity)
    {
        if (_isInitialized) return;
        _rarity = rarity;

        // 1. Temporarily obscure all default visual components (Portrait, texts, borders) on the card Front
        ToggleFrontElements(false);

        // 2. Dynamically build a gorgeous "Sealed Talisman" parchment overlay
        CreateTalismanSealOverlay();

        _isInitialized = true;
        CurrentState = TalismanState.Sealed;

        // Setup click listener on the card button itself
        var btn = GetComponent<Button>();
        if (btn == null) btn = gameObject.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(OnCardClicked);
    }

    /// <summary>
    /// Executes the "Ink-Drop" staggered entry bounce animation.
    /// </summary>
    public IEnumerator PlayInkDropEntry(float initialDelay)
    {
        transform.localScale = Vector3.zero;
        yield return new WaitForSeconds(initialDelay);

        float elapsed = 0f;
        float duration = 0.35f;

        // Phase 1: Aggressive overshoot expansion
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float ratio = elapsed / duration;
            // Elegant overshoot ease-out curve
            float scale = Mathf.Lerp(0f, 1.25f, Mathf.Sin(ratio * Mathf.PI * 0.5f));
            transform.localScale = Vector3.one * scale;
            yield return null;
        }

        // Phase 2: Settle bounce back to normal scale (1.0)
        elapsed = 0f;
        duration = 0.2f;
        Vector3 peakScale = transform.localScale;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float ratio = elapsed / duration;
            transform.localScale = Vector3.Lerp(peakScale, Vector3.one, ratio);
            yield return null;
        }
        transform.localScale = Vector3.one;

        // Trigger a gentle idle aura hum sound based on rarity
        OnAuraHum?.Invoke(_rarity);
    }

    private void OnCardClicked()
    {
        if (CurrentState != TalismanState.Sealed) return;
        BreakSeal();
    }

    public void BreakSeal(Action onComplete = null)
    {
        if (CurrentState != TalismanState.Sealed) return;
        CurrentState = TalismanState.Shattering;
        StartCoroutine(ShatterSequence(onComplete));
    }

    private IEnumerator ShatterSequence(Action onComplete)
    {
        // ── 5★ SUPREME IMMORTAL HEAVENLY TRIBULATION ──
        if (_rarity == 5)
        {
            // Move this card to render on top of all other cards in the grid
            transform.SetAsLastSibling();

            float rumbleElapsed = 0f;
            float rumbleDuration = 0.35f;
            Vector3 originalPos = transform.localPosition;
            Vector3 targetScale = Vector3.one * 0.9f;

            // Phase 1: The Rumble (gathering supreme energy, shaking, and shrinking)
            while (rumbleElapsed < rumbleDuration)
            {
                rumbleElapsed += Time.deltaTime;
                float ratio = rumbleElapsed / rumbleDuration;
                
                // Shake on Z-axis and shift slightly in local space
                float shakeZ = UnityEngine.Random.Range(-5f, 5f);
                float offsetX = UnityEngine.Random.Range(-3f, 3f);
                float offsetY = UnityEngine.Random.Range(-3f, 3f);
                
                transform.localRotation = Quaternion.Euler(0f, 0f, shakeZ);
                transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);
                transform.localScale = Vector3.Lerp(Vector3.one, targetScale, ratio);

                yield return null;
            }

            transform.localPosition = originalPos;
            transform.localRotation = Quaternion.identity;

            // Phase 2: The Breakthrough (massive scale-up and dramatic slow Y-spin)
            float breakthroughElapsed = 0f;
            float breakthroughDuration = 0.55f;
            Vector3 breakthroughTargetScale = Vector3.one * 1.35f;

            while (breakthroughElapsed < breakthroughDuration)
            {
                breakthroughElapsed += Time.deltaTime;
                float ratio = breakthroughElapsed / breakthroughDuration;
                
                transform.localScale = Vector3.Lerp(targetScale, breakthroughTargetScale, ratio);
                // Slow rotation up to 90 degrees mid-point
                transform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, 90f, ratio), 0f);

                yield return null;
            }

            // mid-point swap
            SwapVisualsFront();

            // Phase 3: The Revelation (spin from 90 to 180 degrees)
            breakthroughElapsed = 0f;
            breakthroughDuration = 0.25f;
            while (breakthroughElapsed < breakthroughDuration)
            {
                breakthroughElapsed += Time.deltaTime;
                float ratio = breakthroughElapsed / breakthroughDuration;
                transform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(90f, 180f, ratio), 0f);
                yield return null;
            }

            transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Trigger physical screenshake, flash shockwave, and epic sound
            OnSealShattered?.Invoke(transform, _rarity);

            // Settle back to standard size gracefully
            float settleElapsed = 0f;
            float settleDuration = 0.3f;
            while (settleElapsed < settleDuration)
            {
                settleElapsed += Time.deltaTime;
                float ratio = settleElapsed / settleDuration;
                transform.localScale = Vector3.Lerp(breakthroughTargetScale, Vector3.one, ratio);
                yield return null;
            }
            transform.localScale = Vector3.one;
        }
        else
        {
            // ── Standard 3★/4★ Seal Shatter Twist (Fast and snappy)
            float elapsed = 0f;
            float duration = 0.25f;

            // Rotate 0 to 90
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float ratio = elapsed / duration;
                transform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(0f, 90f, ratio), 0f);
                yield return null;
            }

            // mid-point swap
            SwapVisualsFront();

            // Rotate 90 to 180
            elapsed = 0f;
            duration = 0.15f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float ratio = elapsed / duration;
                transform.localRotation = Quaternion.Euler(0f, Mathf.Lerp(90f, 180f, ratio), 0f);
                yield return null;
            }

            transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

            // Trigger audio hook
            OnSealShattered?.Invoke(transform, _rarity);
        }

        // Spring Bounce Impact
        float springElapsed = 0f;
        float springDuration = 0.15f;
        while (springElapsed < springDuration)
        {
            springElapsed += Time.deltaTime;
            float bounceScale = Mathf.Lerp(1.0f, 1.12f, Mathf.Sin((springElapsed / springDuration) * Mathf.PI));
            transform.localScale = Vector3.one * bounceScale;
            yield return null;
        }
        transform.localScale = Vector3.one;

        CurrentState = TalismanState.Awakened;
        onComplete?.Invoke();
    }

    private void SwapVisualsFront()
    {
        if (_sealOverlay != null) _sealOverlay.SetActive(false);
        ToggleFrontElements(true);
        ActivateRarityAuraGlow();
    }

    private void ToggleFrontElements(bool show)
    {
        foreach (Transform child in transform)
        {
            if (child.gameObject == _sealOverlay) continue;
            
            // Backwards compatibility: keep everything else representing the front of the card
            child.gameObject.SetActive(show);

            // Keep text fields facing normal direction even if card is rotated 180 on Y
            if (show)
            {
                var txts = child.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in txts)
                {
                    t.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                }
                var imgs = child.GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    // Avoid flipping portraits backwards
                    if (img.name == "img_Portrait" || img.name == "Portrait")
                    {
                        img.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                    }
                }
            }
        }
    }

    private void CreateTalismanSealOverlay()
    {
        // Spawns a gorgeous cultivation talisman parchment visual block
        _sealOverlay = new GameObject("TalismanSeal", typeof(RectTransform));
        _sealOverlay.transform.SetParent(transform, false);

        var rect = _sealOverlay.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // 1. Parchment Background
        var bgImg = _sealOverlay.AddComponent<Image>();
        // Pure Xianxia: Warm, ancient ink-wash parchment paper color
        bgImg.color = new Color(0.92f, 0.84f, 0.68f, 1f); 

        // 2. Runic Borders
        var borderObj = new GameObject("RunicBorder", typeof(RectTransform), typeof(Image));
        borderObj.transform.SetParent(_sealOverlay.transform, false);
        var borderRect = borderObj.GetComponent<RectTransform>();
        borderRect.anchorMin = new Vector2(0.08f, 0.08f);
        borderRect.anchorMax = new Vector2(0.92f, 0.92f);
        borderRect.sizeDelta = Vector2.zero;
        
        var borderImg = borderObj.GetComponent<Image>();
        borderImg.color = new Color(0.55f, 0.12f, 0.08f, 0.45f); // Deep blood cinnabar borders

        // 3. Central Seal Symbol (Ancient character "封" - Seal)
        var textObj = new GameObject("SealText", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(_sealOverlay.transform, false);
        var textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        var tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.text = "封"; // Ancient Seal Character
        tmp.fontStyle = FontStyles.Bold;
        tmp.color = new Color(0.6f, 0.05f, 0.05f, 0.85f); // Rich cinnabar red
        tmp.fontSize = 54f;

        // 4. Subtle background dynamic dust energy
        var energyObj = new GameObject("QiMist", typeof(RectTransform), typeof(Image));
        energyObj.transform.SetParent(_sealOverlay.transform, false);
        var energyRect = energyObj.GetComponent<RectTransform>();
        energyRect.anchorMin = new Vector2(0.2f, 0.4f);
        energyRect.anchorMax = new Vector2(0.8f, 0.6f);
        energyRect.sizeDelta = Vector2.zero;
        
        var energyImg = energyObj.GetComponent<Image>();
        // Pure Xianxia colors depending on locked strength
        energyImg.color = _rarity switch
        {
            5 => new Color(0.95f, 0.84f, 0f, 0.15f), // Shimmering gold essence
            4 => new Color(0.6f, 0.1f, 0.8f, 0.12f),  // Violet essence
            _ => new Color(0.1f, 0.6f, 0.9f, 0.10f)   // Pure crisp Qi
        };
    }

    private void ActivateRarityAuraGlow()
    {
        // Dynamically creates a gorgeous Xianxia elemental halo under the card
        var glowObj = new GameObject("img_RarityGlow", typeof(RectTransform), typeof(Image));
        glowObj.transform.SetParent(transform, false);
        glowObj.transform.SetAsFirstSibling(); // Render behind portrait

        var rect = glowObj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(-0.15f, -0.15f);
        rect.anchorMax = new Vector2(1.15f, 1.15f);
        rect.sizeDelta = Vector2.zero;

        var img = glowObj.GetComponent<Image>();
        img.color = _rarity switch
        {
            // 5★ Mythic: Blazing Imperial Gold with Crimson undertones
            5 => new Color(1.0f, 0.84f, 0.0f, 0.35f),
            // 4★ Rare: Deep Astral Jade / Eerie Violet
            4 => new Color(0.22f, 0.85f, 0.45f, 0.28f), 
            // 3★ Mortal: Crisp Pale Arcane Blue / Silver mist
            _ => new Color(0.45f, 0.72f, 1.0f, 0.20f)
        };
    }
}
