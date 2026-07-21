using System;
using UnityEngine;

namespace TownLife.Core
{
    /// <summary>
    /// itemId, monsterId, reason 같은 문자열 값을 전달하는 ScriptableObject 이벤트 채널입니다.
    /// 세이브/이벤트 payload에는 SO 참조 대신 문자열 ID를 전달합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TownLife/Events/String Event Channel")]
    public sealed class StringEventChannelSO : ScriptableObject
    {
        /// <summary>
        /// 문자열 이벤트를 받을 구독자 목록입니다.
        /// </summary>
        public event Action<string> OnRaised;

        /// <summary>
        /// 문자열 값을 구독자에게 전달합니다.
        /// </summary>
        public void Raise(string value)
        {
            OnRaised?.Invoke(value);
        }
    }
}
