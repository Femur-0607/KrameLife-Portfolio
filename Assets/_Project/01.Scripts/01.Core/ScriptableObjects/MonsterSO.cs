using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TownLife.Core
{
    // 몬스터의 큰 줄기를 정의 할 enum class
    public enum EnemyFamily
    {
        Slime,
        Goblin,
        Skeleton
    }
    /// <summary>
    /// 몬스터의 공통 데이터입니다. 행동 AI는 여기 넣지 않고 던전/전투 쪽에서 처리합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TownLife/Monster")]
    public sealed class MonsterSO : IdentifiableSO
    {
        [FormerlySerializedAs("_displayName")]
        [SerializeField, Tooltip("UI와 도감에 표시할 몬스터 이름입니다.")]
        private string displayName = string.Empty;

        [FormerlySerializedAs("_description")]
        [SerializeField, TextArea, Tooltip("몬스터 상세 정보 UI나 기획 확인용 설명 문구입니다.")]
        private string description = string.Empty;

        [FormerlySerializedAs("_icon")]
        [SerializeField, Tooltip("도감, 전투 UI, 보상 UI에 표시할 몬스터 아이콘입니다.")]
        private Sprite icon = null;

        [FormerlySerializedAs("_maxHealth")]
        [SerializeField, Min(1), Tooltip("몬스터가 전투 시작 시 가지는 최대 체력입니다.")]
        private int maxHealth = 1;

        [FormerlySerializedAs("_attackDamage")]
        [SerializeField, Min(0), Tooltip("몬스터의 기본 공격 피해량입니다.")]
        private int attackDamage = 0;

        // 나중에 dropItem 스트럭쳐 만들어서 교체 필요 (스트럭쳐 내에서 GameItemSO , 드랍 확률 관리)
        [FormerlySerializedAs("_drop")] [SerializeField, Tooltip("몬스터 처치 시 드롭 후보로 사용할 아이템 ID와 수량입니다.")]
        //private List<ItemStack> drop;
        private List<EnemyDropStack> drop;
        [FormerlySerializedAs("_dropChance")]
        [SerializeField, Range(0f, 1f), Tooltip("드롭 아이템이 나올 확률입니다. 0은 드롭 없음, 1은 항상 드롭입니다.")]
        private float dropChance = 0f;
        [SerializeField, Tooltip("몬스터의 큰 종류를 나타내는 변수")]
        private EnemyFamily enemyFamily;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public int MaxHealth => maxHealth;
        public int AttackDamage => attackDamage;
        // 나중에 dropItem 스트럭쳐 만들어서 교체 필요
        //public IReadOnlyList<ItemStack> Drop => drop;
        public IReadOnlyList<EnemyDropStack> Drop => drop;
        /// <summary>0은 드롭 없음, 1은 항상 드롭입니다.</summary>
        public float DropChance => dropChance;
        public EnemyFamily EnemyFamily => enemyFamily;
    }
}
