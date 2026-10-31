using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>Abre el panel de una pista al tocar su objeto y lo cierra con el siguiente toque.</summary>
public class PistaInteractiva : MonoBehaviour
{
    [Header("Panel de instrucciones")]
    [Tooltip("Arrastra aquí el panel que crearás. Debe empezar desactivado.")]
    [SerializeField] private GameObject panelInstrucciones;

    [Header("Detección táctil")]
    [Tooltip("Cámara desde la que se lanza el rayo. Si queda vacío, se usa la cámara MainCamera.")]
    [SerializeField] private Camera camara;
    [SerializeField, Min(0.1f)] private float distanciaMaxima = 100f;
    [SerializeField] private LayerMask capasDetectables = ~0;

    private void Awake()
    {
        PickupGlow.Aplicar(gameObject, PickupGlow.Tipo.Pista);
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        ReanudarJuego();
        EnhancedTouchSupport.Disable();
    }

    private void Update()
    {
        foreach (Touch touch in Touch.activeTouches)
        {
            if (touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                ProcesarToque(touch.screenPosition);
                return;
            }
        }

        // Permite probar la interacción en el Editor con el clic izquierdo.
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            ProcesarToque(Mouse.current.position.ReadValue());
    }

    private void ProcesarToque(Vector2 posicionPantalla)
    {
        if (panelInstrucciones == null)
        {
            Debug.LogWarning("PistaInteractiva: asigna el panel de instrucciones en el Inspector.", this);
            return;
        }

        // Cualquier toque posterior cierra el panel, sin importar dónde toque la pantalla.
        if (panelInstrucciones.activeSelf)
        {
            panelInstrucciones.SetActive(false);
            ReanudarJuego();
            return;
        }

        Camera camaraActiva = camara != null ? camara : Camera.main;
        if (camaraActiva == null) return;

        Ray rayo = camaraActiva.ScreenPointToRay(posicionPantalla);
        if (!Physics.Raycast(rayo, out RaycastHit impacto, distanciaMaxima, capasDetectables,
                QueryTriggerInteraction.Ignore))
            return;

        Transform objetoTocado = impacto.collider.transform;
        if (objetoTocado == transform || objetoTocado.IsChildOf(transform))
        {
            PausarJuego();
            panelInstrucciones.SetActive(true);
        }
    }

    private float escalaTiempoAnterior = 1f;
    private bool juegoPausadoPorPista;

    private void PausarJuego()
    {
        if (juegoPausadoPorPista) return;

        escalaTiempoAnterior = Time.timeScale;
        Time.timeScale = 0f;
        juegoPausadoPorPista = true;
    }

    private void ReanudarJuego()
    {
        if (!juegoPausadoPorPista) return;

        Time.timeScale = escalaTiempoAnterior;
        juegoPausadoPorPista = false;
    }
}
