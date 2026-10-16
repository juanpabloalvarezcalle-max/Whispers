using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    [SerializeField] private Joystick joystick;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float rotationSpeed = 540f;
    [SerializeField, Range(0f, 0.5f)] private float movementDeadZone = 0.15f;
    [SerializeField] private Animator animator;

    [Header("Ajuste de objetos en las manos")]
    [SerializeField] private Vector3 pistolHandPosition = new Vector3(-0.05f, 0.12f, 0.03f);
    // Corrección de orientación afinada visualmente para alinear la pistola con la mano.
    [SerializeField] private Vector3 pistolHandRotation = new Vector3(354.4f, -90f, -184.6f);
    [SerializeField] private Vector3 flashlightHandPosition;
    [SerializeField] private Vector3 flashlightHandRotation;
    [SerializeField] private Vector3 radioHandPosition;
    [SerializeField] private Vector3 radioHandRotation;
    [SerializeField] private Vector3 batonHandPosition;
    [SerializeField] private Vector3 batonHandRotation;

    private static readonly int SpeedParameter = Animator.StringToHash("Velocidad");
    private static readonly int PunchParameter = Animator.StringToHash("Golpe");
    private static readonly int PistolEquippedParameter = Animator.StringToHash("PistolaEquipada");
    private static readonly int AimingParameter = Animator.StringToHash("Apuntando");
    private static readonly int RadioActiveParameter = Animator.StringToHash("RadioActivada");
    private static readonly int FlashlightInHandParameter = Animator.StringToHash("LinternaEnMano");

    private Rigidbody rb;
    private Vector3 move;
    private Transform cam;
    private Transform rightHand;
    private Transform leftHand;
    private Gun testGun;
    private PropAttachment pistolProp;
    private PropAttachment flashlightProp;
    private PropAttachment radioProp;
    private PropAttachment batonProp;
    private bool returnBatonAfterAnimation;
    private bool batonAnimationStarted;
    private bool pistolPoseActive;
    private bool flashlightPoseActive;
    [SerializeField, Min(0f)] private float segundosParaGuardarArma = 3f;
    private Lantern lantern;
    private bool armaSacada;
    private float guardarArmaEn;
    public bool PistolaSacada => armaSacada;

    void Awake()
    {
        Application.targetFrameRate = 60;
        rb = GetComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        cam = Camera.main.transform;

        // El Animator está en el modelo, que es hijo del objeto Player.
        // Si no se asignó desde el Inspector, lo buscamos en los hijos.
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        // El FBX puede tener los meshes de los objetos como hermanos del Animator.
        // Buscamos bajo Player para encontrarlos, pero nunca fuera del personaje.
        Transform characterRoot = transform;
        rightHand = FindBone(characterRoot, "RightHand");
        leftHand = FindBone(characterRoot, "LeftHand");
        pistolProp = CacheProp(characterRoot, "pistola");
        flashlightProp = CacheProp(characterRoot, "linterna");
        radioProp = CacheProp(characterRoot, "Radio");
        batonProp = CacheProp(characterRoot, "Bolillo");
        testGun = GetComponentInChildren<Gun>();
        lantern = GetComponentInChildren<Lantern>(true);
        if (lantern == null) lantern = FindFirstObjectByType<Lantern>();

        if (animator != null)
        {
            animator.SetBool(FlashlightInHandParameter, false);
        }
    }

    void Update()
    {
        Vector3 forward = cam.forward;
        Vector3 right = cam.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();
        float verticalInput = joystick != null ? joystick.Vertical : 0f;
        float horizontalInput = joystick != null ? joystick.Horizontal : 0f;
#if UNITY_EDITOR
        // Controles de escritorio disponibles solo mientras se prueba desde Unity.
        if (Keyboard.current != null)
        {
            verticalInput += Keyboard.current.wKey.ReadValue() - Keyboard.current.sKey.ReadValue();
            horizontalInput += Keyboard.current.dKey.ReadValue() - Keyboard.current.aKey.ReadValue();
        }
#endif
        move = forward * verticalInput + right * horizontalInput;
        if (move.sqrMagnitude > 1f)
        {
            move.Normalize();
        }

        // El joystick puede quedarse con una señal diminuta al soltarlo.
        // La eliminamos para que Velocidad vuelva a cero y el Animator entre en idle.
        if (move.magnitude < movementDeadZone)
        {
            move = Vector3.zero;
        }

        // El parámetro Velocidad controla el cambio entre Iddle y CaminarLinterna.
        // Suavizamos el valor para evitar cambios bruscos al mover el joystick.
        if (animator != null)
        {
            animator.SetFloat(SpeedParameter, move.magnitude, 0.1f, Time.deltaTime);
        }

        UpdateBatonAttachment();
        if (armaSacada && segundosParaGuardarArma > 0f && Time.time >= guardarArmaEn)
            GuardarPistola();

#if UNITY_EDITOR
        // Teclas de diagnóstico para probar la pose sin usar la interfaz táctil.
        if (Keyboard.current != null)
        {
            if (Keyboard.current.pKey.wasPressedThisFrame)
            {
                bool pistolEquipped = animator != null && animator.GetBool(PistolEquippedParameter);
                if (pistolEquipped) GuardarPistola();
                else
                {
                    EquiparPistola();
                    SetApuntando(true);
                }
            }
            if (Keyboard.current.oKey.wasPressedThisFrame) SetApuntando(false);
            if (Keyboard.current.spaceKey.wasPressedThisFrame) testGun?.Shoot();

            if (Keyboard.current.digit1Key.wasPressedThisFrame) SetPistolTestRotation(Vector3.zero);
            if (Keyboard.current.digit2Key.wasPressedThisFrame) SetPistolTestRotation(new Vector3(90f, 0f, 0f));
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SetPistolTestRotation(new Vector3(0f, 90f, 0f));
            if (Keyboard.current.digit4Key.wasPressedThisFrame) SetPistolTestRotation(new Vector3(0f, 0f, 90f));
        }
#endif

    }

    void LateUpdate()
    {
        if (rightHand == null) return;

        // Conservamos los offsets locales de posición y rotación relativos a la mano.
        // Mantiene el offset sincronizado con el Inspector durante Play Mode,
        // así se puede ajustar la posición sin guardar y volver a equipar.
        if (pistolPoseActive)
        {
            pistolProp?.SetLocalPosition(pistolHandPosition);
            pistolProp?.SetLocalRotation(Quaternion.Euler(pistolHandRotation));
        }

        // Igual para la linterna: los campos Flashlight Hand Position/Rotation
        // se ven en vivo mientras se prueba en Play Mode.
        if (flashlightPoseActive)
        {
            flashlightProp?.SetLocalPosition(flashlightHandPosition);
            flashlightProp?.SetLocalRotation(Quaternion.Euler(flashlightHandRotation));
        }
    }

    void FixedUpdate()
    {
        Vector3 targetVelocity = move * speed;
        targetVelocity.y = rb.linearVelocity.y;
        rb.linearVelocity = targetVelocity;

        if (move.sqrMagnitude > 0.01f)
        {
            Quaternion target = Quaternion.LookRotation(move);
            rb.rotation = Quaternion.RotateTowards(rb.rotation, target, rotationSpeed * Time.fixedDeltaTime);
        }
    }

    private void SetPistolTestRotation(Vector3 rotation)
    {
        pistolHandRotation = rotation;
        Debug.Log($"Ajuste de prueba de pistola: {rotation}");
        if (pistolPoseActive) EquiparPistola();
    }

    // Estos métodos son los "enchufes" que llaman los botones y las mecánicas.
    public void EquiparPistola()
    {
        // La pistola y la linterna comparten la mano derecha.
        lantern?.ApagarPorArma();

        if (pistolProp != null && rightHand != null)
        {
            pistolProp.AttachTo(rightHand, pistolHandPosition, pistolHandRotation);
            pistolPoseActive = true;
        }
        armaSacada = true;
        guardarArmaEn = Time.time + segundosParaGuardarArma;
        SetBool(PistolEquippedParameter, true);
    }

    public void GuardarPistola()
    {
        SetBool(AimingParameter, false);
        SetBool(PistolEquippedParameter, false);
        pistolPoseActive = false;
        armaSacada = false;
        pistolProp?.Restore();

        // Si la linterna estaba encendida antes de sacar la pistola, vuelve a encenderse.
        lantern?.ReanudarTrasArma();
    }

    public void SetApuntando(bool apuntando)
    {
        if (apuntando) SetBool(PistolEquippedParameter, true);
        SetBool(AimingParameter, apuntando);
    }

    public void AlternarApuntado()
    {
        bool apuntando = animator != null && animator.GetBool(AimingParameter);
        SetApuntando(!apuntando);
    }

    public void SetRadioActivada(bool activada)
    {
        SetBool(RadioActiveParameter, activada);
        if (activada) radioProp?.AttachTo(leftHand, radioHandPosition, radioHandRotation);
        else radioProp?.Restore();
    }

    public void AlternarRadio()
    {
        bool activada = animator != null && animator.GetBool(RadioActiveParameter);
        SetRadioActivada(!activada);
    }

    public void SetLinternaEnMano(bool enMano)
    {
        SetBool(FlashlightInHandParameter, enMano);
        if (enMano)
        {
            // La animación de la linterna la sostiene con la mano derecha.
            flashlightProp?.AttachTo(rightHand, flashlightHandPosition, flashlightHandRotation);
            flashlightPoseActive = flashlightProp != null && rightHand != null;
        }
        else
        {
            flashlightPoseActive = false;
            flashlightProp?.Restore();
        }
    }

    public void GolpearConBolillo()
    {
        if (animator == null) return;
        batonProp?.AttachTo(rightHand, batonHandPosition, batonHandRotation);
        returnBatonAfterAnimation = true;
        batonAnimationStarted = false;
        animator.SetTrigger(PunchParameter);
    }

    private void UpdateBatonAttachment()
    {
        if (!returnBatonAfterAnimation || animator == null) return;

        bool playingPunch = animator.GetCurrentAnimatorStateInfo(0).IsName("GolpeBolillo");
        if (playingPunch)
        {
            batonAnimationStarted = true;
        }
        else if (batonAnimationStarted)
        {
            batonProp?.Restore();
            returnBatonAfterAnimation = false;
            batonAnimationStarted = false;
        }
    }

    private static Transform FindChild(Transform root, string exactName)
    {
        string target = NormalizeName(exactName);
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            string candidate = NormalizeName(child.name);
            // Unity adds numeric suffixes to duplicated/imported FBX objects
            // (for example "pistola.001"). Treat the suffix as part of the
            // same prop name so the real mesh is found and attached to the hand.
            if (candidate == target || (candidate.StartsWith(target) && HasOnlyNumericSuffix(candidate, target)))
                return child;
        }
        return null;
    }

    private static bool HasOnlyNumericSuffix(string candidate, string prefix)
    {
        if (candidate.Length <= prefix.Length) return false;
        for (int i = prefix.Length; i < candidate.Length; i++)
        {
            if (!char.IsDigit(candidate[i])) return false;
        }
        return true;
    }

    private static Transform FindBone(Transform root, string boneName)
    {
        string target = NormalizeName(boneName);
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            string normalized = NormalizeName(child.name);
            if (normalized.EndsWith(target)) return child;
        }
        return null;
    }

    private static string NormalizeName(string value)
    {
        var result = new System.Text.StringBuilder(value.Length);
        foreach (char character in value)
        {
            if (char.IsLetter(character)) result.Append(char.ToLowerInvariant(character));
        }
        return result.ToString();
    }

    private static PropAttachment CacheProp(Transform root, string exactName)
    {
        Transform prop = FindChild(root, exactName);
        return prop != null ? new PropAttachment(prop) : null;
    }

    private void SetBool(int parameter, bool value)
    {
        if (animator != null) animator.SetBool(parameter, value);
    }

    private sealed class PropAttachment
    {
        private readonly Transform prop;
        private readonly Transform originalParent;
        private readonly Vector3 originalPosition;
        private readonly Quaternion originalRotation;
        private readonly Vector3 originalScale;

        public PropAttachment(Transform prop)
        {
            this.prop = prop;
            originalParent = prop.parent;
            originalPosition = prop.localPosition;
            originalRotation = prop.localRotation;
            originalScale = prop.localScale;
        }

        public void AttachTo(Transform hand, Vector3 localPosition, Vector3 localEulerAngles)
        {
            if (prop == null || hand == null) return;

            if (prop.parent != hand) prop.SetParent(hand, false);
            prop.localPosition = localPosition;
            prop.localRotation = Quaternion.Euler(localEulerAngles);
            prop.localScale = originalScale;
        }

        public void Restore()
        {
            if (prop == null || originalParent == null) return;
            if (prop.parent != originalParent) prop.SetParent(originalParent, false);
            SetOriginalLocalTransform();
        }

        public void SetLocalPosition(Vector3 localPosition)
        {
            if (prop == null) return;
            prop.localPosition = localPosition;
        }

        public void SetLocalRotation(Quaternion localRotation)
        {
            if (prop == null) return;
            prop.localRotation = localRotation;
        }

        private void SetOriginalLocalTransform()
        {
            prop.localPosition = originalPosition;
            prop.localRotation = originalRotation;
            prop.localScale = originalScale;
        }
    }
}