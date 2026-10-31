using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Crea una señal luminosa ligera y pulsante para objetos recogibles.</summary>
public sealed class PickupGlow : MonoBehaviour
{
    public enum Tipo { Municion, Bateria, Pista }

    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static Mesh beamMesh;
    private static Material beamMaterial;
    private static bool warnedMissingMaterial;

    private Renderer beamRenderer;
    private MaterialPropertyBlock propertyBlock;
    private Camera mainCamera;
    private Color color;
    private float alphaMinimo;
    private float alphaMaximo;
    private float phase;
    private bool created;

    /// <summary>Agrega o actualiza el indicador en un objeto recogible.</summary>
    public static void Aplicar(GameObject objeto, Tipo tipo)
    {
        if (objeto == null) return;

        PickupGlow indicator = objeto.GetComponent<PickupGlow>();
        if (indicator == null) indicator = objeto.AddComponent<PickupGlow>();
        indicator.Configurar(tipo);
    }

    private void Awake()
    {
        Configurar(Tipo.Municion);
    }

    private void Configurar(Tipo tipo)
    {
        switch (tipo)
        {
            case Tipo.Bateria:
                color = new Color(0.62f, 0.9f, 1f, 1f);
                alphaMinimo = 0.18f;
                alphaMaximo = 0.46f;
                break;
            case Tipo.Pista:
                color = new Color(0.92f, 0.84f, 0.68f, 1f);
                alphaMinimo = 0.14f;
                alphaMaximo = 0.36f;
                break;
            default:
                color = new Color(1f, 0.68f, 0.3f, 1f);
                alphaMinimo = 0.18f;
                alphaMaximo = 0.46f;
                break;
        }

        if (created)
            ActualizarColor();
        else
            CrearHaz();
    }

    private void CrearHaz()
    {
        if (beamMaterial == null)
            beamMaterial = Resources.Load<Material>("PickupGlow");

        if (beamMaterial == null)
        {
            if (!warnedMissingMaterial)
            {
                Debug.LogWarning("PickupGlow: no se encontró Assets/Resources/PickupGlow.mat; no se mostrará el halo.", this);
                warnedMissingMaterial = true;
            }
            return;
        }

        GameObject beam = new GameObject("Halo de recogida");
        beam.transform.SetParent(transform, false);
        beam.transform.localScale = EscalaLocalCompensada(transform.lossyScale);
        beam.transform.localPosition = transform.InverseTransformPoint(ObtenerPuntoInicial());

        MeshFilter meshFilter = beam.AddComponent<MeshFilter>();
        meshFilter.sharedMesh = ObtenerMallaHaz();

        beamRenderer = beam.AddComponent<MeshRenderer>();
        beamRenderer.sharedMaterial = beamMaterial;
        beamRenderer.shadowCastingMode = ShadowCastingMode.Off;
        beamRenderer.receiveShadows = false;
        beamRenderer.lightProbeUsage = LightProbeUsage.Off;
        beamRenderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        beamRenderer.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;

        propertyBlock = new MaterialPropertyBlock();
        mainCamera = Camera.main;
        phase = Random.value * Mathf.PI * 2f;
        created = true;
        ActualizarColor();
    }

    private Vector3 ObtenerPuntoInicial()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        bool found = false;
        Bounds bounds = default;

        foreach (Renderer itemRenderer in renderers)
        {
            if (itemRenderer == null || itemRenderer is ParticleSystemRenderer) continue;
            if (!found)
            {
                bounds = itemRenderer.bounds;
                found = true;
            }
            else
            {
                bounds.Encapsulate(itemRenderer.bounds);
            }
        }

        if (found)
            return new Vector3(bounds.center.x, bounds.min.y + 0.02f, bounds.center.z);

        return transform.position + Vector3.up * 0.25f;
    }

    private static Vector3 EscalaLocalCompensada(Vector3 escalaMundo)
    {
        return new Vector3(
            Mathf.Abs(escalaMundo.x) > 0.0001f ? 1f / escalaMundo.x : 1f,
            Mathf.Abs(escalaMundo.y) > 0.0001f ? 1f / escalaMundo.y : 1f,
            Mathf.Abs(escalaMundo.z) > 0.0001f ? 1f / escalaMundo.z : 1f);
    }

    private static Mesh ObtenerMallaHaz()
    {
        if (beamMesh != null) return beamMesh;

        beamMesh = new Mesh { name = "Malla ligera de halo de recogida" };
        beamMesh.vertices = new[]
        {
            new Vector3(-0.22f, 0f, 0f),
            new Vector3( 0.22f, 0f, 0f),
            new Vector3( 0.22f, 1.15f, 0f),
            new Vector3(-0.22f, 1.15f, 0f)
        };
        beamMesh.uv = new[]
        {
            new Vector2(0f, 0f), new Vector2(1f, 0f),
            new Vector2(1f, 1f), new Vector2(0f, 1f)
        };
        beamMesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
        beamMesh.RecalculateBounds();
        beamMesh.UploadMeshData(true);
        return beamMesh;
    }

    private void LateUpdate()
    {
        if (beamRenderer == null) return;

        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
            beamRenderer.transform.rotation = Quaternion.LookRotation(-mainCamera.transform.forward, Vector3.up);

        float pulse = (Mathf.Sin((Time.unscaledTime + phase) * 2.2f) + 1f) * 0.5f;
        Color tint = color;
        tint.a = Mathf.Lerp(alphaMinimo, alphaMaximo, pulse);
        beamRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(TintId, tint);
        beamRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ActualizarColor()
    {
        if (beamRenderer == null) return;
        beamRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(TintId, color);
        beamRenderer.SetPropertyBlock(propertyBlock);
    }
}
