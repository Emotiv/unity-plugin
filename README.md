# Emotiv Cortex Unity Integration Guide

This guide will help you work with the Emotiv Cortex API to build your Unity application, supporting both mobile (Android, iOS) and desktop (Windows, macOS) platforms. It covers both available integration options: Emotiv Cortex Service and Emotiv Embedded Library.

## Overview

- **Supported Platforms:**
  - Desktop: Windows, macOS
  - Mobile: Android, iOS
- **Integration Options:**
  1. **Emotiv Cortex Service** (Desktop only)
  2. **Emotiv Embedded Library** (Mobile and Desktop with `USE_EMBEDDED_LIB` defined)

---

## Option 1: Emotiv Cortex Service (Desktop Only)

### What is it?
Connects your Unity app to the Emotiv Cortex Service running on your desktop. This is the simplest way to get started on Windows and macOS.

### Setup Steps
1. **Install EMOTIV Launcher**
   - Download and install the [EMOTIV Launcher](https://www.emotiv.com/products/emotiv-launcher) on your desktop.
2. **Project Configuration**
   - Ensure the scripting symbol `USE_EMBEDDED_LIB` is **not** defined in your Unity project.
   - No need to configure or add the Emotiv Embedded library.
3. **Authentication**
   - Login via the EMOTIV Launcher before running your Unity application.

---

## Option 2: Emotiv Embedded Library (Mobile & Desktop)

### What is it?
Integrates the Emotiv Cortex API directly into your Unity app using the Emotiv Embedded Library. This is required for mobile platforms and can also be used on desktop (in development).

### Setup Steps
1. **Contact Emotiv**
  - Contact Emotiv to request access to the Emotiv Embedded Library and the private UniWebView submodule: [Contact Emotiv](https://www.emotiv.com/pages/contact).
2. **Add the Embedded Library**
  - **Android:** Place `EmotivCortexLib.aar` in `Src/AndroidPlugin/EmotivCortexLib/`.
  - **iOS:** Place `EmotivCortexLib.xcframework` in `Src/IosPlugin/`.
3. **Add UniWebView Submodule**
   - Pull the UniWebView submodule (private repo; access required from Emotiv).
   - UniWebView is used to open a webview for authentication on mobile.
4. **Project Configuration**
   - Define the scripting symbol `USE_EMBEDDED_LIB` in your Unity project settings.
   - For desktop, support is experimental and still under development.
5. **Authentication**
   - On mobile, authentication is handled in-app via a webview.

---

## Quick Start: Using `EmotivUnityItf.cs`

The main interface for your Unity app is the `EmotivUnityItf` class.

### Initialization
1. **Call `Init()`**
   - Pass your client ID, client secret, app name, and other optional parameters.
   - It is recommended to set `isDataBufferUsing = true` so you can retrieve data from the buffer (see example below).
2. **Call `Start()`**
   - On Android, pass the current activity as a parameter: `Start(currentActivity)`
   - On other platforms, you can call `Start()` with no parameters.

### Connecting to a Headset
1. **After authorization is complete**, call `QueryHeadsets()` to discover available headsets.
2. **Create a session** with a headset using `CreateSessionWithHeadset(headsetId)`.
   - This only creates a session. To receive data, you must call `SubscribeData(streamList)` with the desired data streams (e.g., EEG, motion, etc.).
   - Alternatively, you can use `StartDataStream(streamList, headsetId)` to create a session and subscribe to data in one step.

#### Example: Creating a session and subscribing to EEG data
```csharp
// Create session with headset
EmotivUnityItf.Instance.CreateSessionWithHeadset(headsetId);
// Before subscribing, check if the session is created
if (EmotivUnityItf.Instance.IsSessionCreated)
{
  // Subscribe to EEG data
  EmotivUnityItf.Instance.SubscribeData(new List<string> { "eeg" });
}
// OR combine both steps:
EmotivUnityItf.Instance.StartDataStream(new List<string> { "eeg" }, headsetId);
```

#### Example: Getting EEG data from buffer
```csharp
// Make sure isDataBufferUsing = true in Init()
int n = EmotivUnityItf.Instance.GetNumberEEGSamples();
if (n > 0)
{
    foreach (var chan in EmotivUnityItf.Instance.GetEEGChannels())
    {
        double[] data = EmotivUnityItf.Instance.GetEEGData(chan);
        // process data
    }
}
```

---

## Recording and Markers

After creating a session, you can start recording EEG data and inject markers:

```csharp
// Start recording
EmotivUnityItf.Instance.StartRecord("MyRecordTitle");
// Inject a marker
EmotivUnityItf.Instance.InjectMarker("EventLabel", "EventValue");
// Stop recording
EmotivUnityItf.Instance.StopRecord();
```

---

## Profile Management and Training

To use BCI training, you need to load a profile for the current headset.

```csharp
// Load or create and load a profile for the current headset.If the profile does not exist, it will be created and loaded automatically.
EmotivUnityItf.Instance.LoadProfile("ProfileName");
// Start mental command training for an action (e.g., "push")
EmotivUnityItf.Instance.StartMCTraining("push");
```

See `EmotivUnityItf.cs` for more training and profile management functions.

---

## Additional Notes
- For mobile builds, ensure all required permissions (Bluetooth, etc.) are set in your Unity project.
- For Option 2, both the Embedded Library and UniWebView are private and require Emotiv approval for access.
- For the latest updates and troubleshooting, contact Emotiv support.

---

## Example Code

```csharp
// Initialization
EmotivUnityItf.Instance.Init(clientId, clientSecret, appName);

// Start authorization (Android requires currentActivity)
#if UNITY_ANDROID
EmotivUnityItf.Instance.Start(currentActivity);
#else
EmotivUnityItf.Instance.Start();
#endif

// After authorization
EmotivUnityItf.Instance.QueryHeadsets();
EmotivUnityItf.Instance.CreateSessionWithHeadset(headsetId);
```

---

For more details, see the comments in `EmotivUnityItf.cs` or contact Emotiv for support.

## Release Notes
See [Documentation/ReleaseNotes.md](Documentation/ReleaseNotes.md).

## License
This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
