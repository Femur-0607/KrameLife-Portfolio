using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ProjectTownLife.Core.SceneFlow
{
    public sealed class BootManager : MonoBehaviour
    {
        [Header("Scenes")]
        // 상주 매니저가 들어있는 Persistent 씬 이름입니다.
        private string persistentSceneName = SceneNames.Persistent;

        // 게임 시작 후 처음 열 게임플레이 씬입니다.
        private GameplaySceneId firstGameplaySceneId = GameplaySceneId.ShopYard;

        [SerializeField]
        [Tooltip("첫 게임플레이 씬에서 플레이어가 도착할 SceneSpawnPoint ID입니다.")]
        private SceneSpawnId firstSpawnId = SceneSpawnId.ShopYardFromBoot;

        private bool isLoading;
        private bool didEnableNextAction;

        /// <summary>
        /// 타이틀 화면의 게임 시작 버튼에서 호출합니다.
        /// </summary>
        public async void OnStartButtonClicked()
        {
            await StartGameAsync();
        }

        /// <summary>
        /// 타이틀 화면의 게임 종료 버튼에서 호출합니다.
        /// </summary>
        public void OnQuitButtonClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        /// <summary>
        /// 게임 시작 흐름입니다.
        /// async/await를 쓰는 이유는 씬 로드가 끝나기 전에 다음 단계가 실행되지 않도록 기다리기 위해서입니다.
        /// </summary>
        public async Task StartGameAsync()
        {
            // Start, 버튼, 입력 등에서 중복 호출되어도 씬 로드가 두 번 겹치지 않게 막습니다.
            if (isLoading)
            {
                return;
            }

            isLoading = true;

            try
            {
                // 1. 저장, 인벤토리, 씬 전환 매니저 같은 상주 오브젝트가 있는 Persistent 씬을 먼저 로드합니다.
                // await 때문에 Persistent 씬 로드가 끝난 뒤에 아래 코드가 이어서 실행됩니다.
                await LoadSceneIfNeededAsync(persistentSceneName);

                // 2. Persistent 씬 안에 생긴 SceneFlowManager를 찾습니다.
                SceneFlowManager sceneFlowManager = FindFirstObjectByType<SceneFlowManager>();

                if (sceneFlowManager == null)
                {
                    Debug.LogWarning($"{nameof(SceneFlowManager)}를 찾지 못해 첫 게임플레이 씬으로 이동하지 못했습니다.", this);
                    isLoading = false;
                    return;
                }

                // 3. 첫 게임플레이 씬으로 이동하고, 지정한 스폰 지점으로 플레이어를 옮깁니다.
                // 이 작업도 씬 로드가 포함되므로 끝날 때까지 기다립니다.
                await sceneFlowManager.RequestMoveAsync(firstGameplaySceneId, firstSpawnId);

                // 4. Persistent와 게임플레이 씬이 준비되었으므로 시작 전용 Boot 씬을 언로드합니다.
                await UnloadBootSceneAsync();
            }
            catch (Exception exception)
            {
                // async void에서 시작된 흐름은 예외를 놓치기 쉬우므로 여기서 직접 로그를 남깁니다.
                Debug.LogException(exception);
                isLoading = false;
            }
        }

        private async Task UnloadBootSceneAsync()
        {
            Scene bootScene = gameObject.scene;

            // 이미 언로드되었거나 잘못된 씬이면 할 일이 없습니다.
            if (!bootScene.IsValid() || !bootScene.isLoaded)
            {
                return;
            }

            // Unity의 씬 언로드 작업이 끝날 때까지 기다립니다.
            await AwaitAsync(SceneManager.UnloadSceneAsync(bootScene));
        }

        private static async Task<Scene> LoadSceneIfNeededAsync(string sceneName)
        {
            Scene loadedScene = SceneManager.GetSceneByName(sceneName);

            // 이미 로드된 씬이면 다시 로드하지 않고 그대로 돌려줍니다.
            if (loadedScene.IsValid() && loadedScene.isLoaded)
            {
                return loadedScene;
            }

            // Additive 로드는 기존 Boot 씬을 유지한 채 새 씬을 추가로 올립니다.
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            await AwaitAsync(operation);

            // 로드가 끝난 뒤 Unity 씬 목록에서 다시 찾아 반환합니다.
            return SceneManager.GetSceneByName(sceneName);
        }

        private static Task AwaitAsync(AsyncOperation operation)
        {
            TaskCompletionSource<bool> completionSource = new TaskCompletionSource<bool>();

            // 작업이 없거나 이미 끝났으면 즉시 완료된 Task를 돌려줍니다.
            if (operation == null || operation.isDone)
            {
                completionSource.SetResult(true);
                return completionSource.Task;
            }

            // Unity AsyncOperation은 바로 await할 수 없어서, completed 이벤트를 Task 완료 신호로 바꿉니다.
            operation.completed += _ => completionSource.SetResult(true);
            return completionSource.Task;
        }
    }
}
