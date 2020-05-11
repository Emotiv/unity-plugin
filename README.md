# Emotiv Unity Plugin

There are some pieces of codes as plugin for Unity applications work with Emotiv Cortex Service (shortly called Cortex).

## Prerequisites

Firstly, you must [download and install](https://www.emotiv.com/developer/) the Cortex service. Please note that currently, the Cortex service is only available for Windows and macOS.

Secondly, You must install Unity. You can get it for free at [www.unity3d.com](https://unity3d.com/get-unity/download).

## Installing

You can download or add the plugin as submodule of your project.
For example:
```
git submodule add https://github.com/Emotiv/unity-plugin.git /Assets/Plugins/Emotiv-Unity-Plugin
```
Please refer to [unity example](https://github.com/Emotiv/cortex-v2-example/tree/master/unity) 

## Code structure
The code structure of Emotiv Unity Plugin as below image:

<p align="center">
  <img width="460" height="300" src="Documentation/Images/CodeStructure.png">
</p>

There are 3 classes as interface role. Your application will call public methods of the interfaces to work with Emotiv. The CortexClient will build request message and communicate with Cortex. The others are helper classes. 

**DataStreamManager**: Reponsible for managing data streams subscribing from connecting to Cortex to data subscribing.

**RecordManager**: Reponsible for managing and handling records and markers.

**BCITraining**: Reponsible for brain–computer interface (BCI) training included mental command and facial expression.

**Authorizer**: Reponsible for authorizing process and manage cortex token.

**SessionHandler**: Reponsible for handling sessions and records.

**HeadsetFinder**: Reponsible for finding headsets.

**DataStreamProcess**: Process data stream subscribing.

**Logger**: Log handler will print log to file with date time prefix format. Currently, The log files are only created at standalone app mode. 
They are located at "%LocalAppData%/UnityApp/logs/" on Windows and "~/Library/Application Support/UnityApp/logs" on MacOS as default. You also can configure the log directory at Config.cs.

**CortexClient**: Create a websocket client, build request message to work with Cortex.

In addition, After subscribing eeg, motion, dev, pm data successfully, the plugin will create corresponding data buffers to keep data return from Cortex. You can change windows size and step size for reading data from the buffers.

## How to use
Please follow the below steps:
1. Setup App configuration: clientId, clientSecret for indentifying Application and other informations such as appName, appVersion to make temp folder.
2. Start authorizing procedure: Start connecting Emotiv Cortex then authorize to get token for working with Cortex. After authorizing successfully, the Plugin will find headsets automatically.
3. Start Data Stream: Create and activate a session with given headset and start subscribe given data streams.
4. You can subscribe or unsubscribe more data streams and do other tasks such as record and training.

```
// setup App configuration
DataStreamManager.Instance.SetAppConfig("clientId_Of_App", "clientSecret_Of_App", "1.0.0", "UnityApp");
// start connect and authorize
DataStreamManager.Instance.StartAuthorize();

// ... 
// authorize successfully then find headsets

// creating session and subscribe data
List<string> dataStreamList = new List<string>(){DataStreamName.DevInfos};
DataStreamManager.Instance.StartDataStream(dataStreamList, "headsetId);

// You also can suscribe more data
DataStreamManager.Instance.SubscribeMoreData(new List<string>(){DataStreamName.EEG, DataStreamName.Motion});

// Or unsubscribe data
DataStreamManager.Instance.UnSubscribeData(new List<string>(){DataStreamName.EEG, DataStreamName.Motion});

// Or start a record or training data
RecordManager.Instance.StartRecord("record title", "record description")

```

More detail please refer to [unity example](https://github.com/Emotiv/cortex-v2-example/tree/master/unity) 

## Release Notes

See <a href="Documentation/ReleaseNotes.md">here</a>.

## Authors

* **TungNguyen**

See also the list of [contributors](https://github.com/Emotiv/unity-plugin/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details


