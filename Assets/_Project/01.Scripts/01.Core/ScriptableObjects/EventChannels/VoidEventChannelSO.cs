using System;
using UnityEngine;

namespace TownLife.Core
{
    /// <summary>
    /// 값 없이 "일이 일어났다"는 사실만 알리는 ScriptableObject 이벤트 채널입니다.
    /// 구독자는 OnEnable에서 등록하고 OnDisable에서 반드시 해제합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TownLife/Events/Void Event Channel")]
    public sealed class VoidEventChannelSO : ScriptableObject
    {
        /// <summary>
        /// 값 없는 이벤트를 받을 구독자 목록입니다.
        /// </summary>
        public event Action OnRaised;

        /// <summary>
        /// 구독자에게 이벤트 발생만 알립니다.
        /// </summary>
        public void Raise()
        {
            OnRaised?.Invoke();
        }
    }
}
