using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the spare flashlight batteries as 3 blocks (left to right).
/// Empty slots stay dim. While a battery is being swapped, the block about to be used blinks.
/// </summary>
public class FlashlightBatteryUI : MonoBehaviour
{
    [SerializeField] private BatteryInventory inventory;
    [SerializeField] private Lantern lantern;

    [Tooltip("Los 3 bloques, de izquierda a derecha. Se colocan encima del dibujo de la linterna.")]
    [SerializeField] private Image[] blocks = new Image[BatteryInventory.MaxBatteries];

    [SerializeField] private Color filledColor = Color.white;
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.15f);
    [Tooltip("Velocidad del parpadeo durante el cambio de batería.")]
    [SerializeField, Min(0.1f)] private float blinkSpeed = 4f;

    private void Start()
    {
        if (inventory == null) inventory = FindFirstObjectByType<BatteryInventory>();
        if (lantern == null) lantern = FindFirstObjectByType<Lantern>();

        if (inventory == null)
            Debug.LogWarning("FlashlightBatteryUI necesita un BatteryInventory (agrégalo al Player).", this);
    }

    private void Update()
    {
        if (inventory == null) return;

        int count = inventory.Count;
        bool swapping = lantern != null && lantern.CambiandoBateria && count > 0;
        float blink = swapping ? Mathf.PingPong(Time.unscaledTime * blinkSpeed, 1f) : 1f;

        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i] == null) continue;

            Color target = i < count ? filledColor : emptyColor;

            // El último bloque lleno es la pila que se está por usar.
            if (swapping && i == count - 1)
                target = Color.Lerp(emptyColor, filledColor, blink);

            blocks[i].color = target;
        }
    }
}