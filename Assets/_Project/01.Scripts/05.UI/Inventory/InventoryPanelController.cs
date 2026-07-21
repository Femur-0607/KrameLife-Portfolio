using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using TownLife.Core;
using TownLife.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace TownLife.UI
{
    /// <summary>
    /// 일반 아이템 인벤토리를 슬롯 목록으로 표시하고, 퀵슬롯 노출 모션을 제어합니다.
    /// </summary>
    public sealed class InventoryPanelController : MonoBehaviour, ISelectedInventoryItemProvider
    {
        [Header("References")]
        [SerializeField, Tooltip("열고 닫을 인벤토리 패널 루트입니다.")]
        private GameObject panelRoot = null;

        [SerializeField, Tooltip("인벤토리 슬롯들이 생성될 부모 Transform입니다.")]
        private Transform slotRoot = null;

        [SerializeField, Tooltip("복제해서 사용할 인벤토리 슬롯 프리팹입니다.")]
        private InventorySlotView slotPrefab = null;

        [SerializeField, Tooltip("아이템 ID로 아이템 표시 데이터를 찾을 데이터베이스입니다.")]
        private GameDatabase gameDatabase = null;

        [Header("Text")]
        [SerializeField, Tooltip("선택한 아이템 이름을 표시할 텍스트입니다.")]
        private TMP_Text detailNameText = null;

        [SerializeField, Tooltip("선택한 아이템 수량을 표시할 텍스트입니다.")]
        private TMP_Text detailCountText = null;

        [SerializeField, Tooltip("선택한 아이템 설명을 표시할 텍스트입니다.")]
        private TMP_Text detailText = null;

        [Header("Options")]
        [SerializeField, Tooltip("Play 시작 시 인벤토리 패널을 접힌 상태로 둡니다.")]
        private bool closeOnAwake = true;

        [SerializeField, Tooltip("켜두면 인벤토리 토글 액션으로 인벤토리 패널을 열고 닫습니다.")]
        [FormerlySerializedAs("toggleWithIKey")]
        private bool toggleWithInventoryAction = true;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField, Tooltip("개발 중 테스트용입니다. 인벤토리 확장 액션을 누르면 일반 인벤토리 슬롯을 확장합니다.")]
        [FormerlySerializedAs("expandInventoryWithNumber2Key")]
        private bool expandInventoryWithDebugAction = true;

        [SerializeField, Min(1), Tooltip("개발 확장 액션을 한 번 누를 때 추가로 여는 일반 인벤토리 슬롯 수입니다.")]
        private int debugExpandSlotCount = 4;
#endif

        [Header("Motion")]
        [SerializeField, Min(0.01f), Tooltip("인벤토리가 아래에서 중앙으로 올라오는 시간입니다.")]
        private float openDuration = 0.35f;

        [SerializeField, Min(0.01f), Tooltip("인벤토리가 중앙에서 아래로 내려가는 시간입니다.")]
        private float closeDuration = 0.25f;

        [SerializeField, Tooltip("인벤토리 패널이 닫혔을 때의 Anchored Pos Y입니다.")]
        private float closedAnchoredY = -700f;

        [Header("Quick Slots")]
        [SerializeField, Tooltip("퀵슬롯 패널 루트입니다. 하위에 배치한 InventorySlotView를 퀵슬롯으로 사용합니다.")]
        private RectTransform quickSlotRoot = null;

        [SerializeField, Tooltip("퀵슬롯으로 표시할 슬롯들입니다. 비워두면 quickSlotRoot 하위에서 자동으로 찾습니다.")]
        private List<InventorySlotView> quickSlotViews = new();

        [Header("Input Actions")]
        [SerializeField, Tooltip("퀵슬롯/인벤토리 입력을 읽을 Input Action Asset입니다.")]
        private InputActionAsset inputActions = null;

        [SerializeField, Tooltip("인벤토리 열기/닫기 액션 이름입니다.")]
        private string toggleInventoryActionName = "Player/ToggleInventory";

        [SerializeField, Tooltip("퀵슬롯 선택 액션 이름 접두사입니다. 뒤에 1~8을 붙여 찾습니다.")]
        private string selectQuickSlotActionPrefix = "Player/SelectQuickSlot";

        [SerializeField, Tooltip("마우스 휠로 퀵슬롯을 순환하는 액션 이름입니다.")]
        private string quickSlotScrollActionName = "Player/QuickSlotScroll";

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        [SerializeField, Tooltip("개발 중 인벤토리 확장 테스트 액션 이름입니다.")]
        private string debugExpandInventoryActionName = "Player/DebugExpandInventory";
#endif

        private const int QuickSlotInputCount = 8;

        private readonly List<InventorySlotView> slotViews = new();
        private readonly List<InventorySlotView> clickBoundSlotViews = new();
        private readonly InputAction[] selectQuickSlotActions = new InputAction[QuickSlotInputCount];
        private int selectedIndex = -1;
        private RectTransform panelRect = null;
        private Vector2 openAnchoredPosition;
        private Tween panelTween;
        private ItemInventoryManager subscribedInventory = null;
        private bool isOpen;
        private InputAction toggleInventoryAction = null;
        private InputAction quickSlotScrollAction = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private InputAction debugExpandInventoryAction = null;
#endif

        /// <summary>
        /// 현재 선택한 인벤토리 슬롯의 아이템 ID입니다.
        /// </summary>
        public string SelectedItemId
        {
            get
            {
                if (selectedIndex < 0 || selectedIndex >= slotViews.Count)
                {
                    return string.Empty;
                }

                ItemStack stack = slotViews[selectedIndex].CurrentStack;
                return stack.IsValid ? stack.ItemId : string.Empty;
            }
        }

        /// <summary>
        /// 패널 RectTransform과 퀵슬롯 참조를 초기화하고 패널을 활성 상태로 준비합니다.
        /// </summary>
        private void Awake()
        {
            panelRect = panelRoot != null ? panelRoot.GetComponent<RectTransform>() : null;
            if (panelRect != null)
            {
                openAnchoredPosition = panelRect.anchoredPosition;
            }

            EnsureQuickSlots();
            EnsureDetailTexts();
            TrySubscribeInventoryChanged();

            SetPanelActive(true);
        }

        /// <summary>
        /// 시작 설정에 따라 인벤토리 패널을 접거나 현재 상태에 맞춰 퀵슬롯을 표시합니다.
        /// </summary>
        private void Start()
        {
            Canvas.ForceUpdateCanvases();
            Refresh();

            if (closeOnAwake)
            {
                SnapClosed();
                return;
            }

            isOpen = panelRoot != null && panelRoot.activeSelf;
            SetQuickSlotsVisible(!isOpen);
        }

        /// <summary>
        /// 오브젝트가 파괴될 때 진행 중인 패널 트윈을 정리합니다.
        /// </summary>
        private void OnDestroy()
        {
            UnsubscribeInventoryChanged();
            panelTween?.Kill();
        }

        private void OnEnable()
        {
            CacheInputActions();
            SetInputActionsEnabled(true);
        }

        private void OnDisable()
        {
            SetInputActionsEnabled(false);
        }

        /// <summary>
        /// Input System 액션을 감지해 인벤토리와 퀵슬롯 입력을 처리합니다.
        /// </summary>
        private void Update()
        {
            if (toggleWithInventoryAction && WasPressedThisFrame(toggleInventoryAction))
            {
                Toggle();
            }

            for (int index = 0; index < selectQuickSlotActions.Length; index++)
            {
                if (WasPressedThisFrame(selectQuickSlotActions[index]))
                {
                    SelectQuickSlot(index);
                    return;
                }
            }

            HandleQuickSlotScroll();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (expandInventoryWithDebugAction && WasPressedThisFrame(debugExpandInventoryAction))
            {
                ExpandInventoryForDebug();
            }
#endif
        }

        /// <summary>
        /// 인벤토리 패널을 열고 현재 아이템 목록을 다시 그립니다.
        /// </summary>
        public void Open()
        {
            isOpen = true;
            SetPanelActive(true);
            SetQuickSlotsVisible(false);
            MovePanel(openAnchoredPosition, openDuration, Ease.OutCubic, null);
        }

        /// <summary>
        /// 인벤토리 패널을 접힌 위치로 닫습니다.
        /// </summary>
        public void Close()
        {
            isOpen = false;
            SetQuickSlotsVisible(false);
            MovePanel(GetCollapsedAnchoredPosition(), closeDuration, Ease.InCubic, () => SetQuickSlotsVisible(true));
        }

        /// <summary>
        /// 인벤토리 패널 열림 상태를 반대로 바꿉니다.
        /// </summary>
        public void Toggle()
        {
            if (panelRoot == null)
            {
                Debug.LogWarning("인벤토리 패널 루트가 연결되지 않았습니다.", this);
                return;
            }

            if (isOpen)
            {
                Close();
                return;
            }

            Open();
        }

        /// <summary>
        /// 현재 일반 인벤토리 내용을 슬롯 UI에 반영합니다.
        /// </summary>
        public void Refresh()
        {
            TrySubscribeInventoryChanged();

            if (!TryGetInventory(out ItemInventoryManager inventoryManager))
            {
                SetDetail(string.Empty, string.Empty, "Inventory missing.");
                HideAllSlots();
                return;
            }

            IReadOnlyList<ItemStack> slots = inventoryManager.Slots;
            int slotCount = slots != null ? slots.Count : 0;
            int unlockedSlotCount = inventoryManager.UnlockedSlotCount;

            EnsureSlotCount(inventoryManager.MaxUniqueItemSlots);

            for (int index = 0; index < slotViews.Count; index++)
            {
                InventorySlotView slotView = slotViews[index];
                if (index >= unlockedSlotCount)
                {
                    slotView.Clear();
                    continue;
                }

                if (index >= slotCount || slots[index].IsEmpty)
                {
                    slotView.Clear(true);
                    continue;
                }

                ItemStack stack = slots[index];
                GameItemSO item = ResolveItem(stack.ItemId);
                slotView.Bind(stack, item);
            }

            int nextSelectedIndex = IsValidSelection(slots, unlockedSlotCount, selectedIndex)
                ? selectedIndex
                : FindFirstOccupiedSlotIndex(slots, unlockedSlotCount);
            SelectSlot(nextSelectedIndex);
            RefreshQuickSlots(slots, unlockedSlotCount);
        }

        /// <summary>
        /// 지정한 순번의 슬롯을 선택하고 상세 설명을 갱신합니다.
        /// </summary>
        public void SelectSlot(int index)
        {
            selectedIndex = index;

            UpdateSlotSelections();

            if (selectedIndex < 0 || selectedIndex >= slotViews.Count)
            {
                SetDetail(string.Empty, string.Empty, "Select an item.");
                return;
            }

            InventorySlotView selectedSlot = slotViews[selectedIndex];
            GameItemSO item = selectedSlot.CurrentItem;
            ItemStack stack = selectedSlot.CurrentStack;

            if (!stack.IsValid)
            {
                SetDetail(string.Empty, string.Empty, "Select an item.");
                return;
            }

            if (item == null)
            {
                SetDetail(stack.ItemId, stack.Count.ToString(), string.Empty);
                return;
            }

            SetDetail(item.DisplayName, stack.Count.ToString(), item.Description);
        }

        private void SelectQuickSlot(int quickSlotIndex)
        {
            if (quickSlotIndex < 0 || quickSlotIndex >= GetSelectableQuickSlotCount())
            {
                return;
            }

            SelectSlot(quickSlotIndex);
        }

        private void HandleQuickSlotScroll()
        {
            if (quickSlotScrollAction == null || !quickSlotScrollAction.triggered)
            {
                return;
            }

            float scrollY = quickSlotScrollAction.ReadValue<Vector2>().y;
            if (Mathf.Approximately(scrollY, 0f))
            {
                return;
            }

            int quickSlotCount = GetSelectableQuickSlotCount();
            if (quickSlotCount <= 0)
            {
                return;
            }

            int currentIndex = selectedIndex >= 0 && selectedIndex < quickSlotCount ? selectedIndex : 0;
            int direction = scrollY < 0f ? 1 : -1;
            int nextIndex = (currentIndex + direction + quickSlotCount) % quickSlotCount;
            SelectSlot(nextIndex);
        }

        private int GetSelectableQuickSlotCount()
        {
            int viewCount = CountAssignedQuickSlotViews();
            if (viewCount <= 0)
            {
                viewCount = QuickSlotInputCount;
            }

            int quickSlotCount = Mathf.Min(QuickSlotInputCount, viewCount);

            if (!TryGetInventory(out ItemInventoryManager inventoryManager))
            {
                return quickSlotCount;
            }

            IReadOnlyList<ItemStack> slots = inventoryManager.Slots;
            int inventorySlotCount = slots != null ? slots.Count : 0;
            return Mathf.Min(quickSlotCount, inventoryManager.UnlockedSlotCount, inventorySlotCount);
        }

        private int CountAssignedQuickSlotViews()
        {
            int count = 0;
            for (int index = 0; index < quickSlotViews.Count; index++)
            {
                if (quickSlotViews[index] != null)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// GameContext에서 일반 아이템 인벤토리 매니저를 가져옵니다.
        /// </summary>
        private bool TryGetInventory(out ItemInventoryManager inventoryManager)
        {
            inventoryManager = null;

            if (GameContext.Instance == null || GameContext.Instance.ItemInventory == null)
            {
                return false;
            }

            inventoryManager = GameContext.Instance.ItemInventory as ItemInventoryManager;
            return inventoryManager != null;
        }

        private void CacheInputActions()
        {
            if (inputActions == null)
            {
                return;
            }

            toggleInventoryAction ??= inputActions.FindAction(toggleInventoryActionName, false);
            quickSlotScrollAction ??= inputActions.FindAction(quickSlotScrollActionName, false);

            for (int index = 0; index < selectQuickSlotActions.Length; index++)
            {
                selectQuickSlotActions[index] ??= inputActions.FindAction($"{selectQuickSlotActionPrefix}{index + 1}", false);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            debugExpandInventoryAction ??= inputActions.FindAction(debugExpandInventoryActionName, false);
#endif
        }

        private void SetInputActionsEnabled(bool isEnabled)
        {
            SetInputActionEnabled(toggleInventoryAction, isEnabled);
            SetInputActionEnabled(quickSlotScrollAction, isEnabled);

            for (int index = 0; index < selectQuickSlotActions.Length; index++)
            {
                SetInputActionEnabled(selectQuickSlotActions[index], isEnabled);
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            SetInputActionEnabled(debugExpandInventoryAction, isEnabled);
#endif
        }

        private static void SetInputActionEnabled(InputAction action, bool isEnabled)
        {
            if (action == null)
            {
                return;
            }

            if (isEnabled)
            {
                action.Enable();
                return;
            }

            action.Disable();
        }

        private static bool WasPressedThisFrame(InputAction action)
        {
            return action != null && action.WasPressedThisFrame();
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        /// <summary>
        /// 개발 중 인벤토리 확장 UI를 확인하기 위해 일반 인벤토리 슬롯을 임시로 엽니다.
        /// </summary>
        private void ExpandInventoryForDebug()
        {
            if (!TryGetInventory(out ItemInventoryManager inventoryManager))
            {
                Debug.LogWarning("인벤토리 확장 테스트 실패: ItemInventoryManager가 없습니다.", this);
                return;
            }

            if (inventoryManager.UnlockSlots(debugExpandSlotCount))
            {
                Debug.Log($"인벤토리 확장 테스트: +{debugExpandSlotCount}칸, 현재 {inventoryManager.UnlockedSlotCount}/{inventoryManager.MaxUniqueItemSlots}", this);
            }
            else
            {
                Debug.Log($"인벤토리 확장 테스트 실패: 현재 {inventoryManager.UnlockedSlotCount}/{inventoryManager.MaxUniqueItemSlots}", this);
            }
        }
#endif

        /// <summary>
        /// 일반 인벤토리 변경 이벤트를 구독합니다.
        /// </summary>
        private void TrySubscribeInventoryChanged()
        {
            if (!TryGetInventory(out ItemInventoryManager inventoryManager) || subscribedInventory == inventoryManager)
            {
                return;
            }

            UnsubscribeInventoryChanged();
            subscribedInventory = inventoryManager;
            subscribedInventory.InventoryChanged += OnInventoryChanged;
        }

        /// <summary>
        /// 일반 인벤토리 변경 이벤트 구독을 해제합니다.
        /// </summary>
        private void UnsubscribeInventoryChanged()
        {
            if (subscribedInventory != null)
            {
                subscribedInventory.InventoryChanged -= OnInventoryChanged;
                subscribedInventory = null;
            }
        }

        /// <summary>
        /// 인벤토리 내용이 바뀌면 슬롯과 퀵슬롯 UI를 갱신합니다.
        /// </summary>
        private void OnInventoryChanged()
        {
            Refresh();
        }

        /// <summary>
        /// 아이템 ID에 해당하는 아이템 표시 데이터를 데이터베이스에서 찾습니다.
        /// </summary>
        private GameItemSO ResolveItem(string itemId)
        {
            return gameDatabase != null ? gameDatabase.Get<GameItemSO>(itemId) : null;
        }

        /// <summary>
        /// 표시할 아이템 수만큼 인벤토리 슬롯 UI를 생성합니다.
        /// </summary>
        private void EnsureSlotCount(int count)
        {
            if (slotRoot == null)
            {
                return;
            }

            if (slotViews.Count == 0)
            {
                slotRoot.GetComponentsInChildren(true, slotViews);
            }

            while (slotPrefab != null && slotViews.Count < count)
            {
                InventorySlotView slotView = Instantiate(slotPrefab, slotRoot);
                slotViews.Add(slotView);
            }

            for (int index = 0; index < slotViews.Count; index++)
            {
                BindSlotButton(slotViews[index], index);
            }
        }

        /// <summary>
        /// 인벤토리 슬롯과 퀵슬롯의 선택 표시를 현재 선택 순번에 맞춰 함께 갱신합니다.
        /// </summary>
        private void UpdateSlotSelections()
        {
            for (int slotIndex = 0; slotIndex < slotViews.Count; slotIndex++)
            {
                slotViews[slotIndex].SetSelected(slotIndex == selectedIndex);
            }

            for (int slotIndex = 0; slotIndex < quickSlotViews.Count; slotIndex++)
            {
                quickSlotViews[slotIndex].SetSelected(slotIndex == selectedIndex);
            }
        }

        /// <summary>
        /// 모든 인벤토리 슬롯과 퀵슬롯 표시를 비웁니다.
        /// </summary>
        private void HideAllSlots()
        {
            for (int index = 0; index < slotViews.Count; index++)
            {
                slotViews[index].Clear();
            }

            RefreshQuickSlots(null, 0);
        }

        /// <summary>
        /// 인벤토리 패널 루트의 활성 상태를 설정합니다.
        /// </summary>
        private void SetPanelActive(bool isActive)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(isActive);
            }
        }

        /// <summary>
        /// 인벤토리 패널을 지정한 위치로 트윈 이동시키고 완료 콜백을 실행합니다.
        /// </summary>
        private void MovePanel(Vector2 targetPosition, float duration, Ease ease, TweenCallback onComplete)
        {
            if (panelRect == null)
            {
                SetPanelActive(isOpen);
                onComplete?.Invoke();
                return;
            }

            panelTween?.Kill();
            panelTween = panelRect
                .DOAnchorPos(targetPosition, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .OnComplete(onComplete);
        }

        /// <summary>
        /// 애니메이션 없이 인벤토리 패널을 접힌 위치로 즉시 배치합니다.
        /// </summary>
        private void SnapClosed()
        {
            isOpen = false;
            SetPanelActive(true);

            if (panelRect != null)
            {
                panelRect.anchoredPosition = GetCollapsedAnchoredPosition();
            }

            SetQuickSlotsVisible(true);
        }

        /// <summary>
        /// 지정한 닫힘 Y 위치로 패널을 배치합니다.
        /// </summary>
        private Vector2 GetCollapsedAnchoredPosition()
        {
            return new Vector2(openAnchoredPosition.x, closedAnchoredY);
        }

        /// <summary>
        /// 퀵슬롯 목록이 비어 있으면 퀵슬롯 루트 하위에서 슬롯 뷰를 찾습니다.
        /// </summary>
        private void EnsureQuickSlots()
        {
            if (quickSlotRoot == null)
            {
                return;
            }

            quickSlotRoot.GetComponentsInChildren(true, quickSlotViews);
            for (int index = 0; index < quickSlotViews.Count; index++)
            {
                BindSlotButton(quickSlotViews[index], index);
            }
        }

        /// <summary>
        /// 현재 인벤토리 앞쪽 아이템들을 퀵슬롯 UI에 반영합니다.
        /// </summary>
        private void RefreshQuickSlots(IReadOnlyList<ItemStack> slots, int unlockedSlotCount)
        {
            EnsureQuickSlots();

            for (int index = 0; index < quickSlotViews.Count; index++)
            {
                if (slots != null && index < unlockedSlotCount && index < slots.Count && slots[index].IsValid)
                {
                    ItemStack stack = slots[index];
                    GameItemSO item = ResolveItem(stack.ItemId);
                    quickSlotViews[index].Bind(stack, item);
                    continue;
                }

                quickSlotViews[index].Clear(true);
            }

            UpdateSlotSelections();
        }

        /// <summary>
        /// 선택 초기값으로 사용할 첫 번째 보유 아이템 슬롯 순번을 찾습니다.
        /// </summary>
        private static int FindFirstOccupiedSlotIndex(IReadOnlyList<ItemStack> slots, int unlockedSlotCount)
        {
            if (slots == null)
            {
                return -1;
            }

            int count = Mathf.Min(unlockedSlotCount, slots.Count);
            for (int index = 0; index < count; index++)
            {
                if (slots[index].IsValid)
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// 현재 선택 순번이 해금된 슬롯 안의 실제 아이템을 가리키는지 확인합니다.
        /// </summary>
        private static bool IsValidSelection(IReadOnlyList<ItemStack> slots, int unlockedSlotCount, int selectedSlotIndex)
        {
            return slots != null
                && selectedSlotIndex >= 0
                && selectedSlotIndex < unlockedSlotCount
                && selectedSlotIndex < slots.Count
                && slots[selectedSlotIndex].IsValid;
        }

        /// <summary>
        /// 배치된 인벤토리/퀵슬롯 Button을 공통 슬롯 선택 동작에 연결합니다.
        /// </summary>
        private void BindSlotButton(InventorySlotView slotView, int slotIndex)
        {
            if (slotView == null || clickBoundSlotViews.Contains(slotView))
            {
                return;
            }

            Button button = slotView.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(() => SelectSlot(slotIndex));
            }

            slotView.ConfigureDragDrop(slotIndex, HandleSlotDropped, HandleSlotRightClicked);
            clickBoundSlotViews.Add(slotView);
        }

        public bool TryGetItemAtSlot(int slotIndex, out ItemStack stack, out GameItemSO item)
        {
            stack = ItemStack.Empty;
            item = null;

            if (!TryGetInventory(out ItemInventoryManager inventoryManager))
            {
                return false;
            }

            IReadOnlyList<ItemStack> slots = inventoryManager.Slots;
            if (slots == null
                || slotIndex < 0
                || slotIndex >= inventoryManager.UnlockedSlotCount
                || slotIndex >= slots.Count
                || !slots[slotIndex].IsValid)
            {
                return false;
            }

            stack = slots[slotIndex];
            item = ResolveItem(stack.ItemId);
            return true;
        }

        /// <summary>
        /// 슬롯 드랍이 들어오면 인벤토리 데이터에서 두 슬롯 위치를 교환하고 UI를 다시 그립니다.
        /// </summary>
        private void HandleSlotDropped(int fromIndex, int toIndex)
        {
            if (!TryGetInventory(out ItemInventoryManager inventoryManager))
            {
                return;
            }

            if (inventoryManager.MoveSlot(fromIndex, toIndex))
            {
                selectedIndex = toIndex;
                Refresh();
            }
        }

        private void HandleSlotRightClicked(int slotIndex)
        {
            ShopDisplayStandPanelController displayStandPanel = FindAnyObjectByType<ShopDisplayStandPanelController>();
            if (displayStandPanel != null)
            {
                displayStandPanel.PreviewInventorySlot(slotIndex);
            }
        }

        /// <summary>
        /// 퀵슬롯 루트의 표시 상태를 설정합니다.
        /// </summary>
        private void SetQuickSlotsVisible(bool isVisible)
        {
            if (quickSlotRoot != null)
            {
                quickSlotRoot.gameObject.SetActive(isVisible);
            }
        }

        /// <summary>
        /// 상세 패널의 이름, 수량, 설명 텍스트를 갱신합니다.
        /// </summary>
        private void SetDetail(string itemName, string itemCount, string description)
        {
            SetText(detailNameText, itemName);
            SetText(detailCountText, itemCount);
            SetText(detailText, description);
        }

        /// <summary>
        /// 상세 텍스트 참조가 비어 있으면 패널 하위 오브젝트 이름으로 자동 연결합니다.
        /// </summary>
        private void EnsureDetailTexts()
        {
            if (panelRoot == null)
            {
                return;
            }

            TMP_Text[] texts = panelRoot.GetComponentsInChildren<TMP_Text>(true);
            detailNameText ??= FindText(texts, "ItemNameText");
            detailCountText ??= FindText(texts, "ItemCountText");
            detailText ??= FindText(texts, "DetailText");
        }

        /// <summary>
        /// 지정한 이름의 TMP 텍스트를 찾습니다.
        /// </summary>
        private static TMP_Text FindText(IReadOnlyList<TMP_Text> texts, string textName)
        {
            for (int index = 0; index < texts.Count; index++)
            {
                TMP_Text text = texts[index];
                if (text != null && text.name == textName)
                {
                    return text;
                }
            }

            return null;
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
