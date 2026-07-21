using System.Collections.Generic;
using UnityEngine;

namespace TownLife.Systems
{
    /// <summary>
    /// SoundCueSO를 문자열 ID로 조회하는 사운드 전용 데이터베이스입니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TownLife/Audio/Sound Database")]
    public sealed class SoundDatabase : ScriptableObject
    {
        [SerializeField]
        [Tooltip("ID로 조회할 사운드 Cue 목록입니다. 예: sound.ui.click, sound.sfx.mining.hit")]
        private List<SoundCueSO> cues = new();

        private Dictionary<string, SoundCueSO> cueMap;

        /// <summary>
        /// soundId에 해당하는 SoundCueSO를 찾습니다.
        /// </summary>
        public SoundCueSO Get(string soundId)
        {
            if (string.IsNullOrWhiteSpace(soundId))
            {
                return null;
            }

            EnsureMap();

            if (!cueMap.TryGetValue(soundId, out SoundCueSO cue))
            {
                return null;
            }

            return cue;
        }

        private void EnsureMap()
        {
            if (cueMap != null)
            {
                return;
            }

            cueMap = new Dictionary<string, SoundCueSO>();

            foreach (SoundCueSO cue in cues)
            {
                if (cue == null)
                {
                    Debug.LogWarning($"{nameof(SoundDatabase)} has an empty cue.");
                    continue;
                }

                if (string.IsNullOrWhiteSpace(cue.Id))
                {
                    Debug.LogWarning($"{cue.name} has an empty sound id.");
                    continue;
                }

                if (cueMap.ContainsKey(cue.Id))
                {
                    // 중복 ID는 먼저 등록된 데이터를 유지하고, 나중 항목은 무시합니다.
                    Debug.LogWarning($"Duplicate sound id found. id: {cue.Id}");
                    continue;
                }

                cueMap.Add(cue.Id, cue);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            cueMap = null;
        }
#endif
    }
}
