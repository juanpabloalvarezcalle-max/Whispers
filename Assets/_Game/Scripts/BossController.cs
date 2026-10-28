using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : MonoBehaviour
{
    public enum SpellPattern
    {
        Aimed, // Va directo hacia el jugador.
        Fan    // Abanico de proyectiles.
    }

    [System.Serializable]
    public class Spell
    {
        public string name = "Hechizo";
        public SpellPattern pattern = SpellPattern.Aimed;
        [Min(1)] public int projectiles = 1;
        [Range(0f, 180f)] public float fanAngle = 40f;
        [Min(0f)] public float damage = 10f;
        [Min(0.1f)] public float speed = 8f;
        [Min(0.1f)] public float lifetime = 5f;
        public Color color = new Color(0.7f, 0.2f, 1f);
        public GameObject prefab;
    }

    [Header("Objetivo")]
    [SerializeField] private PlayerHealth player;

    [Header("Movimiento y Patrulla")]
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float range = 6f;
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "Velocidad";

    [Header("Hechizos")]
    [SerializeField]
    private List<Spell> spells = new List<Spell>
    {
        new Spell { name = "Bola", pattern = SpellPattern.Aimed, projectiles = 1, damage = 12f, speed = 9f, color = new Color(0.7f, 0.2f, 1f) },
        new Spell { name = "Abanico", pattern = SpellPattern.Fan, projectiles = 5, fanAngle = 60f, damage = 6f, speed = 7f, color = new Color(1f, 0.5f, 0.1f) }
    };
    [SerializeField, Min(0.5f)] private float spellInterval = 4f;
    [SerializeField] private bool randomSpell = true;
    [SerializeField] private Transform castPoint;
    [SerializeField, Min(0f)] private float shotInterval = 0.4f;
    [SerializeField] private string castTrigger = "Hechizo";

    [Header("Invocaciones")]
    [SerializeField] private GameObject minionPrefab;
    [SerializeField, Min(0.5f)] private float summonInterval = 12f;
    [SerializeField, Min(1)] private int minionsPerSummon = 2;
    [SerializeField, Min(1)] private int maxMinions = 4;
    [SerializeField, Min(0f)] private float summonRadius = 4f;
    [SerializeField] private string summonTrigger = "Invocar";

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool movingForward = true;
    private float currentAnimSpeed = 0f;

    private float nextSpellTime;
    private float nextSummonTime;
    private int spellIndex;

    private readonly List<GameObject> minions = new List<GameObject>();
    private readonly Dictionary<Color, Material> placeholderMaterials = new Dictionary<Color, Material>();

    private bool PlayerDead => player != null && player.IsDead;
    private Vector3 AimPoint => player != null ? player.transform.position + Vector3.up * 1.0f : transform.position + transform.forward * 5f;

    private void Start()
    {
        if (player == null) player = FindFirstObjectByType<PlayerHealth>();
        if (animator == null) animator = GetComponentInChildren<Animator>();

        startPosition = transform.position;
        SetNewDestination();

        // Inicializamos los tiempos de forma segura
        nextSpellTime = Time.time + spellInterval;
        nextSummonTime = Time.time + summonInterval;
    }

    private void Update()
    {
        // Limpiamos la lista de súbditos destruidos para que no sigan contando
        minions.RemoveAll(m => m == null);

        if (PlayerDead)
        {
            SetLocomotion(0f);
            return;
        }

        // 1. Control de Invocaciones por tiempo absoluto
        if (Time.time >= nextSummonTime)
        {
            if (minions.Count < maxMinions)
            {
                TriggerSummon();
            }
            nextSummonTime = Time.time + summonInterval; // Renueva el ciclo pase lo que pase
        }

        // 2. Control de Hechizos por tiempo absoluto
        if (Time.time >= nextSpellTime)
        {
            if (spells.Count > 0)
            {
                StartCoroutine(ExecuteSpellSequence());
            }
            nextSpellTime = Time.time + spellInterval; // Renueva el ciclo pase lo que pase
        }

        // 3. Movimiento y vista hacia el jugador permanentes
        PatrolMovement();
        FacePlayer();
    }

    private void PatrolMovement()
    {
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget > 0.2f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, 1f, Time.deltaTime * 5f);
        }
        else
        {
            currentAnimSpeed = Mathf.Lerp(currentAnimSpeed, 0f, Time.deltaTime * 5f);
            movingForward = !movingForward;
            SetNewDestination();
        }

        SetLocomotion(currentAnimSpeed);
    }

    private void FacePlayer()
    {
        if (player == null) return;

        Vector3 direction = (player.transform.position - transform.position);
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 8f);
        }
    }

    private void SetNewDestination()
    {
        if (movingForward)
            targetPosition = startPosition + transform.forward * range;
        else
            targetPosition = startPosition;
    }

    private void SetLocomotion(float value)
    {
        if (animator != null && !string.IsNullOrEmpty(speedParameter))
        {
            animator.SetFloat(speedParameter, value);
        }
    }

    // ---------- ATAQUE Y HECHIZOS SEGUROS ----------

    private IEnumerator ExecuteSpellSequence()
    {
        Spell spell = NextSpell();

        if (animator != null && !string.IsNullOrEmpty(castTrigger))
            animator.SetTrigger(castTrigger);

        yield return new WaitForSeconds(0.4f);

        int count = Mathf.Max(1, spell.projectiles);
        for (int i = 0; i < count; i++)
        {
            Vector3 origin = GetCastOrigin();
            Vector3 aim = AimPoint - origin;
            if (aim.sqrMagnitude < 0.01f) aim = transform.forward;
            aim.Normalize();

            Vector3 direction = aim;

            if (spell.pattern == SpellPattern.Fan)
            {
                float t = count == 1 ? 0f : i / (float)(count - 1) - 0.5f;
                direction = Quaternion.AngleAxis(t * spell.fanAngle, Vector3.up) * aim;
            }

            SpawnProjectile(spell, origin, direction);

            if (shotInterval > 0f && i < count - 1)
                yield return new WaitForSeconds(shotInterval);
        }

        yield return new WaitForSeconds(0.3f);

        if (animator != null)
        {
            animator.ResetTrigger(castTrigger);
        }
    }

    private Spell NextSpell()
    {
        if (randomSpell) return spells[Random.Range(0, spells.Count)];

        Spell spell = spells[spellIndex % spells.Count];
        spellIndex++;
        return spell;
    }

    private Vector3 GetCastOrigin()
    {
        return castPoint != null
            ? castPoint.position
            : transform.position + Vector3.up * 1.5f + transform.forward * 0.5f;
    }

    private void SpawnProjectile(Spell spell, Vector3 origin, Vector3 direction)
    {
        GameObject projectileObject;

        if (spell.prefab != null)
            projectileObject = Instantiate(spell.prefab, origin, Quaternion.LookRotation(direction));
        else
            projectileObject = CreatePlaceholder(PrimitiveType.Sphere, "Hechizo", spell.color, 0.5f, origin, false);

        BossProjectile projectile = projectileObject.GetComponent<BossProjectile>();
        if (projectile == null) projectile = projectileObject.AddComponent<BossProjectile>();

        projectile.Launch(direction, spell.speed, spell.damage, spell.lifetime, transform);
    }

    // ---------- INVOCACIONES SEGURAS ----------

    private void TriggerSummon()
    {
        StartCoroutine(SummonSequence());
    }

    private IEnumerator SummonSequence()
    {
        if (animator != null && !string.IsNullOrEmpty(summonTrigger))
            animator.SetTrigger(summonTrigger);

        yield return new WaitForSeconds(0.5f);

        int toSpawn = Mathf.Min(minionsPerSummon, maxMinions - minions.Count);
        for (int i = 0; i < toSpawn; i++)
        {
            SpawnMinion();
        }

        yield return new WaitForSeconds(0.3f);

        if (animator != null)
        {
            animator.ResetTrigger(summonTrigger);
        }
    }

    private void SpawnMinion()
    {
        Vector2 offset = Random.insideUnitCircle * summonRadius;
        Vector3 position = new Vector3(
            transform.position.x + offset.x, transform.position.y, transform.position.z + offset.y);

        GameObject minionObject;

        if (minionPrefab != null)
        {
            minionObject = Instantiate(minionPrefab, position, Quaternion.identity);
        }
        else
        {
            const float cubeSize = 0.9f;
            minionObject = CreatePlaceholder(
                PrimitiveType.Cube, "Enemigo (cubo)", new Color(0.9f, 0.15f, 0.15f),
                cubeSize, position + Vector3.up * (cubeSize * 0.5f), true);
        }

        BossMinion minion = minionObject.GetComponent<BossMinion>();
        if (minion == null) minion = minionObject.AddComponent<BossMinion>();
        minion.Setup(player);

        minions.Add(minionObject);
    }

    private GameObject CreatePlaceholder(PrimitiveType type, string objectName, Color color, float size, Vector3 position, bool keepCollider)
    {
        GameObject placeholder = GameObject.CreatePrimitive(type);
        placeholder.name = objectName;
        placeholder.transform.position = position;
        placeholder.transform.localScale = Vector3.one * size;

        if (!keepCollider)
        {
            Collider col = placeholder.GetComponent<Collider>();
            if (col != null) Destroy(col);
        }

        Renderer rend = placeholder.GetComponent<Renderer>();
        rend.sharedMaterial = GetPlaceholderMaterial(color, rend);
        return placeholder;
    }

    private Material GetPlaceholderMaterial(Color color, Renderer fallbackRenderer)
    {
        if (placeholderMaterials.TryGetValue(color, out Material cached) && cached != null) return cached;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = fallbackRenderer.sharedMaterial.shader;

        Material material = new Material(shader) { color = color };
        placeholderMaterials[color] = material;
        return material;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(transform.position, summonRadius);
    }
}