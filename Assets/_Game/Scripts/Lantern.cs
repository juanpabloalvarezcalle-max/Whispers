using Unity.Android.Gradle;
using Unity.AppUI.UI;
using UnityEditor.AdaptivePerformance.Editor;
using UnityEngine;

public class Lantern : MonoBehaviour
{
    [SerializeField] private bool linternaEncendida = false;
    static public float bateria = 100f; 
    private float batMax = 100f; 
    private float batMin = 0f;
    [SerializeField] private Light linterna; 



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (linternaEncendida == true)
        {
            bateria -= 1 * Time.deltaTime; 
            Debug.Log("la bateria de la linterna es:" + linterna);
        }
        if (bateria<= 0)
        {
            linternaEncendida = false; 
            linterna.enabled= false;
        }
        
    }

    public void CambiarLinterna()
    {
        linternaEncendida = !linternaEncendida; 
        linterna.enabled = linternaEncendida; 
    }
    
}
