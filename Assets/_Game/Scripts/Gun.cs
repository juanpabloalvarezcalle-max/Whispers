using System;
using UnityEngine;

/// <summary>Mobile pistol controls, magazine, reserve reload, and forward hitscan damage.</summary>
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

    [Header("Respuesta del arma")]
    [SerializeField, Min(0f)] private float recoilDistance = 0.045f;
    [SerializeField, Min(0.01f)] private float recoilReturnSpeed = 0.35f;
    [SerializeField, Min(0f)] private float shotVolume = 0.65f;

    private float nextShotTime;
    private Vector3 restingLocalPosition;
    private float currentRecoil;
    private AudioSource shotAudioSource;
    private AudioClip generatedShotClip;
    private ParticleSystem impactEffect;
    private readonly RaycastHit[] raycastResults = new RaycastHit[32];

    public int AmmunitionInMagazine => ammunitionInMagazine;
    public int MagazineCapacity => magazineCapacity;
    public int ReserveAmmo => ammunitionInventory != null ? ammunitionInventory.ReserveAmmo : 0;
    public AmmoInventory Inventory => ammunitionInventory;
    public event Action<int, int> AmmunitionChanged;

    private void Awake()
    {
        if (ammunitionInventory == null) ammunitionInventory = GetComponent<AmmoInventory>();
        ammunitionInMagazine = Mathf.Clamp(ammunitionInMagazine, 0, magazineCapacity);
        restingLocalPosition = transform.localPosition;

        shotAudioSource = GetComponent<AudioSource>();
        if (shotAudioSource == null) shotAudioSource = gameObject.AddComponent<AudioSource>();
        shotAudioSource.playOnAwake = false;
        shotAudioSource.spatialBlend = 0f;
        generatedShotClip = CreateGeneratedShotClip();
    }

    private void Update()
    {
        currentRecoil = Mathf.MoveTowards(currentRecoil, 0f, recoilReturnSpeed * Time.deltaTime);
        transform.localPosition = restingLocalPosition - Vector3.forward * currentRecoil;
    }

    private void OnDestroy()
    {
        if (generatedShotClip != null) Destroy(generatedShotClip);
        if (impactEffect != null) Destroy(impactEffect.gameObject);
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

        Transform shotOrigin = muzzle != null ? muzzle : transform;
        ammunitionInMagazine--;
        nextShotTime = Time.time + (shotsPerSecond > 0f ? 1f / shotsPerSecond : 0f);
        AmmunitionChanged?.Invoke(ammunitionInMagazine, ReserveAmmo);
        currentRecoil = recoilDistance;
        if (shotAudioSource != null && generatedShotClip != null)
            shotAudioSource.PlayOneShot(generatedShotClip, shotVolume);

        if (TryGetWorldHit(shotOrigin, out RaycastHit hit))
        {
            PlayImpactEffect(hit);
            Damageable target = hit.collider.GetComponentInParent<Damageable>();
            if (target != null) target.TakeDamage(damage);
        }
    }

    private bool TryGetWorldHit(Transform shotOrigin, out RaycastHit closestHit)
    {
        closestHit = default;
        int hitCount = Physics.RaycastNonAlloc(shotOrigin.position, shotOrigin.forward, raycastResults, range, hitLayers, QueryTriggerInteraction.Ignore);
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

    private void PlayImpactEffect(RaycastHit hit)
    {
        if (impactEffect == null)
        {
            GameObject effectObject = new GameObject("Impacto del disparo");
            effectObject.hideFlags = HideFlags.DontSave;
            impactEffect = effectObject.AddComponent<ParticleSystem>();

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

            var renderer = impactEffect.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
        }

        impactEffect.transform.SetPositionAndRotation(hit.point + hit.normal * 0.01f, Quaternion.FromToRotation(Vector3.up, hit.normal));
        impactEffect.Clear(true);
        impactEffect.Play(true);
    }

    private AudioClip CreateGeneratedShotClip()
    {
        const int sampleRate = 22050;
        const float duration = 0.2f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        uint noiseState = 0xA341316Cu;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            noiseState = 1664525u * noiseState + 1013904223u;
            float noise = ((noiseState >> 8) / 8388607.5f) - 1f;
            float transient = noise * Mathf.Exp(-time * 48f) * 0.48f;
            float lowThump = Mathf.Sin(time * Mathf.PI * 2f * 78f) * Mathf.Exp(-time * 19f) * 0.52f;
            samples[i] = Mathf.Clamp(transient + lowThump, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Disparo procedural", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
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
