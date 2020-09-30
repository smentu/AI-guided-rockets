using UnityEngine;

public class Exploder : MonoBehaviour
{
    public GameObject ExplosionPrefab;
    public float explosionInterval;

    private GameObject explosion;
    private float nextExplosionTime;

    // Start is called before the first frame update
    void Start()
    {
        nextExplosionTime = Time.time + 0.5f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time > nextExplosionTime)
        {
            if (explosion)
            {
                Destroy(explosion);
            }

            explosion = Instantiate(ExplosionPrefab, new Vector3(0, 0, 0), Quaternion.identity);
            nextExplosionTime = Time.time + explosionInterval;
        }
        
    }
}
