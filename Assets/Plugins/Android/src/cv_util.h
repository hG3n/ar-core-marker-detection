#ifndef CV_UTIL_H
#define CV_UTIL_H

#include <opencv2/core.hpp>
#include <opencv2/imgproc.hpp>

class CvUtil {

public:
    static void
    drawCross(cv::Mat *image, const cv::Point2i &position, const cv::Scalar &color, int radius, int thickness);
};

#endif // CV_UTIL_H

