using System;
using UnityEngine;

/// <summary>Stores reserve ammunition shared by the player's weapons.</summary>
public class AmmoInventory : MonoBehaviour
{
    [SerializeField, Min(0)] private int reserveAmmo = 15;
    [SerializeField, Min(0)] private int maximumReserveAmmo = 60;

    public int ReserveAmmo => reserveAmmo;
    public event Action<int> ReserveAmmoChanged;

    private void Awake()
    {
        reserveAmmo = Mathf.Clamp(reserveAmmo, 0, maximumReserveAmmo);
    }

    public int AddAmmo(int amount)
    {
        if (amount <= 0) return 0;
        int previousAmount = reserveAmmo;
        reserveAmmo = Mathf.Min(reserveAmmo + amount, maximumReserveAmmo);
        int addedAmount = reserveAmmo - previousAmount;
        if (addedAmount > 0) ReserveAmmoChanged?.Invoke(reserveAmmo);
        return addedAmount;
    }

    public int TakeAmmo(int amount)
    {
        if (amount <= 0 || reserveAmmo <= 0) return 0;
        int takenAmount = Mathf.Min(amount, reserveAmmo);
        reserveAmmo -= takenAmount;
        ReserveAmmoChanged?.Invoke(reserveAmmo);
        return takenAmount;
    }
}
