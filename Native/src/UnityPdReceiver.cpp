#include "UnityPdReceiver.h"
#include <iostream>

using namespace std;
using namespace pd;

/* Pointer to the callback functions for received Prints, Bangs, Floats and Symbols */
typedef void (*DebugFuncPtr)( const char * );
typedef void (*BangFuncPtr )( const char * );
typedef void (*FloatFuncPtr)( const char * , float);
typedef void (*SymFuncPtr  )( const char * , const char * );

/* Instances of the pointes to the functions, initialised to null */
static DebugFuncPtr Debug   {nullptr};
static BangFuncPtr  RecBang {nullptr};
static FloatFuncPtr RecFloat{nullptr};
static SymFuncPtr   RecSym  {nullptr};

/* Registering the callbacks. These functions are called from the bindings in the Client code.*/
extern "C" void LibPD_SetDebugFunction( DebugFuncPtr fp )
{
    Debug = fp;
}

extern "C" void LibPD_SetBangFunction ( BangFuncPtr bf)
{
    RecBang = bf;
}

extern "C" void LibPD_SetFloatFunction( FloatFuncPtr ff)
{
    RecFloat = ff;
}

extern "C" void LibPD_SetSymbolFunction( SymFuncPtr mf)
{
    RecSym = mf;
}

/* 
 These functions are called when events are raised and in turn call the functions
 that were previously registered 
 */
void UnityPdReceiver::print(const std::string& message)
{
    if (Debug != nullptr) Debug(message.c_str());
}

void UnityPdReceiver::receiveBang(const std::string& source)
{
    if (RecBang != nullptr) RecBang(source.c_str());
    
}

void UnityPdReceiver::receiveFloat(const std::string& source, float num)
{
    if (RecFloat != nullptr) RecFloat(source.c_str(), num);
}

void UnityPdReceiver::receiveSymbol(const std::string& source, const std::string& symbol)
{
    if (RecSym != nullptr) RecSym(source.c_str(), symbol.c_str());
}
