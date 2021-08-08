using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Values"), Min(0)]
    [SerializeField] private float mouseSensitivity = 100f;

    private float xRotation;
    private float yRotation;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        var mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        var mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        yRotation += mouseX;

        xRotation = Mathf.Clamp(xRotation, -80f, 80f);
        yRotation = Mathf.Clamp(yRotation, -80f, 80f);
        
        transform.localRotation = Quaternion.Euler(xRotation, yRotation, 0f);
    }
}