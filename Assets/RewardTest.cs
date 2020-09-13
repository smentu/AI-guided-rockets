using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RewardTest : MonoBehaviour
{
    public GameObject coneOrigin;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 targetDirection = (transform.position - coneOrigin.transform.position).normalized;
        float reward = Vector3.Angle(coneOrigin.transform.up, targetDirection) / 180;

        reward = -reward * 100 + 15;

        Debug.Log(reward);
    }
}
