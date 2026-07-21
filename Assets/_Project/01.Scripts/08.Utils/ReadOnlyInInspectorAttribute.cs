using UnityEngine;

namespace ProjectTownLife.Utils
{
    /// <summary>
    /// Inspector에서 값은 보여주되 수정은 막는 표시용 속성입니다.
    /// </summary>
    public sealed class ReadOnlyInInspectorAttribute : PropertyAttribute
    {
    }
}
