using UnityEngine;

namespace HorseRanch
{
    public enum BgmId { None = -1, Title = 0, Town = 1, Race = 2, Hall = 3, Stadium = 4, GameOver = 5, Ending = 6 }
    public enum SfxId { Click = 0, Coin = 1, Door = 2, Search = 3, Fanfare = 4, Gate = 5, Trash = 6, Cheer = 7, Neigh = 8, Snort = 9, Shout = 10, Boo = 11, Warning = 12, Step = 13, Hoof = 14, WindowOpen = 15, WindowClose = 16 }
    /// <summary>배경음 위에 겹치는 환경음 (반복)</summary>
    public enum AmbienceId { None = -1, HallCrowd = 0, StadiumCrowd = 1 }

    /// <summary>
    /// 배경음(1채널, 반복) + 환경음(1채널, 반복) + 효과음(PlayOneShot).
    /// 싱글톤 없이 GameUI · TitleView가 인스펙터 참조로 사용한다.
    /// 볼륨은 GameSettings(PlayerPrefs)에서 읽고, 설정 창에서 바꾸면 ApplyVolume으로 즉시 반영. 환경음은 효과음 볼륨을 따른다.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        [Header("오디오 소스")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioSource ambienceSource;

        [Header("배경음 (BgmId 순서: 타이틀, 마을, 경주, 도박장, 경기장, 게임 오버, 엔딩)")]
        [SerializeField] private AudioClip[] bgmClips;
        [Header("효과음 (SfxId 순서: 클릭, 동전, 문, 뒤지기, 팡파레, 출발 벨, 쓰레기, 함성, 말 울음, 콧김, 고함, 야유, 경고, 발소리, 말발굽, 창 열기, 창 닫기)")]
        [SerializeField] private AudioClip[] sfxClips;
        [Header("같은 소리 반복이 기계적으로 들리지 않게: 변형 클립 (기본 클립과 섞어 무작위 재생)")]
        [SerializeField] private AudioClip[] cheerVariants;
        [SerializeField] private AudioClip[] booVariants;
        [SerializeField] private AudioClip[] shoutVariants;
        [Tooltip("사람 · 말 · 발소리는 재생할 때마다 음높이를 이만큼 무작위로 (0.08 = ±8%)")]
        [SerializeField] [Range(0f, 0.3f)] private float pitchVariance = 0.08f;
        [Tooltip("음높이를 바꿔 동시에 낼 수 있는 소리 수")]
        [SerializeField] [Range(1, 8)] private int variedVoices = 4;
        [Header("환경음 (AmbienceId 순서: 도박장 웅성임, 경기장 관중)")]
        [SerializeField] private AudioClip[] ambienceClips;

        [Tooltip("배경음 전환 시 페이드 시간 (초)")]
        [SerializeField] [Range(0f, 2f)] private float fadeTime = 0.4f;

        private BgmId current = BgmId.None;
        private BgmId pending = BgmId.None;
        private float fade = 1f;        // 0~1, 현재 곡 페이드 배율
        private bool fadingOut;
        private AmbienceId ambience = AmbienceId.None;
        private float ambienceLevel = 1f;   // 환경음 크기 배율 (경주 중엔 크게)
        private float ambienceNow;

        public BgmId Current { get { return current; } }
        public AmbienceId CurrentAmbience { get { return ambience; } }

        private void Awake()
        {
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            sfxSource.playOnAwake = false;
            ambienceSource.loop = true;
            ambienceSource.playOnAwake = false;
            ApplyVolume();
        }

        /// <summary>설정값(PlayerPrefs)을 볼륨에 반영</summary>
        public void ApplyVolume()
        {
            bgmSource.volume = GameSettings.BgmVolume * fade;
            sfxSource.volume = GameSettings.SfxVolume;
            ambienceSource.volume = GameSettings.SfxVolume * ambienceNow;
        }

        public void PlayBgm(BgmId id)
        {
            if (id == current && !fadingOut) return;
            pending = id;
            if (current == BgmId.None || fadeTime <= 0f) { SwitchNow(); return; }
            fadingOut = true;
        }

        /// <summary>환경음 바꾸기. level = 크기 배율 (0~1.5)</summary>
        public void PlayAmbience(AmbienceId id, float level)
        {
            ambienceLevel = level;
            if (id == ambience) return;
            ambience = id;
            int i = (int)id;
            if (id == AmbienceId.None || ambienceClips == null || i >= ambienceClips.Length || ambienceClips[i] == null) { ambienceSource.Stop(); return; }
            ambienceSource.clip = ambienceClips[i];
            ambienceSource.Play();
            ambienceNow = 0f;
        }

        public void PlaySfx(SfxId id) { PlaySfx(id, 1f); }

        public void PlaySfx(SfxId id, float volumeScale)
        {
            int i = (int)id;
            if (sfxClips == null || i >= sfxClips.Length || sfxClips[i] == null) return;
            if (!IsVaried(id)) { sfxSource.PlayOneShot(sfxClips[i], volumeScale); return; }

            // 사람 · 말 · 발소리: 변형 클립 중 하나 + 음높이 살짝 다르게 (여러 소스를 돌려 써서 겹쳐도 서로 영향 없음)
            var clip = PickVariant(id, sfxClips[i]);
            var src = NextVariedSource();
            src.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            src.volume = GameSettings.SfxVolume;
            src.PlayOneShot(clip, volumeScale);
        }

        private AudioSource[] variedSources;
        private int variedNext;

        private static bool IsVaried(SfxId id)
        {
            return id == SfxId.Cheer || id == SfxId.Boo || id == SfxId.Shout || id == SfxId.Neigh || id == SfxId.Snort || id == SfxId.Step || id == SfxId.Hoof;
        }

        private AudioClip PickVariant(SfxId id, AudioClip main)
        {
            var arr = id == SfxId.Cheer ? cheerVariants : id == SfxId.Boo ? booVariants : id == SfxId.Shout ? shoutVariants : null;
            if (arr == null || arr.Length == 0) return main;
            int k = Random.Range(0, arr.Length + 1);
            return k == arr.Length || arr[k] == null ? main : arr[k];
        }

        /// <summary>음높이 변주용 소스 (처음 쓸 때 sfxSource 밑에 한 번만 만든다)</summary>
        private AudioSource NextVariedSource()
        {
            if (variedSources == null)
            {
                variedSources = new AudioSource[Mathf.Max(1, variedVoices)];
                for (int k = 0; k < variedSources.Length; k++)
                {
                    var s = sfxSource.gameObject.AddComponent<AudioSource>();
                    s.playOnAwake = false;
                    variedSources[k] = s;
                }
            }
            var src = variedSources[variedNext];
            variedNext = (variedNext + 1) % variedSources.Length;
            return src;
        }

        private void Update()
        {
            if (fadingOut)
            {
                fade -= Time.unscaledDeltaTime / fadeTime;
                if (fade <= 0f) { fade = 0f; fadingOut = false; SwitchNow(); }
            }
            else if (fade < 1f) fade = Mathf.Min(1f, fade + Time.unscaledDeltaTime / Mathf.Max(0.01f, fadeTime));
            bgmSource.volume = GameSettings.BgmVolume * fade;

            // 환경음은 부드럽게 목표 크기로
            float target = ambience == AmbienceId.None ? 0f : ambienceLevel;
            ambienceNow = Mathf.MoveTowards(ambienceNow, target, Time.unscaledDeltaTime * 1.5f);
            ambienceSource.volume = GameSettings.SfxVolume * ambienceNow;
        }

        private void SwitchNow()
        {
            current = pending;
            int i = (int)current;
            if (current == BgmId.None || bgmClips == null || i >= bgmClips.Length || bgmClips[i] == null) { bgmSource.Stop(); return; }
            bgmSource.clip = bgmClips[i];
            bgmSource.Play();
            fade = fadeTime > 0f ? 0f : 1f;
        }
    }
}
