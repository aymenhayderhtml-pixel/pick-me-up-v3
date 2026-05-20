/*
 * AudioManager.cs
 * Attach to: _GameManager GameObject in the Bootstrap scene
 * Provides: Background Music (BGM) crossfading and global Sound Effect (SFX) triggers
 */

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("BGM Channels")]
    [SerializeField] private AudioSource BGMChannelA;
    [SerializeField] private AudioSource BGMChannelB;

    [Header("SFX Channel")]
    [SerializeField] private AudioSource SFXChannel;

    [Header("BGM Tracks")]
    [SerializeField] private AudioClip lobbyTheme;
    [SerializeField] private AudioClip summonTheme;
    [SerializeField] private AudioClip battleTheme;
    [SerializeField] private AudioClip memorialTheme;

    [Header("SFX Clips")]
    [SerializeField] private AudioClip clickClip;
    [SerializeField] private AudioClip summonPullClip;
    [SerializeField] private AudioClip synthesisSuccessClip;
    [SerializeField] private AudioClip combatAttackClip;
    [SerializeField] private AudioClip deathClip;
    [SerializeField] private AudioClip claimRewardClip;

    private AudioSource _activeBGMChannel;
    private Coroutine _crossfadeCoroutine;

    private float _musicVolume = 0.8f;
    private float _sfxVolume = 0.8f;

    public float MusicVolume => _musicVolume;
    public float SfxVolume => _sfxVolume;

    // ─── Unity Lifecycle ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeAudioSources();
        LoadSettings();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // ─── Initialization ───────────────────────────────────────────────────────

    private void InitializeAudioSources()
    {
        // If not assigned, programmatically create them as children
        if (BGMChannelA == null) BGMChannelA = CreateSubChannel("BGMChannelA", true);
        if (BGMChannelB == null) BGMChannelB = CreateSubChannel("BGMChannelB", true);
        if (SFXChannel == null) SFXChannel = CreateSubChannel("SFXChannel", false);

        BGMChannelA.volume = 0f;
        BGMChannelB.volume = 0f;

        _activeBGMChannel = BGMChannelA;
    }

    private AudioSource CreateSubChannel(string channelName, bool loop)
    {
        GameObject child = new GameObject(channelName);
        child.transform.SetParent(transform, false);
        AudioSource src = child.AddComponent<AudioSource>();
        src.loop = loop;
        src.playOnAwake = false;
        return src;
    }

    // ─── Scene Sound Track Binding ────────────────────────────────────────────

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AudioClip targetTrack = GetTrackForScene(scene.name);
        if (targetTrack != null)
        {
            TriggerBGMChange(targetTrack);
        }
    }

    private AudioClip GetTrackForScene(string sceneName)
    {
        return sceneName switch
        {
            SceneLoader.SCENE_LOBBY       => lobbyTheme,
            SceneLoader.SCENE_ROSTER      => lobbyTheme,
            SceneLoader.SCENE_SYNTHESIS   => lobbyTheme,
            SceneLoader.SCENE_SUMMON      => summonTheme,
            SceneLoader.SCENE_BATTLE      => battleTheme,
            SceneLoader.SCENE_MEMORIAL    => memorialTheme,
            _                             => null
        };
    }

    // ─── BGM Crossfading ──────────────────────────────────────────────────────

    private void TriggerBGMChange(AudioClip newTrack)
    {
        if (_activeBGMChannel.clip == newTrack && _activeBGMChannel.isPlaying)
            return;

        if (_crossfadeCoroutine != null)
        {
            StopCoroutine(_crossfadeCoroutine);
        }

        _crossfadeCoroutine = StartCoroutine(CrossfadeTo(newTrack));
    }

    private IEnumerator CrossfadeTo(AudioClip newTrack)
    {
        AudioSource fadingOut = _activeBGMChannel;
        AudioSource fadingIn = (_activeBGMChannel == BGMChannelA) ? BGMChannelB : BGMChannelA;

        fadingIn.clip = newTrack;
        fadingIn.volume = 0f;

        if (newTrack != null)
        {
            fadingIn.Play();
        }

        float elapsed = 0f;
        float duration = 1.0f; // 1-second smooth BGM crossfade

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float ratio = elapsed / duration;

            fadingOut.volume = Mathf.Lerp(_musicVolume, 0f, ratio);
            fadingIn.volume = Mathf.Lerp(0f, _musicVolume, ratio);

            yield return null;
        }

        fadingOut.volume = 0f;
        fadingOut.Stop();

        _activeBGMChannel = fadingIn;
        _crossfadeCoroutine = null;
    }

    // ─── Global SFX API ───────────────────────────────────────────────────────

    public void PlayClip(AudioClip clip)
    {
        if (clip == null || SFXChannel == null) return;
        SFXChannel.PlayOneShot(clip, _sfxVolume);
    }

    public static void PlayClick()
    {
        if (Instance != null) Instance.PlayClip(Instance.clickClip);
    }

    public static void PlaySummonPull()
    {
        if (Instance != null) Instance.PlayClip(Instance.summonPullClip);
    }

    public static void PlaySynthesisSuccess()
    {
        if (Instance != null) Instance.PlayClip(Instance.synthesisSuccessClip);
    }

    public static void PlayCombatAttack()
    {
        if (Instance != null) Instance.PlayClip(Instance.combatAttackClip);
    }

    public static void PlayDeath()
    {
        if (Instance != null) Instance.PlayClip(Instance.deathClip);
    }

    public static void PlayClaimReward()
    {
        if (Instance != null) Instance.PlayClip(Instance.claimRewardClip);
    }

    // ─── Settings Persistence ─────────────────────────────────────────────────

    public void SetMusicVolume(float vol)
    {
        _musicVolume = Mathf.Clamp01(vol);
        _activeBGMChannel.volume = _musicVolume;
        PlayerPrefs.SetFloat("AudioManager_MusicVolume", _musicVolume);
    }

    public void SetSfxVolume(float vol)
    {
        _sfxVolume = Mathf.Clamp01(vol);
        PlayerPrefs.SetFloat("AudioManager_SfxVolume", _sfxVolume);
    }

    private void LoadSettings()
    {
        _musicVolume = PlayerPrefs.GetFloat("AudioManager_MusicVolume", 0.8f);
        _sfxVolume = PlayerPrefs.GetFloat("AudioManager_SfxVolume", 0.8f);
    }
}
