using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Vida del jugador, de 0 a 100. Los enemigos (o trampas) llaman a TakeDamage cuando lo golpean.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxHealth = 100f;
    [SerializeField] private float currentHealth = 100f;

    [Header("Recuperación")]
    [Tooltip("Porcentaje de la vida máxima recuperado al eliminar un fantasma.")]
    [SerializeField, Range(0f, 1f)] private float porcentajeVidaPorFantasma = 0.1f;

    private static Material bloodParticleMaterial;

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
        EmitirParticulasSangre();

        if (IsDead)
        {
            float duracionSonidoMuerte = AudioManager.Instance != null
                ? AudioManager.Instance.ReproducirJugadorMuere(transform.position)
                : 0f;
            OnDied?.Invoke();

            // El fundido empieza al mismo tiempo que el sonido y dura lo mismo.
            if (TransitionManager.Instance != null)
                TransitionManager.Instance.ReloadLevel(
                    duracionSonidoMuerte > 0f ? duracionSonidoMuerte : -1f);
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

    /// <summary>Recupera una parte de la vida máxima al eliminar un fantasma.</summary>
    public void RecuperarVidaPorMatarFantasma()
    {
        if (IsDead || porcentajeVidaPorFantasma <= 0f) return;
        Heal(maxHealth * porcentajeVidaPorFantasma);
    }

    private void EmitirParticulasSangre()
    {
        GameObject effectObject = new GameObject("Particulas de sangre");
        effectObject.transform.position = transform.position + Vector3.up * 0.9f;

        ParticleSystem particles = effectObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.35f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.45f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.7f, 1.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.035f, 0.075f);
        main.startColor = new Color(0.62f, 0.015f, 0.025f, 0.9f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 16;
        main.gravityModifier = 0.15f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 8, 12) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.07f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        if (bloodParticleMaterial == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader != null)
                bloodParticleMaterial = new Material(shader) { color = Color.white };
        }
        if (bloodParticleMaterial != null)
            renderer.sharedMaterial = bloodParticleMaterial;

        particles.Play();
        Destroy(effectObject, 2f);
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
