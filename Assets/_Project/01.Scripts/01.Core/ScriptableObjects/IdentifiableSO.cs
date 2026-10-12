using UnityEngine;
using UnityEngine.Serialization;

namespace TownLife.Core
{
    /// <summary>
    /// 세이브/이벤트에서는 SO 직접 참조 대신 Id 문자열만 저장합니다.
    /// 필요할 때 GameDatabase에서 Id로 다시 SO를 찾습니다.
    /// </summary>
    public abstract class IdentifiableSO : ScriptableObject
    {
        [FormerlySerializedAs("_id")]
        [SerializeField, Tooltip("저장 데이터와 데이터베이스 조회에 사용하는 고유 문자열 ID입니다. 다른 SO와 중복되면 안 됩니다.")]
        private string id = string.Empty;

        public string Id => id;
    }
}
