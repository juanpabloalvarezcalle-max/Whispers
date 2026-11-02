using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

/// <summary>
/// Vida del enemigo. La pistola llama a TakeDamage; al llegar a 0 el enemigo muere:
/// se detienen sus scripts y su NavMeshAgent, se lanza la animación de muerte y desaparece.
/// También mantiene una lista estática de enemigos vivos, que usa el autoapuntado.
/// </summary>
public class EnemyHealth : MonoBehaviour
{
    /// <summary>Enemigos vivos en la escena (para el autoapuntado, sin buscar en cada disparo).</summary>
    public static readonly List<EnemyHealth> Active = new List<EnemyHealth>();

    [Header("Vida")]
    [Tooltip("Con 75 de vida y 25 de daño por bala, muere al tercer disparo.")]
    [SerializeField, Min(1f)] private float maxHealth = 75f;
    [Tooltip("Punto al que apunta el autoapuntado (por ejemplo, el pecho). Si se deja vacío, usa el centro del collider.")]
    [SerializeField] private Transform puntoDeApuntado;

    [Tooltip("Daño por bala. 0 = automático (vida máxima / 3). Para el boss pon 10 y morirá con 10 balas.")]
    [SerializeField, Min(0f)] private float danoPorBala = 0f;
    [Tooltip("Multiplicador del daño del bolillo. Permite ajustar la resistencia al bolillo por enemigo.")]
    [SerializeField, Range(0f, 2f)] private float multiplicadorDanoBolillo = 1f;
    public float DanoPorBala => danoPorBala > 0f ? danoPorBala : maxHealth / 3f + 0.001f;
    public float MultiplicadorDanoBolillo => multiplicadorDanoBolillo;

    [Header("Muerte")]
    [SerializeField] private Animator animator;
    [Tooltip("Parámetro Trigger de la animación de muerte (opcional).")]
    [SerializeField] private string parametroMuerte = "Die";
    [SerializeField] private bool destruirAlMorir = true;
    [Tooltip("Segundos que el cuerpo permanece antes de desaparecer (para que se vea la animación).")]
    [SerializeField, Min(0f)] private float segundosHastaDesaparecer = 4f;

    [Header("Audio (opcional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoImpacto;
    [SerializeField] private AudioClip sonidoMuerte;

    [Header("Visibilidad a distancia")]
    [Tooltip("Distancia a la que el enemigo empieza a aparecer. La niebla suaviza la entrada visual.")]
    [SerializeField, Min(1f)] private float distanciaAparicion = 40f;
    [Tooltip("Margen adicional antes de volver a ocultarlo, para evitar parpadeos en el límite.")]
    [SerializeField, Min(0f)] private float margenOcultacion = 4f;
    [Tooltip("Distancia a la que sus luces y brillo alcanzan la intensidad normal.")]
    [SerializeField, Min(0f)] private float distanciaBrilloCompleto = 24f;

    [Header("Eventos")]
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDied;

    private float currentHealth;
    private Collider cachedCollider;
    private PlayerHealth jugadorVisibilidad;
    private Renderer[] renderersEnemigo;
    private bool[] renderersHabilitadosOriginalmente;
    private Light[] lucesEnemigo;
    private bool[] lucesHabilitadasOriginalmente;
    private float[] intensidadesOriginales;
    private readonly List<BrilloMaterial> materialesConBrillo = new List<BrilloMaterial>();
    private bool visualesActivos;
    private float proximaActualizacionVisual;
    private static readonly int GlowId = Shader.PropertyToID("_Glow");

    private sealed class BrilloMaterial
    {
        public Renderer renderer;
        public int indiceMaterial;
        public float intensidadOriginal;
        public MaterialPropertyBlock bloque;
    }

    public float Current => currentHealth;
    public float Maximum => maxHealth;
    public float Normalized => Mathf.Clamp01(currentHealth / maxHealth);
    public bool IsDead => currentHealth <= 0f;

    /// <summary>Punto al que debe apuntar el arma.</summary>
    public Vector3 AimPoint
    {
        get
        {
            if (puntoDeApuntado != null) return puntoDeApuntado.position;
            if (cachedCollider != null) return cachedCollider.bounds.center;
            return transform.position + Vector3.up;
        }
    }

    private void Awake()
    {
        currentHealth = maxHealth;
        cachedCollider = GetComponentInChildren<Collider>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        PrepararVisibilidad();
    }

    private void Update()
    {
        if (IsDead || Time.time < proximaActualizacionVisual) return;
        proximaActualizacionVisual = Time.time + 0.12f;

        if (jugadorVisibilidad == null)
            jugadorVisibilidad = FindAnyObjectByType<PlayerHealth>();
        if (jugadorVisibilidad == null) return;

        float distancia = Vector3.Distance(transform.position, jugadorVisibilidad.transform.position);
        float ocultarA = distanciaAparicion + margenOcultacion;
        if (!visualesActivos && distancia <= distanciaAparicion)
            CambiarVisibilidad(true);
        else if (visualesActivos && distancia >= ocultarA)
            CambiarVisibilidad(false);

        float intensidad = Mathf.InverseLerp(distanciaAparicion, distanciaBrilloCompleto, distancia);
        ActualizarBrillo(intensidad);
    }

    private void PrepararVisibilidad()
    {
        jugadorVisibilidad = FindAnyObjectByType<PlayerHealth>();

        renderersEnemigo = GetComponentsInChildren<Renderer>(true);
        renderersHabilitadosOriginalmente = new bool[renderersEnemigo.Length];
        for (int i = 0; i < renderersEnemigo.Length; i++)
        {
            Renderer rendererEnemigo = renderersEnemigo[i];
            renderersHabilitadosOriginalmente[i] = rendererEnemigo.enabled;
            Material[] materiales = rendererEnemigo.sharedMaterials;
            for (int j = 0; j < materiales.Length; j++)
            {
                Material material = materiales[j];
                if (material == null || !material.HasProperty(GlowId)) continue;

                MaterialPropertyBlock bloque = new MaterialPropertyBlock();
                rendererEnemigo.GetPropertyBlock(bloque, j);
                materialesConBrillo.Add(new BrilloMaterial
                {
                    renderer = rendererEnemigo,
                    indiceMaterial = j,
                    intensidadOriginal = material.GetFloat(GlowId),
                    bloque = bloque
                });
            }
        }

        lucesEnemigo = GetComponentsInChildren<Light>(true);
        lucesHabilitadasOriginalmente = new bool[lucesEnemigo.Length];
        intensidadesOriginales = new float[lucesEnemigo.Length];
        for (int i = 0; i < lucesEnemigo.Length; i++)
        {
            lucesHabilitadasOriginalmente[i] = lucesEnemigo[i].enabled;
            intensidadesOriginales[i] = lucesEnemigo[i].intensity;
        }

        CambiarVisibilidad(false);
        ActualizarBrillo(0f);
    }

    private void CambiarVisibilidad(bool visible)
    {
        visualesActivos = visible;
        if (renderersEnemigo != null)
        {
            for (int i = 0; i < renderersEnemigo.Length; i++)
                renderersEnemigo[i].enabled = visible && renderersHabilitadosOriginalmente[i];
        }

        if (lucesEnemigo != null)
        {
            for (int i = 0; i < lucesEnemigo.Length; i++)
            {
                lucesEnemigo[i].enabled = visible && lucesHabilitadasOriginalmente[i];
                if (!visible) lucesEnemigo[i].intensity = 0f;
            }
        }
    }

    private void ActualizarBrillo(float intensidad)
    {
        intensidad = Mathf.Clamp01(intensidad);
        foreach (BrilloMaterial brillo in materialesConBrillo)
        {
            if (brillo.renderer == null) continue;
            brillo.bloque.SetFloat(GlowId, brillo.intensidadOriginal * intensidad);
            brillo.renderer.SetPropertyBlock(brillo.bloque, brillo.indiceMaterial);
        }

        if (lucesEnemigo == null) return;
        for (int i = 0; i < lucesEnemigo.Length; i++)
            lucesEnemigo[i].intensity = intensidadesOriginales[i] * intensidad;
    }

    private void OnEnable()
    {
        if (!Active.Contains(this)) Active.Add(this);
    }

    private void OnDisable()
    {
        Active.Remove(this);
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(Normalized);
    }

    /// <summary>Resta vida. Al llegar a 0, el enemigo muere.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(Normalized);

        if (IsDead)
            Die();
        else if (AudioManager.Instance != null)
            AudioManager.Instance.ReproducirEnemigoHerido(transform.position);
        else if (audioSource != null && sonidoImpacto != null)
            audioSource.PlayOneShot(sonidoImpacto);
    }

    private void Die()
    {
        GhostEnemy fantasma = GetComponent<GhostEnemy>();
        if (fantasma == null) fantasma = GetComponentInChildren<GhostEnemy>();
        PlayerHealth jugador = fantasma != null ? fantasma.Jugador : null;

        Active.Remove(this);

        // Detiene el movimiento y desactiva los demás scripts (persecución, ataque, visión...).
        NavMeshAgent agente = GetComponent<NavMeshAgent>();
        if (agente != null)
        {
            if (agente.isOnNavMesh) agente.isStopped = true;
            agente.enabled = false;
        }

        foreach (MonoBehaviour script in GetComponents<MonoBehaviour>())
            if (script != this) script.enabled = false;

        // Sin colliders: ya no estorba ni recibe más balas.
        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        if (animator != null && TieneParametroTrigger(parametroMuerte))
            animator.SetTrigger(parametroMuerte);

        if (AudioManager.Instance != null)
            AudioManager.Instance.ReproducirEnemigoMuere(transform.position);
        else if (audioSource != null && sonidoMuerte != null)
            audioSource.PlayOneShot(sonidoMuerte);

        jugador?.RecuperarVidaPorMatarFantasma();

        OnDied?.Invoke();

        if (destruirAlMorir)
            Destroy(gameObject, segundosHastaDesaparecer);
    }

    private bool TieneParametroTrigger(string nombre)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == nombre && p.type == AnimatorControllerParameterType.Trigger) return true;
        return false;
    }
}
