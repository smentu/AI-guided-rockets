using UnityEngine;

public class TargetDirectionIndicator : MonoBehaviour
{
    public Transform rocket; //ex: hips or body
    public Transform target; //target in the scene the indicator will point to
    public float offset;
    public Material pointerMat;

    private static float Sigmoid(float value)
    {
        return 1.0f / (1.0f + Mathf.Exp(-value));
    }

    private void LateUpdate()
    {
        Vector3 direction = target.position - rocket.transform.position;
        transform.position = (rocket.position + new Vector3(0f, -2f, 0f)) + direction.normalized * offset;
        transform.up = direction;

        pointerMat.SetFloat("ArrowOpacity123", Sigmoid(direction.magnitude - 40));
    }
}
