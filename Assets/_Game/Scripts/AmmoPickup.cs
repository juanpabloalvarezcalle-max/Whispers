using UnityEngine;

/// <summary>Adds reserve ammunition when the player enters this trigger.</summary>
[RequireComponent(typeof(Collider))]
public class AmmoPickup : MonoBehaviour
{
    [SerializeField, Min(1)] private int amount = 10;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        AmmoInventory inventory = other.GetComponentInParent<AmmoInventory>();
        if (inventory == null) return;
        if (inventory.AddAmmo(amount) > 0) Destroy(gameObject);
    }
}
