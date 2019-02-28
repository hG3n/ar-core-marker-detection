#include <math.h>
#include <stddef.h>
#include <stdlib.h>
#include <iostream>
#include <string>
#include <sstream>
#include <GLES3/gl3.h>

#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>

#include "native_debug.h"

typedef void (*UnityRenderingEvent)(int eventId);

static float g_Time;

static void *g_TextureHandle = NULL;
static int g_TextureWidth = 0;
static int g_TextureHeight = 0;

cv::Mat current_image(480, 640, CV_8UC4);
//cv::Mat current_image(480, 640, CV_8UC1);
//cv::Mat grey(480, 640, CV_8UC1);

// callbacks
void convertImageToCvMat(int width, int height, int y_row_stride, unsigned char *image_data, cv::Mat *image_matrix);


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
 * @param eventID
 */
static void OnRenderEvent(int eventID) {
    GLuint gltex = (GLuint)(size_t)(g_TextureHandle);

    // Update texture data, and free the memory buffer
    glBindTexture(GL_TEXTURE_2D, gltex);
    glTexSubImage2D(GL_TEXTURE_2D, 0, 0, 0, 640, 480, GL_RGBA, GL_UNSIGNED_BYTE, current_image.data);
}

/**
 * GetRenderEventFunc, an example function we export which is used to get a rendering event callback function.
 */
extern "C" UnityRenderingEvent GetRenderEventFunc() {
    return OnRenderEvent;
}


/**
 *
 */
extern "C" void findMarkersInImage(int width, int height, int y_row_stride, int uv_row_stride, int uv_pixel_stride,
                                   unsigned char *image_data, int buffer_size) {
    convertImageToCvMat(width, height, y_row_stride, image_data, &current_image);
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
//            unsigned int idx = r * y_row_stride + c;
            p_out[c] = static_cast<uchar>(image_data[idx]);
            p_out[c + 1] = static_cast<uchar>(image_data[idx]);
            p_out[c + 2] = static_cast<uchar>(image_data[idx]);
            p_out[c + 3] = static_cast<uchar>(255);
        }
    }
}
