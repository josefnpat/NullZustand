using UnityEngine;

[RequireComponent(typeof(TransformTweener))]
public class PlayerController : MonoBehaviour
{
    private TransformTweener _transformTweener;

    private void Start()
    {
        _transformTweener = GetComponent<TransformTweener>();
    }

    public void SetLocation(Vector3 position, Quaternion rotation)
    {
        _transformTweener.TweenToLocation(position, rotation);
    }
}
