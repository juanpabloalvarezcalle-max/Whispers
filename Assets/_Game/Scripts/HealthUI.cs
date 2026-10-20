using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Barra de vida en pantalla. Al recibir daño se vacía de derecha a izquierda.
/// Va en la Image de la barra, que necesita un sprite asignado.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth health;
    [Tooltip("La imagen que se vacía. Si se deja vacío usa la Image de este mismo objeto.")]
    [SerializeField] private Image fillImage;
    [Tooltip("Qué tan rápido se mueve la barra, en fracción de la barra por segundo. Más alto = más brusco.")]
    [SerializeField, Min(0.1f)] private float fillSpeed = 1.5f;

    private float displayed = 1f;

    private void Start()
    {
        if (health == null) health = FindFirstObjectByType<PlayerHealth>();
        if (fillImage == null) fillImage = GetComponent<Image>();

        if (health == null || fillImage == null)
        {
            Debug.LogWarning("HealthBarUI necesita un PlayerHealth (en el Player) y una Image.", this);
            enabled = false;
            return;
        }

        if (fillImage.sprite == null)
            Debug.LogWarning("La Image de la barra necesita un Source Image para poder vaciarse.", this);

        // El borde izquierdo queda fijo, así que al bajar la vida se vacía de derecha a izquierda.
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;

        displayed = health.Normalized;
        fillImage.fillAmount = displayed;
    }

    private void Update()
    {
        displayed = Mathf.MoveTowards(displayed, health.Normalized, fillSpeed * Time.deltaTime);
        fillImage.fillAmount = displayed;
    }
}