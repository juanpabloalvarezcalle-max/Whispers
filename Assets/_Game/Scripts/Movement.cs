using UnityEngine;

public class Movement : MonoBehaviour
{
    [SerializeField] private Joystick joystick;
    [SerializeField] private float speed = 5f;

    private Rigidbody rb;
    private Vector3 move;
    private Transform cam; 

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + move * speed * Time.fixedDeltaTime);

        if (move.sqrMagnitude > 0.01f)
        {
            transform.forward = move;
        }
    }
}
