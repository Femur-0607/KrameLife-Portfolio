using TownLife.Core;
using UnityEngine;

namespace TownLife.Systems
{
    /// <summary>
    /// 하나의 재생 가능한 사운드 데이터를 정의합니다.
    /// </summary>
    [CreateAssetMenu(menuName = "TownLife/Audio/Sound Cue")]
    public sealed class SoundCueSO : IdentifiableSO
    {
        [SerializeField]
        [Tooltip("실제로 재생할 AudioClip입니다.")]
        private AudioClip clip;

        [SerializeField]
        [Tooltip("이 사운드를 보낼 Audio Mixer 채널입니다.")]
        private SoundChannel channel = SoundChannel.Sfx;

        [SerializeField, Range(0f, 1f)]
        [Tooltip("개별 사운드의 상대 볼륨입니다. 전체 볼륨은 Audio Mixer 볼륨과 함께 적용됩니다.")]
        private float volumeScale = 1f;

        [SerializeField]
        [Tooltip("BGM 재생 시 반복 재생할지 여부입니다. SFX/UI에서는 사용하지 않습니다.")]
        private bool loop = true;

        [SerializeField, Min(0f)]
        [Tooltip("같은 사운드 ID가 다시 재생되기 전 기다릴 최소 시간입니다. 0이면 제한하지 않습니다.")]
        private float cooldown = 0f;

        /// <summary>
        /// 실제 재생할 AudioClip입니다.
        /// </summary>
        public AudioClip Clip => clip;

        /// <summary>
        /// 이 사운드를 재생할 Audio Mixer 채널입니다.
        /// </summary>
        public SoundChannel Channel => channel;

        /// <summary>
        /// 개별 사운드의 상대 볼륨입니다.
        /// </summary>
        public float VolumeScale => volumeScale;

        /// <summary>
        /// BGM 재생 시 반복 재생할지 여부입니다.
        /// </summary>
        public bool Loop => loop;

        /// <summary>
        /// 같은 사운드 ID의 연속 재생을 막는 최소 간격입니다.
        /// </summary>
        public float Cooldown => cooldown;
    }
}
