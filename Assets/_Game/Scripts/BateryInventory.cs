using UnityEngine;

/// <summary>Spare flashlight batteries the player carries. The limit is 3.</summary>
public class BatteryInventory : MonoBehaviour
{
    public const int MaxBatteries = 3;

    [Tooltip("Pilas de repuesto con las que empieza el jugador.")]
    [SerializeField, Range(0, MaxBatteries)] private int batteries = 1;

    public int Count => batteries;
    public bool IsFull => batteries >= MaxBatteries;

    private void Awake()
    {
        batteries = Mathf.Clamp(batteries, 0, MaxBatteries);
    }

    /// <summary>Adds one battery. Returns false if the inventory is already full.</summary>
    public bool TryAdd()
    {
        if (IsFull) return false;
        batteries++;
        return true;
    }

    /// <summary>Uses one battery. Returns false if there are none.</summary>
    public bool TryUse()
    {
        if (batteries <= 0) return false;
        batteries--;
        return true;
    }
}