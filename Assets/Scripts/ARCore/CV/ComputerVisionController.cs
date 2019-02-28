//-----------------------------------------------------------------------
// <copyright file="ComputerVisionController.cs" company="Google">
//
// Copyright 2017 Google Inc. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// </copyright>
//-----------------------------------------------------------------------

    using System;
    using System.Collections.Generic;
    using GoogleARCore;
    using UnityEngine;
    using UnityEngine.UI;

    #if UNITY_EDITOR
    // Set up touch input propagation while using Instant Preview in the editor.
using Input = GoogleARCore.InstantPreviewInput;
    #endif  // UNITY_EDITOR

    /// <summary>
    /// Controller for the ComputerVision example that accesses the CPU camera image (i.e. image bytes), performs
    /// edge detection on the image, and renders an overlay to the screen.
    /// </summary>
    public class ComputerVisionController : MonoBehaviour
    {
        /// <summary>
        /// The ARCoreSession monobehavior that manages the ARCore session.
        /// </summary>
        public ARCoreSession ARSessionManager;

        /// <summary>
        /// A buffer that stores the result of performing edge detection on the camera image each frame.
        /// </summary>
        private byte[] m_EdgeDetectionResultImage = null;

        /// <summary>
        /// Texture created from the result of running edge detection on the camera image bytes.
        /// </summary>
        private Texture2D m_EdgeDetectionBackgroundTexture = null;


        // privates
        private bool m_IsQuitting = false;
        private bool m_UseHighResCPUTexture = false;
        private ARCoreSession.OnChooseCameraConfigurationDelegate m_OnChoseCameraConfiguration = null;
        private bool m_Resolutioninitialized = false;

        /// <summary>
        /// The Unity Start() method.
        /// </summary>
        public void Start()
        {
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // Register the callback to set camera config before arcore session is enabled.
            m_OnChoseCameraConfiguration = _ChooseCameraConfiguration;
            ARSessionManager.RegisterChooseCameraConfigurationCallback(m_OnChoseCameraConfiguration);

            ARSessionManager.enabled = true;
            _SetAutoFocus(false);
        }

        /// <summary>
        /// The Unity Update() method.
        /// </summary>
        public void Update()
        {
            if (Input.GetKey(KeyCode.Escape))
                Application.Quit();
            _QuitOnConnectionErrors();


            if (!Session.Status.IsValid())
                return;

            using (var image = Frame.CameraImage.AcquireCameraImageBytes())
            {
                if (!image.IsAvailable)
                    return;
                _OnImageAvailable(image.Width, image.Height, image.YRowStride, image.Y, 0);
            }
        }


        /// <summary>
        /// Activates or deactivates autofocus
        /// </summary>
        /// <param name="autoFocusEnabled">If set to <c>true</c> auto focus will be enabled.</param>
        private void _SetAutoFocus(bool autoFocusEnabled)
        {
            var config = ARSessionManager.SessionConfig;
            if (config != null)
                config.CameraFocusMode = autoFocusEnabled ? CameraFocusMode.Auto : CameraFocusMode.Fixed;
        }

        /// <summary>
        /// Handles a new CPU image.
        /// </summary>
        /// <param name="width">Width of the image, in pixels.</param>
        /// <param name="height">Height of the image, in pixels.</param>
        /// <param name="rowStride">Row stride of the image, in pixels.</param>
        /// <param name="pixelBuffer">Pointer to raw image buffer.</param>
        /// <param name="bufferSize">The size of the image buffer, in bytes.</param>
        private void _OnImageAvailable(int width, int height, int rowStride, IntPtr pixelBuffer, int bufferSize)
        {
            //if (!EdgeDetectionBackgroundImage.enabled)
            //{
            //    return;
            //}

            //if (m_EdgeDetectionBackgroundTexture == null || m_EdgeDetectionResultImage == null ||
            //    m_EdgeDetectionBackgroundTexture.width != width || m_EdgeDetectionBackgroundTexture.height != height)
            //{
            //    m_EdgeDetectionBackgroundTexture = new Texture2D(width, height, TextureFormat.R8, false, false);
            //    m_EdgeDetectionResultImage = new byte[width * height];
            //    _UpdateCameraImageToDisplayUVs();
            //}

            //if (m_CachedOrientation != Screen.orientation || m_CachedScreenDimensions.x != Screen.width ||
            //    m_CachedScreenDimensions.y != Screen.height)
            //{
            //    _UpdateCameraImageToDisplayUVs();
            //    m_CachedOrientation = Screen.orientation;
            //    m_CachedScreenDimensions = new Vector2(Screen.width, Screen.height);
            //}

            //// Detect edges within the image.
            //if (EdgeDetector.Detect(m_EdgeDetectionResultImage, pixelBuffer, width, height, rowStride))
            //{
            //    // Update the rendering texture with the edge image.
            //    m_EdgeDetectionBackgroundTexture.LoadRawTextureData(m_EdgeDetectionResultImage);
            //    m_EdgeDetectionBackgroundTexture.Apply();
            //    EdgeDetectionBackgroundImage.material.SetTexture("_ImageTex", m_EdgeDetectionBackgroundTexture);

            //    const string TOP_LEFT_RIGHT = "_UvTopLeftRight";
            //    const string BOTTOM_LEFT_RIGHT = "_UvBottomLeftRight";
            //    EdgeDetectionBackgroundImage.material.SetVector(TOP_LEFT_RIGHT, new Vector4(
            //        m_CameraImageToDisplayUvTransformation.TopLeft.x,
            //        m_CameraImageToDisplayUvTransformation.TopLeft.y,
            //        m_CameraImageToDisplayUvTransformation.TopRight.x,
            //        m_CameraImageToDisplayUvTransformation.TopRight.y));
            //    EdgeDetectionBackgroundImage.material.SetVector(BOTTOM_LEFT_RIGHT, new Vector4(
            //        m_CameraImageToDisplayUvTransformation.BottomLeft.x,
            //        m_CameraImageToDisplayUvTransformation.BottomLeft.y,
            //        m_CameraImageToDisplayUvTransformation.BottomRight.x,
            //        m_CameraImageToDisplayUvTransformation.BottomRight.y));
            //}
        }

        /// <summary>
        /// Gets the rotation that needs to be applied to the device camera image in order for it to match
        /// the current orientation of the display.
        /// </summary>
        /// <returns>The needed rotation.</returns>
        private int _GetCameraImageToDisplayRotation()
        {

#if !UNITY_EDITOR
            AndroidJavaClass cameraClass = new AndroidJavaClass("android.hardware.Camera");
            AndroidJavaClass cameraInfoClass = new AndroidJavaClass("android.hardware.Camera$CameraInfo");
            AndroidJavaObject cameraInfo = new AndroidJavaObject("android.hardware.Camera$CameraInfo");
            cameraClass.CallStatic("getCameraInfo", cameraInfoClass.GetStatic<int>("CAMERA_FACING_BACK"),
                cameraInfo);
            int cameraRotationToNaturalDisplayOrientation = cameraInfo.Get<int>("orientation");

            AndroidJavaClass contextClass = new AndroidJavaClass("android.content.Context");
            AndroidJavaClass unityPlayerClass = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayerClass.GetStatic<AndroidJavaObject>("currentActivity");
            AndroidJavaObject windowManager =
                unityActivity.Call<AndroidJavaObject>("getSystemService",
                contextClass.GetStatic<string>("WINDOW_SERVICE"));

            AndroidJavaClass surfaceClass = new AndroidJavaClass("android.view.Surface");
            int displayRotationFromNaturalEnum = windowManager
                .Call<AndroidJavaObject>("getDefaultDisplay").Call<int>("getRotation");

            int displayRotationFromNatural = 0;
            if (displayRotationFromNaturalEnum == surfaceClass.GetStatic<int>("ROTATION_90"))
            {
                displayRotationFromNatural = 90;
            }
            else if (displayRotationFromNaturalEnum == surfaceClass.GetStatic<int>("ROTATION_180"))
            {
                displayRotationFromNatural = 180;
            }
            else if (displayRotationFromNaturalEnum == surfaceClass.GetStatic<int>("ROTATION_270"))
            {
                displayRotationFromNatural = 270;
            }

            return (cameraRotationToNaturalDisplayOrientation + displayRotationFromNatural) % 360;
#else  // !UNITY_EDITOR
            // Using Instant Preview in the Unity Editor, the display orientation is always portrait.
            return 0;
#endif  // !UNITY_EDITOR
        }

        /// <summary>
        /// Gets the percentage of space needed to be cropped on the device camera image to match the display
        /// aspect ratio.
        /// </summary>
        /// <param name="uBorder">The cropping of the 'u' dimension.</param>
        /// <param name="vBorder">The cropping of the 'v' dimension.</param>
        private void _GetUvBorders(out float uBorder, out float vBorder)
        {
            int imageWidth = m_EdgeDetectionBackgroundTexture.width;
            int imageHeight = m_EdgeDetectionBackgroundTexture.height;

            float screenAspectRatio;
            var cameraToDisplayRotation = _GetCameraImageToDisplayRotation();
            if (cameraToDisplayRotation == 90 || cameraToDisplayRotation == 270)
                screenAspectRatio = (float)Screen.height / Screen.width;
            else
                screenAspectRatio = (float)Screen.width / Screen.height;

            var imageAspectRatio = (float)imageWidth / imageHeight;
            var croppedWidth = 0.0f;
            var croppedHeight = 0.0f;

            if (screenAspectRatio < imageAspectRatio)
            {
                croppedWidth = imageHeight * screenAspectRatio;
                croppedHeight = imageHeight;
            }
            else
            {
                croppedWidth = imageWidth;
                croppedHeight = imageWidth / screenAspectRatio;
            }

            uBorder = (imageWidth - croppedWidth) / imageWidth / 2.0f;
            vBorder = (imageHeight - croppedHeight) / imageHeight / 2.0f;
        }

        /// <summary>
        /// Quit the application if there was a connection error for the ARCore session.
        /// </summary>
        private void _QuitOnConnectionErrors()
        {
            if (m_IsQuitting)
                return;

            // Quit if ARCore was unable to connect and give Unity some time for the toast to appear.
            if (Session.Status == SessionStatus.ErrorPermissionNotGranted)
            {
                _ShowAndroidToastMessage("Camera permission is needed to run this application.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 0.5f);
            }
            else if (Session.Status == SessionStatus.FatalError)
            {
                _ShowAndroidToastMessage("ARCore encountered a problem connecting.  Please start the app again.");
                m_IsQuitting = true;
                Invoke("_DoQuit", 0.5f);
            }
        }

        /// <summary>
        /// Show an Android toast message.
        /// </summary>
        /// <param name="message">Message string to show in the toast.</param>
        private void _ShowAndroidToastMessage(string message)
        {
            AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject unityActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");

            if (unityActivity != null)
            {
                AndroidJavaClass toastClass = new AndroidJavaClass("android.widget.Toast");
                unityActivity.Call("runOnUiThread", new AndroidJavaRunnable(() =>
                {
                    AndroidJavaObject toastObject = toastClass.CallStatic<AndroidJavaObject>("makeText", unityActivity,
                        message, 0);
                    toastObject.Call("show");
                }));
            }
        }

        /// <summary>
        /// Actually quit the application.
        /// </summary>
        private void _DoQuit()
        {
            Application.Quit();
        }

        /// <summary>
        /// Generate string to print the value in CameraIntrinsics.
        /// </summary>
        /// <param name="intrinsics">The CameraIntrinsics to generate the string from.</param>
        /// <param name="intrinsicsType">The string that describe the type of the intrinsics.</param>
        /// <returns>The generated string.</returns>
        private string _CameraIntrinsicsToString(CameraIntrinsics intrinsics, string intrinsicsType)
        {
            float fovX = 2.0f * Mathf.Atan2(intrinsics.ImageDimensions.x, 2 * intrinsics.FocalLength.x) * Mathf.Rad2Deg;
            float fovY = 2.0f * Mathf.Atan2(intrinsics.ImageDimensions.y, 2 * intrinsics.FocalLength.y) * Mathf.Rad2Deg;

            string message = string.Format("Unrotated Camera {4} Intrinsics:{0}  Focal Length: {1}{0}  " +
                "Principal Point: {2}{0}  Image Dimensions: {3}{0}  Unrotated Field of View: ({5}°, {6}°)",
                Environment.NewLine, intrinsics.FocalLength.ToString(),
                intrinsics.PrincipalPoint.ToString(), intrinsics.ImageDimensions.ToString(),
                intrinsicsType, fovX, fovY);
            return message;
        }

        /// <summary>
        /// Select the desired camera configuration.
        /// </summary>
        /// <param name="supportedConfigurations">A list of all supported camera configuration.</param>
        /// <returns>The desired configuration index.</returns>
        private int _ChooseCameraConfiguration(List<CameraConfig> supportedConfigurations)
        {
            Debug.Log("Grumbenbark | supported camera configurations: " + supportedConfigurations);

            if (!m_Resolutioninitialized)
            {
                Vector2 ImageSize = supportedConfigurations[0].ImageSize;
                m_Resolutioninitialized = true;
            }

            if (m_UseHighResCPUTexture)
            {
                return supportedConfigurations.Count - 1;
            }

            return 0;
        }
    }
