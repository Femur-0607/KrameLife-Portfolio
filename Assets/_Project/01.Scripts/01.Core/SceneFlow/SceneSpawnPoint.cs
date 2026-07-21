using UnityEngine;

namespace ProjectTownLife.Core.SceneFlow
{
    public sealed class SceneSpawnPoint : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("씬 전환 후 플레이어가 도착할 위치 ID입니다.")]
        private SceneSpawnId spawnId;

        public SceneSpawnId SpawnId => spawnId;
    }
}
