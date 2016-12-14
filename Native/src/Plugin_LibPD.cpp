#include <map>
#include <memory>
#include <functional>
#include "UnityPdReceiver.h"
#include "AudioPluginUtil.h"
#include "PdBase.hpp"

namespace LibPD
{
    const int NUM_INPUT = 2;
    const int NUM_OUTPUT = 2;
    
    struct InstanceData
    {
        std::unique_ptr<pd::PdBase> context{nullptr};
        int patchCount{0};
        std::map<int, pd::Patch> patchIndexToPointer;
        UnityPdReceiver receiver; // used for log only, at the moment
    };
    
    // store pd instances in a map
    // as pd instances are accessed by client code using an id
    std::map<int, InstanceData> instancesData;
    
    // try to retrieve the context of a pd instance
    pd::PdBase* getContext(const int id)
    {
        if (LibPD::instancesData.find(id) == LibPD::instancesData.end())
        {
            return nullptr;
        }
        return LibPD::instancesData[id].context.get();
    }
    
    // free a pd instance if it exists
    bool release(const int id, bool erase)
    {
        auto context = getContext(id);
        if (context == nullptr) return false;
        
        context->computeAudio(false);
        
        for(auto it = LibPD::instancesData[id].patchIndexToPointer.begin();
            it != LibPD::instancesData[id].patchIndexToPointer.end(); it++)
        {
            context->closePatch(it->second);
            //it->second.clear();
        }
        LibPD::instancesData[id].patchIndexToPointer.clear();
        LibPD::instancesData[id].context = nullptr;
        if (erase)
        {
            LibPD::instancesData.erase (id);
        }
        return true;
    }
    
    bool tryWithPatch(const int pdId, const int patchId, std::function<bool(pd::Patch &, int)> action)
    {
        auto patchMap = LibPD::instancesData[pdId].patchIndexToPointer;
        auto it = patchMap.find(patchId);
        if (it != patchMap.end())
        {
            return action(it->second, it->first);
        }
        return false;
    }
    
    
    /*
     ------------------------------------------------------------------------------------------------------------
     Functions to be called from the binding in the client code, in this case C# for Unity.
    */
    extern "C" bool LibPD_Create(const int id)
    {
        if (getContext(id) != nullptr) return false; // context already exists
        
        LibPD::instancesData[id] = InstanceData();
        LibPD::instancesData[id].context = std::unique_ptr<pd::PdBase>(new pd::PdBase());
        LibPD::instancesData[id].context->setReceiver(&(LibPD::instancesData[id].receiver));
        return true;
    }
    
    extern "C" bool LibPD_Init(const int id, const float samplerate)
    {
        auto context = getContext(id);
        if (context == nullptr) return false;
        context->init(NUM_INPUT, NUM_OUTPUT, samplerate);
        return true;
    }
    
    extern "C" bool LibPD_Release(const int id)
    {
        return release(id, true);
    }
    
    extern "C" bool LibPD_ReleaseAll()
    {
        for(auto it = LibPD::instancesData.begin();
            it != LibPD::instancesData.end(); it++)
        {
            // we do not erase free instances as we'll clear the map anyway
            release(it->first, false);
        }
        LibPD::instancesData.clear();
        return true;
    }
    
    extern "C" bool LibPD_WriteArray(const int id, const char* name, float* buffer, const int numSamples)
    {
        auto context = getContext(id);
        if (context == nullptr) return false;
        context->writeArray(name, buffer, numSamples, 0);
        return true;
    }
    
    extern "C" bool LibPD_SetComputeAudio(const int id, bool state)
    {
        auto context = getContext(id);
        if (context == nullptr) return false;
        context->computeAudio(state);
        return true;
    }
    
    extern "C" bool LibPD_SendBang(const int id, const char* dest)
    {
        auto context = getContext(id);
        if (context == nullptr) return false;
        context->sendBang(dest);
        return true;
    }
    
    extern "C" bool LibPD_SendFloat(const int id, const char* dest, float num)
    {
        auto context = getContext(id);
        if (context == nullptr) return false;
        context->sendFloat(dest, num);
        return true;
    }
    
    extern "C" bool LibPD_SendSymbol(const int id, const char* dest, const char* symbol)
    {
        auto context = getContext(id);
        if (context == nullptr) return false;
        context->sendSymbol(dest, symbol);
        return true;
    }
    
    extern "C" bool LibPD_SendMessage(const int id, const char* dest, const char* message)
    {
        auto context = getContext(id);
        if (context == nullptr) return false;
        context->sendMessage(dest, message);
        return true;
    }
    
    extern "C" bool LibPD_SendNoteOn(const int id, const int channel, const int pitch)
    {
        auto context = getContext(id);
        if (context == nullptr) return false;
        context->sendNoteOn(channel, pitch);
        return true;
    }
    
    extern "C" bool LibPD_Subscribe(const int id, const char* source)
    {
        auto context = getContext(id);
        if (context == nullptr) return false;
        context->subscribe(source);
        return true;
    }
    
    extern "C" bool LibPD_Unsubscribe(const int id, const char* source)
    {
        auto context = getContext(id);
        if(context == nullptr) return false;
        context->unsubscribe(source);
        return true;
    }
    
    extern "C" bool LibPD_ClosePatch(const int pdId, const int patchId)
    {
        return tryWithPatch(pdId, patchId,
                            [&pdId](pd::Patch& patch, int patchIndex_) -> bool
                            {
                                auto pdCtx = getContext(pdId);
                                if (pdCtx == nullptr) return false;
                                pdCtx->closePatch(patch);
                                //patch.clear();
                                LibPD::instancesData[pdId].patchIndexToPointer.erase (patchIndex_);
                                return true;
                            });
    }
    
    extern "C" bool LibPD_CloseAllPatches(const int id)
    {
        auto context = getContext(id);
        if (context == nullptr) return false;
        for(auto it = LibPD::instancesData[id].patchIndexToPointer.begin();
            it != LibPD::instancesData[id].patchIndexToPointer.end(); it++)
        {
            context->closePatch(it->second);
            //it->second.clear();
        }
        LibPD::instancesData[id].patchIndexToPointer.clear();
        return true;
    }
    
    extern "C" int LibPD_OpenPatch(const int id, const char* patch, const char* path)
    {
        auto context = getContext(id);
        if (context == nullptr) return -1;
        
        auto pdPatch = context->openPatch(patch, path);
        
        auto patchIndex = ++LibPD::instancesData[id].patchCount;
        LibPD::instancesData[id].patchIndexToPointer[patchIndex] = pdPatch;
        return patchIndex;
    }
    
    extern "C" int LibPD_ProcessAudio(const int id, float* inbuffer, float* outbuffer, unsigned int length, int inchannels, int outchannels)
    {
        auto context = getContext(id);
        if (context == nullptr) return false;
        
        const auto ticks = length / 64;
        context->processFloat(ticks, inbuffer, outbuffer);
        return true;
    }
}
