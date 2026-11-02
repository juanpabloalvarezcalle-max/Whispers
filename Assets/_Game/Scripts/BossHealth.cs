using UnityEngine;

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
        Debug.Log("BossDeath: activando muerte");
        if (animator != null)
            animator.SetBool(parametroMuerte, true);
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