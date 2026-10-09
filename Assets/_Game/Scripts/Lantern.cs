using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Lantern : MonoBehaviour
{
    [Header("Configuración de Luz")]
    [SerializeField] private Light linterna;
    [SerializeField] private bool linternaEncendida = false;
    [Tooltip("Dejar en -1 para usar automáticamente la intensidad configurada en el componente Light")]
    [SerializeField] private float intensidadMaxima = -1f;

    [Header("Batería")]
    [SerializeField] private float batMax = 100f;
    [SerializeField] private float bateria = 100f;
    [SerializeField] private float tasaDescargaPorSegundo = 1.5f;
    [SerializeField] private bool atenuarConBateria = true;

    [Header("Efectos de Terror / Baja Batería")]
    [SerializeField] private float umbralBateriaBaja = 25f;
    [SerializeField] private bool parpadeoEnBajaBateria = true;
    [Tooltip("Frecuencia aproximada de parpadeo por segundo al llegar al umbral de batería baja")]
    [SerializeField] private float probabilidadParpadeo = 0.15f;
    [SerializeField] private float frecuenciaMaximaParpadeo = 1.5f;

    [Header("Audio (Opcional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoInterruptor;
    [SerializeField] private AudioClip sonidoApagadoBateria;

    [Header("Eventos UI")]
    // Emite el porcentaje de 0 a 1 para alimentar directamente una barra/slider de UI
    public UnityEvent<float> OnBateriaCambiada;
    public UnityEvent<bool> OnLinternaEstadoCambiado;

    private bool estaParpadeando = false;
    private float intensidadOriginal;

    // Propiedad pública de solo lectura para acceder a la batería actual
    public float BateriaActual => bateria;
    public float BateriaPorcentaje => Mathf.Clamp01(bateria / batMax);
    public bool EstaEncendida => linternaEncendida;

    void Awake()
    {
        if (linterna == null)
            linterna = GetComponentInChildren<Light>();

        if (linterna != null)
        {
            // Usar la intensidad configurada en el componente Light (en URP suele ser 1000+ lúmenes)
            intensidadOriginal = (intensidadMaxima > 0) ? intensidadMaxima : linterna.intensity;
            if (intensidadMaxima <= 0) intensidadMaxima = linterna.intensity;
            linterna.enabled = linternaEncendida;
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        bateria = Mathf.Clamp(bateria, 0f, batMax);
    }

    void Start()
    {
        ActualizarIntensidadYLuz();
        OnBateriaCambiada?.Invoke(BateriaPorcentaje);
        OnLinternaEstadoCambiado?.Invoke(linternaEncendida);
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (UnityEngine.InputSystem.Keyboard.current != null)
        {
            if (UnityEngine.InputSystem.Keyboard.current.fKey.wasPressedThisFrame)
            {
                CambiarLinterna();
            }

#if UNITY_EDITOR
            if (UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
            {
                bateria = 15f;
                Debug.Log("<color=yellow>[Lantern]</color> Modo prueba: Batería forzada a 15% para probar parpadeo.");
                ActualizarIntensidadYLuz();
            }
#endif
        }
#endif

        if (linternaEncendida)
        {
            ConsumirBateria();
        }
    }

    private void ConsumirBateria()
    {
        if (bateria > 0f)
        {
            bateria -= tasaDescargaPorSegundo * Time.deltaTime;
            bateria = Mathf.Max(0f, bateria);

            OnBateriaCambiada?.Invoke(BateriaPorcentaje);
            ActualizarIntensidadYLuz();

            // El parpadeo se vuelve más frecuente a medida que la batería se acerca a cero.
            if (parpadeoEnBajaBateria && bateria <= umbralBateriaBaja && !estaParpadeando)
            {
                float severidad = umbralBateriaBaja > 0f
                    ? 1f - Mathf.Clamp01(bateria / umbralBateriaBaja)
                    : 1f;
                float frecuencia = Mathf.Lerp(probabilidadParpadeo, frecuenciaMaximaParpadeo, severidad);
                float probabilidadEsteFrame = 1f - Mathf.Exp(-frecuencia * Time.deltaTime);
                if (Random.value < probabilidadEsteFrame)
                {
                    StartCoroutine(EfectoParpadeo());
                }
            }

            // Agotamiento total de batería
            if (bateria <= 0f)
            {
                ApagarPorBateria();
            }
        }
    }

    private void ActualizarIntensidadYLuz()
    {
        if (linterna == null || estaParpadeando) return;

        if (atenuarConBateria && batMax > 0f)
        {
            // Mantiene la intensidad nominal hasta la mitad de batería y luego cae gradualmente.
            float cargaEnZonaDeAtenuacion = Mathf.InverseLerp(0f, 0.5f, BateriaPorcentaje);
            float factor = Mathf.Lerp(0.35f, 1f, cargaEnZonaDeAtenuacion);
            linterna.intensity = intensidadOriginal * factor;
        }
        else
        {
            linterna.intensity = intensidadOriginal;
        }
    }

    private IEnumerator EfectoParpadeo()
    {
        estaParpadeando = true;
        int parpadeos = Random.Range(1, 4);

        for (int i = 0; i < parpadeos; i++)
        {
            if (linterna != null) linterna.enabled = false;
            yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));

            if (linterna != null && linternaEncendida)
            {
                linterna.enabled = true;
                linterna.intensity = intensidadOriginal * Random.Range(0.2f, 0.7f);
            }
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }

        estaParpadeando = false;
        ActualizarIntensidadYLuz();
    }

    private void ApagarPorBateria()
    {
        linternaEncendida = false;
        if (linterna != null) linterna.enabled = false;

        if (audioSource != null && sonidoApagadoBateria != null)
        {
            audioSource.PlayOneShot(sonidoApagadoBateria);
        }

        OnLinternaEstadoCambiado?.Invoke(false);
    }

    /// <summary>
    /// Llamado por el botón UI o interacción para alternar la linterna.
    /// </summary>
    public void CambiarLinterna()
    {
        // Si no hay batería, no se puede encender
        if (!linternaEncendida && bateria <= 0f)
        {
            // Efecto de intento fallido (sonido de click sin luz)
            ReproducirClick();
            return;
        }

        linternaEncendida = !linternaEncendida;

        if (linterna != null)
        {
            linterna.enabled = linternaEncendida;
            if (linternaEncendida)
            {
                ActualizarIntensidadYLuz();
            }
        }

        ReproducirClick();
        OnLinternaEstadoCambiado?.Invoke(linternaEncendida);
    }

    /// <summary>
    /// Recarga la batería (por ejemplo, al recoger pilas o baterías en el juego).
    /// </summary>
    public void RecargarBateria(float cantidad)
    {
        bateria = Mathf.Clamp(bateria + cantidad, 0f, batMax);
        OnBateriaCambiada?.Invoke(BateriaPorcentaje);
        ActualizarIntensidadYLuz();
    }

    private void ReproducirClick()
    {
        if (audioSource != null && sonidoInterruptor != null)
        {
            audioSource.PlayOneShot(sonidoInterruptor);
        }
    }
}
