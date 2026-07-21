using System;
using Newtonsoft.Json;
using UnityEngine;

namespace TownLife.Core
{
    /// <summary>
    /// 인벤토리, 레시피, 드롭, 세이브에서 공통으로 쓰는 아이템 수량 단위입니다.
    /// </summary>
    [Serializable]
    public struct ItemStack
    {
        /// <summary>
        /// null 대신 빈 아이템 묶음을 표현할 때 사용하는 값입니다.
        /// </summary>
        public static readonly ItemStack Empty = new(string.Empty, 0);

        [SerializeField, Tooltip("Inspector에서 편하게 지정할 아이템 데이터입니다. 저장 시에는 Id만 사용합니다.")]
        private GameItemSO item;

        // 저장 파일에는 Unity 에셋 참조 대신 문자열 ID만 남깁니다.
        [HideInInspector]
        public string itemId;

        public int count;

        /// <summary>
        /// 저장 데이터처럼 itemId만 알고 있을 때 아이템 묶음을 만듭니다.
        /// </summary>
        public ItemStack(string itemId, int count)
        {
            item = null;
            this.itemId = itemId;
            this.count = count;
        }

        /// <summary>
        /// Inspector나 런타임 에셋 참조를 기준으로 아이템 묶음을 만듭니다.
        /// </summary>
        public ItemStack(GameItemSO item, int count)
        {
            this.item = item;
            itemId = item != null ? item.Id : string.Empty;
            this.count = count;
        }

        /// <summary>
        /// Inspector에서 연결한 아이템 에셋입니다.
        /// </summary>
        [JsonIgnore]
        public GameItemSO Item => item;

        /// <summary>
        /// 에셋 참조가 있으면 에셋 ID를, 없으면 저장된 문자열 ID를 반환합니다.
        /// </summary>
        [JsonIgnore]
        public string ItemId => item != null ? item.Id : itemId;

        /// <summary>
        /// 아이템 수량입니다.
        /// </summary>
        [JsonIgnore]
        public int Count => count;

        /// <summary>
        /// 저장/획득/드랍에 사용할 수 있는 유효한 아이템 묶음인지 확인합니다.
        /// </summary>
        [JsonIgnore]
        public bool IsValid => !string.IsNullOrWhiteSpace(ItemId) && count > 0;

        /// <summary>
        /// 아이템 ID가 없거나 수량이 없어 비어 있는 묶음인지 확인합니다.
        /// </summary>
        [JsonIgnore]
        public bool IsEmpty => !IsValid;

        /// <summary>
        /// 저장 파일에 남길 수 있도록 SO 참조를 문자열 ID로 바꾼 복사본을 만듭니다.
        /// </summary>
        public ItemStack ToSaveStack()
        {
            return new ItemStack(ItemId, count);
        }
    }
}
