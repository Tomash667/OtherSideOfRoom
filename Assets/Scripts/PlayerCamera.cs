using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public GameObject player;

    void LateUpdate()
    {
        transform.position = player.transform.position - player.transform.forward * 3.8f + Vector3.up * 4;
        Vector3 forward = (player.transform.position - transform.position).normalized;
        transform.rotation = Quaternion.LookRotation(forward);
    }
}
