using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PanningCamera : MonoBehaviour
{
    public float fovConstant;
    public GameObject target;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetDelta = (target.transform.position - transform.position);
        transform.forward = targetDelta.normalized;

        GetComponent<Camera>().fieldOfView = fovConstant / targetDelta.magnitude;
    }
}
