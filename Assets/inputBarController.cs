using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class inputBarController : MonoBehaviour
{
    public Slider sldr;

    // Start is called before the first frame update
    void Start()
    {
        sldr.value = 0.5f;
    }

    public void setFill(float fill)
    {
        sldr.value = fill;
    }
}
