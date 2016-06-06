#pragma once

#ifdef __cplusplus
extern "C"
{
#endif
    
int LibPD_ProcessAudio(const int id, float* inbuffer, float* outbuffer, unsigned int length, int inchannels, int outchannels);

#ifdef __cplusplus
}
#endif