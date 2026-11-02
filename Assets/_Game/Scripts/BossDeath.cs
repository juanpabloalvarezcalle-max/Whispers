using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BossDeath : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private EnemyHealth vida;

    [Header("Animación de muerte")]
    [SerializeField] private Animator animator;
    [SerializeField] private string parametroMuerte = "Muerto";

    [Header("Barra de vida (sprite)")]
    [SerializeField] private SpriteRenderer barra;
    [SerializeField, Min(0.01f)] private float anchoTotal = 1f;

    [Header("Cambio de escena al morir")]
    [Tooltip("Nombre exacto de la escena que se carga después de morir.")]
    [SerializeField] private string escenaSiguiente;
    [Tooltip("Segundos de espera tras la muerte antes de empezar el fundido.")]
    [SerializeField, Min(0f)] private float esperaAntesDeCambiar = 3f;

    [Header("Fundido a negro")]
    [Tooltip("CanvasGroup del panel negro (alpha 0 al inicio).")]
    [SerializeField] private CanvasGroup fundido;
    [SerializeField, Min(0.1f)] private float duracionFundido = 1.5f;

    private float altoOriginal;

    private void Awake()
    {
        if (vida == null) vida = GetComponentInParent<EnemyHealth>();
        if (animator == null) animator = GetComponent<Animator>();

        if (barra != null)
        {
            barra.drawMode = SpriteDrawMode.Sliced;
            altoOriginal = barra.size.y;
        }

        if (fundido != null)
        {
            fundido.alpha = 0f;
            fundido.blocksRaycasts = false;
        }
    }

    private void OnEnable()
    {
        if (vida == null) return;
        vida.OnHealthChanged.AddListener(ActualizarBarra);
        vida.OnDied.AddListener(Morir);
    }

    private void OnDisable()
    {
        if (vida == null) return;
        vida.OnHealthChanged.RemoveListener(ActualizarBarra);
        vida.OnDied.RemoveListener(Morir);
    }

    private void Morir()
    {
        if (animator != null)
            animator.SetBool(parametroMuerte, true);

        if (!string.IsNullOrEmpty(escenaSiguiente))
            StartCoroutine(CambiarEscena());
    }

    private IEnumerator CambiarEscena()
    {
        yield return new WaitForSeconds(esperaAntesDeCambiar);

        if (fundido != null)
        {
            fundido.blocksRaycasts = true;
            float t = 0f;
            while (t < duracionFundido)
            {
                t += Time.deltaTime;
                fundido.alpha = Mathf.Clamp01(t / duracionFundido);
                yield return null;
            }
            fundido.alpha = 1f;
        }

        SceneManager.LoadScene(escenaSiguiente);
    }

    private void ActualizarBarra(float normalizado)
    {
        if (barra == null) return;

        if (normalizado <= 0f)
        {
            barra.enabled = false;
            return;
        }

        float ancho = anchoTotal * Mathf.Clamp01(normalizado);
        barra.size = new Vector2(ancho, altoOriginal);

        float escalaX = barra.transform.localScale.x;
        Vector3 pos = barra.transform.localPosition;
        pos.x = -(anchoTotal - ancho) * 0.5f * escalaX;
        barra.transform.localPosition = pos;
    }
}