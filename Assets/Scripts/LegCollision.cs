using UnityEngine;

public class LegCollision : MonoBehaviour
{
    public AudioSource thud;

    void OnCollisionEnter(Collision collision)
    {
        //Debug.Log("leg hit ground with velocity: " + collision.relativeVelocity.magnitude);
        thud.volume = Mathf.Min(1, collision.relativeVelocity.magnitude / 200f);
        thud.Play();
        transform.parent.GetComponent<LanderAgent>().CollisionDetected(collision);
    }
}
