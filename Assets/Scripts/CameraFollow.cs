using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;

    [Header("Settings")]
    public float smoothSpeed = 6f;
    public Vector2 bounds = new Vector2(10f, 7f);

    Vector3 offset;
    float shakeTimer;
    float shakeMag;
    float shakeDuration;

    void Start()
    {
        offset = new Vector3(0f, 0f, transform.position.z - (target != null ? target.position.z : 0f));
        if (target != null) SnapToTarget();
    }

    void LateUpdate()
    {
        if (target == null) return;

        Vector3 desired = new Vector3(target.position.x, target.position.y, 0f) + offset;
        desired.x = Mathf.Clamp(desired.x, -bounds.x, bounds.x);
        desired.y = Mathf.Clamp(desired.y, -bounds.y, bounds.y);

        transform.position = Vector3.Lerp(transform.position, desired, smoothSpeed * Time.deltaTime);

        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            float intensity = shakeMag * (shakeTimer / shakeDuration);
            transform.position += (Vector3)(Random.insideUnitCircle * intensity);
        }
    }

    public void SnapToTarget()
    {
        if (target == null) return;
        Vector3 pos = new Vector3(target.position.x, target.position.y, 0f) + offset;
        pos.x = Mathf.Clamp(pos.x, -bounds.x, bounds.x);
        pos.y = Mathf.Clamp(pos.y, -bounds.y, bounds.y);
        transform.position = pos;
    }

    public void Shake(float duration, float magnitude)
    {
        shakeDuration = duration;
        shakeTimer    = duration;
        shakeMag      = magnitude;
    }
}
