using System.Collections;
using System.Runtime.InteropServices;

using UnityEngine;

using GoogleARCore;

public class MarkerDetector : MonoBehaviour
{
    public ARCoreSession arCoreSession;
    public GameObject debugScreen;

    public int imageWidth;
    public int imageHeight;

    private CameraImageBytes current_image_bytes;
    private CameraIntrinsics camera_intrinsics;

    /// <summary>
    /// Finds the markers in image.
    /// </summary>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <param name="row_stride">Row stride.</param>
    /// <param name="uv_stride">Uv stride.</param>
    /// <param name="uv_pixel_stride">Uv pixel stride.</param>
    /// <param name="pixel_buffer">Pixel buffer.</param>
    /// <param name="bufferSize">Buffer size.</param>
    [DllImport("native")]
    private static extern void findMarkersInImage(int width, int height, int row_stride, int uv_stride, int uv_pixel_stride, byte[] pixel_buffer, int bufferSize);

    [DllImport("native")]
    private static extern void SetTextureFromUnity(System.IntPtr texture, int w, int h);

    [DllImport("native")]
    private static extern void SetTimeFromUnity(float t);

    [DllImport("native")]
    private static extern System.IntPtr GetRenderEventFunc();

    IEnumerator Start()
    {
        CreateTextureAndPassToPlugin();
        yield return StartCoroutine("CallPluginAtEndOfFrames");
    }

    private IEnumerator CallPluginAtEndOfFrames()
    {
        while (true)
        {
            // Wait until all frame rendering is done
            yield return new WaitForEndOfFrame();

            // Set time for the plugin
            SetTimeFromUnity(Time.timeSinceLevelLoad);

            if (Session.Status.IsValid())
            {
                using (var image = Frame.CameraImage.AcquireCameraImageBytes())
                {
                    if (image.IsAvailable)
                    {
                        _OnImageAvailable(image.Width, image.Height, image.YRowStride, image.UVRowStride, image.UVPixelStride, image.Y, 0);

                        // release buffer
                        image.Release();
                    }
                    else
                    {
                        Debug.LogWarning("No Image seems to be available");
                    }
                }

            }
            else
            {
                Debug.LogWarning("AR Core session seems to be invalid");
            }

            // Issue a plugin event with arbitrary integer identifier.
            // The plugin can distinguish between different
            // things it needs to do based on this ID.
            // For our simple plugin, it does not matter which ID we pass here.
            GL.IssuePluginEvent(GetRenderEventFunc(), 1);
        }
    }

    private void CreateTextureAndPassToPlugin()
    {
        // Create a texture - FIXME: non-power-of-two (NPOT) is inefficient
        Texture2D tex = new Texture2D(640, 480, TextureFormat.RGBA32, false);
        // Set point filtering just so we can see the pixels clearly
        tex.filterMode = FilterMode.Point;
        // Call Apply() so it's actually uploaded to the GPU
        tex.Apply();

        // Set texture onto our material
        debugScreen.GetComponent<Renderer>().material.mainTexture = tex;

        // Pass texture pointer to the plugin
        SetTextureFromUnity(tex.GetNativeTexturePtr(), tex.width, tex.height);
    }


    /// <summary>
    /// Runs if a new image is available
    /// </summary>
    /// <param name="width">Width.</param>
    /// <param name="height">Height.</param>
    /// <param name="rowStride">Row stride.</param>
    /// <param name="pixel_buffer">Pixel buffer.</param>
    /// <param name="buffer_size">Buffer size.</param>
    private void _OnImageAvailable(int width, int height, int rowStride, int uvStride, int uvPixelStride, System.IntPtr pixel_buffer, int buffer_size)
    {
        // Adjust buffer size if necessary.
        int actual_bugger_size = rowStride * height;
        byte[] image_buffer = new byte[actual_bugger_size];

        // Move raw data into managed buffer.
        Marshal.Copy(pixel_buffer, image_buffer, 0, actual_bugger_size);
        try
        {
            findMarkersInImage(width, height, rowStride, uvStride, uvPixelStride, image_buffer, actual_bugger_size);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Function: 'void findMarkersInImage(int, int, int, int, int, byte[], int)'\n  Failed with exception:");
            Debug.LogError(e);
        }

    }
}