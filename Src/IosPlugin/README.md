# Emotiv Unity Plugin - iOS Integration Guide

## Download and Setup
Download the most recent version of [EmotivCortexLib](https://github.com/Emotiv/cortex-embedded-lib-example/releases) and place in this directory
## Integration
In your Unity project, integrate the following functions to interface with CortexLib via the EmotivUnityPlugin
    - bool InitCortexLib();
    - void StopCortexLib();
    - void RegisterUnityResponseCallback(MessageCallback callback);
    - void SendRequest(string request);