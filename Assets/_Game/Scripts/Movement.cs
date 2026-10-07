using Unity.Mathematics;
using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField] private Joystick joystick;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 540f; 

    private Rigidbody rb;
    private Vector3 move;
    private Transform cam; 

    void Awake()
    {
        Application.targetFrameRate = 60;
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        cam = Camera.main.transform; 
    }

    void Update()
    {
        Vector3 forward = cam.forward; 
        Vector3 right = cam.right; 
        forward.y = 0f; 
        right.y = 0f;
        forward.Normalize(); 
        right.Normalize(); 
        move = forward*joystick.Vertical + right*joystick.Horizontal;
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }
    }

    void FixedUpdate()
    {
        Vector3 targetVelocity = move * speed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;

        if (move.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(move);
            rb.rotation = Quaternion.RotateTowards(rb.rotation, target, rotationSpeed * Time.fixedDeltaTime);
        }
    }
}
