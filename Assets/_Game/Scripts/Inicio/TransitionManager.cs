using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("Ajustes")]
    [SerializeField] private float duracionFadeOut = 0.5f;   // pantalla se oscurece
    [SerializeField] private float duracionFadeIn = 0.5f;    // pantalla se aclara
    [SerializeField] private float esperaMinimaNegro = 0.1f; // pausa en negro
    [SerializeField] private Color colorTransicion = Color.black;

    private CanvasGroup canvasGroup;
    private bool cargando = false;

    // Se crea automáticamente al iniciar, antes de cargar la primera escena
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Crear()
    {
        if (Instance != null) return;
        GameObject go = new GameObject("TransitionManager");
        go.AddComponent<TransitionManager>();
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        ConstruirCanvas();
    }

    void OnEnable() { SceneManager.sceneLoaded += AlCargarEscena; }
    void OnDisable() { SceneManager.sceneLoaded -= AlCargarEscena; }

    // ---------- API pública ----------

    public void LoadLevel(string nombreEscena)
    {
        if (cargando) return;
        StartCoroutine(Transicion(nombreEscena, -1));
    }

    public void LoadLevel(int indiceEscena)
    {
        if (cargando) return;
        StartCoroutine(Transicion(null, indiceEscena));
    }

    public void ReloadLevel()
    {
        LoadLevel(SceneManager.GetActiveScene().buildIndex);
    }

    // ---------- Lógica ----------

    private IEnumerator Transicion(string nombre, int indice)
    {
        cargando = true;

        // Bloquea clics mientras dura la transición
        canvasGroup.blocksRaycasts = true;

        yield return Fade(0f, 1f, duracionFadeOut);

        // Carga en segundo plano
        AsyncOperation op = !string.IsNullOrEmpty(nombre)
            ? SceneManager.LoadSceneAsync(nombre)
            : SceneManager.LoadSceneAsync(indice);

        if (op == null)
        {
            Debug.LogError("TransitionManager: no se pudo cargar la escena. ¿Está en Build Settings?");
            yield return Fade(1f, 0f, duracionFadeIn);
            canvasGroup.blocksRaycasts = false;
            cargando = false;
            yield break;
        }

        while (!op.isDone)
            yield return null;

        yield return new WaitForSecondsRealtime(esperaMinimaNegro);
        // El fade in lo dispara AlCargarEscena
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        // Evita fade in innecesario en el arranque del juego
        if (!cargando) return;
        StartCoroutine(TerminarTransicion());
    }

    private IEnumerator TerminarTransicion()
    {
        yield return Fade(1f, 0f, duracionFadeIn);
        canvasGroup.blocksRaycasts = false;
        cargando = false;
    }

    private IEnumerator Fade(float desde, float hasta, float duracion)
    {
        float t = 0f;
        canvasGroup.alpha = desde;

        while (t < duracion)
        {
            t += Time.unscaledDeltaTime; // funciona aunque Time.timeScale = 0
            canvasGroup.alpha = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / duracion));
            yield return null;
        }

        canvasGroup.alpha = hasta;
    }

    // ---------- Construcción del Canvas ----------

    private void ConstruirCanvas()
    {
        GameObject canvasGO = new GameObject("TransitionCanvas");
        canvasGO.transform.SetParent(transform);

        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999; // encima de todo

        canvasGO.AddComponent<CanvasScaler>();
        canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        GameObject imgGO = new GameObject("Fondo");
        imgGO.transform.SetParent(canvasGO.transform, false);

        Image img = imgGO.AddComponent<Image>();
        img.color = colorTransicion;
        img.raycastTarget = true;

        RectTransform rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}