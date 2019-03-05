using System.Collections;
using System.Collections.Generic;
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

    private List<Marker> marker_list;


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
    private static extern void findMarkersInImage(int width, int height, int row_stride, byte[] pixel_buffer);

    [DllImport("native")]
    private static extern void SetTextureFromUnity(System.IntPtr texture, int w, int h);

    [DllImport("native")]
    private static extern void SetTimeFromUnity(float t);

    [DllImport("native")]
    private static extern System.IntPtr GetRenderEventFunc();

    [DllImport("native")]
    private static extern void GetFoundMarkers(out int length, out int marker_stride, out System.IntPtr array);


    IEnumerator Start()
    {
        //marker_list = new List<Marker>();
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
                        _OnImageAvailable(image.Width, image.Height, image.YRowStride, image.Y);
                        image.Release();
                    }
                    else
                    {
                        Debug.LogWarning("There is no new image available!");
                    }
                }

            }
            else
            {
                Debug.LogWarning("The AR Core session seems to be invalid!");
            }

            // Issue a plugin event with arbitrary integer identifier.
            // The plugin can distinguish between different
            // things it needs to do based on this ID.
            // For our simple plugin, it does not matter which ID we pass here.
            GL.IssuePluginEvent(GetRenderEventFunc(), 1);

            // gather detected markers from native plugin
            _GetMarkersFromPlugin();


            //// Raycast for each marker location to find the three dimensional references
            //TrackableHitFlags raycastFilter = TrackableHitFlags.PlaneWithinPolygon |
            //    TrackableHitFlags.FeaturePointWithSurfaceNormal;
            //foreach (var marker in marker_list)
            //{
            //    var centroid = marker.GetCentroid();

            //    TrackableHit hit;
            //    Frame.Raycast(centroid.x, centroid.y, raycastFilter, out hit);
            //    Debug.Log("Latest hit: " + hit);
            //}

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
    private void _OnImageAvailable(int width, int height, int rowStride, System.IntPtr pixel_buffer)
    {
        // Adjust buffer size if necessary.
        int actual_bugger_size = rowStride * height;
        byte[] image_buffer = new byte[actual_bugger_size];

        // Move raw data into managed buffer.
        Marshal.Copy(pixel_buffer, image_buffer, 0, actual_bugger_size);
        try
        {
            findMarkersInImage(width, height, rowStride, image_buffer);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Function: 'void findMarkersInImage(int, int, int, int, int, byte[], int)'\n  Failed with exception:");
            Debug.LogError(e);
        }

    }

    /// <summary>
    /// Gets the markers from plugin.
    /// </summary>
    private void _GetMarkersFromPlugin()
    {
        // clear local marker list
        //marker_list.Clear();

        // create local variables & gather values
        int length;
        int marker_stride;
        System.IntPtr marker_array_ptr;
        GetFoundMarkers(out length, out marker_stride, out marker_array_ptr);
        byte[] marker_array = new byte[length];

        // copy data
        Marshal.Copy(marker_array_ptr, marker_array, 0, length);
        Marshal.FreeCoTaskMem(marker_array_ptr);

        int num_markers = length / marker_stride;
        Debug.Log("Number of markers: " + num_markers);

        if (length > 0)
        {
            for (int n = 0; n < num_markers; ++n)
            {
                int id = (int)marker_array[n];
                int direction = (int)marker_array[n + 1];
                Vector2 tl = new Vector2(marker_array[n + 2], marker_array[n + 3]);
                Vector2 tr = new Vector2(marker_array[n + 4], marker_array[n + 5]);
                Vector2 br = new Vector2(marker_array[n + 6], marker_array[n + 7]);
                Vector2 bl = new Vector2(marker_array[n + 8], marker_array[n + 9]);
                //Marker m = new Marker(id, direction, tl, tr, br, bl);

                //this.marker_list.Add(m);
            }
        }
    }
}