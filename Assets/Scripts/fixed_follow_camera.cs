using UnityEngine;
using System;

public class fixed_follow_camera : MonoBehaviour { 

    public Transform player;
    public Vector3 offset;
    public float cameraSensitivity = 0.1f;

    private float xAngle = 0;
    private float yAngle = 0;
    private Vector2 cameraDelta;

    PlayerControls controls;

    private void Awake()
    {
        controls = new PlayerControls();

        controls.Gameplay.CameraMove.performed += ctx => cameraDelta = ctx.ReadValue<Vector2>();
        controls.Gameplay.CameraMove.canceled += ctx => cameraDelta = Vector2.zero;

        yAngle = 45;
    }

    // Update is called once per frame
    void Update()
    {
        xAngle += cameraSensitivity * Math.Abs(cameraDelta.x) * cameraDelta.x;
        yAngle += cameraSensitivity * Math.Abs(cameraDelta.y) * cameraDelta.y;

        yAngle = Mathf.Min(yAngle, 90);
        yAngle = Mathf.Max(yAngle, -90);

        transform.position = player.position + Quaternion.Euler(yAngle, xAngle, 0) * offset;
        transform.rotation = Quaternion.Euler(yAngle, xAngle, 0);
    }

    private void FixedUpdate()
    {
        xAngle += cameraSensitivity * cameraDelta.x;
        yAngle += cameraSensitivity * cameraDelta.y;

        yAngle = Mathf.Min(yAngle, 90);
        yAngle = Mathf.Max(yAngle, -90);

    }

    private void OnEnable()
    {
        controls.Gameplay.Enable();
    }

    private void OnDisable()
    {
        controls.Gameplay.Disable();
    }
}
