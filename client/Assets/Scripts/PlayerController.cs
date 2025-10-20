using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public void SetLocation(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
    }
}
