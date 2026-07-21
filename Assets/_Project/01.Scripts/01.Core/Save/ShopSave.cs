using System;
using System.Collections.Generic;

namespace TownLife.Core
{
    /// <summary>
    /// 상점 관련 저장 데이터입니다. v1에서는 가판대 모듈, 현재 진열 슬롯, 가구 배치, 창고 목록을 보관합니다.
    /// </summary>
    [Serializable]
    public class ShopSave
    {
        public int lastBusinessOpenedDay = -1;
        public List<ModuleState> modules = new();
        public List<DisplaySlotSave> displayed = new();
        public List<DisplayStandState> displayStands = new();
        public List<FurnitureState> furnitures = new();
        public List<StorageState> storages = new();
    }

    [Serializable]
    public class DisplayStandState
    {
        public string standId = string.Empty;
        public List<DisplaySlotSave> displayed = new();

        public DisplayStandState()
        {
        }

        public DisplayStandState(string standId, List<DisplaySlotSave> displayed)
        {
            this.standId = standId ?? string.Empty;
            this.displayed = displayed ?? new List<DisplaySlotSave>();
        }

        public bool HasStandId => !string.IsNullOrWhiteSpace(standId);
    }

    /// <summary>
    /// 저장 파일에 기록되는 진열 슬롯 1칸의 아이템 ID, 수량, 판매가입니다.
    /// </summary>
    [Serializable]
    public class DisplaySlotSave
    {
        public string itemId = string.Empty;
        public int count;
        public int price;

        /// <summary>
        /// Unity 직렬화와 JSON 역직렬화를 위한 빈 생성자입니다.
        /// </summary>
        public DisplaySlotSave()
        {
        }

        /// <summary>
        /// 저장용 진열 슬롯 데이터를 만들고 음수 수량/가격을 0으로 보정합니다.
        /// </summary>
        public DisplaySlotSave(string itemId, int count, int price)
        {
            this.itemId = itemId ?? string.Empty;
            this.count = Math.Max(0, count);
            this.price = Math.Max(0, price);
        }

        /// <summary>
        /// 저장 슬롯이 실제 아이템을 담고 있는지 확인합니다.
        /// </summary>
        public bool HasItem => !string.IsNullOrWhiteSpace(itemId) && count > 0;
    }

    /// <summary>
    /// 저장 파일에 기록되는 고정 가구 슬롯 1칸의 배치 상태입니다.
    /// </summary>
    [Serializable]
    public class FurnitureState
    {
        public string slotId = string.Empty;
        public string furnitureId = string.Empty;

        /// <summary>
        /// Unity 직렬화와 JSON 역직렬화를 위한 빈 생성자입니다.
        /// </summary>
        public FurnitureState()
        {
        }

        /// <summary>
        /// 저장용 가구 배치 데이터를 만듭니다.
        /// </summary>
        public FurnitureState(string slotId, string furnitureId)
        {
            this.slotId = slotId ?? string.Empty;
            this.furnitureId = furnitureId ?? string.Empty;
        }

        /// <summary>
        /// 저장 슬롯이 실제 가구 배치 정보를 담고 있는지 확인합니다.
        /// </summary>
        public bool HasFurniture => !string.IsNullOrWhiteSpace(slotId)
            && !string.IsNullOrWhiteSpace(furnitureId);
    }

    /// <summary>
    /// 저장 파일에 기록되는 개별 창고 1개의 ID와 보관 아이템 목록입니다.
    /// </summary>
    [Serializable]
    public class StorageState
    {
        public string storageId = string.Empty;
        public InventorySave inventory = new();

        /// <summary>
        /// Unity 직렬화와 JSON 역직렬화를 위한 빈 생성자입니다.
        /// </summary>
        public StorageState()
        {
        }

        /// <summary>
        /// 저장용 창고 데이터를 만듭니다.
        /// </summary>
        public StorageState(string storageId, InventorySave inventory)
        {
            this.storageId = storageId ?? string.Empty;
            this.inventory = inventory ?? new InventorySave();
        }

        /// <summary>
        /// 저장 슬롯이 실제 창고 ID를 담고 있는지 확인합니다.
        /// </summary>
        public bool HasStorage => !string.IsNullOrWhiteSpace(storageId);
    }
}
