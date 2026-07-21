using System;
using System.Threading.Tasks;
using ProjectTownLife.Utils;
using TownLife.Core;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectTownLife.Core.SceneFlow
{
    public sealed class SceneFlowManager : MonoBehaviour
    {
        [Header("Runtime References")]
        [SerializeField]
        [Tooltip("Persistent 씬에 있는 플레이어 Transform입니다.")]
        private Transform playerTransform;

        [Header("Scene Names")]
        [SerializeField]
        [ReadOnlyInInspector]
        [Tooltip("현재 기본 플레이 씬 이름입니다.")]
        private string currentGameplaySceneName = SceneNames.ShopYard;

        [SerializeField]
        [ReadOnlyInInspector]
        [Tooltip("숲 귀환 시 돌아갈 씬 이름입니다.")]
        private string forestReturnSceneName = SceneNames.ShopYard;

        [Header("Spawn IDs")]
        [SerializeField]
        [Tooltip("숲에서 상점 앞마당으로 귀환할 때 사용할 스폰 ID입니다.")]
        private SceneSpawnId shopYardFromForestSpawnId = SceneSpawnId.ShopYardFromForest;

        private static readonly string[] GameplaySceneNames =
        {
            SceneNames.ShopYard,
            SceneNames.ShopInterior,
            SceneNames.Village,
            SceneNames.Forest
        };

        private bool isTransitioning;

        /// <summary>
        /// 목표 씬으로 이동하고 지정한 스폰 지점으로 플레이어를 옮깁니다.
        /// </summary>
        public async void RequestMove(GameplaySceneId targetSceneId, SceneSpawnId targetSpawnId)
        {
            await RequestMoveAsync(targetSceneId, targetSpawnId);
        }

        public Task RequestMoveAsync(GameplaySceneId targetSceneId, SceneSpawnId targetSpawnId)
        {
            return MoveToSceneAsync(SceneFlowIds.ToSceneName(targetSceneId), targetSpawnId);
        }

        /// <summary>
        /// 숲 귀환 사유를 받아 상점 앞마당으로 복귀시킵니다.
        /// </summary>
        public async void RequestForestReturn(string returnReason)
        {
            Debug.Log($"숲 귀환 요청: {returnReason}", this);
            await MoveToSceneAsync(forestReturnSceneName, shopYardFromForestSpawnId);
        }

        /// <summary>
        /// 이전 Dungeon API 호출부 호환용입니다. 새 흐름에서는 Forest 귀환으로 처리합니다.
        /// </summary>
        public void RequestDungeonReturn(string returnReason)
        {
            RequestForestReturn(returnReason);
        }

        private async Task MoveToSceneAsync(string targetSceneName, SceneSpawnId targetSpawnId)
        {
            if (isTransitioning)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(targetSceneName))
            {
                Debug.LogWarning("목표 씬 이름이 비어 있습니다.", this);
                return;
            }

            isTransitioning = true;

            try
            {
                SaveLoadedState();
                await UnloadOtherGameplayScenesAsync(targetSceneName);
                Scene targetScene = await LoadSceneIfNeededAsync(targetSceneName);

                if (targetScene.IsValid())
                {
                    SceneManager.SetActiveScene(targetScene);
                    currentGameplaySceneName = targetSceneName;
                }

                RestoreLoadedState();
                MovePlayerToSpawnPoint(targetScene, targetSpawnId);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                isTransitioning = false;
            }
        }

        private static async Task UnloadOtherGameplayScenesAsync(string targetSceneName)
        {
            for (int i = 0; i < GameplaySceneNames.Length; i++)
            {
                string sceneName = GameplaySceneNames[i];

                if (sceneName == targetSceneName)
                {
                    continue;
                }

                Scene scene = SceneManager.GetSceneByName(sceneName);

                if (!scene.IsValid() || !scene.isLoaded)
                {
                    continue;
                }

                await AwaitAsync(SceneManager.UnloadSceneAsync(scene));
            }
        }

        private void MovePlayerToSpawnPoint(Scene targetScene, SceneSpawnId targetSpawnId)
        {
            Transform resolvedPlayerTransform = ResolvePlayerTransform();

            if (resolvedPlayerTransform == null)
            {
                Debug.LogWarning("플레이어 Transform을 찾지 못해 스폰 위치로 이동하지 못했습니다.", this);
                return;
            }

            SceneSpawnPoint spawnPoint = FindSpawnPoint(targetScene, targetSpawnId);

            if (spawnPoint == null)
            {
                Debug.LogWarning($"스폰 지점을 찾지 못했습니다. scene={targetScene.name}, spawnId={SceneFlowIds.ToSpawnId(targetSpawnId)}", this);
                return;
            }

            resolvedPlayerTransform.SetPositionAndRotation(spawnPoint.transform.position, spawnPoint.transform.rotation);
        }

        private Transform ResolvePlayerTransform()
        {
            if (playerTransform != null)
            {
                playerTransform = ResolvePlayerRootTransform(playerTransform);
                return playerTransform;
            }

            global::PlayerMovement[] players = FindObjectsByType<global::PlayerMovement>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);

            for (int i = 0; i < players.Length; i++)
            {
                if (players[i].CompareTag("Player"))
                {
                    playerTransform = players[i].transform;
                    return playerTransform;
                }
            }

            playerTransform = players.Length > 0 ? players[0].transform : null;
            return playerTransform;
        }

        /// <summary>
        /// 자식 콜라이더가 연결되어도 실제 플레이어 루트 Transform으로 보정합니다.
        /// </summary>
        private static Transform ResolvePlayerRootTransform(Transform candidateTransform)
        {
            if (candidateTransform.TryGetComponent(out global::PlayerMovement playerMovement))
            {
                return playerMovement.transform;
            }

            global::PlayerMovement parentPlayerMovement = candidateTransform.GetComponentInParent<global::PlayerMovement>();
            return parentPlayerMovement != null ? parentPlayerMovement.transform : candidateTransform;
        }

        private static SceneSpawnPoint FindSpawnPoint(Scene scene, SceneSpawnId spawnId)
        {
            SceneSpawnPoint[] spawnPoints = FindObjectsByType<SceneSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            for (int i = 0; i < spawnPoints.Length; i++)
            {
                SceneSpawnPoint spawnPoint = spawnPoints[i];

                if (spawnPoint.gameObject.scene == scene && spawnPoint.SpawnId == spawnId)
                {
                    return spawnPoint;
                }
            }

            return null;
        }

        private static async Task<Scene> LoadSceneIfNeededAsync(string sceneName)
        {
            Scene loadedScene = SceneManager.GetSceneByName(sceneName);

            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                return loadedScene;
            }

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            await AwaitAsync(operation);

            return SceneManager.GetSceneByName(sceneName);
        }

        private static void SaveLoadedState()
        {
            SaveManager saveManager = FindAnyObjectByType<SaveManager>();
            if (saveManager != null)
            {
                saveManager.Save();
            }
        }

        private static void RestoreLoadedState()
        {
            SaveManager saveManager = FindAnyObjectByType<SaveManager>();
            if (saveManager != null && saveManager.HasSave())
            {
                saveManager.Load();
            }
        }

        private static Task AwaitAsync(AsyncOperation operation)
        {
            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

            if (operation == null || operation.isDone)
            {
                completionSource.SetResult(true);
                return completionSource.Task;
            }

            operation.completed += _ => completionSource.SetResult(true);
            return completionSource.Task;
        }
    }
}
