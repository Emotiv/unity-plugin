# Emotiv Unity Plugin

This plugin enables Unity applications to work with the Emotiv Cortex Service (Cortex) for EEG headset integration, data streaming, training, and recording.

---

## Prerequisites

1. [Download and install](https://www.emotiv.com/developer/) the EMOTIV Launcher with Cortex service (Windows/macOS).
2. Install Unity from [unity3d.com](https://unity3d.com/get-unity/download).

---

## Setup

Clone the repo manually or add as a submodule:
```sh
git submodule add https://github.com/Emotiv/unity-plugin.git /Assets/Plugins/Emotiv-Unity-Plugin
```
See the [Unity example](https://github.com/Emotiv/cortex-v2-example/tree/master/unity) for reference.

---

## Platform Support

The Emotiv Unity Plugin supports:
- **Cortex Service** (Windows/macOS)
- **Embedded Cortex** (internal use) on **Windows**, **iOS**, and **Android**

---

## Usage Guide

### Unified Interface

**You only need to use [`EmotivUnityItf`](./Src/EmotivUnityItf.cs)** for all Cortex operations.  
You do **not** need to interact directly with `DataStreamManager`, `BCITraining`, or `RecordManager` as previous version.

All public methods and properties are documented in [`EmotivUnityItf.cs`](./Src/EmotivUnityItf.cs).

---

### Typical Workflow

**1.Initialize the Plugin**

   Call `Init()` with your app credentials and options:
   ```csharp
   EmotivUnityPlugin.EmotivUnityItf.Instance.Init(
       clientId: "YOUR_CLIENT_ID",
       clientSecret: "YOUR_CLIENT_SECRET",
       appName: "YOUR_APP_NAME"
       allowSaveLogToFile: true,
       isDataBufferUsing: true // or false, see Data Streaming section below
       // ...other optional parameters
       // ...other optional parameters
   );
   ```

**2.Start Connection and Authorization**

   Call `Start()` to connect to Cortex and authorize:
   ```csharp
   EmotivUnityPlugin.EmotivUnityItf.Instance.Start();
   ```

**3.Query for Available Headsets**

   Call `QueryHeadsets()` to get the list of detected headsets:
   ```csharp
   EmotivUnityPlugin.EmotivUnityItf.Instance.QueryHeadsets();
   ```

**4.Create a Session with a Headset**

   Once you have at least one headset, call `CreateSessionWithHeadset()`:
   ```csharp
   EmotivUnityPlugin.EmotivUnityItf.Instance.CreateSessionWithHeadset(headsetId);
   ```
   - `headsetId` is a string like `"EPOCX-12345"`.
   - If you pass an empty string, the first headset in the list will be used.

**5.Subscribe to Data, Training, and Recording**

   After the session is created, you can:
   - **Subscribe to data streams:**  
     `SubscribeData(listOfStreams)`
   - **Start/stop recording:**  
     `StartRecord(title)`, `StopRecord()`
   - **Inject markers:**  
     `InjectMarker(label, value)`
   - **Manage profiles:**  
     `LoadProfile(profileName)`, `SaveProfile()`, `UnLoadProfile()`
   - **Start/stop training:**  
	 `StartMCTraining()`, `AcceptMCTraining()`, etc.

   See [`EmotivUnityItf.cs`](./Src/EmotivUnityItf.cs) for all available methods and detailed documentation.

---

### Data Streaming Modes

When calling `Init()`, you can set the `isDataBufferUsing` parameter:

- **`isDataBufferUsing = false`:**
  Subscribed data (EEG, motion, etc.) will be saved to the internal `_messageLog` and can be accessed via the `MessageLog` property.  
  This is suitable for simple applications or demos.

- **`isDataBufferUsing = true` (default):**
  Data is not automatically buffered.  
  You must manually retrieve data using functions such as:
  - `GetNumberEEGSamples()`
  - `GetEEGData(Channel_t chan)`
  - `GetNumberMotionSamples()`
  - `GetMotionData(Channel_t chan)`
  
  **Usage Example:**
  ```csharp
  int n = emotiv.GetNumberEEGSamples();
  if (n > 0)
  {
      foreach (var chan in emotiv.GetEEGChannels())
      {
          double[] data = emotiv.GetEEGData(chan);
          // process data
      }
  }
  ```
  Always check that the number of samples is greater than 0 before retrieving data for each channel.

---


### Example

```csharp
var emotiv = EmotivUnityPlugin.EmotivUnityItf.Instance;
emotiv.Init("clientId", "clientSecret", "appName");
emotiv.Start();
// ... should wait for authorize done...
emotiv.QueryHeadsets();
// ...wait for headset list...
emotiv.CreateSessionWithHeadset("HEADSET_ID");
// Now you can subscribe to data, train, or record
```

---

## API Reference

- All public methods and properties are documented in [`EmotivUnityItf.cs`](./Src/EmotivUnityItf.cs).
- Please refer to the source file for up-to-date descriptions and parameter details.

---

## Additional Resources

- [Unity Example Project](https://github.com/Emotiv/cortex-v2-example/tree/master/unity)
- [Cortex API Documentation](https://emotiv.gitbook.io/cortex-api/)

---

## Release Notes

See [Documentation/ReleaseNotes.md](Documentation/ReleaseNotes.md).

---

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
