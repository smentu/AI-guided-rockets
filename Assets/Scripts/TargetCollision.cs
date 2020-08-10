using UnityEngine;

public class TargetCollision : MonoBehaviour
{
    private bool collectedFlag = false;

    public bool Collected()
    {
        return collectedFlag;
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.tag == "Player") {
            Debug.Log("target collision");
            collectedFlag = true;
            // Destroy(gameObject);
        }
    }
}
