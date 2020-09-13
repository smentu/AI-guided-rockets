using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class timeTest : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        print("delta time " + Time.deltaTime);
        print("unscaled delta time " + Time.unscaledDeltaTime);
        print("maximum delta time " + Time.maximumDeltaTime);
    }
}
