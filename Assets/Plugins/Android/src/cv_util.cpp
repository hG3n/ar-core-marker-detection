#include "cv_util.h"

void
CvUtil::drawCross(cv::Mat *image, const cv::Point2i &position, const cv::Scalar &color, int radius, int thickness) {
    cv::line(*image,
             cv::Point2i(position.x - radius, position.y),
             cv::Point2i(position.x + radius, position.y),
             color,
             thickness);
    cv::line(*image,
             cv::Point2i(position.x, position.y - radius),
             cv::Point2i(position.x, position.y + radius),
             color,
             thickness);

}

