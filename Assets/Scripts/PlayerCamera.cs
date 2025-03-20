using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public GameObject player;

    private readonly Vector2 angleLimit = new(-30, 80);
    private readonly float h = 1.75f;
    private readonly float dist = 4;

    public float angle = 30;

    void LateUpdate()
    {
        angle -= Input.GetAxis("Mouse Y") * 2;
        if (angle < angleLimit.x)
            angle = angleLimit.x;
        else if (angle > angleLimit.y)
            angle = angleLimit.y;

        Vector3 to = player.transform.position;
        to.y += h;

        Vector3 ray = new(0, 0, -dist);
        Matrix4x4 m = Matrix4x4.Rotate(player.transform.rotation * Quaternion.Euler(angle, 0, 0));
        ray = m.MultiplyVector(ray);
        Vector3 from = to + ray;
        transform.SetPositionAndRotation(from, Quaternion.LookRotation(-ray));
    }
}
