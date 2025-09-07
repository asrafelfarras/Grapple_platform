using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public Transform target;
    public Vector2 offset = new Vector2(2f, 2f);
    public float smoothSpeed = 5f;

    [Header("Zoom Settings")]
    public float zoom = 5f;               // Default orthographic size
    public float zoomSpeed = 5f;          // Smoothing speed when changing zoom

    private Camera cam;

    private void Start()
    {
        cam = GetComponent<Camera>();
        if (cam.orthographic)
            cam.orthographicSize = zoom;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        // Follow Position
        Vector3 desiredPosition = new Vector3(
            target.position.x + offset.x,
            target.position.y + offset.y,
            transform.position.z
        );
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Smooth Zoom
        if (cam.orthographic)
            cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zoom, zoomSpeed * Time.deltaTime);
    }
}
