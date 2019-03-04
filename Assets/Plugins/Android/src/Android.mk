include $(CLEAR_VARS)

OPENCV_SDK_ROOT:=/opt/opencv-3.4.1-android
OPENCV_INSTALL_MODULES:=on
include $(OPENCV_SDK_ROOT)/sdk/native/jni/OpenCV.mk

# override strip command to strip all symbols from output library; no need to ship with those..
# cmd-strip = $(TOOLCHAIN_PREFIX)strip $1

LOCAL_ARM_MODE  := arm
LOCAL_PATH      := $(NDK_PROJECT_PATH)
LOCAL_MODULE    := libnative
LOCAL_CFLAGS    := -Werror -Wall
LOCAL_SRC_FILES := MarkerDetection.cpp marker_detector.cpp
LOCAL_LDLIBS    := -llog -lGLESv2

include $(BUILD_SHARED_LIBRARY)
