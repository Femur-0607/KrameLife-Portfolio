using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace TownLife.Core
{
    /// <summary>
    /// 아이템 아이콘을 직접 참조 Sprite 또는 Addressables 주소로 불러오는 공용 유틸입니다.
    /// </summary>
    public static class ItemIconLoader
    {
        private static readonly Dictionary<string, CachedIcon> cachedIconsByAddress = new();

        /// <summary>
        /// Addressables 로드 전 또는 실패 시 사용할 기본 아이콘을 반환합니다.
        /// </summary>
        public static Sprite GetFallbackIcon(GameItemSO item)
        {
            return item != null ? item.Icon : null;
        }

        /// <summary>
        /// 아이템의 Addressables 주소로 아이콘 Sprite를 비동기 로드합니다.
        /// </summary>
        public static bool TryLoadAddressableIcon(
            GameItemSO item,
            Action<Sprite> onLoaded,
            out AsyncOperationHandle<Sprite> handle)
        {
            handle = default;

            if (!CanLoadAddressableIcon(item))
            {
                return false;
            }

            CachedIcon cachedIcon = GetOrCreateCachedIcon(item.IconAddress);
            handle = cachedIcon.Handle;

            if (cachedIcon.IsLoaded)
            {
                onLoaded?.Invoke(cachedIcon.Sprite ?? GetFallbackIcon(item));
                return true;
            }

            if (cachedIcon.IsFailed)
            {
                onLoaded?.Invoke(GetFallbackIcon(item));
                return true;
            }

            cachedIcon.Handle.Completed += completedHandle =>
            {
                Sprite loadedIcon = completedHandle.Status == AsyncOperationStatus.Succeeded && completedHandle.Result != null
                    ? completedHandle.Result
                    : GetFallbackIcon(item);

                onLoaded?.Invoke(loadedIcon);
            };

            return true;
        }

        /// <summary>
        /// 호출자가 보유한 아이콘 로드 핸들 상태를 비웁니다.
        /// </summary>
        public static void ReleaseIconHandle(ref AsyncOperationHandle<Sprite> handle, ref bool hasHandle)
        {
            if (!hasHandle)
            {
                return;
            }

            handle = default;
            hasHandle = false;
        }

        /// <summary>
        /// 보관 중인 아이콘 캐시를 모두 해제합니다.
        /// </summary>
        public static void ClearCache()
        {
            foreach (CachedIcon cachedIcon in cachedIconsByAddress.Values)
            {
                if (cachedIcon.CanRelease)
                {
                    Addressables.Release(cachedIcon.Handle);
                }
            }

            cachedIconsByAddress.Clear();
        }

        /// <summary>
        /// Addressables로 아이콘을 로드할 수 있는 아이템인지 확인합니다.
        /// </summary>
        private static bool CanLoadAddressableIcon(GameItemSO item)
        {
            return item != null && !string.IsNullOrWhiteSpace(item.IconAddress);
        }

        /// <summary>
        /// 주소별 아이콘 로드 핸들을 만들거나 기존 캐시를 반환합니다.
        /// </summary>
        private static CachedIcon GetOrCreateCachedIcon(string address)
        {
            if (cachedIconsByAddress.TryGetValue(address, out CachedIcon cachedIcon))
            {
                return cachedIcon;
            }

            AsyncOperationHandle<Sprite> loadHandle = Addressables.LoadAssetAsync<Sprite>(address);
            cachedIcon = new CachedIcon(loadHandle);
            cachedIconsByAddress.Add(address, cachedIcon);
            return cachedIcon;
        }

        private sealed class CachedIcon
        {
            /// <summary>
            /// Addressables 로드 핸들의 완료 상태를 캐시에 반영할 준비를 합니다.
            /// </summary>
            public CachedIcon(AsyncOperationHandle<Sprite> handle)
            {
                Handle = handle;
                Handle.Completed += OnCompleted;
            }

            public AsyncOperationHandle<Sprite> Handle { get; }
            public Sprite Sprite { get; private set; }
            public bool IsLoaded { get; private set; }
            public bool IsFailed { get; private set; }
            public bool CanRelease => Handle.IsValid();

            /// <summary>
            /// 로드가 끝난 Sprite 또는 실패 상태를 캐시에 저장합니다.
            /// </summary>
            private void OnCompleted(AsyncOperationHandle<Sprite> completedHandle)
            {
                if (completedHandle.Status == AsyncOperationStatus.Succeeded && completedHandle.Result != null)
                {
                    Sprite = completedHandle.Result;
                    IsLoaded = true;
                    return;
                }

                IsFailed = true;
            }
        }
    }
}
