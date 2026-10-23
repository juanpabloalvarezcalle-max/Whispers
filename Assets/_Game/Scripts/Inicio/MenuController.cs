using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;

public class MenuController : MonoBehaviour
{
    public GameObject SinNigromante;
    public GameObject ConNigromante;
    public string sceneToLoad = "JuegoBase";
    public float introDuration = 1f;
    [SerializeField] private RectTransform panelCreditos;
    [SerializeField] private float duracionAnimacionPanel = 0.3f;

    [Header("Audio")]
    [SerializeField] private float pitchSonidoCriatura = 2.7f;
    [SerializeField] private bool detenerMusicaAlJugar = true;
    private Vector2 posCreditosVisible;
    private Coroutine animacionCreditosActual;
    private VideoPlayer vpCon;
    private bool isPlaying = false;

    void Start()
    {
        SinNigromante.SetActive(true);
        ConNigromante.SetActive(false);

        vpCon = ConNigromante.GetComponentInChildren<VideoPlayer>(true);
        if (vpCon != null) vpCon.playOnAwake = false;

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
        StartCoroutine(WaitAndLoadScene());
    }

    private IEnumerator WaitAndLoadScene()
    {
        yield return StartCoroutine(CambiarAVideoNigromante());

        yield return new WaitForSeconds(introDuration);

        if (TransitionManager.Instance != null)
            TransitionManager.Instance.LoadLevel(sceneToLoad);
        else
            SceneManager.LoadScene(sceneToLoad);
    }

    private IEnumerator CambiarAVideoNigromante()
    {
        // Se activa detrás del video actual, que sigue visible mientras tanto
        ConNigromante.SetActive(true);

        if (vpCon != null)
        {
            vpCon.Prepare();
            while (!vpCon.isPrepared) yield return null;

            vpCon.Play();
            while (vpCon.frame < 1) yield return null; // espera al primer frame real
            yield return null;
        }

        // Recién ahora se apaga el primero: ya no queda ningún hueco
        SinNigromante.SetActive(false);
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