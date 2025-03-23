using UnityEngine;

public class FallDown : MonoBehaviour
{
    public float delay;

    private readonly float gravity = 9.81f * 2;

    void Update()
    {
        delay -= Time.deltaTime;
        if (delay <= 0)
        {
            transform.Translate(Vector3.down * (gravity * Time.deltaTime));
            if (transform.position.y < -10)
                Destroy(gameObject);
        }
    }
}
