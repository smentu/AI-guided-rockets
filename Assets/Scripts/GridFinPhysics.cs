using UnityEngine;

public class GridFinPhysics : MonoBehaviour
{
    public float nFins;
    public float dragCoefficient;
    public float liftCoefficient;
    private float sizeX;
    private float sizeZ;
    private float sizeY;

    // surface area perpendicular to X and Z
    private float surfaceX;
    private float surfaceZ;
    private Vector3 forceVector;

    private UnityEngine.Mesh mesh;

    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;

        sizeX = mesh.bounds.extents.x * transform.localScale.x * 2.0f;
        sizeZ = mesh.bounds.extents.z * transform.localScale.z * 2.0f;
        sizeY = mesh.bounds.extents.y * transform.localScale.y * 2.0f;

        surfaceX = sizeY * sizeZ;
        surfaceZ = sizeY * sizeX;

        //Debug.Log(GetDims());
    }

    public Vector3 GetDims()
    {
        return new Vector3(sizeX, sizeY, sizeZ);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 velocity = GetComponent<Rigidbody>().velocity;
        Vector3 direction = velocity.normalized;
        float vsquared = Mathf.Pow(velocity.magnitude, 2);

        float zAngle = Vector3.Dot(direction, transform.right);
        float xAngle = Vector3.Dot(direction, transform.forward);
        float crossSectionMultiplier = 1 + Mathf.Abs(Vector3.Dot(direction, transform.up)) * nFins;

        //Debug.Log("spuff");
        //Debug.Log(crossSectionMultiplier);

        float perpendicularForceX = - liftCoefficient * zAngle * surfaceX * crossSectionMultiplier * vsquared;
        float perpendicularForceZ = - liftCoefficient * xAngle * surfaceZ * crossSectionMultiplier * vsquared;
        float drag = -0.05f * sizeX * sizeZ * Vector3.Dot(direction, transform.up) * vsquared * dragCoefficient;

        forceVector = new Vector3(perpendicularForceX, drag, perpendicularForceZ);
        //Vector3 forceVector = transform.up * 10f;

        GetComponent<Rigidbody>().AddForce(transform.rotation * forceVector);
    }

    void OnDrawGizmos()
    {
        // Draws a 5 unit long red line in front of the object
        Gizmos.color = Color.red;
        Gizmos.DrawRay(GetComponent<Rigidbody>().transform.position, transform.rotation * forceVector);
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(GetComponent<Rigidbody>().transform.position, GetComponent<Rigidbody>().velocity);
    }
}
