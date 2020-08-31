using UnityEngine;

public class LegCollision : MonoBehaviour
{
    void OnCollisionEnter(Collision collision)
    {
        transform.parent.GetComponent<LanderAgent>().CollisionDetected(collision);
    }
}
