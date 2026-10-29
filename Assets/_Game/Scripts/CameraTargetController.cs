using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

[DefaultExecutionOrder(-100)]
public class CameraTargetController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Joystick lookJoystick; // opcional
    [Tooltip("Altura del objetivo de cámara respecto al pivote del jugador (aprox. hombros).")]
    [SerializeField] private float heightOffset = 1.05f;
    [SerializeField] private float sensitivityX = 120f;
    [SerializeField] private float sensitivityY = 80f;
    [SerializeField] private float minPitch = 0f;
    [SerializeField] private float maxPitch = 45f;

    [Header("Cámara táctil (arrastrar con el dedo)")]
    [Tooltip("Grados por píxel de arrastre horizontal.")]
    [SerializeField] private float touchSensitivityX = 0.15f;
    [Tooltip("Grados por píxel de arrastre vertical.")]
    [SerializeField] private float touchSensitivityY = 0.12f;
    [Tooltip("Solo se usan toques que empiecen a la derecha de esta fracción de la pantalla (0.4 = 40%).")]
    [Range(0f, 1f)]
    [SerializeField] private float lookAreaStartX = 0.4f;
    [SerializeField] private bool invertY = false;

    [Header("Temblor al disparar")]
    [Tooltip("Ángulo máximo del movimiento de cámara provocado por un disparo.")]
    [SerializeField, Min(0f)] private float temblorDisparo = 0.65f;
    [Tooltip("Qué tan rápido desaparece el temblor.")]
    [SerializeField, Min(1f)] private float recuperacionTemblor = 22f;
    [SerializeField, Min(1f)] private float frecuenciaTemblor = 24f;

    private float yaw;
    private float pitch = 12f;
    private int lookFingerId = -1;
    private float fuerzaTemblor;
    private float tiempoTemblor;

    public void TemblarPorDisparo()
    {
        fuerzaTemblor = Mathf.Min(fuerzaTemblor + 1f, 1.8f);
        tiempoTemblor = 0f;
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        lookFingerId = -1;
    }

    void Awake()
    {
        transform.SetParent(null);
        yaw = transform.eulerAngles.y;
        if (player != null)
            transform.position = player.position + Vector3.up * heightOffset;
    }

    void LateUpdate()
    {
        if (player == null) return;

        // El objetivo conserva siempre la misma distancia y altura respecto al jugador.
        // El amortiguado breve de Cinemachine quita vibración sin dejar que la cámara se atrase.
        transform.position = player.position + Vector3.up * heightOffset;

        // Cámara con el dedo (un dedo libre, fuera del joystick y de la UI).
        HandleTouchLook();

        // Joystick de cámara opcional: puedes dejarlo o quitarlo del Inspector.
        if (lookJoystick != null)
        {
            yaw += lookJoystick.Horizontal * sensitivityX * Time.deltaTime;
            pitch -= lookJoystick.Vertical * sensitivityY * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        float envolvente = fuerzaTemblor * Mathf.Exp(-recuperacionTemblor * tiempoTemblor);
        float fase = tiempoTemblor * frecuenciaTemblor * Mathf.PI * 2f;
        float temblorPitch = temblorDisparo * envolvente * Mathf.Sin(fase);
        float temblorYaw = temblorDisparo * 0.4f * envolvente * Mathf.Sin(fase + Mathf.PI * 0.5f);
        transform.rotation = Quaternion.Euler(pitch + temblorPitch, yaw + temblorYaw, 0f);

        tiempoTemblor += Time.deltaTime;
        fuerzaTemblor = Mathf.MoveTowards(fuerzaTemblor, 0f, Time.deltaTime * 8f);
    }

    private void HandleTouchLook()
    {
        bool fingerStillDown = false;

        foreach (Touch touch in Touch.activeTouches)
        {
            // 1) Elegir el dedo de la cámara cuando empieza un toque válido.
            if (lookFingerId == -1 && touch.phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                bool enZonaDerecha = touch.screenPosition.x > Screen.width * lookAreaStartX;
                bool sobreUI = EventSystem.current != null &&
                               EventSystem.current.IsPointerOverGameObject(touch.touchId);

                if (enZonaDerecha && !sobreUI)
                    lookFingerId = touch.touchId;
            }

            // 2) Mover la cámara solo con ese dedo.
            if (touch.touchId == lookFingerId)
            {
                fingerStillDown = true;

                if (touch.phase == UnityEngine.InputSystem.TouchPhase.Moved)
                {
                    yaw += touch.delta.x * touchSensitivityX;
                    pitch += (invertY ? 1f : -1f) * touch.delta.y * touchSensitivityY;
                    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                }
            }
        }

        // 3) Si ese dedo ya no está en pantalla, liberarlo.
        if (!fingerStillDown) lookFingerId = -1;
    }
}

