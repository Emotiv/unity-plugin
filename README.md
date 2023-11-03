# Emotiv Unity Plugin

Here is the plugin for Unity application to work with Emotiv Cortex Service (aka Cortex).

## Prerequisites

1. [Download and install](https://www.emotiv.com/developer/) the EMOTIV Launcher with Cortex service, which is currently available for Windows and macOS.

2. Install Unity. You can get it for free at [unity3d.com](https://unity3d.com/get-unity/download).

## Setting up

You can clone the repo manually or add the plugin as a submodule of your project.
For example:
```
git submodule add https://github.com/Emotiv/unity-plugin.git /Assets/Plugins/Emotiv-Unity-Plugin
```
Please refer to the [Unity example](https://github.com/Emotiv/cortex-v2-example/tree/master/unity).

## How to use
In the previous version, there were 3 main classes DataStreamManager.cs will handle connect headset and subscribe data, the RecordManager.cs will handle record and marker, and BCITraining.cs will handle training. But now, you only need to care about the EmotivUnityItf.cs. It will help to handle all.  

1. Firstly, you need to initialize via Init(). You need to set clientId, clientSecret of your application and set isDataBufferUsing = false if you don't want to save subscribed data to DataBuffer before obtaining.
2. Then call Start() to start connecting to Cortex and authorize the application.
3. From Emotiv Cortex 3.7, you need to call ScanHeadsets() at DataStreamManager.cs to start headset scanning. Otherwise your headsets might not appeared in the headset list return from queryHeadsets(). If IsHeadsetScanning = false, you need re-call the ScanHeadsets() if want to re-scan headsets again.
4. Connect to your headset in the headset list via CreateSessionWithHeadset(string headsetId) method. It will connect, then create a working session with the headset. The headsetId has a format such as EPOCX-12345. If you set an empty string for the headsetId, unity-plugin will use the first headset in the headset list.
5. After a session is activated successfully, You can create a record, subscribe data or load a profile for training.  
	- **Start and Stop Record**: create a record via StartRecord(). The record title is a required parameter. You can stop the record via StopRecord().
	- **Inject Marker**: You can inject an instance marker into the record via InjectMarker(). The markerValue and markerLabel are required parameters. You also can update the current instance marker via UpdateMarker() to make the marker to interval marker.  
	- **Subscribe and UnSubscribe Data**: You can subscribe to one or more data streams via SubscribeData(list_data_streams). Currently, the unity-plugin support EEG(eeg), Motion (mot),Device Information (dev), Facial Expression(fac), Mental Command (com), Performance metrics(met), System Event (sys), Band power (pow) data streams. There are 2 choices for output data retrieving:
		- If use DataBuffer by set isDataBufferUsing = true, The received data of EEG, Motion, Device Information, Band Power will be saved to the data buffer when received from Cortex. You need to use GetEEGData(), GetNumberEEGSamples()  .etc.. to get data.
		- If don't use DataBuffer by set isDataBufferUsing = false, The receive data will be handled at OnEEGDataReceived(), OnMotionDataReceived() .etc..
		- The subscribed data of Facial Expression, Mental Command, and System Event will be handled at OnFacialExpReceived(), OnMentalCommandReceived(), OnSysEventsReceived() both cases Data Buffer using or not.  
	- **Setup profile**: There are some functions as below:
		- The  LoadProfile(string profileName) will help to load a profile if it exists or create and load a profile if it does not exist.  
		- The SaveProfile() will help to save training data to the profile. You should call the function each training.
		- The UnLoadProfile() will help unload profile. You should call the function before closing the project to release the loaded profile for the next use.
	- **Training**: After loading a profile successfully. You can do training via StartMCTraining(), AcceptMCTraining() .etc... You might need to subscribe "sys" first before training to see the training event.


For more details please refer to [Unity example](https://github.com/Emotiv/cortex-v2-example/tree/master/unity) 
and [Cortex-API](https://emotiv.gitbook.io/cortex-api/)
## Release Notes

See <a href="Documentation/ReleaseNotes.md">here</a>.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details


