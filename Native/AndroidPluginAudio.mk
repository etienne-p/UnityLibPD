LOCAL_PATH := $(call my-dir)

include $(CLEAR_VARS)
LOCAL_MODULE := pd-prebuilt
LOCAL_SRC_FILES := $(LOCAL_PATH)/libs/$(TARGET_ARCH_ABI)/libpd.so
LOCAL_EXPORT_C_INCLUDES := $(LOCAL_PATH)/..
include $(PREBUILT_SHARED_LIBRARY)

include $(CLEAR_VARS)

LOCAL_MODULE := AudioPluginLibPD

LOCAL_C_INCLUDES := $(LOCAL_PATH)/pure-data/src
LOCAL_C_INCLUDES += $(LOCAL_PATH)/libpd_wrapper
LOCAL_C_INCLUDES += $(LOCAL_PATH)/libpd_wrapper/util
LOCAL_C_INCLUDES += $(LOCAL_PATH)/cpp

#LOCAL_CFLAGS := -DPD

LOCAL_CPPFLAGS := -std=c++0x

LOCAL_SRC_FILES := \
  cpp/PdTypes.cpp \
  cpp/PdBase.cpp \
  src/UnityPdReceiver.cpp \
  src/AudioPluginUtil.cpp \
  src/Plugin_LibPD.cpp \
  src/AudioPlugin_LibPD.cpp

LOCAL_LDLIBS := -lm -llog
LOCAL_SHARED_LIBRARIES := pd-prebuilt

include $(BUILD_SHARED_LIBRARY)