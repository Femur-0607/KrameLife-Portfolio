using System.Globalization;
using DG.Tweening;
using TMPro;
using TownLife.Characters;
using TownLife.Core;
using TownLife.Gameplay;
using TownLife.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace TownLife.UI
{
    /// <summary>
    /// 가게 운영용 상단 HUD와 플레이어 상태 HUD를 갱신합니다.
    /// </summary>
    public sealed class ShopHudController : MonoBehaviour
    {
        // 요일 표기는 TimeManager의 전체 날짜를 7로 나눠 순환합니다.
        private static readonly string[] WeekdayNames = { "월", "화", "수", "목", "금", "토", "일" };

        // TimeManager의 하루 진행 범위와 HUD 화살표 개수입니다.
        private const int DayStartMinute = 420;
        private const int ForcedSleepMinute = 1560;
        private const int ArrowCount = 5;

        [Header("Shop Status")]
        // ShopStatusPanel: 골드와 명성 텍스트만 관리합니다.
        [Tooltip("현재 보유 골드를 표시할 텍스트입니다.")]
        [SerializeField] private TMP_Text goldText = null;
        [Tooltip("현재 상점 명성을 표시할 텍스트입니다.")]
        [SerializeField] private TMP_Text reputationText = null;

        [Header("Player Status")]
        // PlayerStatusPanel: 체력/스태미너 텍스트와 게이지를 관리합니다.
        [Tooltip("플레이어 체력을 표시할 텍스트입니다.")]
        [SerializeField] private TMP_Text hpText = null;
        [Tooltip("플레이어 스태미너를 표시할 텍스트입니다.")]
        [SerializeField] private TMP_Text staminaText = null;
        [Tooltip("플레이어 체력 비율을 표시할 이미지입니다.")]
        [SerializeField] private Image hpFill = null;
        [Tooltip("플레이어 스태미너 비율을 표시할 이미지입니다.")]
        [SerializeField] private Image staminaFill = null;

        [Header("Time Weather")]
        // TimeWeatherPanel: 날짜 표기, 날씨 이미지, 시간 흐름 화살표를 관리합니다.
        [Tooltip("계절과 월을 표시할 텍스트입니다.")]
        [SerializeField] private TMP_Text dateText1 = null;
        [Tooltip("요일과 날짜를 표시할 텍스트입니다.")]
        [SerializeField] private TMP_Text dateText2 = null;
        [Tooltip("현재 날씨에 따라 교체될 프레임 이미지입니다.")]
        [SerializeField] private Image weatherFrameImage = null;
        [Tooltip("날씨 인덱스에 따라 사용할 프레임 스프라이트 목록입니다.")]
        [SerializeField] private Sprite[] weatherFrameSprites = null;
        [Tooltip("현재 중앙에 보이는 날씨 아이콘입니다.")]
        [SerializeField] private RectTransform weatherImage1 = null;
        [Tooltip("다음 날씨 전환 때 중앙으로 이동할 대기 아이콘입니다.")]
        [SerializeField] private RectTransform weatherImage2 = null;
        [Tooltip("하루 시간 진행도를 5단계로 표시할 화살표 이미지 목록입니다.")]
        [SerializeField] private Image[] arrowImages = null;

        [Header("Motion")]
        // UI 연출 값은 디자이너가 인스펙터에서 조정할 수 있게 직렬화합니다.
        [Tooltip("날씨 아이콘 전환 애니메이션 시간입니다.")]
        [SerializeField, Min(0.01f)] private float weatherSwapDuration = 0.35f;
        [Tooltip("사라지는 날씨 아이콘이 아래로 내려갈 거리입니다.")]
        [SerializeField, Min(0f)] private float weatherDropDistance = 40f;
        [Tooltip("화살표가 켜질 때 커지는 배율입니다.")]
        [SerializeField, Min(1f)] private float arrowPulseScale = 1.25f;
        [Tooltip("화살표가 커졌다 돌아오는 각 단계의 시간입니다.")]
        [SerializeField, Min(0.01f)] private float arrowPulseDuration = 0.12f;
        [Tooltip("HUD 정보를 다시 읽어 갱신하는 간격입니다.")]
        [SerializeField, Min(0.02f)] private float refreshInterval = 0.15f;

        // 런타임에서 찾아 캐시하는 참조입니다. 인스펙터 연결이 필요 없어 직렬화하지 않습니다.
        private TimeManager timeManager = null;
        private ShopReputationManager reputationManager = null;
        private PlayerHealth playerHealth = null;
        private PlayerStamina playerStamina = null;

        // HUD 갱신과 애니메이션 상태입니다. 저장/인스펙터 노출 대상이 아니므로 직렬화하지 않습니다.
        private float nextRefreshTime;
        private int lastDay = -1;
        private int lastArrowIndex = -1;
        private int currentWeatherIndex;
        private Vector2 weatherCenterPosition;
        private Vector2 weatherStandbyPosition;
        private bool hasWeatherPositions;
        private Vector3[] arrowBaseScales = null;
        private Sequence weatherSequence = null;
        private Tween arrowTween = null;

        /// <summary>
        /// HUD가 켜질 때 참조와 초기 UI 위치를 준비하고 즉시 갱신합니다.
        /// </summary>
        private void OnEnable()
        {
            // 활성화될 때 필요한 참조와 초기 좌표를 잡고 즉시 한 번 갱신합니다.
            CacheReferences();
            CacheLayout();
            Refresh();
        }

        /// <summary>
        /// HUD가 꺼질 때 진행 중인 DOTween 연출을 정리합니다.
        /// </summary>
        private void OnDisable()
        {
            weatherSequence?.Kill();
            arrowTween?.Kill();
        }

        /// <summary>
        /// 지정된 주기마다 HUD 정보를 다시 읽어 화면에 반영합니다.
        /// </summary>
        private void Update()
        {
            // 매 프레임 갱신하지 않고 지정된 간격으로 HUD 텍스트/게이지를 갱신합니다.
            if (Time.unscaledTime < nextRefreshTime)
            {
                return;
            }

            nextRefreshTime = Time.unscaledTime + refreshInterval;
            Refresh();
        }

        /// <summary>
        /// 상점 상태, 플레이어 상태, 날짜/날씨 HUD를 한 번 갱신합니다.
        /// </summary>
        private void Refresh()
        {
            // 외부 매니저가 늦게 생성될 수 있어서 갱신 시점마다 누락 참조를 보충합니다.
            CacheReferences();
            RefreshEconomy();
            RefreshPlayerStatus();
            RefreshTimeWeather();
        }

        /// <summary>
        /// 골드와 명성 텍스트를 현재 상점 데이터로 갱신합니다.
        /// </summary>
        private void RefreshEconomy()
        {
            // 골드는 GameContext, 명성은 ShopReputationManager에서 읽어 표시합니다.
            IGold gold = GameContext.Instance != null ? GameContext.Instance.Gold : null;
            int currentGold = gold != null ? gold.Gold : 0;
            int reputation = reputationManager != null ? reputationManager.Reputation : 0;

            SetText(goldText, $"{currentGold.ToString("N0", CultureInfo.InvariantCulture)} G");
            SetText(reputationText, $"명성 {reputation.ToString("N0", CultureInfo.InvariantCulture)}");
        }

        /// <summary>
        /// 플레이어 체력과 스태미너 텍스트/게이지를 갱신합니다.
        /// </summary>
        private void RefreshPlayerStatus()
        {
            // 플레이어 상태 컴포넌트가 없으면 0/100으로 안전하게 표시합니다.
            int currentHp = playerHealth != null ? playerHealth.CurrentHp : 0;
            int maxHp = playerHealth != null ? playerHealth.MaxHp : 100;
            int currentStamina = playerStamina != null ? playerStamina.CurrentStamina : 0;
            int maxStamina = playerStamina != null ? playerStamina.MaxStamina : 100;

            SetText(hpText, $"HP {currentHp}/{maxHp}");
            SetText(staminaText, $"STM {currentStamina}/{maxStamina}");
            SetFill(hpFill, currentHp, maxHp);
            SetFill(staminaFill, currentStamina, maxStamina);
        }

        /// <summary>
        /// 날짜, 요일, 날씨 프레임, 시간 화살표를 갱신합니다.
        /// </summary>
        private void RefreshTimeWeather()
        {
            // TimeManager가 없을 때도 HUD가 빈 값으로 보이지 않도록 기본 날짜를 표시합니다.
            if (timeManager == null)
            {
                SetText(dateText1, "봄 1월");
                SetText(dateText2, "월 1일");
                ApplyWeatherFrame();
                SetArrowIndex(0, false);
                return;
            }

            int day = timeManager.CurrentDay;
            int dayOfSeason = timeManager.CurrentDayOfSeason;

            // DateText_1은 계절/월, DateText_2는 요일/계절 내 날짜를 표시합니다.
            SetText(dateText1, $"{GetSeasonName(timeManager.CurrentSeason)} {GetMonthInSeason(dayOfSeason)}월");
            SetText(dateText2, $"{GetWeekdayName(day)} {dayOfSeason}일");
            ApplyWeatherFrame();

            // 날짜가 바뀐 순간 날씨 아이콘 교체 연출을 한 번 재생합니다.
            if (lastDay >= 0 && lastDay != day)
            {
                PlayWeatherSwap();
            }

            lastDay = day;
            SetArrowIndex(GetArrowIndex(timeManager.CurrentMinuteOfDay), true);
        }

        /// <summary>
        /// 외부 날씨 시스템에서 현재 날씨 인덱스를 전달할 때 사용합니다.
        /// </summary>
        public void SetWeatherIndex(int weatherIndex)
        {
            // 추후 날씨 시스템에서 호출할 날씨 인덱스 진입점입니다.
            currentWeatherIndex = Mathf.Max(0, weatherIndex);
            ApplyWeatherFrame();
        }

        /// <summary>
        /// HUD 갱신에 필요한 씬 내 매니저와 플레이어 컴포넌트를 캐시합니다.
        /// </summary>
        private void CacheReferences()
        {
            // 씬에 있는 시스템/플레이어 컴포넌트를 필요한 시점에 찾아 캐시합니다.
            if (timeManager == null)
            {
                timeManager = FindAnyObjectByType<TimeManager>();
            }

            if (reputationManager == null)
            {
                reputationManager = FindAnyObjectByType<ShopReputationManager>();
            }

            if (playerHealth == null)
            {
                playerHealth = FindAnyObjectByType<PlayerHealth>();
            }

            if (playerStamina == null)
            {
                playerStamina = FindAnyObjectByType<PlayerStamina>();
            }
        }

        /// <summary>
        /// 날씨 아이콘 위치와 화살표 기본 스케일을 최초 1회 저장합니다.
        /// </summary>
        private void CacheLayout()
        {
            // 날씨 아이콘의 중앙/대기 위치와 화살표 기본 스케일을 최초 1회 저장합니다.
            if (!hasWeatherPositions && weatherImage1 != null && weatherImage2 != null)
            {
                weatherCenterPosition = weatherImage1.anchoredPosition;
                weatherStandbyPosition = weatherImage2.anchoredPosition;
                hasWeatherPositions = true;
            }

            if (arrowImages == null || arrowBaseScales != null && arrowBaseScales.Length == arrowImages.Length)
            {
                return;
            }

            arrowBaseScales = new Vector3[arrowImages.Length];

            for (int i = 0; i < arrowImages.Length; i++)
            {
                arrowBaseScales[i] = arrowImages[i] != null ? arrowImages[i].rectTransform.localScale : Vector3.one;
            }
        }

        /// <summary>
        /// 날짜 변경 시 현재 날씨 아이콘을 내리고 대기 아이콘을 중앙으로 이동시킵니다.
        /// </summary>
        private void PlayWeatherSwap()
        {
            // 현재 아이콘은 아래로 사라지고, 대기 아이콘은 중앙으로 들어옵니다.
            if (!hasWeatherPositions || weatherImage1 == null || weatherImage2 == null)
            {
                return;
            }

            weatherSequence?.Kill();

            RectTransform disappearing = weatherImage1;
            RectTransform entering = weatherImage2;
            Image disappearingImage = disappearing.GetComponent<Image>();

            disappearing.gameObject.SetActive(true);
            entering.gameObject.SetActive(true);
            SetAlpha(disappearingImage, 1f);
            disappearing.anchoredPosition = weatherCenterPosition;
            entering.anchoredPosition = weatherStandbyPosition;

            weatherSequence = DOTween.Sequence()
                .SetUpdate(true)
                .Append(disappearing.DOAnchorPos(weatherCenterPosition + Vector2.down * weatherDropDistance, weatherSwapDuration));

            if (disappearingImage != null)
            {
                weatherSequence.Join(disappearingImage.DOFade(0f, weatherSwapDuration));
            }

            weatherSequence
                .AppendCallback(() =>
                {
                    disappearing.gameObject.SetActive(false);
                    disappearing.anchoredPosition = weatherStandbyPosition;
                    SetAlpha(disappearingImage, 1f);
                    disappearing.gameObject.SetActive(true);
                })
                .Append(entering.DOAnchorPos(weatherCenterPosition, weatherSwapDuration))
                .OnComplete(() =>
                {
                    weatherImage1 = entering;
                    weatherImage2 = disappearing;
                });
        }

        /// <summary>
        /// 현재 날씨 인덱스에 맞는 프레임 스프라이트를 적용합니다.
        /// </summary>
        private void ApplyWeatherFrame()
        {
            // 등록된 날씨 프레임 스프라이트가 있으면 현재 날씨 인덱스에 맞춰 교체합니다.
            if (weatherFrameImage == null || weatherFrameSprites == null || weatherFrameSprites.Length == 0)
            {
                return;
            }

            int spriteIndex = currentWeatherIndex % weatherFrameSprites.Length;
            weatherFrameImage.sprite = weatherFrameSprites[spriteIndex];
        }

        /// <summary>
        /// 현재 시간 단계에 맞는 화살표 하나만 켜고 필요하면 펄스 연출을 재생합니다.
        /// </summary>
        private void SetArrowIndex(int arrowIndex, bool animate)
        {
            // 시간 진행도에 맞는 화살표 하나만 켜고, 변경 순간에 펄스 연출을 재생합니다.
            if (arrowImages == null || arrowImages.Length == 0)
            {
                return;
            }

            arrowIndex = Mathf.Clamp(arrowIndex, 0, arrowImages.Length - 1);

            if (lastArrowIndex == arrowIndex)
            {
                return;
            }

            arrowTween?.Kill();

            for (int i = 0; i < arrowImages.Length; i++)
            {
                if (arrowImages[i] == null)
                {
                    continue;
                }

                arrowImages[i].gameObject.SetActive(i == arrowIndex);
                arrowImages[i].rectTransform.localScale = GetArrowBaseScale(i);
            }

            lastArrowIndex = arrowIndex;

            if (!animate || arrowImages[arrowIndex] == null)
            {
                return;
            }

            RectTransform target = arrowImages[arrowIndex].rectTransform;
            Vector3 baseScale = GetArrowBaseScale(arrowIndex);

            target.localScale = baseScale;
            arrowTween = DOTween.Sequence()
                .SetUpdate(true)
                .Append(target.DOScale(baseScale * arrowPulseScale, arrowPulseDuration))
                .Append(target.DOScale(baseScale, arrowPulseDuration));
        }

        /// <summary>
        /// 저장해둔 화살표 기본 스케일을 반환합니다.
        /// </summary>
        private Vector3 GetArrowBaseScale(int index)
        {
            if (arrowBaseScales == null || index < 0 || index >= arrowBaseScales.Length)
            {
                return Vector3.one;
            }

            return arrowBaseScales[index];
        }

        /// <summary>
        /// 계절 내 날짜를 10일 단위 월 표기로 변환합니다.
        /// </summary>
        private static int GetMonthInSeason(int dayOfSeason)
        {
            return Mathf.Clamp((Mathf.Max(1, dayOfSeason) - 1) / 10 + 1, 1, 3);
        }

        /// <summary>
        /// 전체 날짜를 요일 문자열로 변환합니다.
        /// </summary>
        private static string GetWeekdayName(int day)
        {
            int weekdayIndex = (Mathf.Max(1, day) - 1) % WeekdayNames.Length;
            return WeekdayNames[weekdayIndex];
        }

        /// <summary>
        /// 현재 게임 시간을 5단계 화살표 인덱스로 변환합니다.
        /// </summary>
        private static int GetArrowIndex(int minuteOfDay)
        {
            float progress = Mathf.InverseLerp(DayStartMinute, ForcedSleepMinute - 1, minuteOfDay);
            return Mathf.Clamp(Mathf.FloorToInt(progress * ArrowCount), 0, ArrowCount - 1);
        }

        /// <summary>
        /// 계절 번호를 한글 계절 이름으로 변환합니다.
        /// </summary>
        private static string GetSeasonName(int season)
        {
            return season switch
            {
                1 => "여름",
                2 => "가을",
                3 => "겨울",
                _ => "봄",
            };
        }

        /// <summary>
        /// 텍스트 참조가 있을 때만 문자열을 설정합니다.
        /// </summary>
        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        /// <summary>
        /// 이미지 참조가 있을 때만 현재/최대값 비율을 fillAmount에 반영합니다.
        /// </summary>
        private static void SetFill(Image target, int current, int max)
        {
            if (target != null)
            {
                target.fillAmount = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            }
        }

        /// <summary>
        /// 그래픽 참조가 있을 때만 알파 값을 변경합니다.
        /// </summary>
        private static void SetAlpha(Graphic target, float alpha)
        {
            if (target == null)
            {
                return;
            }

            Color color = target.color;
            color.a = alpha;
            target.color = color;
        }
    }
}
