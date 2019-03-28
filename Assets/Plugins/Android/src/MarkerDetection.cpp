#include <math.h>
#include <stddef.h>
#include <stdlib.h>
#include <iostream>
#include <string>
#include <sstream>
#include <GLES3/gl3.h>

#include "native_debug.h"

#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>

#include "marker_detector.h"

typedef void (*UnityRenderingEvent)(int eventId);

// GL Stuff
static float g_Time;

static void *g_TextureHandle = NULL;
static int g_TextureWidth = 0;
static int g_TextureHeight = 0;

cv::Mat current_image(480, 640, CV_8UC4);

// binarize image settings
static int BINARIZE_MAX = 255;
static int BINARIZE_THRESHOLD = 100;

// Marker detection
MarkerDetector MARKER_DETECTOR(81, 9, false);
std::vector<float> CURRENT_MARKERS;

/// --- callbacks
void convertImageToCvMat(int width, int height, int y_row_stride, unsigned char *image_data, cv::Mat *image_matrix);

static void OnRenderEvent(int eventID);
/// ---


/**
 * SetTimeFromUnity, an example function we export which is called by one of the scripts.
 */
extern "C" void SetTimeFromUnity(float t) {
    g_Time = t;
}


/**
 * SetTextureFromUnity, an example function we export which is called by one of the scripts.
 */
extern "C" void SetTextureFromUnity(void *textureHandle, int w, int h) {
    // A script calls this at initialization time; just remember the texture pointer here.
    // Will update texture pixels each frame from the plugin rendering event (texture update
    // needs to happen on the rendering thread).
    g_TextureHandle = textureHandle;
    g_TextureWidth = w;
    g_TextureHeight = h;
}

/**
 *
 */
extern "C" void GetFoundMarkers(int *length, int *marker_stride, float **data) {
    // determine current array length
    *length = CURRENT_MARKERS.size();

    // define marker stride
    *marker_stride = 10;

    // copy data to output array
    auto size = (*length) * sizeof(float);
    *data = static_cast<float *>(malloc(size));
    memcpy(*data, CURRENT_MARKERS.data(), size);
}



/**
 * GetRenderEventFunc, an example function we export which is used to get a rendering event callback function.
 */
extern "C" UnityRenderingEvent GetRenderEventFunc() {
    return OnRenderEvent;
}


/**
 * Finds Markers in an image captured by AR Core
 * @param width
 * @param height
 * @param y_row_stride
 * @param uv_row_stride
 * @param uv_pixel_stride
 * @param image_data
 * @param buffer_size
 */
extern "C" void findMarkersInImage(int width, int height, int y_row_stride, unsigned char *image_data) {
    convertImageToCvMat(width, height, y_row_stride, image_data, &current_image);

    /// make greyscale
    cv::Mat grey(height, width, CV_8UC1);
    cv::cvtColor(current_image, grey, cv::COLOR_RGB2GRAY);

    /// binarize image
    cv::Mat binarized(height, width, CV_8UC1);
    cv::threshold(grey, binarized, BINARIZE_THRESHOLD, BINARIZE_MAX, cv::THRESH_BINARY);

    /// clear last frame marker & find new ones
    CURRENT_MARKERS.clear();
    MARKER_DETECTOR.findMarkers(binarized, &CURRENT_MARKERS, &current_image);
}

/**
 *
 * @param eventID
 */
static void OnRenderEvent(int eventID) {
    GLuint gltex = (GLuint)(size_t)(g_TextureHandle);

    // Update texture data, and free the memory buffer
    glBindTexture(GL_TEXTURE_2D, gltex);
    glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, 640, 480, GL_RGBA, GL_UNSIGNED_BYTE, current_image.data);
}

/**
 * Converts a given raw YUV_420_888 coded image to OpenCV Mat.
 * Since the image is converted to greyscale anyways this function will omit the UV Plane
 * and simply use the Y (luminance) value of the input image as data
 * @param width
 * @param height
 * @param y_row_stride
 * @param uv_row_stride
 * @param uv_pixel_stride
 * @param image_data
 * @param buffer_size
 * @param image_matrix
 */
void convertImageToCvMat(int width, int height, int y_row_stride, unsigned char *image_data, cv::Mat *image_matrix) {
    uchar *p_out;
    int channels = 4;
    for (int r = 0; r < image_matrix->rows; r++) {
        p_out = image_matrix->ptr<uchar>(r);
        for (int c = 0; c < image_matrix->cols * channels; c++) {
            unsigned int idx = (r * y_row_stride) + (c / channels);
            p_out[c] = static_cast<uchar>(image_data[idx]);
            p_out[c + 1] = static_cast<uchar>(image_data[idx]);
            p_out[c + 2] = static_cast<uchar>(image_data[idx]);
            p_out[c + 3] = static_cast<uchar>(255);
        }
    }
}
