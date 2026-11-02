using UnityEngine;

/// <summary>Gestor central de efectos y ambiente del juego.</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Fuentes")]
    [Tooltip("AudioSource para efectos. Si queda vacío, el manager crea uno en este objeto.")]
    [SerializeField] private AudioSource efectosSource;
    [Tooltip("AudioSource para el ambiente en bucle. Si queda vacío, el manager crea uno en este objeto.")]
    [SerializeField] private AudioSource ambienteSource;
    [SerializeField, Range(0f, 1f)] private float volumenEfectos = 1f;

    [Header("Jugador")]
    [SerializeField] private AudioClip pasos;
    [Tooltip("Variantes de pasos que se alternan aleatoriamente con el clip principal.")]
    [SerializeField] private AudioClip[] variantesPasos;
    [SerializeField, Range(0f, 1f)] private float volumenPasos = 1f;
    [SerializeField] private AudioClip sacarArma;
    [SerializeField, Range(0f, 1f)] private float volumenSacarArma = 1f;
    [SerializeField] private AudioClip bolillo;
    [SerializeField, Range(0f, 1f)] private float volumenBolillo = 1f;
    [SerializeField] private AudioClip linterna;
    [SerializeField, Range(0f, 1f)] private float volumenLinterna = 1f;
    [SerializeField] private AudioClip radio;
    [SerializeField, Range(0f, 1f)] private float volumenRadio = 1f;
    [SerializeField] private AudioClip jugadorHerido;
    [SerializeField, Range(0f, 1f)] private float volumenJugadorHerido = 1f;
    [SerializeField] private AudioClip jugadorMuere;
    [SerializeField, Range(0f, 1f)] private float volumenJugadorMuere = 1f;
    [SerializeField, Min(0.05f)] private float intervaloPasos = 0.4f;

    [Header("Enemigos")]
    [SerializeField] private AudioClip enemigo;
    [SerializeField, Range(0f, 1f)] private float volumenEnemigo = 1f;
    [SerializeField] private AudioClip enemigoHerido;
    [SerializeField, Range(0f, 1f)] private float volumenEnemigoHerido = 1f;
    [SerializeField] private AudioClip enemigoMuere;
    [SerializeField, Range(0f, 1f)] private float volumenEnemigoMuere = 1f;

    [Header("Boss final")]
    [SerializeField] private AudioClip boss;
    [SerializeField, Range(0f, 1f)] private float volumenBoss = 1f;
    [SerializeField] private AudioClip bossHerido;
    [SerializeField, Range(0f, 1f)] private float volumenBossHerido = 1f;
    [Tooltip("Primer sonido que acompaña la animación de lanzamiento del boss.")]
    [SerializeField] private AudioClip bossLanzaAtaque1;
    [SerializeField, Range(0f, 1f)] private float volumenBossLanzaAtaque1 = 1f;
    [Tooltip("Segundo sonido que acompaña la animación de lanzamiento del boss.")]
    [SerializeField] private AudioClip bossLanzaAtaque2;
    [SerializeField, Range(0f, 1f)] private float volumenBossLanzaAtaque2 = 1f;
    [SerializeField] private AudioClip bossMuere;
    [SerializeField, Range(0f, 1f)] private float volumenBossMuere = 1f;

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
        efectosSource.spatialBlend = 1f;

        if (ambienteSource == null || ambienteSource == efectosSource)
            ambienteSource = gameObject.AddComponent<AudioSource>();
        ambienteSource.playOnAwake = false;
        ambienteSource.loop = true;
        ambienteSource.spatialBlend = 0f;

        if (bosque != null)
        {
            ambienteSource.clip = bosque;
            ambienteSource.volume = volumenBosque;
            ambienteSource.Play();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (ambienteSource != null && bosque != null && ambienteSource.isPlaying)
            ambienteSource.volume = volumenBosque;
    }

    public void ReproducirPasos(Vector3 posicion)
    {
        if (Time.time < siguientePaso) return;
        siguientePaso = Time.time + intervaloPasos;
        AudioClip clip = ElegirPaso();
        ReproducirEn(clip, posicion, volumenPasos);
    }

    private AudioClip ElegirPaso()
    {
        int cantidad = pasos != null ? 1 : 0;
        if (variantesPasos != null)
        {
            for (int i = 0; i < variantesPasos.Length; i++)
                if (variantesPasos[i] != null) cantidad++;
        }

        if (cantidad == 0) return null;

        int elegido = Random.Range(0, cantidad);
        if (pasos != null)
        {
            if (elegido == 0) return pasos;
            elegido--;
        }

        for (int i = 0; i < variantesPasos.Length; i++)
        {
            if (variantesPasos[i] == null) continue;
            if (elegido-- == 0) return variantesPasos[i];
        }

        return pasos;
    }

    public void ReproducirSacarArma(Vector3 posicion) => ReproducirEn(sacarArma, posicion, volumenSacarArma);
    public void ReproducirBolillo(Vector3 posicion) => ReproducirEn(bolillo, posicion, volumenBolillo);
    public void ReproducirLinterna(Vector3 posicion) => ReproducirEn(linterna, posicion, volumenLinterna);
    public void ReproducirRadio(Vector3 posicion) => ReproducirEn(radio, posicion, volumenRadio);
    public void ReproducirEnemigo(Vector3 posicion) => ReproducirEn(enemigo, posicion, volumenEnemigo);
    public void ReproducirEnemigoHerido(Vector3 posicion) => ReproducirEn(enemigoHerido, posicion, volumenEnemigoHerido);
    public void ReproducirEnemigoMuere(Vector3 posicion) => ReproducirEn(enemigoMuere, posicion, volumenEnemigoMuere);
    public void ReproducirBoss(Vector3 posicion) => ReproducirEn(boss, posicion, volumenBoss);
    public void ReproducirBossHerido(Vector3 posicion) => ReproducirEn(bossHerido, posicion, volumenBossHerido);
    public void ReproducirBossLanzaAtaque(Vector3 posicion)
    {
        ReproducirEn(bossLanzaAtaque1, posicion, volumenBossLanzaAtaque1);
        ReproducirEn(bossLanzaAtaque2, posicion, volumenBossLanzaAtaque2);
    }
    public void ReproducirBossMuere(Vector3 posicion) => ReproducirEn(bossMuere, posicion, volumenBossMuere);
    public void ReproducirJugadorHerido(Vector3 posicion) => ReproducirEn(jugadorHerido, posicion, volumenJugadorHerido);

    /// <summary>Devuelve la duración para que la escena no se recargue antes de terminar el clip.</summary>
    public float ReproducirJugadorMuere(Vector3 posicion)
    {
        ReproducirEn(jugadorMuere, posicion, volumenJugadorMuere);
        return jugadorMuere != null ? jugadorMuere.length : 0f;
    }

    /// <summary>Reproduce un efecto en su posición del mundo usando su volumen individual y el global.</summary>
    public void ReproducirEn(AudioClip clip, Vector3 posicion, float volumenIndividual = 1f)
    {
        if (clip == null) return;

        // Cada sonido tiene su propia fuente temporal para conservar su posición 3D
        // y usar la misma salida/mixer configurada en Efectos Source.
        GameObject sonido = new GameObject($"Audio_{clip.name}");
        sonido.transform.position = posicion;
        AudioSource source = sonido.AddComponent<AudioSource>();
        source.clip = clip;
        source.playOnAwake = false;
        source.loop = false;
        source.volume = volumenEfectos * volumenIndividual;
        source.outputAudioMixerGroup = efectosSource != null ? efectosSource.outputAudioMixerGroup : null;
        if (efectosSource != null)
        {
            source.spatialBlend = efectosSource.spatialBlend;
            source.rolloffMode = efectosSource.rolloffMode;
            source.minDistance = efectosSource.minDistance;
            source.maxDistance = efectosSource.maxDistance;
        }
        source.Play();
        Destroy(sonido, clip.length + 0.1f);
    }
}
