using UnityEngine;
// using System.Collections.Generic;
using System;

public class RocketControl : MonoBehaviour
{
    public GameObject ThrustPoint;
    public float ThrustForce;

    public float MaxThrustAngle = 45;

    private string XAxisL;
    private string YAxisL;
    private KeyCode thrustKey;

    private float xRotation;
    private float yRotation;

    // Start is called before the first frame update
    void Start()
    {
        int number = 1;
        XAxisL = "J" + number + "horizontalL";
        YAxisL = "J" + number + "verticalL";

        string fireString = "Joystick" + number + "Button0";
        thrustKey = (KeyCode)Enum.Parse(typeof(KeyCode), fireString);
    }

    // Update is called once per frame

    private void Update()
    {
        xRotation = Input.GetAxis(YAxisL) * MaxThrustAngle;
        yRotation = Input.GetAxis(XAxisL) * MaxThrustAngle;

        ThrustPoint.transform.localRotation = Quaternion.Euler(xRotation, 0, yRotation);
        //ThrustPoint.transform.rotation = Quaternion.Euler(0, 0, 0);

        //ThrustPoint.transform.eulerAngles = new Vector3(xRotation, 0, zRotation);
    }

    // Fixed updata is called at fixed time intervals
    void FixedUpdate()
    {
        if (Input.GetKey(thrustKey))
        {
            Debug.Log("applying thrust");
            gameObject.GetComponent<Rigidbody>().AddForceAtPosition(ThrustPoint.transform.up, ThrustPoint.transform.position, ForceMode.VelocityChange);
            //ThrustPoint.GetComponent<Rigidbody>().AddForce(ThrustPoint.transform.forward, ForceMode.VelocityChange);
        }
    }
}