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

    [Header("Haz de la linterna")]
    [SerializeField, Min(1f)] private float alcanceHaz = 18f;
    [SerializeField, Range(10f, 80f)] private float anguloHaz = 40f;
    [SerializeField, Range(5f, 70f)] private float anguloInteriorHaz = 16f;
    [SerializeField, Range(0f, 25f)] private float inclinacionHaciaAbajo = 4f;
    [SerializeField] private bool reanudarTrasArma;

    [Header("Origen del haz")]
    [Tooltip("Objeto vacío colocado justo delante del lente de la linterna. La luz sale desde ahí. Si se deja vacío, la luz se queda donde esté su objeto.")]
    [SerializeField] private Transform puntoEmision;
    [Tooltip("Activado: el haz apunta hacia donde mira el eje azul (Z) del punto de emisión. Desactivado: apunta hacia delante del personaje.")]
    [SerializeField] private bool seguirOrientacionDelPunto = false;

    [Header("Resplandor alrededor del haz")]
    [Tooltip("Agrega una segunda luz ancha y tenue que ilumina los alrededores del haz, como el halo de una linterna real.")]
    [SerializeField] private bool usarLuzDerrame = true;
    [Tooltip("Opcional. Si se deja vacío, se crea automáticamente.")]
    [SerializeField] private Light luzDerrame;
    [SerializeField, Range(40f, 120f)] private float anguloDerrame = 80f;
    [Tooltip("Intensidad del resplandor como fracción de la intensidad del haz principal")]
    [SerializeField, Range(0f, 0.6f)] private float proporcionDerrame = 0.2f;

    [Header("Batería")]
    [SerializeField] private float batMax = 100f;
    [SerializeField] private float bateria = 100f;
    [SerializeField] private float tasaDescargaPorSegundo = 1.5f;
    [SerializeField] private bool atenuarConBateria = true;

    [Header("Baterías de repuesto")]
    [Tooltip("Inventario de pilas del jugador. Si se deja vacío, se busca solo.")]
    [SerializeField] private BatteryInventory inventarioBaterias;
    [Tooltip("Segundos de espera antes de cambiar la batería agotada por una de repuesto.")]
    [SerializeField, Min(0f)] private float segundosCambioBateria = 3f;
    [SerializeField] private AudioClip sonidoCambioBateria;

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
    private Movement playerMovement;
    private bool cambiandoBateria;
    private float cambioBateriaEn;
    private bool encenderTrasCambio;

    // Propiedad pública de solo lectura para acceder a la batería actual
    public float BateriaActual => bateria;
    public float BateriaPorcentaje => Mathf.Clamp01(bateria / batMax);
    public bool EstaEncendida => linternaEncendida;
    /// <summary>True mientras corre la espera para cambiar la batería agotada.</summary>
    public bool CambiandoBateria => cambiandoBateria;

    void Awake()
    {
        if (linterna == null)
            linterna = GetComponentInChildren<Light>();

        playerMovement = GetComponentInParent<Movement>();

        if (inventarioBaterias == null) inventarioBaterias = GetComponentInParent<BatteryInventory>();
        if (inventarioBaterias == null) inventarioBaterias = FindFirstObjectByType<BatteryInventory>();

        if (linterna != null)
        {
            // Usar la intensidad configurada en el componente Light
            intensidadOriginal = (intensidadMaxima > 0) ? intensidadMaxima : linterna.intensity;
            if (intensidadMaxima <= 0) intensidadMaxima = linterna.intensity;
            linterna.type = LightType.Spot;
            linterna.range = alcanceHaz;
            linterna.spotAngle = anguloHaz;
            linterna.innerSpotAngle = Mathf.Min(anguloInteriorHaz, anguloHaz);
            linterna.color = new Color(1f, 0.97f, 0.91f);

            ConfigurarLuzDerrame();
            EstablecerLuzActiva(linternaEncendida);
        }

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        bateria = Mathf.Clamp(bateria, 0f, batMax);
    }

    void Start()
    {
        ActualizarIntensidadYLuz();
        playerMovement?.SetLinternaEnMano(linternaEncendida);
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

            if (UnityEngine.InputSystem.Keyboard.current.nKey.wasPressedThisFrame)
            {
                bateria = 1f;
                Debug.Log("<color=yellow>[Lantern]</color> Modo prueba: Batería casi agotada para probar el cambio de pila.");
            }
#endif
        }
#endif

        if (linternaEncendida)
        {
            ConsumirBateria();
        }

        ActualizarCambioBateria();
    }

    private void LateUpdate()
    {
        if (!linternaEncendida || linterna == null) return;

        // 1) Posición: la luz sale del lente de la linterna, no del centro del modelo.
        if (puntoEmision != null)
            linterna.transform.position = puntoEmision.position;

        // 2) Dirección: hacia donde apunta el punto de emisión, o hacia delante del personaje
        //    inclinada un poco hacia el suelo.
        Vector3 beamDirection;
        if (seguirOrientacionDelPunto && puntoEmision != null)
        {
            beamDirection = puntoEmision.forward;
        }
        else
        {
            Vector3 forward = playerMovement != null ? playerMovement.transform.forward : transform.forward;
            beamDirection = (forward - Vector3.up * Mathf.Tan(inclinacionHaciaAbajo * Mathf.Deg2Rad)).normalized;
        }

        if (beamDirection.sqrMagnitude > 0.001f)
            linterna.transform.rotation = Quaternion.LookRotation(beamDirection, Vector3.up);

        // 3) El resplandor siempre acompaña al haz principal.
        if (luzDerrame != null)
            luzDerrame.transform.SetPositionAndRotation(linterna.transform.position, linterna.transform.rotation);
    }

    private void ConfigurarLuzDerrame()
    {
        if (!usarLuzDerrame) return;

        if (luzDerrame == null)
        {
            GameObject derrame = new GameObject("LuzDerrame");
            derrame.transform.SetParent(linterna.transform, false);
            luzDerrame = derrame.AddComponent<Light>();
        }

        luzDerrame.type = LightType.Spot;
        luzDerrame.range = alcanceHaz * 0.7f;
        luzDerrame.spotAngle = anguloDerrame;
        luzDerrame.innerSpotAngle = anguloDerrame * 0.3f;
        luzDerrame.color = linterna.color;
        luzDerrame.shadows = LightShadows.None;
    }

    private void EstablecerLuzActiva(bool activa)
    {
        if (linterna != null) linterna.enabled = activa;
        if (luzDerrame != null) luzDerrame.enabled = activa;
    }

    private void AplicarIntensidad(float factor)
    {
        if (linterna != null) linterna.intensity = intensidadOriginal * factor;
        if (luzDerrame != null) luzDerrame.intensity = intensidadOriginal * proporcionDerrame * factor;
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

        float factor = 1f;
        if (atenuarConBateria && batMax > 0f)
        {
            // Mantiene la intensidad nominal hasta la mitad de batería y luego cae gradualmente.
            float cargaEnZonaDeAtenuacion = Mathf.InverseLerp(0f, 0.5f, BateriaPorcentaje);
            factor = Mathf.Lerp(0.35f, 1f, cargaEnZonaDeAtenuacion);
        }

        AplicarIntensidad(factor);
    }

    private IEnumerator EfectoParpadeo()
    {
        estaParpadeando = true;
        int parpadeos = Random.Range(1, 4);

        for (int i = 0; i < parpadeos; i++)
        {
            EstablecerLuzActiva(false);
            yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));

            if (linterna != null && linternaEncendida)
            {
                EstablecerLuzActiva(true);
                AplicarIntensidad(Random.Range(0.2f, 0.7f));
            }
            yield return new WaitForSeconds(Random.Range(0.05f, 0.15f));
        }

        estaParpadeando = false;
        ActualizarIntensidadYLuz();
    }

    private void ApagarPorBateria()
    {
        // Estaba encendida al agotarse: cuando entre una pila nueva, vuelve a encenderse sola.
        encenderTrasCambio = true;

        linternaEncendida = false;
        EstablecerLuzActiva(false);
        playerMovement?.SetLinternaEnMano(false);

        if (audioSource != null && sonidoApagadoBateria != null)
        {
            audioSource.PlayOneShot(sonidoApagadoBateria);
        }

        OnLinternaEstadoCambiado?.Invoke(false);
    }

    /// <summary>
    /// Con la batería agotada, espera unos segundos y la cambia sola por una de repuesto.
    /// Sin repuestos, la linterna queda apagada hasta recoger una pila del entorno.
    /// </summary>
    private void ActualizarCambioBateria()
    {
        if (inventarioBaterias == null) return;

        if (!cambiandoBateria)
        {
            if (bateria <= 0f && inventarioBaterias.Count > 0)
            {
                cambiandoBateria = true;
                cambioBateriaEn = Time.time + segundosCambioBateria;
            }
            return;
        }

        if (Time.time < cambioBateriaEn) return;

        cambiandoBateria = false;
        if (!inventarioBaterias.TryUse()) return;

        bateria = batMax;
        OnBateriaCambiada?.Invoke(BateriaPorcentaje);
        ActualizarIntensidadYLuz();

        if (audioSource != null && sonidoCambioBateria != null)
            audioSource.PlayOneShot(sonidoCambioBateria);

        if (!encenderTrasCambio) return;
        encenderTrasCambio = false;

        // Si las manos están ocupadas (pistola, radio o bolillo), la linterna vuelve al guardarlos.
        if (ManosOcupadas())
        {
            reanudarTrasArma = true;
            return;
        }

        EncenderLinterna();
    }

    private bool ManosOcupadas()
    {
        return playerMovement != null &&
               (playerMovement.PistolaSacada || playerMovement.RadioEnUso || playerMovement.BolilloEnUso);
    }

    private void EncenderLinterna()
    {
        linternaEncendida = true;
        EstablecerLuzActiva(true);
        playerMovement?.SetLinternaEnMano(true);
        OnLinternaEstadoCambiado?.Invoke(true);
    }

    public void CambiarLinterna()
    {
        if (playerMovement != null && (playerMovement.RadioEnUso || playerMovement.BolilloEnUso)) return;
        if (playerMovement != null && playerMovement.PistolaSacada)
        {
            reanudarTrasArma = true;
            playerMovement.GuardarPistola();
            ReproducirClick();
            return;
        }
        // Si no hay batería, no se puede encender
        if (!linternaEncendida && bateria <= 0f)
        {
            // Efecto de intento fallido (sonido de click sin luz)
            ReproducirClick();
            return;
        }

        linternaEncendida = !linternaEncendida;
        playerMovement?.SetLinternaEnMano(linternaEncendida);

        if (linterna != null)
        {
            EstablecerLuzActiva(linternaEncendida);
            if (linternaEncendida)
            {
                ActualizarIntensidadYLuz();
            }
        }

        ReproducirClick();
        OnLinternaEstadoCambiado?.Invoke(linternaEncendida);
    }

    /// <summary>Apaga la linterna al sacar la pistola y recuerda si estaba encendida.</summary>
    public void ApagarPorArma()
    {
        if (!linternaEncendida) return;
        reanudarTrasArma = true;
        linternaEncendida = false;
        EstablecerLuzActiva(false);
        playerMovement?.SetLinternaEnMano(false);
        OnLinternaEstadoCambiado?.Invoke(false);
    }

    /// <summary>Vuelve a encender la linterna al guardar la pistola, si estaba encendida antes.</summary>
    public void ReanudarTrasArma()
    {
        if (!reanudarTrasArma) return;
        reanudarTrasArma = false;
        if (bateria <= 0f) return;

        linternaEncendida = true;
        EstablecerLuzActiva(true);
        ActualizarIntensidadYLuz();
        playerMovement?.SetLinternaEnMano(true);
        OnLinternaEstadoCambiado?.Invoke(true);
    }
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