using System;
using DG.Tweening;
using TMPro;
using TownLife.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

namespace TownLife.UI
{
    /// <summary>
    /// 인벤토리 아이템 아이콘과 수량을 표시하는 슬롯 뷰입니다.
    /// </summary>
    public sealed class InventorySlotView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler, IPointerClickHandler
    {
        [SerializeField, Tooltip("아이템 아이콘을 표시할 이미지입니다.")]
        private Image iconImage = null;

        [SerializeField, Tooltip("아이템 수량을 표시할 텍스트입니다. 1개일 때는 비웁니다.")]
        private TMP_Text countText = null;

        [SerializeField, Tooltip("선택한 슬롯임을 표시할 오브젝트입니다.")]
        private GameObject selectedMarker = null;

        [SerializeField, Min(1f), Tooltip("드래그 중 마우스를 따라다니는 아이템 고스트 배율입니다.")]
        private float dragGhostScale = 1.15f;

        [SerializeField, Min(0f), Tooltip("드래그 종료 시 고스트가 사라지는 시간입니다.")]
        private float dragGhostReleaseDuration = 0.08f;

        private ItemStack currentStack;
        private GameItemSO currentItem;
        private AsyncOperationHandle<Sprite> iconHandle;
        private CanvasGroup canvasGroup;
        private bool hasIconHandle;
        private int iconLoadVersion;
        private int slotIndex = -1;
        private Action<int, int> dropHandler;
        private Action<int> rightClickHandler;
        private RectTransform dragGhostRect;
        private Tween dragGhostTween;
        private bool isDragging;

        /// <summary>
        /// 슬롯 프리팹에 남아 있는 이름/카테고리 텍스트를 숨깁니다.
        /// </summary>
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);
            for (int index = 0; index < texts.Length; index++)
            {
                TMP_Text text = texts[index];
                if (text != null && text != countText)
                {
                    text.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 이 슬롯이 현재 표시하는 아이템 묶음입니다.
        /// </summary>
        public ItemStack CurrentStack => currentStack;

        /// <summary>
        /// 이 슬롯이 현재 표시하는 아이템 데이터입니다.
        /// </summary>
        public GameItemSO CurrentItem => currentItem;

        /// <summary>
        /// 슬롯 목록 안에서 이 뷰가 담당하는 순번입니다.
        /// </summary>
        public int SlotIndex => slotIndex;

        /// <summary>
        /// 드래그 앤 드랍에 사용할 슬롯 순번과 드랍 콜백을 연결합니다.
        /// </summary>
        public void ConfigureDragDrop(int index, Action<int, int> onDropped, Action<int> onRightClicked = null)
        {
            slotIndex = index;
            dropHandler = onDropped;
            rightClickHandler = onRightClicked;
        }

        /// <summary>
        /// 슬롯에 아이템 정보를 표시합니다.
        /// </summary>
        public void Bind(ItemStack stack, GameItemSO item)
        {
            currentStack = stack;
            currentItem = item;
            iconLoadVersion++;

            // 이전 슬롯에 걸려 있던 Addressables 로드 핸들을 먼저 정리합니다.
            ReleaseIconHandle();

            // Addressables 로드 전까지는 기존 직접 참조 아이콘을 fallback으로 보여줍니다.
            Sprite icon = ItemIconLoader.GetFallbackIcon(item);
            SetText(countText, stack.Count > 1 ? $"x{stack.Count}" : string.Empty);
            SetIcon(icon);

            LoadAddressableIcon(item, iconLoadVersion);

            SetSelected(false);
            gameObject.SetActive(true);
        }

        /// <summary>
        /// 슬롯을 비우고 숨깁니다.
        /// </summary>
        public void Clear()
        {
            Clear(false);
        }

        /// <summary>
        /// 슬롯 내용을 비우고 필요하면 빈 슬롯 배경은 계속 표시합니다.
        /// </summary>
        public void Clear(bool keepVisible)
        {
            iconLoadVersion++;
            currentStack = ItemStack.Empty;
            currentItem = null;
            SetText(countText, string.Empty);
            ReleaseIconHandle();
            SetIcon(null);

            SetSelected(false);
            gameObject.SetActive(keepVisible);
        }

        /// <summary>
        /// 선택 표시 상태를 바꿉니다.
        /// </summary>
        public void SetSelected(bool isSelected)
        {
            if (selectedMarker != null)
            {
                selectedMarker.SetActive(isSelected);
            }
        }

        /// <summary>
        /// 슬롯이 파괴될 때 Addressables 아이콘 로드 핸들을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            // UI 슬롯이 사라진 뒤에도 Addressables 참조가 남지 않도록 해제합니다.
            DestroyDragGhost();
            EndDragCursorState();
            ReleaseIconHandle();
        }

        private void OnDisable()
        {
            DestroyDragGhost();
            EndDragCursorState();
        }

        /// <summary>
        /// 아이템이 있는 슬롯을 드래그할 때 다른 슬롯이 드랍 이벤트를 받을 수 있게 Raycast를 잠시 비웁니다.
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (currentStack.IsEmpty || canvasGroup == null)
            {
                return;
            }

            CreateDragGhost(eventData);
            isDragging = true;
            MouseCursorFollower.SetDragCursorActive(true);
            canvasGroup.blocksRaycasts = false;
        }

        /// <summary>
        /// 슬롯 이동은 드랍 시점에만 처리하므로 드래그 중에는 별도 시각 효과를 만들지 않습니다.
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            MoveDragGhost(eventData);
        }

        /// <summary>
        /// 드래그가 끝나면 슬롯이 다시 클릭과 드랍 판정을 받을 수 있게 복구합니다.
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
            }

            DestroyDragGhost(true);
            EndDragCursorState();
        }

        /// <summary>
        /// 다른 인벤토리 슬롯을 이 슬롯 위에 놓으면 컨트롤러에 출발/도착 순번을 전달합니다.
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            InventorySlotView sourceSlot = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<InventorySlotView>()
                : null;

            if (sourceSlot == null || sourceSlot == this)
            {
                return;
            }

            dropHandler?.Invoke(sourceSlot.SlotIndex, slotIndex);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Right)
            {
                return;
            }

            rightClickHandler?.Invoke(slotIndex);
        }

        /// <summary>
        /// Addressables 주소로 아이템 아이콘 Sprite를 비동기 로드합니다.
        /// </summary>
        private void LoadAddressableIcon(GameItemSO item, int loadVersion)
        {
            hasIconHandle = ItemIconLoader.TryLoadAddressableIcon(item, loadedIcon =>
            {
                // 늦게 도착한 이전 요청이면 현재 슬롯 UI를 덮어쓰지 않습니다.
                if (loadVersion != iconLoadVersion)
                {
                    return;
                }

                // 공용 로더가 성공 아이콘 또는 fallback 아이콘을 넘겨주면 슬롯 Image에 반영합니다.
                SetIcon(loadedIcon);
            }, out iconHandle);
        }

        /// <summary>
        /// 현재 슬롯이 들고 있는 Addressables 아이콘 핸들을 해제합니다.
        /// </summary>
        private void ReleaseIconHandle()
        {
            if (!hasIconHandle)
            {
                return;
            }

            // 이전 아이콘 참조 카운트를 내려 다음 로드나 슬롯 삭제 때 메모리 누수를 줄입니다.
            ItemIconLoader.ReleaseIconHandle(ref iconHandle, ref hasIconHandle);
        }

        /// <summary>
        /// 슬롯 아이콘 Image의 Sprite와 표시 상태를 바꿉니다.
        /// </summary>
        private void SetIcon(Sprite icon)
        {
            if (iconImage == null)
            {
                return;
            }

            // Sprite가 없으면 빈 슬롯처럼 아이콘 이미지를 숨깁니다.
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        /// <summary>
        /// 드래그 중 마우스를 따라다닐 아이콘 복제 이미지를 만듭니다.
        /// </summary>
        private void CreateDragGhost(PointerEventData eventData)
        {
            DestroyDragGhost();

            if (iconImage == null || iconImage.sprite == null)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return;
            }

            GameObject ghostObject = new("Inventory Drag Ghost", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            ghostObject.transform.SetParent(canvas.transform, false);
            dragGhostRect = ghostObject.GetComponent<RectTransform>();
            dragGhostRect.sizeDelta = iconImage.rectTransform.rect.size;
            dragGhostRect.localScale = Vector3.one * dragGhostScale;
            dragGhostRect.SetAsLastSibling();

            CanvasGroup ghostGroup = ghostObject.GetComponent<CanvasGroup>();
            ghostGroup.blocksRaycasts = false;
            ghostGroup.interactable = false;
            ghostGroup.alpha = 0.85f;

            Image dragGhostImage = ghostObject.GetComponent<Image>();
            dragGhostImage.sprite = iconImage.sprite;
            dragGhostImage.preserveAspect = true;
            dragGhostImage.raycastTarget = false;

            MoveDragGhost(eventData);
        }

        /// <summary>
        /// 드래그 복제 아이콘을 현재 포인터 위치로 이동합니다.
        /// </summary>
        private void MoveDragGhost(PointerEventData eventData)
        {
            if (dragGhostRect == null || eventData == null)
            {
                return;
            }

            Canvas canvas = dragGhostRect.GetComponentInParent<Canvas>();
            RectTransform canvasRect = canvas != null ? canvas.transform as RectTransform : null;
            if (canvas == null || canvasRect == null)
            {
                dragGhostRect.position = eventData.position;
                return;
            }

            Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera != null
                    ? canvas.worldCamera
                    : eventData.pressEventCamera;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    eventData.position,
                    eventCamera,
                    out Vector2 localPoint))
            {
                dragGhostRect.anchoredPosition = localPoint;
            }
        }

        /// <summary>
        /// 드래그가 끝난 뒤 복제 아이콘을 정리합니다.
        /// </summary>
        private void DestroyDragGhost(bool playReleaseAnimation = false)
        {
            if (dragGhostRect == null)
            {
                return;
            }

            dragGhostTween?.Kill();

            GameObject ghostObject = dragGhostRect.gameObject;
            if (playReleaseAnimation && dragGhostReleaseDuration > 0f)
            {
                RectTransform releasingGhost = dragGhostRect;
                CanvasGroup releasingGroup = ghostObject.GetComponent<CanvasGroup>();
                dragGhostRect = null;

                Sequence releaseSequence = DOTween.Sequence();
                releaseSequence.Join(releasingGhost.DOScale(Vector3.one, dragGhostReleaseDuration));
                if (releasingGroup != null)
                {
                    releaseSequence.Join(releasingGroup.DOFade(0f, dragGhostReleaseDuration));
                }

                bool isDestroyed = false;
                void DestroyGhostObject()
                {
                    if (isDestroyed)
                    {
                        return;
                    }

                    isDestroyed = true;
                    Destroy(ghostObject);
                }

                dragGhostTween = releaseSequence
                    .SetEase(Ease.OutQuad)
                    .SetUpdate(true)
                    .OnComplete(DestroyGhostObject)
                    .OnKill(DestroyGhostObject);
                return;
            }

            dragGhostRect = null;
            Destroy(ghostObject);
        }

        /// <summary>
        /// 커서 드래그 표시 상태를 중복 해제 없이 종료합니다.
        /// </summary>
        private void EndDragCursorState()
        {
            if (!isDragging)
            {
                return;
            }

            isDragging = false;
            MouseCursorFollower.SetDragCursorActive(false);
        }

        /// <summary>
        /// TMP 텍스트 참조가 있을 때만 표시 문자열을 갱신합니다.
        /// </summary>
        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }
    }
}
