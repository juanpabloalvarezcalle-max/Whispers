using Unity.Collections.Tests.CoreCLR.TestJobs;
using UnityEngine;

public class Batery : MonoBehaviour
{
    [SerializeField] private bool bateriaActiva = false;
    [SerializeField] private float condicionBateria = 100f;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            
        }
    }

    public void EstadoBAteria()
    {
        if (bateriaActiva)
        {
            condicionBateria -= 1; 
        }
        if (condicionBateria <= 0)
        {
           Destroy(gameObject);   
        }
    }
}
