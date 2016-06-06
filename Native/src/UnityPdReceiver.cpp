#include "UnityPdReceiver.h"
#include <iostream>

using namespace std;
using namespace pd;

typedef void (*DebugFuncPtr)( const char * );

static DebugFuncPtr Debug{nullptr};

extern "C" void LibPD_SetDebugFunction( DebugFuncPtr fp )
{
    Debug = fp;
}

void UnityPdReceiver::print(const std::string& message)
{
    if (Debug != nullptr) Debug(message.c_str());
}