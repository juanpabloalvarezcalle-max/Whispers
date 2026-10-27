using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Enemigo tipo fantasma: si el jugador entra en su rango de visión lo persigue,
/// y al llegar a distancia de golpe lo ataca cuerpo a cuerpo (cabezazo) haciendo daño.
/// Requiere un NavMeshAgent y un NavMesh horneado en la escena.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class GhostEnemy : MonoBehaviour
{
    private enum Estado { Reposo, Persiguiendo, Atacando }

    [Header("Objetivo")]
    [Tooltip("Si se deja vacío, se busca el objeto con PlayerHealth.")]
    [SerializeField] private PlayerHealth jugador;

    [Header("Detección")]
    [SerializeField, Min(1f)] private float distanciaVision = 10f;
    [Tooltip("Una vez que te ve, te sigue hasta que te alejes más de esta distancia.")]
    [SerializeField, Min(1f)] private float distanciaPerdida = 16f;
    [Tooltip("Ángulo total de visión frente al zombi. 360 = te detecta en todas direcciones.")]
    [SerializeField, Range(30f, 360f)] private float anguloVision = 140f;
    [Tooltip("Si está activo, las paredes bloquean la visión.")]
    [SerializeField] private bool requiereLineaDeVision = true;
    [SerializeField] private LayerMask capasObstaculos = ~0;
    [SerializeField] private float alturaOjos = 1.6f;
    [Tooltip("Si el jugador está más cerca que esto, lo detecta aunque esté fuera del cono de visión.")]
    [SerializeField, Min(0f)] private float distanciaOido = 3f;

    [Header("Movimiento")]
    [SerializeField, Min(0.1f)] private float velocidadPersecucion = 1.6f;
    [SerializeField, Min(1f)] private float velocidadGiro = 360f;
    [Tooltip("Cada cuántos segundos recalcula la ruta hacia el jugador.")]
    [SerializeField, Min(0.05f)] private float intervaloRuta = 0.2f;

    [Header("Ataque")]
    [SerializeField, Min(0.5f)] private float distanciaAtaque = 1.5f;
    [SerializeField, Min(1f)] private float dano = 15f;
    [Tooltip("Segundos entre el inicio de un golpe y el siguiente.")]
    [SerializeField, Min(0.2f)] private float tiempoEntreAtaques = 2f;
    [Tooltip("Duración total de la animación de ataque. Mientras dura, el zombi no se mueve.")]
    [SerializeField, Min(0.1f)] private float duracionAtaque = 1.2f;
    [Tooltip("Activado: el daño se aplica cuando la animación llama al evento AplicarGolpe(). Desactivado: se aplica solo tras 'retrasoGolpe'.")]
    [SerializeField] private bool usarEventoDeAnimacion = true;
    [Tooltip("Solo se usa si no hay evento de animación.")]
    [SerializeField, Min(0f)] private float retrasoGolpe = 0.5f;
    [Tooltip("Margen extra al momento del impacto: si te alejaste más que distanciaAtaque + este margen, el golpe falla.")]
    [SerializeField, Min(0f)] private float margenImpacto = 0.5f;

    [Header("Animator (opcional)")]
    [SerializeField] private Animator animator;
    [Tooltip("Parámetro Float con la velocidad actual (para caminar/reposo).")]
    [SerializeField] private string parametroVelocidad = "Speed";
    [Tooltip("Parámetro Trigger que inicia la animación de ataque.")]
    [SerializeField] private string parametroAtaque = "Attack";

    [Header("Audio (opcional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoAvistamiento;
    [SerializeField] private AudioClip sonidoGolpe;

    private NavMeshAgent agente;
    private Estado estado = Estado.Reposo;
    private float proximoAtaque;
    private float finAtaque;
    private float momentoGolpeAutomatico;
    private bool golpeAplicado;
    private float proximaRuta;
    private int hashVelocidad;
    private int hashAtaque;
    private bool tieneParamVelocidad;
    private bool tieneParamAtaque;

    private void Awake()
    {
        agente = GetComponent<NavMeshAgent>();
        agente.speed = velocidadPersecucion;
        agente.stoppingDistance = Mathf.Max(0.1f, distanciaAtaque * 0.8f);
        agente.updateRotation = false; // la rotación la controlamos nosotros

        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        hashVelocidad = Animator.StringToHash(parametroVelocidad);
        hashAtaque = Animator.StringToHash(parametroAtaque);
        tieneParamVelocidad = TieneParametro(parametroVelocidad, AnimatorControllerParameterType.Float);
        tieneParamAtaque = TieneParametro(parametroAtaque, AnimatorControllerParameterType.Trigger);

        if (jugador == null) jugador = FindAnyObjectByType<PlayerHealth>();
        if (jugador == null)
            Debug.LogWarning("ZombieEnemy no encontró un PlayerHealth en la escena.", this);
    }

    private void Update()
    {
        if (jugador == null || jugador.IsDead)
        {
            Detener();
            ActualizarAnimator();
            return;
        }

        float distancia = DistanciaPlana(jugador.transform.position);

        switch (estado)
        {
            case Estado.Reposo:
                if (PuedeVerJugador(distancia))
                {
                    estado = Estado.Persiguiendo;
                    Reproducir(sonidoAvistamiento);
                }
                break;

            case Estado.Persiguiendo:
                if (distancia > distanciaPerdida)
                {
                    estado = Estado.Reposo;
                    Detener();
                    break;
                }

                if (distancia <= distanciaAtaque && Time.time >= proximoAtaque)
                {
                    IniciarAtaque();
                    break;
                }

                Perseguir(distancia);
                break;

            case Estado.Atacando:
                MirarAlJugador(0.5f);

                if (usarEventoDeAnimacion == false && golpeAplicado == false && Time.time >= momentoGolpeAutomatico)
                    AplicarGolpe();

                if (Time.time >= finAtaque)
                {
                    estado = Estado.Persiguiendo;
                    agente.isStopped = false;
                }
                break;
        }

        ActualizarAnimator();
    }

    private bool PuedeVerJugador(float distancia)
    {
        if (distancia > distanciaVision) return false;
        if (distancia <= distanciaOido) return true;

        Vector3 haciaJugador = jugador.transform.position - transform.position;
        haciaJugador.y = 0f;
        if (haciaJugador.sqrMagnitude < 0.01f) return true;

        if (Vector3.Angle(transform.forward, haciaJugador) > anguloVision * 0.5f) return false;

        if (!requiereLineaDeVision) return true;

        Vector3 origen = transform.position + Vector3.up * alturaOjos;
        Vector3 destino = jugador.transform.position + Vector3.up * alturaOjos * 0.8f;
        if (Physics.Linecast(origen, destino, out RaycastHit golpe, capasObstaculos, QueryTriggerInteraction.Ignore))
            return golpe.transform.IsChildOf(jugador.transform);

        return true;
    }

    private void Perseguir(float distancia)
    {
        agente.isStopped = false;

        if (Time.time >= proximaRuta)
        {
            proximaRuta = Time.time + intervaloRuta;
            agente.SetDestination(jugador.transform.position);
        }

        // Gira hacia donde se mueve el agente.
        Vector3 direccion = agente.desiredVelocity;
        direccion.y = 0f;
        if (direccion.sqrMagnitude > 0.01f)
        {
            Quaternion objetivo = Quaternion.LookRotation(direccion);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, objetivo, velocidadGiro * Time.deltaTime);
        }
        else if (distancia <= distanciaAtaque * 1.5f)
        {
            MirarAlJugador(1f);
        }
    }

    private void IniciarAtaque()
    {
        estado = Estado.Atacando;
        golpeAplicado = false;
        proximoAtaque = Time.time + tiempoEntreAtaques;
        finAtaque = Time.time + duracionAtaque;
        momentoGolpeAutomatico = Time.time + retrasoGolpe;

        agente.isStopped = true;
        agente.velocity = Vector3.zero;

        if (animator != null && tieneParamAtaque)
            animator.SetTrigger(hashAtaque);
    }

    /// <summary>
    /// Momento del impacto del cabezazo. Se llama desde un Animation Event
    /// en el frame exacto del golpe (o automáticamente si no se usan eventos).
    /// </summary>
    public void AplicarGolpe()
    {
        // Un enemigo muerto (script desactivado) no puede golpear aunque llegue el evento de animación.
        if (!enabled) return;
        if (estado != Estado.Atacando || golpeAplicado) return;
        golpeAplicado = true;

        if (jugador == null || jugador.IsDead) return;

        // Si el jugador ya se alejó o quedó detrás, el golpe falla.
        float distancia = DistanciaPlana(jugador.transform.position);
        if (distancia > distanciaAtaque + margenImpacto) return;

        Vector3 haciaJugador = jugador.transform.position - transform.position;
        haciaJugador.y = 0f;
        if (haciaJugador.sqrMagnitude > 0.01f && Vector3.Angle(transform.forward, haciaJugador) > 90f) return;

        jugador.TakeDamage(dano);
        Reproducir(sonidoGolpe);
    }

    private void MirarAlJugador(float factorVelocidad)
    {
        Vector3 direccion = jugador.transform.position - transform.position;
        direccion.y = 0f;
        if (direccion.sqrMagnitude < 0.001f) return;

        Quaternion objetivo = Quaternion.LookRotation(direccion);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, objetivo, velocidadGiro * factorVelocidad * Time.deltaTime);
    }

    private void Detener()
    {
        if (agente != null && agente.isOnNavMesh)
        {
            agente.isStopped = true;
            agente.velocity = Vector3.zero;
        }
        if (estado == Estado.Atacando) estado = Estado.Reposo;
    }

    private void ActualizarAnimator()
    {
        if (animator == null || !tieneParamVelocidad) return;
        animator.SetFloat(hashVelocidad, agente.velocity.magnitude, 0.1f, Time.deltaTime);
    }

    private float DistanciaPlana(Vector3 punto)
    {
        Vector3 d = punto - transform.position;
        d.y = 0f;
        return d.magnitude;
    }

    private bool TieneParametro(string nombre, AnimatorControllerParameterType tipo)
    {
        if (animator == null || animator.runtimeAnimatorController == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == nombre && p.type == tipo) return true;
        return false;
    }

    private void Reproducir(AudioClip clip)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.ReproducirEnemigo(transform.position);
            return;
        }
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, distanciaVision);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, distanciaOido);
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, distanciaPerdida);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanciaAtaque);
    }
#endif
}
