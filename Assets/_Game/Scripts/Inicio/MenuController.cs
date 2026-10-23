using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    public GameObject SinNigromante;
    public GameObject ConNigromante;
    public string sceneToLoad = "JuegoBase";
    public float introDuration = 1f;
    [SerializeField] private RectTransform panelCreditos;
    [SerializeField] private float duracionAnimacionPanel = 0.3f;

    [Header("Audio")]
    [SerializeField] private AudioClip clipMusicaMenu;
    [SerializeField] private AudioClip clipSonidoCriatura;
    [SerializeField] private float pitchSonidoCriatura = 2.7f;
    [SerializeField] private bool detenerMusicaAlJugar = true;

    private Vector2 posConfirmacionVisible;
    private Vector2 posCreditosVisible;
    private Coroutine animacionConfirmacionActual;
    private Coroutine animacionCreditosActual;

    private bool isPlaying = false;

    void Start()
    {
        SinNigromante.SetActive(true);
        ConNigromante.SetActive(false);

        if (panelCreditos != null)
        {
            posCreditosVisible = panelCreditos.anchoredPosition;
            panelCreditos.gameObject.SetActive(false);
        }
    }

    public void OnPlayButtonPressed()
    {
        if (isPlaying) return;
        isPlaying = true;

        SinNigromante.SetActive(false);
        ConNigromante.SetActive(true);

        StartCoroutine(WaitAndLoadScene());
    }

    private IEnumerator WaitAndLoadScene()
    {
        yield return new WaitForSeconds(introDuration);

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadLevel(sceneToLoad);
        else
            SceneManager.LoadScene(sceneToLoad);
    }

    // ---------- Panel de créditos ----------

    public void MostrarCreditos()
    {
        MostrarPanel(panelCreditos, posCreditosVisible, ref animacionCreditosActual);
    }

    public void OcultarCreditos()
    {
        OcultarPanel(panelCreditos, posCreditosVisible, ref animacionCreditosActual);
    }

    // ---------- Animación genérica (de abajo hacia arriba) ----------

    private void MostrarPanel(RectTransform panel, Vector2 posVisible, ref Coroutine animacionActual)
    {
        if (panel == null) return;
        if (animacionActual != null) StopCoroutine(animacionActual);

        panel.gameObject.SetActive(true);
        Vector2 posOculta = posVisible + Vector2.down * (panel.rect.height + 50f);
        animacionActual = StartCoroutine(AnimarPanel(panel, posOculta, posVisible, desactivarAlTerminar: false));
    }

    private void OcultarPanel(RectTransform panel, Vector2 posVisible, ref Coroutine animacionActual)
    {
        if (panel == null) return;
        if (animacionActual != null) StopCoroutine(animacionActual);

        Vector2 posOculta = posVisible + Vector2.down * (panel.rect.height + 50f);
        animacionActual = StartCoroutine(AnimarPanel(panel, panel.anchoredPosition, posOculta, desactivarAlTerminar: true));
    }

    private IEnumerator AnimarPanel(RectTransform panel, Vector2 desde, Vector2 hasta, bool desactivarAlTerminar)
    {
        float t = 0f;
        panel.anchoredPosition = desde;

        while (t < duracionAnimacionPanel)
        {
            t += Time.unscaledDeltaTime; // tiempo real, funciona aunque el juego esté pausado
            float progreso = Mathf.SmoothStep(0f, 1f, t / duracionAnimacionPanel);
            panel.anchoredPosition = Vector2.Lerp(desde, hasta, progreso);
            yield return null;
        }

        panel.anchoredPosition = hasta;

        if (desactivarAlTerminar)
            panel.gameObject.SetActive(false);
    }
}