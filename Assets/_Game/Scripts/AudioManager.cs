using UnityEngine;

/// <summary>
/// Gestor central de audio. Asigna los clips desde el Inspector y llama sus métodos
/// desde las mecánicas del juego o desde eventos de animación/UI.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Fuentes")]
    [SerializeField] private AudioSource efectosSource;
    [SerializeField] private AudioSource ambienteSource;
    [SerializeField, Range(0f, 1f)] private float volumenEfectos = 1f;

    [Header("Jugador")]
    [SerializeField] private AudioClip pasos;
    [SerializeField] private AudioClip sacarArma;
    [SerializeField] private AudioClip linterna;
    [SerializeField] private AudioClip radio;
    [SerializeField] private AudioClip jugadorHerido;
    [SerializeField] private AudioClip jugadorMuere;
    [SerializeField, Min(0.05f)] private float intervaloPasos = 0.4f;

    [Header("Enemigos")]
    [SerializeField] private AudioClip enemigo;
    [SerializeField] private AudioClip enemigoHerido;
    [SerializeField] private AudioClip enemigoMuere;

    [Header("Boss final")]
    [SerializeField] private AudioClip boss;
    [SerializeField] private AudioClip bossHerido;
    [SerializeField] private AudioClip bossMuere;

    [Header("Ambiente")]
    [SerializeField] private AudioClip bosque;
    [SerializeField, Range(0f, 1f)] private float volumenBosque = 0.6f;

    private float siguientePaso;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("Hay más de un AudioManager en la escena. Se eliminará este duplicado.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (efectosSource == null) efectosSource = GetComponent<AudioSource>();
        if (efectosSource == null) efectosSource = gameObject.AddComponent<AudioSource>();
        efectosSource.playOnAwake = false;
        efectosSource.loop = false;

        if (ambienteSource == null)
        {
            ambienteSource = gameObject.AddComponent<AudioSource>();
            ambienteSource.playOnAwake = false;
            ambienteSource.loop = true;
            ambienteSource.spatialBlend = 0f;
        }

        if (bosque != null)
        {
            ambienteSource.clip = bosque;
            ambienteSource.volume = volumenBosque;
            ambienteSource.loop = true;
            ambienteSource.Play();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void ReproducirPasos(Vector3 posicion)
    {
        if (Time.time < siguientePaso) return;
        siguientePaso = Time.time + intervaloPasos;
        ReproducirEn(pasos, posicion);
    }

    public void ReproducirSacarArma(Vector3 posicion) => ReproducirEn(sacarArma, posicion);
    public void ReproducirLinterna(Vector3 posicion) => ReproducirEn(linterna, posicion);
    public void ReproducirRadio(Vector3 posicion) => ReproducirEn(radio, posicion);
    public void ReproducirEnemigo(Vector3 posicion) => ReproducirEn(enemigo, posicion);
    public void ReproducirEnemigoHerido(Vector3 posicion) => ReproducirEn(enemigoHerido, posicion);
    public void ReproducirEnemigoMuere(Vector3 posicion) => ReproducirEn(enemigoMuere, posicion);
    public void ReproducirBoss(Vector3 posicion) => ReproducirEn(boss, posicion);
    public void ReproducirBossHerido(Vector3 posicion) => ReproducirEn(bossHerido, posicion);
    public void ReproducirBossMuere(Vector3 posicion) => ReproducirEn(bossMuere, posicion);
    public void ReproducirJugadorHerido(Vector3 posicion) => ReproducirEn(jugadorHerido, posicion);
    public void ReproducirJugadorMuere(Vector3 posicion) => ReproducirEn(jugadorMuere, posicion);

    /// <summary>Permite reproducir efectos ubicados en el mundo, como voces enemigas.</summary>
    public void ReproducirEn(AudioClip clip, Vector3 posicion)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, posicion, volumenEfectos);
    }
}
