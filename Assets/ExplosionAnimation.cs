using System.Collections;
using UnityEngine;

public class ExplosionAnimation : MonoBehaviour
{
    // Start is called before the first frame update

    public float duration = 1.0f;

    public Material explosionMaterial;
    public Gradient albedoGradient;
    public Gradient emissionGradient;
    public AnimationCurve sizeCurve;
    public AnimationCurve fillCurve;

    private Vector3 initialScale;

    void Start()
    {
        initialScale = GetComponentInParent<Transform>().localScale;
    }

    // Update is called once per frame
    void Update()
    {
        StartCoroutine(Explode(duration));
    }

    private IEnumerator Explode(float duration)
    {
        float startTime = Time.time;

        while (Time.time - startTime < duration)
        {
            float relativeTime = (Time.time - startTime) / duration;

            // if episode resets before end of countdown, do nothing
            transform.localScale = sizeCurve.Evaluate(relativeTime) * initialScale;

            explosionMaterial.SetColor("ExplosionColor123", albedoGradient.Evaluate(relativeTime));
            explosionMaterial.SetColor("EmissionColor123", emissionGradient.Evaluate(relativeTime));

            explosionMaterial.SetFloat("ExplosionFill123", fillCurve.Evaluate(relativeTime));

            yield return null;
        }

        Destroy(gameObject);
    }
}
