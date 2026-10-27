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

    [Header("Eventos")]
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDied;

    private float currentHealth;
    private Collider cachedCollider;

    public float Current => currentHealth;
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
        else if (audioSource != null && sonidoImpacto != null)
            audioSource.PlayOneShot(sonidoImpacto);
    }

    private void Die()
    {
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

        if (audioSource != null && sonidoMuerte != null)
            audioSource.PlayOneShot(sonidoMuerte);

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
