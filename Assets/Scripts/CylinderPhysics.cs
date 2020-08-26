using UnityEngine;

public class CylinderPhysics : MonoBehaviour
{
    public float dragCoefficient;
    private float radius;
    private float length;

    private UnityEngine.Mesh mesh;
    private Vector3 forceVector;

    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        length = mesh.bounds.extents.y * transform.localScale.y * 2.0f;
        radius = mesh.bounds.extents.x * transform.localScale.x;

        Debug.Log("rocket dimensions are" + GetDims());
    }

    public Vector2 GetDims()
    {
        return new Vector2(length, radius);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 velocity = GetComponent<Rigidbody>().velocity;
        Vector3 direction = velocity.normalized;
        float vsquared = Mathf.Pow(velocity.magnitude, 2);

        Debug.Log(velocity.magnitude);

        float parallelForce = - dragCoefficient * Vector3.Dot(direction, transform.up) * vsquared * Mathf.PI * Mathf.Pow(radius, 2);
        float perpendicularForceX = - dragCoefficient * Vector3.Dot(direction, transform.right) * vsquared * 2 * radius * length;
        float perpendicularForceZ = - dragCoefficient * Vector3.Dot(direction, transform.forward) * vsquared * 2 * radius * length;

        forceVector = new Vector3(perpendicularForceX, parallelForce, perpendicularForceZ);
        //Vector3 forceVector = transform.up * 10f;

        //Vector3 cg = transform.TransformPoint(GetComponent<Rigidbody>().centerOfMass);
        Vector3 cp = transform.TransformPoint(GetComponent<RocketControl>().cpOffset);
        GetComponent<Rigidbody>().AddForceAtPosition(transform.rotation * forceVector, cp);
    }

    void OnDrawGizmos()
    {
        Vector3 cp = transform.TransformPoint(GetComponent<RocketControl>().cpOffset);

        Gizmos.color = Color.red;
        Gizmos.DrawRay(cp, transform.rotation * forceVector);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(cp, GetComponent<Rigidbody>().velocity);
    }
}
