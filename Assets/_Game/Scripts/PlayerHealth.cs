using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Vida del jugador, de 0 a 100. Los enemigos (o trampas) llaman a TakeDamage cuando lo golpean.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Eventos")]
    [Tooltip("Emite la vida como porcentaje de 0 a 1 cada vez que cambia.")]
    public UnityEvent<float> OnHealthChanged;
    [Tooltip("Se dispara una sola vez, cuando la vida llega a 0. Aquí se puede conectar el game over.")]
    public UnityEvent OnDied;

    public float Current => currentHealth;
    public float Max => maxHealth;
    public float Normalized => Mathf.Clamp01(currentHealth / maxHealth);
    public bool IsDead => currentHealth <= 0f;

    private void Awake()
    {
        currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(Normalized);
    }

    /// <summary>Resta vida. Al llegar a 0 dispara OnDied.</summary>
    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        OnHealthChanged?.Invoke(Normalized);

        if (IsDead)
        {
            AudioManager.Instance?.ReproducirJugadorMuere(transform.position);
            OnDied?.Invoke();
        }
        else
        {
            AudioManager.Instance?.ReproducirJugadorHerido(transform.position);
        }
    }

    /// <summary>Suma vida sin pasar del máximo. No revive a un jugador que ya murió.</summary>
    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f) return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        OnHealthChanged?.Invoke(Normalized);
    }

#if ENABLE_INPUT_SYSTEM && UNITY_EDITOR
    // Teclas de prueba mientras no hay enemigos: H = recibir 10 de daño, J = curar 10.
    private void Update()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.hKey.wasPressedThisFrame) TakeDamage(10f);
        if (keyboard.jKey.wasPressedThisFrame) Heal(10f);
    }
#endif
}
