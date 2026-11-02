using UnityEngine;

/// <summary>Loads the boss arena when the player enters the boss trigger.</summary>
public class BossFightEntrance : MonoBehaviour
{
    [SerializeField] private string sceneName = "BossFight";

    private bool transitionStarted;

    private void OnTriggerEnter(Collider other) => TryStartTransition(other);

    // Reintenta mientras el jugador siga dentro por si el manager todavía se está inicializando.
    private void OnTriggerStay(Collider other) => TryStartTransition(other);

    private void TryStartTransition(Collider other)
    {
        if (transitionStarted || !EsJugador(other))
            return;

        if (TransitionManager.Instance == null)
        {
            Debug.LogError("BossFightEntrance: TransitionManager no está disponible.", this);
            return;
        }

        transitionStarted = true;
        TransitionManager.Instance.LoadLevel(sceneName);
    }

    private static bool EsJugador(Collider other)
    {
        if (other.GetComponentInParent<PlayerHealth>() != null)
            return true;

        return other.transform.root.CompareTag("Player");
    }
}
