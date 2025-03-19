using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private readonly float speed = 10;
    private readonly float rotateSpeed = 120;

    void Update()
    {
        float vertical = Input.GetAxis("Vertical");
        float horizontal = Input.GetAxis("Horizontal");

        transform.Translate(Vector3.forward * (speed * vertical * Time.deltaTime));
        transform.Rotate(0, horizontal * rotateSpeed * Time.deltaTime, 0);
    }
}
