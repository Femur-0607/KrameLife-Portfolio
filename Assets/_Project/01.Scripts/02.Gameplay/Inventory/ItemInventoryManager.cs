using System;
using System.Collections.Generic;
using TownLife.Core;
using UnityEngine;

namespace TownLife.Gameplay
{
    /// <summary>
    /// 일반 아이템을 itemId와 수량으로 보관하고 SaveData.inventory에 저장/복원하는 인벤토리 관리자입니다.
    /// </summary>
    public sealed class ItemInventoryManager : MonoBehaviour, IInventory, ISaveParticipant
    {
        [SerializeField, Tooltip("저장 데이터를 불러오지 않았을 때 사용할 시작 아이템 목록입니다.")]
        private List<ItemStack> startingItems = new();

        [SerializeField, Min(1), Tooltip("새 게임에서 열려 있는 일반 인벤토리 슬롯 수입니다.")]
        private int startingUnlockedSlotCount = 8;

        [SerializeField, Min(1), Tooltip("확장으로 열 수 있는 일반 인벤토리 최대 슬롯 수입니다.")]
        private int maxUniqueItemSlots = 40;

        private readonly List<ItemStack> slots = new();
        private readonly List<ItemStack> items = new();
        private int unlockedSlotCount;

        /// <summary>
        /// 현재 보관 중인 일반 아이템 목록입니다. 외부에서는 읽기만 할 수 있습니다.
        /// </summary>
        public IReadOnlyList<ItemStack> Items => items;

        /// <summary>
        /// 빈칸을 포함한 실제 인벤토리 슬롯 목록입니다.
        /// </summary>
        public IReadOnlyList<ItemStack> Slots => slots;

        /// <summary>
        /// 현재 사용할 수 있는 일반 인벤토리 슬롯 수입니다.
        /// </summary>
        public int UnlockedSlotCount => unlockedSlotCount;

        /// <summary>
        /// 확장으로 열 수 있는 일반 인벤토리 최대 슬롯 수입니다.
        /// </summary>
        public int MaxUniqueItemSlots => maxUniqueItemSlots;

        /// <summary>
        /// 아이템 추가, 제거, 복원으로 인벤토리 내용이 바뀔 때 발생합니다.
        /// </summary>
        public event Action InventoryChanged;

        private void Awake()
        {
            Restore(new InventorySave { items = startingItems });
        }

        /// <summary>
        /// Inspector에서 슬롯 수 설정이 잘못 들어오면 안전한 범위로 보정합니다.
        /// </summary>
        private void OnValidate()
        {
            maxUniqueItemSlots = Mathf.Max(1, maxUniqueItemSlots);
            startingUnlockedSlotCount = Mathf.Clamp(startingUnlockedSlotCount, 1, maxUniqueItemSlots);
        }

        /// <summary>
        /// 일반 인벤토리에 아이템을 추가합니다. 잘못된 ID나 0 이하 수량은 실패합니다.
        /// </summary>
        public bool Add(string itemId, int count)
        {
            if (!CanUseStackInput(itemId, count))
            {
                return false;
            }

            int existingIndex = FindSlotIndex(itemId);
            if (existingIndex < 0 && FindFirstEmptyUnlockedSlotIndex() < 0)
            {
                Debug.LogWarning($"Inventory add failed because slots are full: {itemId}", this);
                return false;
            }

            int currentCount = existingIndex >= 0 ? slots[existingIndex].Count : 0;
            if (int.MaxValue - currentCount < count)
            {
                Debug.LogWarning($"Inventory add overflow ignored: {itemId}", this);
                return false;
            }

            if (existingIndex >= 0)
            {
                slots[existingIndex] = new ItemStack(itemId, currentCount + count);
            }
            else
            {
                slots[FindFirstEmptyUnlockedSlotIndex()] = new ItemStack(itemId, count);
            }

            RebuildItems();
            InventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 일반 인벤토리에서 아이템을 제거합니다. 수량이 부족하면 상태를 바꾸지 않고 실패합니다.
        /// </summary>
        public bool Remove(string itemId, int count)
        {
            if (!CanUseStackInput(itemId, count))
            {
                return false;
            }

            if (Count(itemId) < count)
            {
                return false;
            }

            int remainingCount = count;
            for (int index = 0; index < unlockedSlotCount && index < slots.Count && remainingCount > 0; index++)
            {
                ItemStack stack = slots[index];
                if (!stack.IsValid || stack.ItemId != itemId)
                {
                    continue;
                }

                int removeCount = Mathf.Min(stack.Count, remainingCount);
                int nextCount = stack.Count - removeCount;
                slots[index] = nextCount > 0 ? new ItemStack(itemId, nextCount) : ItemStack.Empty;
                remainingCount -= removeCount;
            }

            RebuildItems();
            InventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 지정한 itemId의 현재 보유 수량을 반환합니다.
        /// </summary>
        public int Count(string itemId)
        {
            if (string.IsNullOrWhiteSpace(itemId))
            {
                return 0;
            }

            int totalCount = 0;
            for (int index = 0; index < slots.Count; index++)
            {
                ItemStack stack = slots[index];
                if (stack.IsValid && stack.ItemId == itemId)
                {
                    totalCount += stack.Count;
                }
            }

            return totalCount;
        }

        /// <summary>
        /// 출발 슬롯의 아이템을 도착 슬롯과 맞바꾸거나 빈칸으로 이동합니다.
        /// </summary>
        public bool MoveSlot(int fromIndex, int toIndex)
        {
            if (!CanUseSlotIndex(fromIndex) || !CanUseSlotIndex(toIndex) || fromIndex == toIndex)
            {
                return false;
            }

            if (fromIndex >= unlockedSlotCount || toIndex >= unlockedSlotCount)
            {
                return false;
            }

            ItemStack source = slots[fromIndex];
            if (source.IsEmpty)
            {
                return false;
            }

            slots[fromIndex] = slots[toIndex];
            slots[toIndex] = source;
            RebuildItems();
            InventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 일반 인벤토리 슬롯을 추가로 엽니다.
        /// </summary>
        public bool UnlockSlots(int additionalSlotCount)
        {
            if (additionalSlotCount <= 0 || unlockedSlotCount >= maxUniqueItemSlots)
            {
                return false;
            }

            int nextSlotCount = Mathf.Clamp(unlockedSlotCount + additionalSlotCount, 1, maxUniqueItemSlots);
            if (nextSlotCount == unlockedSlotCount)
            {
                return false;
            }

            unlockedSlotCount = nextSlotCount;
            InventoryChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 현재 일반 인벤토리 상태를 InventorySave로 복사합니다.
        /// </summary>
        public InventorySave Capture()
        {
            // 외부에서 현재 목록을 직접 바꾸지 못하도록 새 리스트로 복사해서 넘긴다.
            return new InventorySave
            {
                items = CreateSaveStacks(slots),
                unlockedSlotCount = unlockedSlotCount
            };
        }

        /// <summary>
        /// InventorySave에 담긴 일반 아이템 목록으로 인벤토리를 복원합니다.
        /// </summary>
        public void Restore(InventorySave save)
        {
            EnsureSlotCapacity();
            ClearSlots();
            int restoredSlotCount = save != null && save.unlockedSlotCount > 0
                ? save.unlockedSlotCount
                : startingUnlockedSlotCount;

            if (save?.items == null)
            {
                unlockedSlotCount = Mathf.Clamp(restoredSlotCount, 1, maxUniqueItemSlots);
                RebuildItems();
                InventoryChanged?.Invoke();
                return;
            }

            int highestOccupiedSlotCount = 0;
            int slotCount = Mathf.Min(save.items.Count, maxUniqueItemSlots);
            for (int index = 0; index < slotCount; index++)
            {
                ItemStack stack = save.items[index];
                if (!stack.IsValid)
                {
                    continue;
                }

                slots[index] = stack.ToSaveStack();
                highestOccupiedSlotCount = index + 1;
            }

            unlockedSlotCount = Mathf.Clamp(Mathf.Max(restoredSlotCount, highestOccupiedSlotCount), 1, maxUniqueItemSlots);
            RebuildItems();
            InventoryChanged?.Invoke();
        }

        /// <summary>
        /// SaveData의 일반 인벤토리 섹션에 현재 아이템 목록을 저장합니다.
        /// </summary>
        public void Capture(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            data.inventory = Capture();
        }

        /// <summary>
        /// SaveData의 일반 인벤토리 섹션에서 아이템 목록을 복원합니다.
        /// </summary>
        public void Restore(SaveData data)
        {
            if (data?.inventory == null)
            {
                Restore(new InventorySave { items = startingItems });
                return;
            }

            Restore(data.inventory);
        }

        /// <summary>
        /// 아이템 추가/제거 요청에 사용할 수 있는 itemId와 수량인지 확인합니다.
        /// </summary>
        private static bool CanUseStackInput(string itemId, int count)
        {
            return !string.IsNullOrWhiteSpace(itemId) && count > 0;
        }

        /// <summary>
        /// 저장 파일에 남길 수 있도록 슬롯 목록을 itemId 기반 묶음 목록으로 복사합니다.
        /// </summary>
        private static List<ItemStack> CreateSaveStacks(IReadOnlyList<ItemStack> source)
        {
            List<ItemStack> saveStacks = new(source.Count);

            for (int index = 0; index < source.Count; index++)
            {
                saveStacks.Add(source[index].ToSaveStack());
            }

            return saveStacks;
        }

        /// <summary>
        /// 슬롯 배열을 기준으로 빈칸 없는 보유 아이템 목록을 다시 만듭니다.
        /// </summary>
        private void RebuildItems()
        {
            items.Clear();
            EnsureSlotCapacity();

            // 빈칸은 제외하고 UI 상세/기존 기능에서 쓰는 보유 아이템 목록만 다시 만든다.
            for (int index = 0; index < slots.Count; index++)
            {
                ItemStack stack = slots[index];
                if (stack.IsValid)
                {
                    items.Add(stack);
                }
            }
        }

        /// <summary>
        /// 내부 슬롯 배열 크기를 현재 최대 슬롯 수와 맞춥니다.
        /// </summary>
        private void EnsureSlotCapacity()
        {
            while (slots.Count < maxUniqueItemSlots)
            {
                slots.Add(ItemStack.Empty);
            }

            if (slots.Count > maxUniqueItemSlots)
            {
                slots.RemoveRange(maxUniqueItemSlots, slots.Count - maxUniqueItemSlots);
            }
        }

        /// <summary>
        /// 모든 내부 슬롯을 빈칸으로 초기화합니다.
        /// </summary>
        private void ClearSlots()
        {
            for (int index = 0; index < slots.Count; index++)
            {
                slots[index] = ItemStack.Empty;
            }
        }

        /// <summary>
        /// 해금된 슬롯 중 지정한 itemId가 들어 있는 첫 번째 슬롯 순번을 찾습니다.
        /// </summary>
        private int FindSlotIndex(string itemId)
        {
            for (int index = 0; index < unlockedSlotCount && index < slots.Count; index++)
            {
                ItemStack stack = slots[index];
                if (stack.IsValid && stack.ItemId == itemId)
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// 해금된 슬롯 중 아이템을 새로 넣을 수 있는 첫 번째 빈칸 순번을 찾습니다.
        /// </summary>
        private int FindFirstEmptyUnlockedSlotIndex()
        {
            for (int index = 0; index < unlockedSlotCount && index < slots.Count; index++)
            {
                if (slots[index].IsEmpty)
                {
                    return index;
                }
            }

            return -1;
        }

        /// <summary>
        /// 지정한 순번이 내부 슬롯 배열 범위 안에 있는지 확인합니다.
        /// </summary>
        private bool CanUseSlotIndex(int index)
        {
            return index >= 0 && index < slots.Count;
        }

    }
}
