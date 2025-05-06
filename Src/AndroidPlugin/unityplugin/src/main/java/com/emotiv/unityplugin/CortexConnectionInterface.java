package com.emotiv.unityplugin;

import org.json.JSONObject;

public interface CortexConnectionInterface {
    void onReceivedMessage(String msg);
    void onCortexStarted();
}
