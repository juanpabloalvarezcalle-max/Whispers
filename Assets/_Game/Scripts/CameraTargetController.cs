using UnityEngine;

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

    void LateUpdate()
    {
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
