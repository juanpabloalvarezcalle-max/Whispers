using UnityEngine;

public class FlashlightSway : MonoBehaviour
{
    [Header("Objetivo a seguir")]
    [Tooltip("La cámara o el punto que debe seguir la dirección de la linterna")]
    [SerializeField] private Transform objetivoSeguimiento;

    [Header("Inercia / Suavizado")]
    [Tooltip("Velocidad con la que la linterna alcanza la rotación del objetivo (menor = más inercia)")]
    [SerializeField] private float velocidadRotacion = 10f;

    [Tooltip("Ángulo máximo de desfase permitido")]
    [SerializeField] private float anguloMaximo = 25f;

    private Quaternion rotacionRelativaInicial = Quaternion.identity;

    void Start()
    {
        if (objetivoSeguimiento == null)
        {
            // Si es hijo del Player, seguir al Player con inercia; si no, seguir a la cámara
            if (transform.parent != null)
            {
                objetivoSeguimiento = transform.parent;
            }
            else if (Camera.main != null)
            {
                objetivoSeguimiento = Camera.main.transform;
            }
        }

        if (objetivoSeguimiento != null)
        {
            // Guardar la rotación relativa inicial para respetar la orientación del modelo 3D
            rotacionRelativaInicial = Quaternion.Inverse(objetivoSeguimiento.rotation) * transform.rotation;
        }
    }

    void LateUpdate()
    {
        if (objetivoSeguimiento == null) return;

        // Suavizado esférico de la rotación respetando la orientación del modelo
        Quaternion rotacionDeseada = objetivoSeguimiento.rotation * rotacionRelativaInicial;

        if (Quaternion.Angle(transform.rotation, rotacionDeseada) > anguloMaximo)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, rotacionDeseada, velocidadRotacion * Time.deltaTime * 2f);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, rotacionDeseada, velocidadRotacion * Time.deltaTime);
        }
    }
}

