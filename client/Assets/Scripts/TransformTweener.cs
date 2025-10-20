using UnityEngine;

public class TransformTweener : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Duration in seconds for the tween animation")]
    private float tweenDuration = 0.3f;

    [SerializeField]
    [Tooltip("Animation curve for easing (leave empty for linear)")]
    private AnimationCurve easingCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    private float tweenProgress = 1f;
    private bool isTweening = false;

    private void Start()
    {
        InitializeFromCurrentTransform();
    }

    private void InitializeFromCurrentTransform()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        targetPosition = transform.position;
        targetRotation = transform.rotation;
    }

    public void TweenToLocation(Vector3 position, Quaternion rotation)
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        targetPosition = position;
        targetRotation = rotation;
        tweenProgress = 0f;
        isTweening = true;
    }

    private void Update()
    {
        if (isTweening)
        {
            tweenProgress += Time.deltaTime / tweenDuration;

            if (tweenProgress >= 1f)
            {
                tweenProgress = 1f;
                isTweening = false;
                transform.SetPositionAndRotation(targetPosition, targetRotation);
            }
            else
            {
                float easedProgress = easingCurve.Evaluate(tweenProgress);
                Vector3 newPosition = Vector3.Lerp(startPosition, targetPosition, easedProgress);
                Quaternion newRotation = Quaternion.Slerp(startRotation, targetRotation, easedProgress);
                transform.SetPositionAndRotation(newPosition, newRotation);
            }
        }
    }
}

