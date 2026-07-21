using UnityEngine;
using UnityEngine.Serialization;

namespace TownLife.Core
{
    /// <summary>
    /// NPC/네임드 캐릭터의 공통 데이터입니다. 호감도 변화와 대화 진행 로직은 별도 시스템에서 처리합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TownLife/Character")]
    public sealed class CharacterSO : IdentifiableSO
    {
        [FormerlySerializedAs("_displayName")]
        [SerializeField, Tooltip("UI와 대화창에 표시할 캐릭터 이름입니다.")]
        private string displayName = string.Empty;

        [FormerlySerializedAs("_description")]
        [SerializeField, TextArea, Tooltip("캐릭터의 설명 문구입니다. 도감, 소개 UI, 기획 확인용으로 사용합니다.")]
        private string description = string.Empty;

        [FormerlySerializedAs("_portrait")]
        [SerializeField, Tooltip("대화창이나 캐릭터 정보 UI에 표시할 초상화 이미지입니다.")]
        private Sprite portrait = null;

        [FormerlySerializedAs("_startingAffection")]
        [SerializeField, Min(0), Tooltip("새 게임 또는 초기 상태에서 시작하는 기본 호감도 값입니다.")]
        private int startingAffection = 0;

        [FormerlySerializedAs("_defaultDialogueKey")]
        [SerializeField, Tooltip("기본 대화를 찾을 때 사용하는 대화 데이터 키입니다.")]
        private string defaultDialogueKey = string.Empty;

        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Portrait => portrait;
        public int StartingAffection => startingAffection;
        public string DefaultDialogueKey => defaultDialogueKey;
    }
}
