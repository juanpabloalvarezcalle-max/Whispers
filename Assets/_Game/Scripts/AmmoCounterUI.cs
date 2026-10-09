using TMPro;
using UnityEngine;

/// <summary>Displays the current pistol magazine and reserve ammunition.</summary>
public class AmmoCounterUI : MonoBehaviour
{
    [SerializeField] private Gun gun;
    [SerializeField] private TMP_Text displayText;

    private void Start()
    {
        if (gun == null) gun = FindFirstObjectByType<Gun>();
        if (displayText == null) displayText = GetComponent<TMP_Text>();

        if (gun == null || displayText == null)
        {
            Debug.LogWarning("AmmoCounterUI necesita una Gun y un texto TMP.", this);
            return;
        }

        gun.AmmunitionChanged += UpdateDisplay;
        UpdateDisplay(gun.AmmunitionInMagazine, gun.ReserveAmmo);
    }

    private void OnDestroy()
    {
        if (gun != null) gun.AmmunitionChanged -= UpdateDisplay;
    }

    private void UpdateDisplay(int magazine, int reserve)
    {
        if (displayText != null && gun != null)
            displayText.text = $"CARGADOR  {magazine} / {gun.MagazineCapacity}\nRESERVA    {reserve}";
    }
}
