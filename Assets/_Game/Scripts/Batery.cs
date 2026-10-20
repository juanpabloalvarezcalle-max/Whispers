using UnityEngine;

/// <summary>
/// Flashlight battery found in the environment. The player picks it up by touching it,
/// as long as the inventory has room (max 3). If it is full, the battery stays where it is.
/// </summary>
[RequireComponent(typeof(Collider))]
public class Batery : MonoBehaviour
{
    [SerializeField] private AudioClip pickupSound;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.8f;

    // Al agregar el script, el collider queda como Trigger.
    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other) => TryCollect(other);

    // Si el inventario estaba lleno al entrar, se vuelve a intentar mientras el jugador siga encima.
    private void OnTriggerStay(Collider other) => TryCollect(other);

    private void TryCollect(Collider other)
    {
        BatteryInventory inventory = other.GetComponentInParent<BatteryInventory>();
        if (inventory == null || !inventory.TryAdd()) return;

        if (pickupSound != null)
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, pickupVolume);

        Destroy(gameObject);
    }
}