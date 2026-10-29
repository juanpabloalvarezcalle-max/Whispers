using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Mobile pistol controls, magazine, reserve reload, and forward hitscan damage (con autoapuntado opcional).</summary>
public class Gun : MonoBehaviour
{
    [Header("Munición")]
    [SerializeField, Min(1)] private int magazineCapacity = 5;
    [SerializeField, Min(0)] private int ammunitionInMagazine = 5;
    [SerializeField] private AmmoInventory ammunitionInventory;

    [Header("Disparo")]
    [SerializeField] private Transform muzzle;
    [SerializeField, Min(0f)] private float damage = 25f;
    [SerializeField, Min(0.1f)] private float range = 30f;
    [SerializeField, Min(0f)] private float shotsPerSecond = 3f;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private AudioClip sonidoDisparo;

    [Header("Respuesta del arma")]
    [SerializeField, Min(0f)] private float recoilDistance = 0.045f;
    [SerializeField, Min(0.01f)] private float recoilReturnSpeed = 0.35f;
    [SerializeField, Min(0f)] private float shotVolume = 0.65f;

    [Header("Fogonazo (partículas en la boca del arma)")]
    [Tooltip("Material opcional para las partículas. Si lo dejas vacío, se intenta crear uno por código. " +
             "Lo más fiable en iOS es asignar aquí un material con shader 'Universal Render Pipeline/Particles/Unlit'.")]
    [SerializeField] private Material particleMaterial;
    [SerializeField] private Color muzzleFlashColor = new Color(1f, 0.75f, 0.3f, 1f);
    [SerializeField, Min(0f)] private float muzzleFlashSize = 0.22f;
    [Tooltip("Pulso de luz breve al disparar. Si notas lag en el celular, desactívalo.")]
    [SerializeField] private bool useMuzzleLight = true;
    [SerializeField, Min(0f)] private float muzzleLightIntensity = 3f;
    [SerializeField, Min(0.01f)] private float muzzleLightDuration = 0.05f;

    private float nextShotTime;
    private Vector3 restingLocalPosition;
    private float currentRecoil;
    private AudioSource shotAudioSource;
    private ParticleSystem impactEffect;
    private ParticleSystem muzzleFlash;
    private ParticleSystem muzzleSparks;
    private Light muzzleLight;
    private float muzzleLightOffTime;
    private Material fallbackMaterial;
    private Texture2D softTexture;
    private Movement playerMovement;
    private CameraTargetController cameraTargetController;
    private GunAutoAim autoAim;
    private Transform lastParent;
    private readonly RaycastHit[] raycastResults = new RaycastHit[32];

    public int AmmunitionInMagazine => ammunitionInMagazine;
    public int MagazineCapacity => magazineCapacity;
    public int ReserveAmmo => ammunitionInventory != null ? ammunitionInventory.ReserveAmmo : 0;
    public AmmoInventory Inventory => ammunitionInventory;
    public event Action<int, int> AmmunitionChanged;

    private void Awake()
    {
        if (ammunitionInventory == null) ammunitionInventory = GetComponent<AmmoInventory>();
        playerMovement = GetComponentInParent<Movement>();
        cameraTargetController = FindAnyObjectByType<CameraTargetController>();
        autoAim = GetComponent<GunAutoAim>();
        ammunitionInMagazine = Mathf.Clamp(ammunitionInMagazine, 0, magazineCapacity);
        restingLocalPosition = transform.localPosition;
        lastParent = transform.parent;

        shotAudioSource = GetComponent<AudioSource>();
        if (shotAudioSource == null) shotAudioSource = gameObject.AddComponent<AudioSource>();
        shotAudioSource.playOnAwake = false;
        shotAudioSource.spatialBlend = 0f;
        if (sonidoDisparo == null) sonidoDisparo = shotAudioSource.clip;
    }

    private void Update()
    {
        // La pistola cambia de padre al equiparse y al guardarse. Su posición base
        // debe actualizarse para que el retroceso no la saque de la mano.
        if (transform.parent != lastParent)
        {
            lastParent = transform.parent;
            restingLocalPosition = transform.localPosition;
        }

        currentRecoil = Mathf.MoveTowards(currentRecoil, 0f, recoilReturnSpeed * Time.deltaTime);
        transform.localPosition = restingLocalPosition - Vector3.forward * currentRecoil;

        // Apaga la luz del fogonazo pasado su tiempo.
        if (muzzleLight != null && muzzleLight.enabled && Time.time >= muzzleLightOffTime)
            muzzleLight.enabled = false;
    }

    private void OnDestroy()
    {
        if (impactEffect != null) Destroy(impactEffect.gameObject);
        if (fallbackMaterial != null) Destroy(fallbackMaterial);
        if (softTexture != null) Destroy(softTexture);
    }

    private void OnEnable()
    {
        if (ammunitionInventory != null)
            ammunitionInventory.ReserveAmmoChanged += HandleReserveAmmoChanged;
    }

    private void OnDisable()
    {
        if (ammunitionInventory != null)
            ammunitionInventory.ReserveAmmoChanged -= HandleReserveAmmoChanged;
    }

    private void HandleReserveAmmoChanged(int _) => AmmunitionChanged?.Invoke(ammunitionInMagazine, ReserveAmmo);

    public void Shoot()
    {
        if (ammunitionInMagazine <= 0 || Time.time < nextShotTime) return;

        // Disparar también saca la pistola y pone al personaje en pose de apuntado.
        playerMovement?.EquiparPistola();
        playerMovement?.SetApuntando(true);

        Transform shotOrigin = muzzle != null ? muzzle : transform;

        // Autoapuntado: si hay un enemigo a la vista, gira al jugador y dirige la bala hacia él.
        Vector3 shotDirection = shotOrigin.forward;
        Transform shooterBody = playerMovement != null ? playerMovement.transform : transform.root;
        if (autoAim != null && autoAim.TryAim(shooterBody, shotOrigin, out Vector3 aimPoint))
            shotDirection = (aimPoint - shotOrigin.position).normalized;

        ammunitionInMagazine--;
        nextShotTime = Time.time + (shotsPerSecond > 0f ? 1f / shotsPerSecond : 0f);
        AmmunitionChanged?.Invoke(ammunitionInMagazine, ReserveAmmo);
        currentRecoil = recoilDistance;
        cameraTargetController?.TemblarPorDisparo();
        if (sonidoDisparo != null)
        {
            // Cada disparo usa una fuente temporal independiente para que las
            // pulsaciones rápidas no corten ni bloqueen la reproducción anterior.
            AudioSource.PlayClipAtPoint(sonidoDisparo, shotOrigin.position, shotVolume);
        }

        PlayMuzzleFlash(shotOrigin);

        if (TryGetWorldHit(shotOrigin.position, shotDirection, out RaycastHit hit))
        {
            PlayImpactEffect(hit);

            Damageable target = hit.collider.GetComponentInParent<Damageable>();
            if (target != null)
            {
                target.TakeDamage(damage);
            }
            else
            {
                EnemyHealth enemy = hit.collider.GetComponentInParent<EnemyHealth>();
                if (enemy != null) enemy.TakeDamage(damage);
            }
        }
    }

    private bool TryGetWorldHit(Vector3 origin, Vector3 direction, out RaycastHit closestHit)
    {
        closestHit = default;
        int hitCount = Physics.RaycastNonAlloc(origin, direction, raycastResults, range, hitLayers, QueryTriggerInteraction.Ignore);
        float closestDistance = float.MaxValue;
        bool foundWorldHit = false;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = raycastResults[i];
            if (hit.collider == null || hit.collider.transform.IsChildOf(transform.root)) continue;
            if (hit.distance >= closestDistance) continue;

            closestDistance = hit.distance;
            closestHit = hit;
            foundWorldHit = true;
        }

        return foundWorldHit;
    }

    // ───────────────────────── Fogonazo ─────────────────────────

    private void PlayMuzzleFlash(Transform origin)
    {
        EnsureMuzzleFlash(origin);

        // La pistola cambia de padre; el fogonazo debe seguir a la boca del arma.
        if (muzzleFlash.transform.parent != origin)
        {
            muzzleFlash.transform.SetParent(origin, false);
            muzzleFlash.transform.localPosition = Vector3.zero;
            muzzleFlash.transform.localRotation = Quaternion.identity;
        }

        muzzleFlash.Clear(true);
        muzzleFlash.Play(true);

        if (muzzleSparks != null)
        {
            muzzleSparks.Clear(true);
            muzzleSparks.Play(true);
        }

        if (useMuzzleLight && muzzleLight != null)
        {
            muzzleLight.intensity = muzzleLightIntensity;
            muzzleLight.enabled = true;
            muzzleLightOffTime = Time.time + muzzleLightDuration;
        }
    }

    private void EnsureMuzzleFlash(Transform origin)
    {
        if (muzzleFlash != null) return;

        // Destello principal: 2 partículas muy cortas que se encogen.
        muzzleFlash = CreateParticleSystem("Fogonazo", origin);
        ParticleSystem.MainModule main = muzzleFlash.main;
        main.duration = 0.1f;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = 0.06f;
        main.startSpeed = 0f;
        main.startSize = muzzleFlashSize;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = muzzleFlashColor;
        main.maxParticles = 4;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = muzzleFlash.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 2) });

        ParticleSystem.ShapeModule shape = muzzleFlash.shape;
        shape.enabled = false;

        ParticleSystem.SizeOverLifetimeModule size = muzzleFlash.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Chispas que salen hacia adelante desde la boca del arma.
        muzzleSparks = CreateParticleSystem("Chispas", muzzleFlash.transform);
        ParticleSystem.MainModule sparkMain = muzzleSparks.main;
        sparkMain.duration = 0.1f;
        sparkMain.loop = false;
        sparkMain.playOnAwake = false;
        sparkMain.startLifetime = 0.12f;
        sparkMain.startSpeed = new ParticleSystem.MinMaxCurve(3f, 6f);
        sparkMain.startSize = 0.02f;
        sparkMain.startColor = new Color(1f, 0.85f, 0.4f, 1f);
        sparkMain.maxParticles = 8;
        sparkMain.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule sparkEmission = muzzleSparks.emission;
        sparkEmission.rateOverTime = 0f;
        sparkEmission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5) });

        ParticleSystem.ShapeModule sparkShape = muzzleSparks.shape;
        sparkShape.shapeType = ParticleSystemShapeType.Cone; // emite hacia +Z (adelante)
        sparkShape.angle = 10f;
        sparkShape.radius = 0.005f;

        // Pulso de luz (opcional): barato y no depende de ningún shader.
        if (useMuzzleLight)
        {
            GameObject lightObject = new GameObject("Luz del fogonazo");
            lightObject.hideFlags = HideFlags.DontSave;
            lightObject.transform.SetParent(muzzleFlash.transform, false);
            muzzleLight = lightObject.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.color = muzzleFlashColor;
            muzzleLight.range = 3f;
            muzzleLight.shadows = LightShadows.None;
            muzzleLight.enabled = false;
        }
    }

    /// <summary>Crea un ParticleSystem con material asignado (esto evita las partículas rosadas).</summary>
    private ParticleSystem CreateParticleSystem(string objectName, Transform parent)
    {
        GameObject obj = new GameObject(objectName);
        obj.hideFlags = HideFlags.DontSave;
        obj.transform.SetParent(parent, false);

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystemRenderer psRenderer = obj.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.sharedMaterial = GetParticleMaterial();
        return ps;
    }

    private Material GetParticleMaterial()
    {
        if (particleMaterial != null) return particleMaterial;
        if (fallbackMaterial != null) return fallbackMaterial;

        string[] shaderNames =
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Legacy Shaders/Particles/Additive",
            "Sprites/Default"
        };

        Shader shader = null;
        foreach (string shaderName in shaderNames)
        {
            shader = Shader.Find(shaderName);
            if (shader != null) break;
        }

        if (shader == null)
        {
            Debug.LogWarning("Gun: no se encontró ningún shader para las partículas. " +
                             "Asigna un material en el campo 'Particle Material' del Inspector.", this);
            return null;
        }

        fallbackMaterial = new Material(shader) { name = "Partículas del arma (código)" };
        fallbackMaterial.mainTexture = GetSoftTexture();

        // El shader de partículas de URP viene opaco: lo pasamos a transparente aditivo.
        if (shader.name.StartsWith("Universal Render Pipeline"))
        {
            fallbackMaterial.SetFloat("_Surface", 1f);
            fallbackMaterial.SetFloat("_Blend", 2f);
            fallbackMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            fallbackMaterial.SetFloat("_DstBlend", (float)BlendMode.One);
            fallbackMaterial.SetFloat("_ZWrite", 0f);
            fallbackMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            fallbackMaterial.SetOverrideTag("RenderType", "Transparent");
            fallbackMaterial.renderQueue = (int)RenderQueue.Transparent;
        }

        return fallbackMaterial;
    }

    /// <summary>Textura circular suave generada por código para que las partículas no sean cuadrados.</summary>
    private Texture2D GetSoftTexture()
    {
        if (softTexture != null) return softTexture;

        const int resolution = 64;
        softTexture = new Texture2D(resolution, resolution, TextureFormat.RGBA32, false)
        {
            name = "Partícula suave",
            wrapMode = TextureWrapMode.Clamp
        };

        Color32[] pixels = new Color32[resolution * resolution];
        float center = (resolution - 1) * 0.5f;
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 2f);
                byte value = (byte)Mathf.RoundToInt(alpha * 255f);
                pixels[y * resolution + x] = new Color32(255, 255, 255, value);
            }
        }

        softTexture.SetPixels32(pixels);
        softTexture.Apply();
        return softTexture;
    }

    // ───────────────────────── Impacto ─────────────────────────

    private void PlayImpactEffect(RaycastHit hit)
    {
        if (impactEffect == null)
        {
            impactEffect = CreateParticleSystem("Impacto del disparo", null);

            ParticleSystem.MainModule main = impactEffect.main;
            main.duration = 0.18f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.16f;
            main.startSpeed = 1.1f;
            main.startSize = 0.055f;
            main.startColor = new Color(1f, 0.72f, 0.28f, 1f);
            main.maxParticles = 8;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            ParticleSystem.EmissionModule emission = impactEffect.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 6) });

            ParticleSystem.ShapeModule shape = impactEffect.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 22f;
            shape.radius = 0.025f;
        }

        // El cono emite hacia +Z, así que se orienta con LookRotation para salir de la superficie.
        impactEffect.transform.SetPositionAndRotation(hit.point + hit.normal * 0.01f, Quaternion.LookRotation(hit.normal));
        impactEffect.Clear(true);
        impactEffect.Play(true);
    }

    public void Reload()
    {
        if (ammunitionInventory == null || ammunitionInMagazine >= magazineCapacity) return;
        int loadedAmmo = ammunitionInventory.TakeAmmo(magazineCapacity - ammunitionInMagazine);
        if (loadedAmmo <= 0) return;
        ammunitionInMagazine += loadedAmmo;
        AmmunitionChanged?.Invoke(ammunitionInMagazine, ReserveAmmo);
    }
}
