using System;
using UnityEngine;

namespace TownLife.Core
{
    /// <summary>
    /// itemId와 수량을 함께 전달하는 ScriptableObject 이벤트 채널입니다.
    /// 세이브/이벤트 payload에는 SO 참조 대신 문자열 ID와 수량을 전달합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TownLife/Events/Item Stack Event Channel")]
    public sealed class ItemStackEventChannelSO : ScriptableObject
    {
        /// <summary>
        /// 아이템 ID와 수량 이벤트를 받을 구독자 목록입니다.
        /// </summary>
        public event Action<string, int> OnRaised;

        /// <summary>
        /// 아이템 ID와 수량을 구독자에게 전달합니다.
        /// </summary>
        public void Raise(string itemId, int count)
        {
            OnRaised?.Invoke(itemId, count);
        }
    }
}
