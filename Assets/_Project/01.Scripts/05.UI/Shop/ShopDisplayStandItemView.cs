using System;
using TMPro;
using TownLife.Core;
using TownLife.Gameplay;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TownLife.UI
{
    /// <summary>
    /// 가판대에 진열된 아이템 아이콘과 가격 말풍선을 월드 오브젝트로 표시합니다.
    /// </summary>
    public sealed class ShopDisplayStandItemView : MonoBehaviour
    {
        [SerializeField, Tooltip("표시할 진열 상태를 가진 가판대입니다. 비워두면 부모에서 찾습니다.")]
        private ShopDisplayStand displayStand = null;

        [SerializeField, Tooltip("가판대 슬롯 순서대로 연결할 아이콘/가격 말풍선 표시 목록입니다.")]
        private DisplaySlotViewBinding[] slotViews =
        {
            new(),
            new()
        };

        [SerializeField, Tooltip("가격 텍스트 형식입니다. {0}에 가격이 들어갑니다.")]
        private string priceFormat = "{0}";

        private void Awake()
        {
            CacheReferences();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureSlotViews();
        }
#endif

        private void OnEnable()
        {
            CacheReferences();

            if (displayStand != null)
            {
                displayStand.DisplaySlotsChanged += Refresh;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (displayStand != null)
            {
                displayStand.DisplaySlotsChanged -= Refresh;
            }

            ReleaseIconHandles();
        }

        /// <summary>
        /// 현재 진열 상태를 아이콘과 가격 말풍선에 반영합니다.
        /// </summary>
        public void Refresh()
        {
            CacheReferences();

            for (int index = 0; index < slotViews.Length; index++)
            {
                RefreshSlotView(index, slotViews[index]);
            }
        }

        /// <summary>
        /// 비어 있는 표시 참조를 부모 오브젝트에서 찾습니다.
        /// </summary>
        private void CacheReferences()
        {
            displayStand ??= GetComponentInParent<ShopDisplayStand>();
            EnsureSlotViews();
        }

        /// <summary>
        /// 고정 2칸 표시 배열을 준비하고 빈 요소를 생성합니다.
        /// </summary>
        private void EnsureSlotViews()
        {
            if (slotViews == null || slotViews.Length != 2)
            {
                Array.Resize(ref slotViews, 2);
            }

            for (int index = 0; index < slotViews.Length; index++)
            {
                slotViews[index] ??= new DisplaySlotViewBinding();
            }
        }

        /// <summary>
        /// 지정 슬롯의 표시 바인딩을 현재 가판대 상태로 갱신합니다.
        /// </summary>
        private void RefreshSlotView(int slotIndex, DisplaySlotViewBinding slotView)
        {
            if (slotView == null)
            {
                return;
            }

            if (displayStand == null || !displayStand.TryGetDisplayedSlot(slotIndex, out ShopDisplayStand.DisplaySlot slot))
            {
                slotView.Clear();
                return;
            }

            GameItemSO item = displayStand.ResolveItem(slot.ItemId);
            slotView.SetItem(item, slot.Price, priceFormat);
        }

        /// <summary>
        /// 표시 바인딩들이 들고 있는 Addressables 아이콘 핸들을 해제합니다.
        /// </summary>
        private void ReleaseIconHandles()
        {
            if (slotViews == null)
            {
                return;
            }

            for (int index = 0; index < slotViews.Length; index++)
            {
                slotViews[index]?.ReleaseIconHandle();
            }
        }

        [Serializable]
        private sealed class DisplaySlotViewBinding
        {
            [SerializeField, Tooltip("아이템 아이콘을 표시할 스프라이트 렌더러입니다.")]
            private SpriteRenderer iconRenderer = null;

            [SerializeField, Tooltip("가격 말풍선 루트입니다. 아이템이 없으면 숨깁니다.")]
            private GameObject priceBubbleRoot = null;

            [SerializeField, Tooltip("판매 가격을 표시할 텍스트입니다.")]
            private TMP_Text priceText = null;

            private AsyncOperationHandle<Sprite> iconHandle;
            private bool hasIconHandle;
            private int iconLoadVersion;

            /// <summary>
            /// 지정 아이템과 가격을 표시합니다.
            /// </summary>
            public void SetItem(GameItemSO item, int price, string priceFormat)
            {
                iconLoadVersion++;
                ReleaseIconHandle();
                SetVisible(true);
                SetPrice(price, priceFormat);
                SetIcon(ItemIconLoader.GetFallbackIcon(item));

                int currentVersion = iconLoadVersion;
                hasIconHandle = ItemIconLoader.TryLoadAddressableIcon(item, loadedIcon =>
                {
                    if (currentVersion != iconLoadVersion)
                    {
                        return;
                    }

                    SetIcon(loadedIcon);
                }, out iconHandle);
            }

            /// <summary>
            /// 표시를 비우고 아이콘 로드 핸들을 정리합니다.
            /// </summary>
            public void Clear()
            {
                iconLoadVersion++;
                ReleaseIconHandle();
                SetIcon(null);
                SetPrice(0, string.Empty);
                SetVisible(false);
            }

            /// <summary>
            /// 현재 보유 중인 Addressables 아이콘 핸들을 해제합니다.
            /// </summary>
            public void ReleaseIconHandle()
            {
                ItemIconLoader.ReleaseIconHandle(ref iconHandle, ref hasIconHandle);
            }

            /// <summary>
            /// 아이템 아이콘과 가격 말풍선 표시 상태를 바꿉니다.
            /// </summary>
            private void SetVisible(bool isVisible)
            {
                if (iconRenderer != null)
                {
                    iconRenderer.enabled = isVisible;
                }

                if (priceBubbleRoot != null)
                {
                    priceBubbleRoot.SetActive(isVisible);
                }
            }

            /// <summary>
            /// 아이템 아이콘 스프라이트를 반영합니다.
            /// </summary>
            private void SetIcon(Sprite icon)
            {
                if (iconRenderer != null)
                {
                    iconRenderer.sprite = icon;
                    iconRenderer.enabled = icon != null;
                }
            }

            /// <summary>
            /// 진열 가격 텍스트를 갱신합니다.
            /// </summary>
            private void SetPrice(int price, string priceFormat)
            {
                if (priceText != null)
                {
                    priceText.text = price > 0 ? string.Format(priceFormat, price) : string.Empty;
                }
            }
        }
    }
}
