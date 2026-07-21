using TMPro;
using UnityEngine;

namespace TownLife.UI.Dialogue
{
    /// <summary>
    /// 대화 선택지 버튼 한 줄을 표시합니다.
    /// </summary>
    public sealed class DialogueChoiceButtonView : MonoBehaviour
    {
        [SerializeField, Tooltip("선택지 문장을 표시할 텍스트입니다.")]
        private TMP_Text labelText = null;

        /// <summary>
        /// 선택지 문장을 버튼에 반영합니다.
        /// </summary>
        public void Bind(string text)
        {
            if (labelText != null)
            {
                labelText.text = text;
            }

            gameObject.SetActive(true);
        }
    }
}
