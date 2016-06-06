//
//  Plugin_LibPD.cpp
//  UnityLibPD
//
//  Created by etienne cella on 2016-05-27.
//  Copyright © 2016 dpt. All rights reserved.
//

#include "AudioPluginUtil.h"
#include "Plugin_LibPD.h"
#include <cstring>

namespace LibPD
{
    const int MAX_INDEX = 12; // arbitrary, tied to the plugins interface which wants a range
   
    enum Param
    {
        P_INDEX,
        P_NUM // mandatory
    };

    struct EffectData
    {
        // at the moment we ignore PS3 related constraints
        // see http://docs.unity3d.com/Manual/AudioMixerNativeAudioPlugin.html
        // to understand constraints related to DSP processing occuring on the PS3 spu
        float p[P_NUM];
    };

    int InternalRegisterEffectDefinition(UnityAudioEffectDefinition& definition)
    {
        int numparams = P_NUM;
        definition.paramdefs = new UnityAudioParameterDefinition [numparams];

        // Instance accessed by index as Unity will have to call static methods to access it
        RegisterParameter(
                definition, "Index", "", 0.0f,
                MAX_INDEX - 1, 0.0f, 1.0f, 1.0f,
                P_INDEX);

        return numparams;
    }

    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK
    CreateCallback(UnityAudioEffectState* state)
    {
        EffectData* effectdata = new EffectData;
        memset(effectdata, 0, sizeof(EffectData));
        state->effectdata = effectdata;
        InitParametersFromDefinitions(InternalRegisterEffectDefinition, effectdata->p);
        return UNITY_AUDIODSP_OK;
    }

    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK
    ReleaseCallback(UnityAudioEffectState* state)
    {
        EffectData* data = state->GetEffectData<EffectData>();
        delete data;
        return UNITY_AUDIODSP_OK;
    }

    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK
    SetFloatParameterCallback(UnityAudioEffectState* state, int index, float value)
    {
        EffectData* data = state->GetEffectData<EffectData>();
        if(index >= P_NUM)
            return UNITY_AUDIODSP_ERR_UNSUPPORTED;
        data->p[index] = value;
        return UNITY_AUDIODSP_OK;
    }

    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK
    GetFloatParameterCallback(UnityAudioEffectState* state, int index, float* value, char *valuestr)
    {
        EffectData* data = state->GetEffectData<EffectData>();
        if(index >= P_NUM)
            return UNITY_AUDIODSP_ERR_UNSUPPORTED;
        if(value != NULL)
            *value = data->p[index];
        if(valuestr != NULL)
            valuestr[0] = 0;
        return UNITY_AUDIODSP_OK;
    }

    int UNITY_AUDIODSP_CALLBACK
    GetFloatBufferCallback (UnityAudioEffectState* state, const char* name, float* buffer, int numsamples)
    {
        return UNITY_AUDIODSP_OK;
    }

    UNITY_AUDIODSP_RESULT UNITY_AUDIODSP_CALLBACK
    ProcessCallback(
            UnityAudioEffectState* state,
            float* inbuffer, float* outbuffer,
            unsigned int length,
            int inchannels, int outchannels)
    {
        const auto id = state->GetEffectData<EffectData>()->p[P_INDEX];
        
        // attempts to have a pd context to fullfill the request
        auto status = LibPD_ProcessAudio(
            id, inbuffer, outbuffer, length, inchannels, outchannels);
        
        if (!status)
        {
            std::memset(outbuffer, .0f, sizeof(float) * length * outchannels);
        }

        return status ? UNITY_AUDIODSP_OK : UNITY_AUDIODSP_ERR_UNSUPPORTED;
    }
}