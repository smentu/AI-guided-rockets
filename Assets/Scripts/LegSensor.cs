using UnityEngine;

public class LegSensor : MonoBehaviour
{
    private bool touchingPad;
    private bool touchingGround;

    // Start is called before the first frame update
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.collider.name == GetComponentInParent<LanderAgent>().arena.platform.name)
        {
            touchingPad = true;
            touchingGround = false;
        } else if (collision.collider.tag == "ground")
        {
            touchingPad = false;
            touchingGround = true;
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        touchingPad = false;
        touchingGround = false;
    }

    public bool OnPad()
    {
        return touchingPad;
    }

    public bool OnGround()
    {
        return touchingGround;
    }

    public void Reset()
    {
        touchingPad = false;
    }
}
