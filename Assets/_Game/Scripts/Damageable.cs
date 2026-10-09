using System;
using UnityEngine;

/// <summary>Health component that can receive damage from weapons.</summary>
public class Damageable : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maximumHealth = 100f;
    [SerializeField] private bool destroyOnDeath;
    private float currentHealth;

    public float CurrentHealth => currentHealth;
    public float MaximumHealth => maximumHealth;
    public event Action<float, float> HealthChanged;
    public event Action Died;

    private void Awake() => currentHealth = maximumHealth;

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f) return;
        currentHealth = Mathf.Max(0f, currentHealth - amount);
        HealthChanged?.Invoke(currentHealth, maximumHealth);
        if (currentHealth > 0f) return;
        Died?.Invoke();
        if (destroyOnDeath) Destroy(gameObject);
    }
}
