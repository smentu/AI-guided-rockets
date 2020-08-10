using UnityEngine;

public class fixed_follow_camera : MonoBehaviour { 

    public Transform player;
    public Vector3 offset;

    // Update is called once per frame
    void Update()
    {
        transform.position = player.position + offset;
    }
}
