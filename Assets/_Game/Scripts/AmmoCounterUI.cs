using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the pistol magazine as a row of bullet icons (left to right),
/// one icon per bullet. Reserve ammunition can optionally stay as text.
/// </summary>
public class AmmoCounterUI : MonoBehaviour
{
    [SerializeField] private Gun gun;

    [Header("Balas del cargador")]
    [SerializeField] private Sprite bulletSprite;
    [Tooltip("Contenedor donde se crean las balas. Si se deja vacío usa este mismo objeto.")]
    [SerializeField] private RectTransform bulletContainer;
    [SerializeField] private Vector2 bulletSize = new Vector2(24f, 48f);
    [SerializeField] private float spacing = 4f;

    [Header("Reserva (opcional)")]
    [SerializeField] private TMP_Text reserveText;

    private readonly List<Image> bullets = new List<Image>();

    private void Start()
    {
        if (gun == null) gun = FindAnyObjectByType<Gun>();
        if (bulletContainer == null) bulletContainer = transform as RectTransform;

        if (gun == null || bulletContainer == null || bulletSprite == null)
        {
            Debug.LogWarning("AmmoCounterUI necesita una Gun, un contenedor y un Bullet Sprite.", this);
            return;
        }

        SetupLayout();
        EnsureBullets(gun.MagazineCapacity);

        gun.AmmunitionChanged += UpdateDisplay;
        UpdateDisplay(gun.AmmunitionInMagazine, gun.ReserveAmmo);
    }

    private void OnDestroy()
    {
        if (gun != null) gun.AmmunitionChanged -= UpdateDisplay;
    }
    private void SetupLayout()
    {
        HorizontalLayoutGroup layout = bulletContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) layout = bulletContainer.gameObject.AddComponent<HorizontalLayoutGroup>();

        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.spacing = spacing;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private void EnsureBullets(int count)
    {
        while (bullets.Count < count)
        {
            GameObject bulletObject = new GameObject("Bullet", typeof(RectTransform), typeof(Image));
            bulletObject.transform.SetParent(bulletContainer, false);

            RectTransform rect = (RectTransform)bulletObject.transform;
            rect.sizeDelta = bulletSize;

            Image image = bulletObject.GetComponent<Image>();
            image.sprite = bulletSprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            bullets.Add(image);
        }
    }

    private void UpdateDisplay(int magazine, int reserve)
    {
        EnsureBullets(magazine);

        for (int i = 0; i < bullets.Count; i++)
        {
            bool shouldShow = i < magazine;
            if (bullets[i].gameObject.activeSelf != shouldShow)
                bullets[i].gameObject.SetActive(shouldShow);
        }

        if (reserveText != null)
            reserveText.text = $"RESERVA    {reserve}";
    }
}