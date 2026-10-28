using UnityEngine;

/// <summary>
/// Enemigo invocado por el jefe (por ahora un cubo): persigue al jugador y le hace daño al tocarlo.
/// </summary>
public class BossMinion : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 2.5f;
    [Tooltip("Distancia a la que ataca.")]
    [SerializeField, Min(0.1f)] private float attackRange = 1.3f;
    [SerializeField, Min(0f)] private float damage = 8f;
    [SerializeField, Min(0.1f)] private float attackCooldown = 1f;

    private PlayerHealth target;
    private float nextAttackAt;

    public void Setup(PlayerHealth player)
    {
        target = player;
    }

    private void Awake()
    {
        // Para que las balas del jugador le hagan daño.
        if (GetComponent<Damageable>() == null) gameObject.AddComponent<Damageable>();
    }

    private void Update()
    {
        if (target == null || target.IsDead) return;

        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        float distance = toTarget.magnitude;

        if (distance > attackRange)
        {
            Vector3 direction = toTarget / distance;
            transform.position += direction * (moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(direction);
        }
        else if (Time.time >= nextAttackAt)
        {
            nextAttackAt = Time.time + attackCooldown;
            target.TakeDamage(damage);
        }
    }
}