using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FarmingSystem
{
    /// <summary>
    /// Displays the player's current coin balance on the HUD.
    /// Updates immediately on currency changes with an animated visual pulse.
    /// </summary>
    public class CoinHUD : MonoBehaviour
    {
        public static CoinHUD Instance { get; private set; }

        [Header("UI Elements")]
        [SerializeField] private Text coinText;
        [SerializeField] private Image coinIcon;
        [SerializeField] private Transform pulseTarget;

        private Coroutine pulseCoroutine;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (pulseTarget == null) pulseTarget = transform;
        }

        private void Start()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged += UpdateCoinDisplay;
                UpdateCoinDisplay(CurrencyManager.Instance.Coins);
            }
            else
            {
                if (coinText != null) coinText.text = "0 G";
            }
        }

        private void OnDestroy()
        {
            if (CurrencyManager.Instance != null)
            {
                CurrencyManager.Instance.OnCoinsChanged -= UpdateCoinDisplay;
            }
        }

        public void UpdateCoinDisplay(long currentCoins)
        {
            if (coinText != null)
            {
                coinText.text = $"{currentCoins:N0} G";
            }

            if (gameObject.activeInHierarchy && pulseTarget != null)
            {
                if (pulseCoroutine != null) StopCoroutine(pulseCoroutine);
                pulseCoroutine = StartCoroutine(PulseRoutine());
            }
        }

        private IEnumerator PulseRoutine()
        {
            Vector3 orig = Vector3.one;
            Vector3 peak = Vector3.one * 1.15f;
            float dur = 0.12f;
            float elapsed = 0f;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                pulseTarget.localScale = Vector3.Lerp(orig, peak, elapsed / dur);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                pulseTarget.localScale = Vector3.Lerp(peak, orig, elapsed / dur);
                yield return null;
            }

            pulseTarget.localScale = orig;
            pulseCoroutine = null;
        }
    }
}
