using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace TownLife.Core
{
    /// <summary>
    /// 아이템, 몬스터, NPC 같은 공통 콘텐츠 데이터를 문자열 ID로 조회하는 중앙 데이터베이스입니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TownLife/GameDatabase")]
    public sealed class GameDatabase : ScriptableObject
    {
        [FormerlySerializedAs("_entries")]
        [SerializeField, Tooltip("ID로 조회할 수 있는 모든 공통 데이터 SO 목록입니다. 아이템, 몬스터, 캐릭터 같은 IdentifiableSO 에셋을 등록합니다.")]
        private List<IdentifiableSO> entries = new();

        private Dictionary<string, IdentifiableSO> map;

        /// <summary>
        /// ID와 타입이 모두 맞는 공통 데이터 SO를 조회합니다.
        /// </summary>
        public T Get<T>(string id) where T : IdentifiableSO
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            EnsureMap();

            if (!map.TryGetValue(id, out IdentifiableSO entry))
            {
                Debug.LogWarning($"GameDatabase entry not found. id: {id}");
                return null;
            }

            if (entry is not T typedEntry)
            {
                // 같은 ID라도 요청 타입이 다르면 잘못된 데이터 연결이므로 null로 처리합니다.
                Debug.LogWarning($"GameDatabase entry type mismatch. id: {id}, expected: {typeof(T).Name}, actual: {entry.GetType().Name}");
                return null;
            }

            return typedEntry;
        }

        /// <summary>
        /// 지정한 ID가 데이터베이스에 등록되어 있는지 확인합니다.
        /// </summary>
        public bool Contains(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return false;
            }

            EnsureMap();
            return map.ContainsKey(id);
        }

        private void EnsureMap()
        {
            if (map != null)
            {
                return;
            }

            map = new Dictionary<string, IdentifiableSO>();

            foreach (IdentifiableSO entry in entries)
            {
                if (entry == null)
                {
                    Debug.LogWarning($"{nameof(GameDatabase)} has an empty entry.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(entry.Id))
                {
                    Debug.LogWarning($"{entry.name} has an empty id.");
                    continue;
                }

                if (map.ContainsKey(entry.Id))
                {
                    // 중복 ID는 먼저 등록된 데이터를 유지하고, 나중 항목은 무시합니다.
                    continue;
                }

                map.Add(entry.Id, entry);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            map = null;
        }
#endif
    }
}
