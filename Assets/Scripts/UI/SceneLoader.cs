using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles all scene transitions with a fade in/out effect.
/// Attach to a GameObject in the Bootstrap scene.
/// Has a full-screen black Image child that fades.
/// Call SceneLoader.LoadScene("SceneName") from anywhere.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] Image fadeImage;           // black full-screen Image
    [SerializeField] float fadeDuration = 0.3f;

    // Scene name constants — match these exactly in Unity Build Settings
    public const string SCENE_BOOTSTRAP      = "Bootstrap";
    public const string SCENE_LOBBY          = "Lobby";
    public const string SCENE_ROSTER         = "Roster";
    public const string SCENE_SUMMON         = "Summon";
    public const string SCENE_SQUAD_FORM     = "SquadFormation";
    public const string SCENE_BATTLE         = "Battle";
    public const string SCENE_RESULTS        = "Results";
    public const string SCENE_MEMORIAL       = "Memorial";
    public const string SCENE_SYNTHESIS      = "Synthesis";

    // ─────────────────────────────────────────────────────
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Start fully black, then fade in
        if (fadeImage != null)
        {
            fadeImage.color = Color.black;
            StartCoroutine(FadeIn());
        }
    }

    // ─────────────────────────────────────────────────────
    // PUBLIC API
    // ─────────────────────────────────────────────────────
    public static void LoadScene(string sceneName)
    {
        if (Instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }
        Instance.StartCoroutine(Instance.Transition(sceneName));
    }

    // Shortcut helpers
    public static void GoToLobby()        => LoadScene(SCENE_LOBBY);
    public static void GoToRoster()       => LoadScene(SCENE_ROSTER);
    public static void GoToSummon()       => LoadScene(SCENE_SUMMON);
    public static void GoToSquadForm()    => LoadScene(SCENE_SQUAD_FORM);
    public static void GoToBattle()       => LoadScene(SCENE_BATTLE);
    public static void GoToResults()      => LoadSceneIfAvailable(SCENE_RESULTS, SCENE_LOBBY);
    public static void GoToMemorial()     => LoadScene(SCENE_MEMORIAL);
    public static void GoToSynthesis()    => LoadScene(SCENE_SYNTHESIS);

    private static void LoadSceneIfAvailable(string sceneName, string fallbackScene)
    {
        if (IsSceneInBuildSettings(sceneName))
        {
            LoadScene(sceneName);
            return;
        }

        Debug.LogWarning($"[SceneLoader] Scene '{sceneName}' is not present in Build Settings. Falling back to '{fallbackScene}'.");
        if (IsSceneInBuildSettings(fallbackScene))
        {
            LoadScene(fallbackScene);
        }
    }

    private static bool IsSceneInBuildSettings(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return false;
        return SceneManager.sceneCountInBuildSettings > 0 &&
               Enumerable.Range(0, SceneManager.sceneCountInBuildSettings)
                   .Select(SceneUtility.GetScenePathByBuildIndex)
                   .Any(path => !string.IsNullOrWhiteSpace(path) &&
                                System.IO.Path.GetFileNameWithoutExtension(path) == sceneName);
    }

    // ─────────────────────────────────────────────────────
    // TRANSITION COROUTINE
    // ─────────────────────────────────────────────────────
    IEnumerator Transition(string sceneName)
    {
        if (fadeImage != null)
            yield return StartCoroutine(FadeOut());
        else
            yield return null;

        yield return SceneManager.LoadSceneAsync(sceneName);

        if (fadeImage != null)
            yield return StartCoroutine(FadeIn());
    }

    IEnumerator FadeOut()
    {
        if (fadeImage == null) yield break;
        float t = 0f;
        Color c = fadeImage.color;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = Mathf.Clamp01(t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 1f;
        fadeImage.color = c;
    }

    IEnumerator FadeIn()
    {
        if (fadeImage == null) yield break;
        float t = 0f;
        Color c = fadeImage.color;
        c.a = 1f;
        fadeImage.color = c;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            c.a = 1f - Mathf.Clamp01(t / fadeDuration);
            fadeImage.color = c;
            yield return null;
        }
        c.a = 0f;
        fadeImage.color = c;
    }
}
