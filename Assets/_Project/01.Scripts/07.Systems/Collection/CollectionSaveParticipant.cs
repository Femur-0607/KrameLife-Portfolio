using System.Collections.Generic;
using TownLife.Core;
using UnityEngine;

namespace TownLife.Systems.Collection
{
    public sealed class CollectionSaveParticipant : MonoBehaviour, ISaveParticipant
    {
        [SerializeField, Tooltip("도감 발견 상태를 저장하고 복원할 수집 기록자입니다.")]
        private CollectionTracker collectionTracker = null;

        private void Awake()
        {
            // 같은 오브젝트에 붙은 CollectionTracker를 기본 저장 대상으로 사용합니다.
            ResolveTracker();
        }

        public void Capture(SaveData data)
        {
            if (data == null)
            {
                return;
            }

            if (!ResolveTracker())
            {
                return;
            }

            // 오래된 세이브 구조나 수동 생성 SaveData에서도 collection 영역을 보장합니다.
            data.collection ??= new CollectionSave();
            data.collection.dex ??= new List<string>();
            data.collection.dex.Clear();
            data.collection.dex.AddRange(collectionTracker.DiscoveredItemIds);
            // 저장 파일 diff가 안정적으로 보이도록 ID 순서를 고정합니다.
            data.collection.dex.Sort();
        }

        public void Restore(SaveData data)
        {
            if (!ResolveTracker())
            {
                return;
            }

            // 로드 데이터가 현재 런타임 상태를 완전히 대체하도록 먼저 비웁니다.
            collectionTracker.ClearDiscoveredItems();

            if (data == null)
            {
                return;
            }

            if (data.collection == null || data.collection.dex == null)
            {
                return;
            }

            // 삭제되었거나 비어 있는 ID는 CollectionTracker 쪽에서 한 번 더 걸러집니다.
            for (int index = 0; index < data.collection.dex.Count; index++)
            {
                collectionTracker.RecordDiscoveredItem(data.collection.dex[index]);
            }
        }

        private bool ResolveTracker()
        {
            // Inspector에 직접 연결된 경우 그 참조를 우선 사용합니다.
            if (collectionTracker != null)
            {
                return true;
            }

            // 보통은 CollectionTracker와 같은 GameObject에 함께 붙여 사용합니다.
            collectionTracker = GetComponent<CollectionTracker>();
            return collectionTracker != null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // 에디터에서 컴포넌트를 붙였을 때 참조 누락을 줄입니다.
            if (collectionTracker == null)
            {
                collectionTracker = GetComponent<CollectionTracker>();
            }
        }
#endif
    }
}
