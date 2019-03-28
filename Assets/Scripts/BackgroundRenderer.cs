using System.Collections;
using System.Collections.Generic;
using GoogleARCoreInternal;
using UnityEngine;
using UnityEngine.XR;

using GoogleARCore;

/// <summary>
/// Renders the device's camera as a background to the attached Unity camera component.
/// </summary>
[RequireComponent(typeof(Camera))]
[HelpURL("https://developers.google.com/ar/reference/unity/class/GoogleARCore/ARCoreBackgroundRenderer")]
public class BackgroundRenderer : MonoBehaviour
{
    [Tooltip("A material used to render the AR background image.")]
    public Material backgroundMaterial_;
    public Material debugMaterial_;

    [Tooltip("Enables hardware camera background.")]
    public bool enableCameraBackground = true;

    [Tooltip("Flag whether to create debug primitve geometries.")]
    public bool createDebugPrimitives = true;

    private Camera camera_;
    private ARBackgroundRenderer backgroundRenderer_;

    private bool sessionEnabled_ = false;
    private bool debugPlaneInitialized_ = false;

    private Dictionary<string, Vector3> cameraImagePlaneCorners_;
    private Plane cameraImagePlane_;

    /// <summary>
    /// Start this instance.
    /// </summary>
    private void Start()
    {
        cameraImagePlaneCorners_ = new Dictionary<string, Vector3>();
    }

    /// <summary>
    /// Ons the enable.
    /// </summary>
    private void OnEnable()
    {
        if (backgroundRenderer_ == null)
            backgroundRenderer_ = new ARBackgroundRenderer();

        if (backgroundMaterial_ == null)
        {
            Debug.LogError("ArCameraBackground:: No material assigned.");
            return;
        }

        LifecycleManager.Instance.OnSessionSetEnabled += _OnSessionSetEnabled;

        // get camera component & setup background renderer
        camera_ = GetComponent<Camera>();
        backgroundRenderer_.backgroundMaterial = backgroundMaterial_;
        backgroundRenderer_.camera = camera_;

        if (enableCameraBackground)
        {
            backgroundRenderer_.mode = ARRenderMode.MaterialAsBackground;
        }
        else
        {
            backgroundRenderer_.mode = ARRenderMode.StandardBackground;
        }

        // set material brightness so that the background can be seen
        backgroundMaterial_.SetFloat("_Brightness", 1.0f);
    }

    /// <summary>
    /// Ons the disable.
    /// </summary>
    private void OnDisable()
    {
        LifecycleManager.Instance.OnSessionSetEnabled -= _OnSessionSetEnabled;

        camera_.ResetProjectionMatrix();
        if (backgroundRenderer_ != null)
        {
            backgroundRenderer_.mode = ARRenderMode.StandardBackground;
            backgroundRenderer_.camera = null;
        }
    }

    /// <summary>
    /// Framewise update.
    /// </summary>
    private void Update()
    {
        _UpdateShaderVariables();
    }

    /// <summary>
    /// Updates the shader variables.
    /// </summary>
    private void _UpdateShaderVariables()
    {
        // Don't render if no texture is available
        if (Frame.CameraImage.Texture == null)
        {
            return;
        }

        backgroundMaterial_.SetTexture("_MainTex", Frame.CameraImage.Texture);

        var uvQuad = Frame.CameraImage.TextureDisplayUvs;
        backgroundMaterial_.SetVector("_UvTopLeftRight",
            new Vector4(uvQuad.TopLeft.x, uvQuad.TopLeft.y, uvQuad.TopRight.x, uvQuad.TopRight.y));
        backgroundMaterial_.SetVector("_UvBottomLeftRight",
        new Vector4(uvQuad.BottomLeft.x, uvQuad.BottomLeft.y, uvQuad.BottomRight.x, uvQuad.BottomRight.y));

        camera_.projectionMatrix = Frame.CameraImage.GetCameraProjectionMatrix(
            camera_.nearClipPlane, camera_.farClipPlane);

        if (!debugPlaneInitialized_)
        {
            _InitializeCameraImagePlane();
            debugPlaneInitialized_ = true;
        }
    }

    /// <summary>
    /// Callback on session enabled.
    /// </summary>
    /// <param name="sessionEnabled">If set to <c>true</c> session enabled.</param>
    private void _OnSessionSetEnabled(bool sessionEnabled)
    {
        sessionEnabled_ = sessionEnabled;
        if (!sessionEnabled_)
        {
            _UpdateShaderVariables();
        }
    }

    /// <summary>
    /// Initializes a plane matching the actual cameras field of view.
    /// </summary>
    private void _InitializeCameraImagePlane()
    {
        int w = Screen.currentResolution.width;
        int h = Screen.currentResolution.height;

        Vector3 screen_bl = camera_.ScreenToWorldPoint(new Vector3(0, 0, camera_.nearClipPlane + 1));
        Vector3 screen_tl = camera_.ScreenToWorldPoint(new Vector3(0, h, camera_.nearClipPlane + 1));
        Vector3 screen_tr = camera_.ScreenToWorldPoint(new Vector3(w, h, camera_.nearClipPlane + 1));
        Vector3 screen_br = camera_.ScreenToWorldPoint(new Vector3(w, 0, camera_.nearClipPlane + 1));

        Vector3 primitive_scale = new Vector3(0.1f, 0.1f, 0.1f);
        if (createDebugPrimitives)
        {
            createPrimitive(PrimitiveType.Cube, screen_tl, primitive_scale, Color.red, this.gameObject);
            createPrimitive(PrimitiveType.Cube, screen_tr, primitive_scale, Color.green, this.gameObject);
            createPrimitive(PrimitiveType.Cube, screen_br, primitive_scale, Color.cyan, this.gameObject);
            createPrimitive(PrimitiveType.Cube, screen_bl, primitive_scale, Color.black, this.gameObject);
        }

        Vector3 v_center_l = calculateCenter(screen_tl, screen_bl);
        Vector3 v_center_r = calculateCenter(screen_tr, screen_br);

        float x_scale = Vector3.Distance(v_center_l, v_center_r);
        float y_scale = (x_scale * 3) / 4;

        // calculate final camera plane borders
        Vector3 final_tl = calculatePointOnVector(v_center_l, screen_tl, y_scale / 2);
        Vector3 final_bl = calculatePointOnVector(v_center_l, screen_bl, y_scale / 2);
        Vector3 final_tr = calculatePointOnVector(v_center_r, screen_tr, y_scale / 2);
        Vector3 final_br = calculatePointOnVector(v_center_r, screen_br, y_scale / 2);

        cameraImagePlaneCorners_["tl"] = final_tl;
        cameraImagePlaneCorners_["tr"] = final_tr;
        cameraImagePlaneCorners_["bl"] = final_bl;
        cameraImagePlaneCorners_["br"] = final_br;

        cameraImagePlane_ = new Plane(final_tl, final_tr, final_br);

        if (createDebugPrimitives)
        {
            createPrimitive(PrimitiveType.Sphere, final_tl, primitive_scale, Color.magenta, this.gameObject);
            createPrimitive(PrimitiveType.Sphere, final_bl, primitive_scale, Color.magenta, this.gameObject);
            createPrimitive(PrimitiveType.Sphere, final_tr, primitive_scale, Color.magenta, this.gameObject);
            createPrimitive(PrimitiveType.Sphere, final_br, primitive_scale, Color.magenta, this.gameObject);
        }

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Quad);
        plane.transform.parent = camera_.transform;
        plane.transform.position = calculateCenter(screen_tl, screen_br);
        plane.transform.rotation = Quaternion.AngleAxis(90.0f, new Vector3(0.0f, 0.0f, 1.0f));
        plane.transform.localScale = new Vector3(x_scale, y_scale, 1.0f);

        MeshRenderer mr = plane.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Diffuse"));

        Renderer r = plane.GetComponent<Renderer>();
        r.material.mainTexture = FindObjectOfType<MarkerDetector>().getTexture();
        r.material.mainTextureScale = new Vector2(-1, 1);
    }

    /// <summary>
    /// Creates a primitive Geometry.
    /// </summary>
    /// <param name="type">Type.</param>
    /// <param name="position">Position.</param>
    /// <param name="scale">Scale.</param>
    /// <param name="color">Color.</param>
    /// <param name="parent">Parent.</param>
    private void createPrimitive(PrimitiveType type, Vector3 position, Vector3 scale, Color color, GameObject parent)
    {
        var primitive = GameObject.CreatePrimitive(type);
        primitive.transform.parent = parent.transform;
        primitive.transform.localScale = scale;
        primitive.transform.position = position;

        MeshRenderer mr = primitive.GetComponent<MeshRenderer>();
        mr.material = new Material(Shader.Find("Diffuse"));

        primitive.GetComponent<Renderer>().material.color = color;
    }

    /// <summary>
    /// Calculates the center between two vectors.
    /// </summary>
    /// <returns>The center.</returns>
    /// <param name="a">The alpha component.</param>
    /// <param name="b">The blue component.</param>
    private Vector3 calculateCenter(Vector3 a, Vector3 b)
    {
        Vector3 ab = b - a;
        return a + (ab.magnitude / 2) * ab.normalized;
    }

    /// <summary>
    /// Calculates the point on a vector with given distance.
    /// </summary>
    /// <returns>The point on vector.</returns>
    /// <param name="a">The alpha component.</param>
    /// <param name="b">The blue component.</param>
    /// <param name="distance">Distance.</param>
    private Vector3 calculatePointOnVector(Vector3 a, Vector3 b, float distance)
    {
        return a + (distance * (b - a).normalized);
    }

    /// <summary>
    /// Returns the initialization state of the debug plane.
    /// </summary>
    /// <returns><c>true</c>, if debug plane intialized was ised, <c>false</c> otherwise.</returns>
    public bool IsDebugPlaneIntialized()
    {
        return debugPlaneInitialized_;
    }


    /// <summary>
    /// Returns the camera image plane corners.
    /// </summary>
    /// <returns>The camera image plane corners.</returns>
    public Dictionary<string, Vector3> getCameraImagePlaneCorners()
    {
        return cameraImagePlaneCorners_;
    }

    /// <summary>
    /// Returns the mathematically constructed camera image plane.
    /// </summary>
    /// <returns>The camera image plane.</returns>
    public Plane getCameraImagePlane()
    {
        return cameraImagePlane_;
    }

}


