using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    [SerializeField] private AudioCatalog audioCatalog;
    [SerializeField] [Range(0f, 1f)] private float startVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float startFondoVolume = 0.35f;
    [SerializeField] [Range(0f, 1f)] private float menuMusicVolume = 0.42f;
    [SerializeField] private float menuMusicFadeDuration = 0.75f;
    [SerializeField] [Range(0f, 1f)] private float paintingAmbientVolume = 0.5f;
    [SerializeField] [Range(0f, 1f)] private float paintingLoopVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float paintingSecondaryVolume = 1f;
    [SerializeField] [Range(0f, 1f)] private float transitionVolume = 1f;

    private AudioSource startSource;
    private AudioSource startFondoSource;
    private AudioSource menuMusicSource;
    private AudioSource paintingAmbientSource;
    private AudioSource paintingLoopSource;
    private AudioSource paintingSecondarySource;
    private AudioSource transitionSource;
    private bool hasPlayedStartSound;
    private int activePaintGestureAudioRequests;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AudioManager>();

                if (instance == null)
                {
                    GameObject audioManagerObject = new GameObject(nameof(AudioManager));
                    instance = audioManagerObject.AddComponent<AudioManager>();
                }
            }

            return instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Bootstrap()
    {
        Instance.Initialize();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void PlayMenuAmbienceAfterInitialSceneLoad()
    {
        Instance.StartMenuAmbienceRetries();
    }

    void Awake()
    {
        Initialize();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        CursorInputRouter.Instance.Pressed += HandlePaintingStarted;
        CursorInputRouter.Instance.Released += HandlePaintingStopped;
        ScoreManager.PhaseChanged += HandlePhaseChanged;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (CursorInputRouter.HasInstance)
        {
            CursorInputRouter.Instance.Pressed -= HandlePaintingStarted;
            CursorInputRouter.Instance.Released -= HandlePaintingStopped;
        }

        ScoreManager.PhaseChanged -= HandlePhaseChanged;
    }

    public void PlayFinishSound()
    {
        PlayTransitionClip(audioCatalog != null ? audioCatalog.finishClip : null);
    }

    public void PlayResultsSound()
    {
        PlayTransitionClip(audioCatalog != null ? audioCatalog.resultsClip : null);
    }

    public void PlayVictorySound()
    {
        AudioClip clip = audioCatalog != null && audioCatalog.victoryClip != null
            ? audioCatalog.victoryClip
            : audioCatalog != null ? audioCatalog.resultsClip : null;

        PlayTransitionClip(clip);
    }

    public void PlayDefeatSound()
    {
        AudioClip clip = audioCatalog != null && audioCatalog.defeatClip != null
            ? audioCatalog.defeatClip
            : audioCatalog != null ? audioCatalog.resultsClip : null;

        PlayTransitionClip(clip);
    }

    public void PlayUIClickSound()
    {
        AudioClip clip = audioCatalog != null && audioCatalog.uiClickClip != null ? audioCatalog.uiClickClip : null;
        PlayTransitionClip(clip);
    }

    public void PlayMenuAmbience()
    {
        PlayMainMenuMusic();
    }

    public void StartMenuAmbienceRetries()
    {
        PlayMainMenuMusic();
    }

    public void PlayMainMenuMusic()
    {
        Initialize();
        AudioListener.pause = false;
        StopCoroutineSafe(nameof(FadeMenuMusicOutRoutine));
        StopCoroutineSafe(nameof(PlayMainMenuMusicWhenReady));
        StartCoroutine(PlayMainMenuMusicWhenReady());
    }

    public void StopMainMenuMusic()
    {
        StopCoroutineSafe(nameof(PlayMainMenuMusicWhenReady));
        StopCoroutineSafe(nameof(FadeMenuMusicOutRoutine));
        StartCoroutine(FadeMenuMusicOutRoutine(0.35f));
    }

    public void FadeGameplayAudio(float targetVolumeMultiplier, float duration)
    {
        StartCoroutine(FadeGameplayAudioRoutine(Mathf.Clamp01(targetVolumeMultiplier), Mathf.Max(0.01f, duration)));
    }

    public void PlayStartIntroSoundOnce()
    {
        if (hasPlayedStartSound)
        {
            return;
        }

        hasPlayedStartSound = true;
        PlayStartSound();
    }

    public void StopPaintingAudio()
    {
        activePaintGestureAudioRequests = 0;

        if (paintingAmbientSource != null)
            paintingAmbientSource.Stop();

        StopPaintGestureAudio();
    }

    public void StopAllGameplayAudio()
    {
        StopAllCoroutines();
        hasPlayedStartSound = false;
        activePaintGestureAudioRequests = 0;
        AudioListener.pause = false;

        StopSource(startSource);
        StopSource(startFondoSource);
        StopSource(menuMusicSource);
        StopSource(paintingAmbientSource);
        StopSource(paintingLoopSource);
        StopSource(paintingSecondarySource);
        StopSource(transitionSource);
    }

    public void PlayPaintGestureAudio()
    {
        BeginPaintGestureAudio();
    }

    public void BeginPaintGestureAudio()
    {
        if (!StartIntroAnimator.GameStarted || ScoreManager.CurrentPhase != ScoreManager.GamePhase.Painting || audioCatalog == null)
        {
            return;
        }

        activePaintGestureAudioRequests++;
        StopStartSound();

        if (audioCatalog.paintingLoopClip != null && paintingLoopSource != null && !paintingLoopSource.isPlaying)
        {
            paintingLoopSource.clip = audioCatalog.paintingLoopClip;
            paintingLoopSource.time = 0f;
            paintingLoopSource.Play();
        }

        if (audioCatalog.paintingSecondaryClip != null && paintingSecondarySource != null && !paintingSecondarySource.isPlaying)
        {
            paintingSecondarySource.clip = audioCatalog.paintingSecondaryClip;
            paintingSecondarySource.time = 0f;
            paintingSecondarySource.Play();
        }
    }

    public void StopPaintGestureAudioFromGame()
    {
        EndPaintGestureAudio();
    }

    public void EndPaintGestureAudio()
    {
        activePaintGestureAudioRequests = Mathf.Max(0, activePaintGestureAudioRequests - 1);

        if (activePaintGestureAudioRequests > 0)
        {
            return;
        }

        StopPaintGestureAudio();
    }

    private void StopPaintGestureAudio()
    {
        if (paintingLoopSource != null)
            paintingLoopSource.Stop();

        if (paintingSecondarySource != null)
            paintingSecondarySource.Stop();
    }

    private void StopCoroutineSafe(string coroutineName)
    {
        try
        {
            StopCoroutine(coroutineName);
        }
        catch
        {
            // The coroutine may not have been started yet.
        }
    }

    public void StopStartSound()
    {
        if (startSource != null)
            startSource.Stop();

        if (startFondoSource != null)
            startFondoSource.Stop();
    }

    private void Initialize()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (audioCatalog == null)
        {
            audioCatalog = Resources.Load<AudioCatalog>("AudioCatalog");
        }

        EnsureAudioSources();

        if (audioCatalog == null)
        {
            Debug.LogWarning("AudioManager could not load Resources/AudioCatalog. Transition sounds will stay silent until the catalog is assigned.");
            return;
        }

        if (audioCatalog.paintingLoopClip == null)
        {
            Debug.LogWarning("AudioManager is missing the painting loop clip. Add loop.mp3 to the AudioCatalog asset to enable the held painting loop.");
        }

        if (audioCatalog.startClip == null)
        {
            Debug.LogWarning("AudioManager is missing the start clip. Add start.mp3 to the AudioCatalog asset to enable the intro sound.");
        }

        if (audioCatalog.startFondoClip == null)
        {
            Debug.LogWarning("AudioManager is missing the start fondo clip. Add startfondo.mp3 to the AudioCatalog asset to enable the intro background accent.");
        }

        if (audioCatalog.paintingAmbientClip == null)
        {
            Debug.LogWarning("AudioManager is missing the painting ambient clip. Add sonido fondo.mp3 to the AudioCatalog asset to enable the post-start ambience.");
        }
    }

    private void EnsureAudioSources()
    {
        if (startSource == null)
        {
            startSource = CreateConfiguredSource("StartSource", false, startVolume);
        }

        if (startFondoSource == null)
        {
            startFondoSource = CreateConfiguredSource("StartFondoSource", false, startFondoVolume);
        }

        if (paintingAmbientSource == null)
        {
            paintingAmbientSource = CreateConfiguredSource("PaintingAmbientSource", true, paintingAmbientVolume);
        }

        if (menuMusicSource == null)
        {
            menuMusicSource = CreateConfiguredSource("MainMenuMusicSource", true, 0f);
        }

        if (paintingLoopSource == null)
        {
            paintingLoopSource = CreateConfiguredSource("PaintingLoopSource", true, paintingLoopVolume);
        }

        if (paintingSecondarySource == null)
        {
            paintingSecondarySource = CreateConfiguredSource("PaintingSecondarySource", false, paintingSecondaryVolume);
        }

        if (transitionSource == null)
        {
            transitionSource = CreateConfiguredSource("TransitionSource", false, transitionVolume);
        }
    }

    private void StopSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.Stop();
        source.clip = null;
    }

    private System.Collections.IEnumerator FadeGameplayAudioRoutine(float targetVolumeMultiplier, float duration)
    {
        float elapsed = 0f;
        float ambientStart = paintingAmbientSource != null ? paintingAmbientSource.volume : 0f;
        float loopStart = paintingLoopSource != null ? paintingLoopSource.volume : 0f;
        float secondaryStart = paintingSecondarySource != null ? paintingSecondarySource.volume : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (paintingAmbientSource != null)
                paintingAmbientSource.volume = Mathf.Lerp(ambientStart, paintingAmbientVolume * targetVolumeMultiplier, t);

            if (paintingLoopSource != null)
                paintingLoopSource.volume = Mathf.Lerp(loopStart, paintingLoopVolume * targetVolumeMultiplier, t);

            if (paintingSecondarySource != null)
                paintingSecondarySource.volume = Mathf.Lerp(secondaryStart, paintingSecondaryVolume * targetVolumeMultiplier, t);

            yield return null;
        }
    }

    private AudioSource CreateConfiguredSource(string sourceName, bool shouldLoop, float volume)
    {
        GameObject sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);

        AudioSource audioSource = sourceObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = shouldLoop;
        audioSource.volume = volume;
        audioSource.spatialBlend = 0f;
        return audioSource;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode _)
    {
        hasPlayedStartSound = false;
        StopStartSound();

        if (!MainMenuUI.HasStartedFromMainMenu)
        {
            PlayMainMenuMusic();
        }
    }

    private void HandlePaintingStarted(Vector3 _)
    {
        if (!StartIntroAnimator.GameStarted || ScoreManager.CurrentPhase != ScoreManager.GamePhase.Painting || audioCatalog == null)
        {
            return;
        }

        BeginPaintGestureAudio();
    }

    private void HandlePaintingStopped(Vector3 _)
    {
        EndPaintGestureAudio();
    }

    private void HandlePhaseChanged(ScoreManager.GamePhase newPhase)
    {
        if (newPhase != ScoreManager.GamePhase.Painting)
        {
            StopStartSound();
            StopPaintingAudio();
        }
    }

    private void PlayStartSound()
    {
        if (audioCatalog == null || startSource == null || startFondoSource == null)
        {
            return;
        }

        AudioListener.pause = false;
        startSource.Stop();
        startFondoSource.Stop();
        startSource.volume = startVolume;
        startFondoSource.volume = startFondoVolume;
        startSource.spatialBlend = 0f;
        startFondoSource.spatialBlend = 0f;

        if (audioCatalog.startFondoClip != null)
        {
            if (audioCatalog.startFondoClip.loadState == AudioDataLoadState.Unloaded)
            {
                audioCatalog.startFondoClip.LoadAudioData();
            }

            startFondoSource.PlayOneShot(audioCatalog.startFondoClip, startFondoVolume);
        }

        if (audioCatalog.startClip != null)
        {
            if (audioCatalog.startClip.loadState == AudioDataLoadState.Unloaded)
            {
                audioCatalog.startClip.LoadAudioData();
            }

            startSource.PlayOneShot(audioCatalog.startClip, startVolume);
        }

        StartCoroutine(PlayPaintingAmbienceAfterStartIntro());
    }

    private System.Collections.IEnumerator PlayPaintingAmbienceAfterStartIntro()
    {
        float delay = 0f;

        if (audioCatalog.startClip != null)
        {
            delay = Mathf.Max(delay, audioCatalog.startClip.length);
        }

        yield return new WaitForSeconds(delay);

        if (!hasPlayedStartSound || ScoreManager.CurrentPhase != ScoreManager.GamePhase.Painting)
        {
            yield break;
        }

        PlayPaintingAmbience();
    }

    private System.Collections.IEnumerator PlayMenuAmbienceWhenReady()
    {
        yield return PlayMainMenuMusicWhenReady();
    }

    private System.Collections.IEnumerator PlayMainMenuMusicWhenReady()
    {
        for (int i = 0; i < 60; i++)
        {
            Initialize();
            AudioListener.pause = false;

            if (audioCatalog != null && audioCatalog.paintingAmbientClip != null && menuMusicSource != null)
            {
                if (audioCatalog.paintingAmbientClip.loadState == AudioDataLoadState.Unloaded)
                {
                    audioCatalog.paintingAmbientClip.LoadAudioData();
                }

                if (audioCatalog.paintingAmbientClip.loadState == AudioDataLoadState.Loaded || i > 5)
                {
                    PlayMenuMusicClip();
                    yield break;
                }
            }

            yield return null;
        }
    }

    private void PlayMenuMusicClip()
    {
        if (audioCatalog == null || audioCatalog.paintingAmbientClip == null || menuMusicSource == null)
        {
            return;
        }

        if (menuMusicSource.isPlaying && menuMusicSource.clip == audioCatalog.paintingAmbientClip)
        {
            return;
        }

        menuMusicSource.Stop();
        menuMusicSource.clip = audioCatalog.paintingAmbientClip;
        menuMusicSource.loop = true;
        menuMusicSource.playOnAwake = false;
        menuMusicSource.spatialBlend = 0f;
        menuMusicSource.volume = 0f;
        menuMusicSource.Play();
        StartCoroutine(FadeMenuMusicInRoutine());
    }

    private System.Collections.IEnumerator FadeMenuMusicInRoutine()
    {
        if (menuMusicSource == null)
        {
            yield break;
        }

        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, menuMusicFadeDuration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            menuMusicSource.volume = Mathf.Lerp(0f, menuMusicVolume, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        menuMusicSource.volume = menuMusicVolume;
    }

    private System.Collections.IEnumerator FadeMenuMusicOutRoutine(float duration)
    {
        if (menuMusicSource == null || !menuMusicSource.isPlaying)
        {
            yield break;
        }

        float elapsed = 0f;
        float startVolume = menuMusicSource.volume;
        duration = Mathf.Max(0.01f, duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            menuMusicSource.volume = Mathf.Lerp(startVolume, 0f, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        menuMusicSource.Stop();
        menuMusicSource.volume = menuMusicVolume;
    }

    private void PlayPaintingAmbience()
    {
        if (audioCatalog == null || audioCatalog.paintingAmbientClip == null || paintingAmbientSource == null)
        {
            return;
        }

        paintingAmbientSource.volume = paintingAmbientVolume;
        paintingAmbientSource.spatialBlend = 0f;

        if (paintingAmbientSource.isPlaying)
        {
            return;
        }

        if (audioCatalog.paintingAmbientClip.loadState == AudioDataLoadState.Unloaded)
        {
            audioCatalog.paintingAmbientClip.LoadAudioData();
        }

        paintingAmbientSource.clip = audioCatalog.paintingAmbientClip;
        paintingAmbientSource.Play();
    }

    private void PlayTransitionClip(AudioClip clip)
    {
        if (clip == null || transitionSource == null)
        {
            return;
        }

        // Reusing one transition source prevents duplicated finish / results playback.
        transitionSource.Stop();
        transitionSource.clip = clip;
        transitionSource.Play();
    }
}
