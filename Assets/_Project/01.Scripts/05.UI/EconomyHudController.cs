using TMPro;
using TownLife.Core;
using TownLife.Gameplay;
using UnityEngine;

namespace TownLife.UI
{
    /// <summary>
    /// 화면 좌상단에 골드와 명성을 표시하는 HUD입니다.
    /// </summary>
    public sealed class EconomyHudController : MonoBehaviour
    {
        [SerializeField, Tooltip("현재 골드를 표시할 TMP 텍스트입니다.")]
        private TMP_Text goldText = null;

        [SerializeField, Tooltip("현재 명성을 표시할 TMP 텍스트입니다.")]
        private TMP_Text reputationText = null;

        [SerializeField, Min(0.02f), Tooltip("골드와 명성 표시를 갱신하는 간격입니다.")]
        private float refreshInterval = 0.2f;

        private ShopReputationManager reputationManager = null;
        private float nextRefreshTime;
        private int displayedGold = int.MinValue;
        private int displayedReputation = int.MinValue;

        private void OnEnable()
        {
            Refresh();
        }

        private void Update()
        {
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + refreshInterval;
            Refresh();
        }

        private void Refresh()
        {
            IGold gold = GameContext.Instance != null ? GameContext.Instance.Gold : null;
            int currentGold = gold != null ? gold.Gold : 0;

            if (reputationManager == null)
            {
                reputationManager = FindFirstObjectByType<ShopReputationManager>();
            }

            int currentReputation = reputationManager != null ? reputationManager.Reputation : 0;

            if (displayedGold != currentGold)
            {
                displayedGold = currentGold;
                SetText(goldText, $"골드 {currentGold}");
            }

            if (displayedReputation != currentReputation)
            {
                displayedReputation = currentReputation;
                SetText(reputationText, $"명성 {currentReputation}");
            }
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
