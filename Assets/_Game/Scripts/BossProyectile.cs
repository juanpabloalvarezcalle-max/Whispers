using UnityEngine;

/// <summary>
/// Proyectil de un hechizo del jefe. Avanza en línea recta, le quita vida al jugador si lo toca
/// y desaparece al chocar con una pared. Los cubos invocados y otros hechizos no lo frenan.
/// </summary>
public class BossProjectile : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float radius = 0.25f;

    private Vector3 direction;
    private float speed;
    private float damage;
    private Transform owner;
    private readonly RaycastHit[] hits = new RaycastHit[8];

    public void Launch(Vector3 direction, float speed, float damage, float lifetime, Transform owner)
    {
        this.direction = direction.normalized;
        this.speed = speed;
        this.damage = damage;
        this.owner = owner;

        Destroy(gameObject, lifetime);
    }

    private void Update()
    {
        float step = speed * Time.deltaTime;

        // Se revisa el trayecto de este frame para que un proyectil rápido no atraviese al jugador.
        int count = Physics.SphereCastNonAlloc(
            transform.position, radius, direction, hits, step, ~0, QueryTriggerInteraction.Ignore);

        float closestDistance = float.MaxValue;
        RaycastHit closestHit = default;
        bool found = false;

        for (int i = 0; i < count; i++)
        {
            Collider other = hits[i].collider;
            if (other == null) continue;
            if (other.transform.IsChildOf(transform)) continue;                    // él mismo
            if (owner != null && other.transform.IsChildOf(owner)) continue;       // el jefe
            if (other.GetComponentInParent<BossMinion>() != null) continue;        // los cubos invocados
            if (other.GetComponentInParent<BossProjectile>() != null) continue;    // otros hechizos
            if (hits[i].distance >= closestDistance) continue;

            closestDistance = hits[i].distance;
            closestHit = hits[i];
            found = true;
        }

        if (found)
        {
            PlayerHealth health = closestHit.collider.GetComponentInParent<PlayerHealth>();
            if (health != null) health.TakeDamage(damage);

            Destroy(gameObject);
            return;
        }

        transform.position += direction * step;
    }
}