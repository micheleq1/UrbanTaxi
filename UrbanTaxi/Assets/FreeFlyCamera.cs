using UnityEngine;

public class FreeFlyCamera : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 10f;
    public float sprintMultiplier = 3f;
    public float verticalSpeed = 8f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public bool lockCursor = true;

    float yaw;
    float pitch;

    void Start()
    {
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;

        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void Update()
    {
        // Toggle cursor lock with ESC
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = locked;
        }

        // Mouse look (only if cursor locked)
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * mouseSensitivity;
            pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        // Movement
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);

        float x = Input.GetAxisRaw("Horizontal"); // A/D
        float z = Input.GetAxisRaw("Vertical");   // W/S

        float y = 0f;
        if (Input.GetKey(KeyCode.E)) y += 1f;     // up
        if (Input.GetKey(KeyCode.Q)) y -= 1f;     // down

        Vector3 move = (transform.right * x + transform.forward * z) * speed;
        move += Vector3.up * y * verticalSpeed;

        transform.position += move * Time.deltaTime;
    }
}
