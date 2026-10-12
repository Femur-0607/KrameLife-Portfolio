using TMPro;
using TownLife.Core;
using UnityEngine;
using UnityEngine.UI;

namespace TownLife.UI
{
    /// <summary>
    /// 획득 로그 한 줄의 아이콘과 문구를 표시합니다.
    /// </summary>
    public sealed class AcquisitionLogEntryView : MonoBehaviour
    {
        [SerializeField, Tooltip("아이템 이름과 수량을 표시할 TMP 텍스트입니다.")]
        private TMP_Text entryText = null;

        [SerializeField, Tooltip("획득한 아이템의 아이콘을 표시할 Image입니다.")]
        private Image itemIcon = null;

        private int iconLoadVersion;

        /// <summary>
        /// 획득 문구와 아이템 아이콘을 설정합니다.
        /// </summary>
        public void SetEntry(string message, GameItemSO item)
        {
            if (entryText != null)
            {
                entryText.text = message;
            }

            SetItemIcon(item);
        }

        /// <summary>
        /// 아이콘을 기본값으로 먼저 표시한 뒤 Addressables 아이콘으로 교체합니다.
        /// </summary>
        private void SetItemIcon(GameItemSO item)
        {
            iconLoadVersion++;
            int loadVersion = iconLoadVersion;

            Sprite fallbackIcon = ItemIconLoader.GetFallbackIcon(item);
            ApplyIcon(fallbackIcon);

            if (item == null)
            {
                return;
            }

            ItemIconLoader.TryLoadAddressableIcon(item, loadedIcon =>
            {
                if (this == null || loadVersion != iconLoadVersion)
                {
                    return;
                }

                ApplyIcon(loadedIcon);
            }, out _);
        }

        /// <summary>
        /// 아이콘 Image의 스프라이트와 표시 여부를 함께 갱신합니다.
        /// </summary>
        private void ApplyIcon(Sprite icon)
        {
            if (itemIcon == null)
            {
                return;
            }

            itemIcon.sprite = icon;
            itemIcon.enabled = icon != null;
        }
    }
}
