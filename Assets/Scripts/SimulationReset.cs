using System.Collections.Generic;
using UnityEngine;

public class SimulationReset : MonoBehaviour
{
    public float resetTime;

    private List<GameObject> childList = new List<GameObject>();
    private List<Vector3> originalPositions = new List<Vector3>();
    private List<Quaternion> originalRotations = new List<Quaternion>();

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log(transform.childCount);
        foreach (Transform child in transform)
        {
            childList.Add(child.gameObject);
            originalPositions.Add(child.position);
            originalRotations.Add(child.rotation);

            InvokeRepeating("ResetPositions", resetTime, resetTime);
        }
    }

    void ResetPositions()
    {
        for (int i = 0; i < childList.Count; i++)
        {
            Debug.Log(childList[i].name);

            childList[i].transform.position = originalPositions[i];
            childList[i].transform.rotation = originalRotations[i];
            childList[i].GetComponent<Rigidbody>().velocity = Vector3.zero;
            childList[i].GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }
}
