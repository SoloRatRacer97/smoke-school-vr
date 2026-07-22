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
    [SerializeField] private float distanceFromCamera = 3.0f;
    [SerializeField] private float surfaceWidth = 4.2f;
    [SerializeField] private float centerForSeconds = 5.0f;

    private readonly List<GraphicState> legacyVideoGraphics = new List<GraphicState>();
    private Camera mainCamera;
    private Transform videoSurface;
    private MeshRenderer videoRenderer;
    private Material videoMaterial;
    private bool requestedVisible;
    private float requestedAt;

    public void Initialize(VideoPlayer player)
    {
        videoPlayer = player;
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
        requestedAt = Time.realtimeSinceStartup;

        if (videoMaterial != null)
        {
            SetWorldTexture(null);
        }

        PlaceInFrontOfCamera();
        ApplyVideoTexture();
    }

    public void RequestShow()
    {
        requestedVisible = true;
        requestedAt = Time.realtimeSinceStartup;
        SetLegacyVideoGraphicsVisible(false);
        SetSurfaceVisible(true);
        PlaceInFrontOfCamera();
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

        if (Time.realtimeSinceStartup - requestedAt <= centerForSeconds)
        {
            PlaceInFrontOfCamera();
        }

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

    private void PlaceInFrontOfCamera()
    {
        if (mainCamera == null)
        {
            mainCamera = FindPlaybackCamera();
        }
        if (mainCamera == null || videoSurface == null)
        {
            return;
        }

        float aspect = 4096f / 2160f;
        videoSurface.position = mainCamera.transform.position + mainCamera.transform.forward * distanceFromCamera;
        videoSurface.rotation = mainCamera.transform.rotation;
        videoSurface.localScale = new Vector3(surfaceWidth, surfaceWidth / aspect, 1f);
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
