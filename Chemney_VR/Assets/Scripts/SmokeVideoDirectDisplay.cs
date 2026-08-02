using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

[DisallowMultipleComponent]
public sealed class SmokeVideoDirectDisplay : MonoBehaviour
{
    private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int CullId = Shader.PropertyToID("_Cull");

    private struct GraphicState
    {
        public Graphic graphic;
        public Color color;
    }

    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RectTransform displayTarget;
    [SerializeField] private float surfaceDepthOffset = 0.03f;

    private readonly List<GraphicState> legacyVideoGraphics = new List<GraphicState>();
    private Camera mainCamera;
    private Transform videoSurface;
    private MeshRenderer videoRenderer;
    private Material videoMaterial;
    private bool requestedVisible;

    public void Initialize(VideoPlayer player, RectTransform target)
    {
        videoPlayer = player;
        displayTarget = target;
        mainCamera = FindPlaybackCamera();
        CaptureLegacyVideoGraphics(player);
        EnsureWorldSurface();
        SetVideoPlayer(player);
        Hide();
    }

    public void SetVideoPlayer(VideoPlayer player)
    {
        videoPlayer = player;
        ConfigureVideoPlayerForDirectTexture();

        if (videoMaterial != null)
        {
            SetWorldTexture(null);
        }

        PlaceOnDisplayTarget();
        ApplyVideoTexture();
    }

    public void RequestShow()
    {
        requestedVisible = true;
        SetLegacyVideoGraphicsVisible(false);
        SetSurfaceVisible(true);
        PlaceOnDisplayTarget();
        ApplyVideoTexture();
    }

    public void Hide()
    {
        requestedVisible = false;
        SetLegacyVideoGraphicsVisible(false);
        SetSurfaceVisible(false);
    }

    private void LateUpdate()
    {
        if (mainCamera == null)
        {
            mainCamera = FindPlaybackCamera();
        }

        if (!requestedVisible)
        {
            return;
        }

        PlaceOnDisplayTarget();
        ApplyVideoTexture();
    }

    private void OnDisable()
    {
        Hide();
    }

    private void OnDestroy()
    {
        RestoreLegacyVideoGraphics();

        if (videoSurface != null)
        {
            Destroy(videoSurface.gameObject);
        }

        if (videoMaterial != null)
        {
            Destroy(videoMaterial);
        }
    }

    private void CaptureLegacyVideoGraphics(VideoPlayer sourcePlayer)
    {
        legacyVideoGraphics.Clear();

        if (sourcePlayer == null)
        {
            return;
        }

        AddLegacyGraphic(sourcePlayer.GetComponent<Graphic>());

        Texture targetTexture = sourcePlayer.targetTexture;
        if (targetTexture == null)
        {
            return;
        }

        RawImage[] rawImages = sourcePlayer.GetComponentsInChildren<RawImage>(true);
        foreach (RawImage image in rawImages)
        {
            if (image != null && image.texture == targetTexture)
            {
                AddLegacyGraphic(image);
            }
        }
    }

    private void AddLegacyGraphic(Graphic graphic)
    {
        if (graphic == null)
        {
            return;
        }

        foreach (GraphicState state in legacyVideoGraphics)
        {
            if (state.graphic == graphic)
            {
                return;
            }
        }

        legacyVideoGraphics.Add(new GraphicState
        {
            graphic = graphic,
            color = graphic.color
        });
    }

    private void SetLegacyVideoGraphicsVisible(bool isVisible)
    {
        foreach (GraphicState state in legacyVideoGraphics)
        {
            if (state.graphic == null)
            {
                continue;
            }

            Color color = state.color;
            if (!isVisible)
            {
                color.a = 0f;
            }

            state.graphic.color = color;
        }
    }

    private void RestoreLegacyVideoGraphics()
    {
        foreach (GraphicState state in legacyVideoGraphics)
        {
            if (state.graphic != null)
            {
                state.graphic.color = state.color;
            }
        }
    }

    private void ConfigureVideoPlayerForDirectTexture()
    {
        if (videoPlayer == null)
        {
            return;
        }

        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.targetTexture = null;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = false;
    }

    private void EnsureWorldSurface()
    {
        if (videoRenderer != null)
        {
            return;
        }

        GameObject surfaceObject = GameObject.CreatePrimitive(PrimitiveType.Quad);
        surfaceObject.name = "Direct Cloudinary Video Surface";

        Collider collider = surfaceObject.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        videoSurface = surfaceObject.transform;
        videoRenderer = surfaceObject.GetComponent<MeshRenderer>();
        videoRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        videoRenderer.receiveShadows = false;

        EnsureMaterial();
        SetSurfaceVisible(false);
    }

    private void EnsureMaterial()
    {
        if (videoRenderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Texture");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogError("SmokeVideoDirectDisplay could not find an unlit texture shader.");
            return;
        }

        videoMaterial = new Material(shader)
        {
            name = "Direct Cloudinary Video Material"
        };

        if (videoMaterial.HasProperty(BaseColorId))
        {
            videoMaterial.SetColor(BaseColorId, Color.white);
        }
        if (videoMaterial.HasProperty(ColorId))
        {
            videoMaterial.SetColor(ColorId, Color.white);
        }
        if (videoMaterial.HasProperty(CullId))
        {
            videoMaterial.SetFloat(CullId, 0f);
        }

        videoRenderer.sharedMaterial = videoMaterial;
    }

    private void SetSurfaceVisible(bool isVisible)
    {
        if (videoSurface != null)
        {
            videoSurface.gameObject.SetActive(isVisible && videoMaterial != null);
        }
    }

    private void ApplyVideoTexture()
    {
        if (videoMaterial == null || videoPlayer == null || videoPlayer.texture == null)
        {
            return;
        }

        SetWorldTexture(videoPlayer.texture);
    }

    private void SetWorldTexture(Texture texture)
    {
        if (videoMaterial == null)
        {
            return;
        }

        videoMaterial.mainTexture = texture;
        if (videoMaterial.HasProperty(BaseMapId))
        {
            videoMaterial.SetTexture(BaseMapId, texture);
        }
        if (videoMaterial.HasProperty(MainTexId))
        {
            videoMaterial.SetTexture(MainTexId, texture);
        }
    }

    private void PlaceOnDisplayTarget()
    {
        if (mainCamera == null)
        {
            mainCamera = FindPlaybackCamera();
        }
        if (mainCamera == null || videoSurface == null || displayTarget == null)
        {
            return;
        }

        Vector3[] corners = new Vector3[4];
        displayTarget.GetWorldCorners(corners);

        float availableWidth = Vector3.Distance(corners[0], corners[3]);
        float availableHeight = Vector3.Distance(corners[0], corners[1]);

        Vector3 center = (corners[0] + corners[2]) * 0.5f;
        Vector3 awayFromCamera = (center - mainCamera.transform.position).normalized;
        videoSurface.position = center + awayFromCamera * surfaceDepthOffset;
        videoSurface.rotation = displayTarget.rotation;
        videoSurface.localScale = new Vector3(availableWidth, availableHeight, 1f);
    }

    private static Camera FindPlaybackCamera()
    {
        Camera camera = Camera.main;
        if (camera != null && camera.isActiveAndEnabled)
        {
            return camera;
        }

        Camera[] cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None);
        foreach (Camera candidate in cameras)
        {
            if (candidate != null && candidate.isActiveAndEnabled && !candidate.orthographic && candidate.targetTexture == null)
            {
                return candidate;
            }
        }

        foreach (Camera candidate in cameras)
        {
            if (candidate != null && candidate.isActiveAndEnabled && candidate.targetTexture == null)
            {
                return candidate;
            }
        }

        return null;
    }
}
