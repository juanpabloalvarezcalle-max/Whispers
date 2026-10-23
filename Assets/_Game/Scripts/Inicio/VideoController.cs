using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoCutsceneController : MonoBehaviour
{
    [Header("Configuración de Escena")]
    [SerializeField] private string nextSceneName = "Room_01";

    [Header("Referencias UI")]
    [SerializeField] private Button skipButton;

    private VideoPlayer videoPlayer;
    private bool isTransitioning = false;

    void Awake()
    {
        videoPlayer = GetComponent<VideoPlayer>();
    }

    void Start()
    {
        if (videoPlayer != null)
        {
            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.loopPointReached += OnVideoEnd;
            videoPlayer.Play();
        }

        // 2. Configurar evento del botón Skip
        if (skipButton != null)
        {
            // Limpia eventos anteriores para evitar duplicados
            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(SkipCutscene);
        }
        else
        {
            Debug.LogWarning("¡Atención! El SkipButton no está asignado en el Inspector.");
        }
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogError($"[VideoCutscene] Error de reproducción: {message}");
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnd;
            videoPlayer.errorReceived -= OnVideoError;
        }
    }

    public void SkipCutscene()
    {
        Debug.Log("Botón Skip presionado.");
        GoToNextLevel();
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        Debug.Log("El video ha finalizado automáticamente.");
        GoToNextLevel();
    }

    private void GoToNextLevel()
    {
        // Evita dobles llamadas
        if (isTransitioning) return;
        isTransitioning = true;

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoEnd;
            videoPlayer.Stop();
        }

        // Ejecutar transición
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.LoadLevel(nextSceneName);
        }
        else
        {
            Debug.LogWarning("TransitionManager.Instance es nulo. Cargando escena directamente.");
            UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
        }
    }
}