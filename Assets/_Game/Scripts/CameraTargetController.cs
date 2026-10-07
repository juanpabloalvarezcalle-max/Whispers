using UnityEngine;

[DefaultExecutionOrder(-100)]
public class CameraTargetController : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Joystick lookJoystick; // opcional
    [SerializeField] private float heightOffset = 0.6f;
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
    }

    void LateUpdate()
    {
        if (player == null) return;

        // Se ejecuta en LateUpdate con DefaultExecutionOrder(-100)
        // para posicionarse DESPUÉS de la interpolación de física pero ANTES de CinemachineBrain.
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

