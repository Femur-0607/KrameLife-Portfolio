using System.Collections.Generic;
using UnityEngine;

namespace TownLife.Core
{
    /// <summary>
    /// 씬 안의 저장 참여자들을 모아 현재 게임 상태를 저장하거나 복원합니다.
    /// </summary>
    public sealed class SaveManager : MonoBehaviour
    {
        [SerializeField, Min(0), Tooltip("저장/로드에 사용할 슬롯 번호입니다. v1은 0번 단일 슬롯을 기본으로 사용합니다.")]
        private int slot = 0;

        [SerializeField, Tooltip("저장에 참여할 MonoBehaviour 목록입니다. 각 컴포넌트는 ISaveParticipant를 구현해야 합니다.")]
        private List<MonoBehaviour> participantBehaviours = new();

        private ISaveStorage storage;
        private readonly List<ISaveParticipant> participants = new();

        private void Awake()
        {
            storage = new JsonSaveStorage();
            RebuildParticipants();
        }

        /// <summary>
        /// 현재 슬롯에 저장 파일이 있는지 확인합니다.
        /// </summary>
        public bool HasSave()
        {
            EnsureStorage();
            return storage.Exists(slot);
        }

        /// <summary>
        /// Inspector에 설정된 기본 슬롯에 저장합니다.
        /// </summary>
        public void Save()
        {
            Save(slot);
        }

        /// <summary>
        /// 지정한 슬롯에 현재 게임 상태를 저장합니다.
        /// </summary>
        public void Save(int targetSlot)
        {
            EnsureStorage();
            RebuildParticipants();
            SaveData data = storage.Exists(targetSlot)
                ? storage.Load(targetSlot)
                : new SaveData();

            foreach (ISaveParticipant participant in participants)
            {
                participant.Capture(data);
            }

            storage.Save(targetSlot, data);
        }

        /// <summary>
        /// Inspector에 설정된 기본 슬롯에서 로드합니다.
        /// </summary>
        public void Load()
        {
            Load(slot);
        }

        /// <summary>
        /// 지정한 슬롯에서 게임 상태를 불러와 저장 참여자들에게 복원합니다.
        /// </summary>
        public void Load(int targetSlot)
        {
            EnsureStorage();
            RebuildParticipants();
            SaveData data = storage.Load(targetSlot);

            foreach (ISaveParticipant participant in participants)
            {
                participant.Restore(data);
            }
        }

        private void RebuildParticipants()
        {
            participants.Clear();
            HashSet<ISaveParticipant> registeredParticipants = new();

            foreach (MonoBehaviour behaviour in participantBehaviours)
            {
                if (behaviour == null)
                {
                    continue;
                }

                if (behaviour is ISaveParticipant participant)
                {
                    AddParticipant(participant, registeredParticipants);
                    continue;
                }

                Debug.LogWarning($"{behaviour.name} does not implement {nameof(ISaveParticipant)}.", behaviour);
            }

            MonoBehaviour[] sceneBehaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (MonoBehaviour behaviour in sceneBehaviours)
            {
                if (behaviour is ISaveParticipant participant)
                {
                    AddParticipant(participant, registeredParticipants);
                }
            }
        }

        private void AddParticipant(ISaveParticipant participant, HashSet<ISaveParticipant> registeredParticipants)
        {
            if (participant == null || !registeredParticipants.Add(participant))
            {
                return;
            }

            participants.Add(participant);
        }

        private void EnsureStorage()
        {
            // 테스트에서 Awake를 거치지 않고 호출해도 저장소가 항상 준비되게 합니다.
            storage ??= new JsonSaveStorage();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            participantBehaviours.RemoveAll(behaviour => behaviour == null);
        }
#endif
    }
}
