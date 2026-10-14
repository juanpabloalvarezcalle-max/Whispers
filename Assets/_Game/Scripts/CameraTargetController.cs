using UnityEngine;

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

    private float yaw;
    private float pitch = 12f;

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

        if (lookJoystick != null)
        {
            yaw += lookJoystick.Horizontal * sensitivityX * Time.deltaTime;
            pitch -= lookJoystick.Vertical * sensitivityY * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}

