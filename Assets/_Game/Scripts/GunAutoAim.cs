using UnityEngine;

/// <summary>
/// Autoapuntado de la pistola. Cuando el jugador dispara, busca el enemigo vivo más cercano
/// que esté al alcance y a la vista, gira al jugador hacia él y devuelve el punto al que debe ir la bala.
/// Se coloca en el mismo objeto que el script Gun.
/// </summary>
public class GunAutoAim : MonoBehaviour
{
    [SerializeField] private bool activo = true;
    [Tooltip("Distancia máxima a la que el arma se engancha a un enemigo.")]
    [SerializeField, Min(1f)] private float alcance = 20f;
    [Tooltip("Ángulo total (frente al jugador) en el que busca enemigos. 360 = a su alrededor.")]
    [SerializeField, Range(30f, 360f)] private float anguloBusqueda = 180f;
    [Tooltip("Gira al jugador hacia el enemigo al disparar.")]
    [SerializeField] private bool girarAlJugador = true;
    [Tooltip("Capas que pueden bloquear la línea de tiro (paredes, árboles...).")]
    [SerializeField] private LayerMask capasVision = ~0;

    private readonly RaycastHit[] resultados = new RaycastHit[16];

    /// <summary>
    /// Busca un objetivo. Si lo encuentra, gira al jugador hacia él y devuelve el punto de impacto deseado.
    /// Si no hay ninguno, devuelve false y el arma dispara hacia delante como siempre.
    /// </summary>
    public bool TryAim(Transform cuerpo, Transform boca, out Vector3 puntoDeApuntado)
    {
        puntoDeApuntado = default;
        if (!activo || cuerpo == null || boca == null) return false;

        EnemyHealth mejor = null;
        float mejorDistancia = float.MaxValue;

        for (int i = 0; i < EnemyHealth.Active.Count; i++)
        {
            EnemyHealth enemigo = EnemyHealth.Active[i];
            if (enemigo == null || enemigo.IsDead) continue;

            Vector3 plano = enemigo.transform.position - cuerpo.position;
            plano.y = 0f;
            float distancia = plano.magnitude;

            if (distancia > alcance || distancia >= mejorDistancia) continue;
            if (distancia > 0.01f && Vector3.Angle(cuerpo.forward, plano) > anguloBusqueda * 0.5f) continue;
            if (!TieneLineaDeTiro(cuerpo, boca.position, enemigo)) continue;

            mejor = enemigo;
            mejorDistancia = distancia;
        }

        if (mejor == null) return false;

        puntoDeApuntado = mejor.AimPoint;

        if (girarAlJugador)
        {
            Vector3 haciaEnemigo = puntoDeApuntado - cuerpo.position;
            haciaEnemigo.y = 0f;
            if (haciaEnemigo.sqrMagnitude > 0.01f)
                cuerpo.rotation = Quaternion.LookRotation(haciaEnemigo.normalized, Vector3.up);
        }

        return true;
    }

    /// <summary>True si lo primero que hay entre la boca del arma y el enemigo es el propio enemigo.</summary>
    private bool TieneLineaDeTiro(Transform cuerpo, Vector3 origen, EnemyHealth enemigo)
    {
        Vector3 direccion = enemigo.AimPoint - origen;
        float distancia = direccion.magnitude;
        if (distancia < 0.01f) return true;

        int cantidad = Physics.RaycastNonAlloc(origen, direccion / distancia, resultados,
            distancia + 0.1f, capasVision, QueryTriggerInteraction.Ignore);

        float masCercano = float.MaxValue;
        Transform dueno = null;

        for (int i = 0; i < cantidad; i++)
        {
            RaycastHit golpe = resultados[i];
            if (golpe.collider == null) continue;
            if (golpe.collider.transform.IsChildOf(cuerpo.root)) continue; // ignora al propio jugador
            if (golpe.distance >= masCercano) continue;

            masCercano = golpe.distance;
            dueno = golpe.collider.transform;
        }

        return dueno != null && dueno.IsChildOf(enemigo.transform);
    }
}
