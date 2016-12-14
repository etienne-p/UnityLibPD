///
//  UnityPdReceiver.h
//  AudioPluginLibPD
//
//  Created by etienne cella on 2016-06-03.
//  Copyright © 2016 etienne. All rights reserved.
//

#pragma once

#include <string>
#include <queue>
#include "PdBase.hpp"

class UnityPdReceiver : public pd::PdReceiver
{
public:
    
    std::queue<std::string> logs;
    
    // pd message receiver callbacks
    void print          (const std::string& message);
    void receiveBang    (const std::string& source);
    void receiveFloat   (const std::string& source, float num);
    void receiveSymbol  (const std::string& source, const std::string& symbol);
};

