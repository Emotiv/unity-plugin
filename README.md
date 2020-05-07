# Emotiv Unity Plugin

There are some pieces of codes as plugin for Unity applications work with Emotiv CortexService.

## Prerequisites

To work with Emotiv CortexService you must [download and install](https://www.emotiv.com/developer/) the Cortex service. Please note that currently, the Cortex service is only available for Windows and macOS.

You must install Unity. You can get it for free at [www.unity3d.com](https://unity3d.com/get-unity/download).

## Installing

You can download or add the plugin as submodule of your project.
For example:
```
git submodule add https://github.com/Emotiv/unity-plugin.git /Assets/Plugins/Emotiv-Unity-Plugin
```
Please reference to [unity example](https://github.com/Emotiv/cortex-v2-example) 

## Code structure
There are some main classes:

**DataStreamManager**: Reponsible for managing data streams subscribing from connecting to Emotiv CortexService to data subscribing data.

**RecordManager**: Reponsible for managing and handling records and markers.

**BCITraining**: Reponsible for brain–computer interface (BCI) training included mental command and facial expression.

**CortexClient**: Create a websocket client, build request message to work with Emotiv CortexService.

## How to use
Please follow the below steps:
1. Setup App configuration: clientId, clientSecret for indentifying Application and other informations such as appName, appVersion to make temp folder.
2. Start authorizing procedure: The plugin will start connecting Emotiv Cortex then authorize to get token for working with Cortex. After successful authorizing, The Plugin will find headsets.
3. Start Data Stream: The plugin will create and activate a session with given headset and start subscribe given data streams.
4. You can subscribe or unsubscribe more data streams and do other tasks such as record and training.

```
// setup App configuration
DataStreamManager.Instance.SetAppConfig("clientId_Of_App", "clientId_Of_App", "1.0.0", "UnityApp");
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

More detail please reference to [unity example](https://github.com/Emotiv/cortex-v2-example)

## Release Notes
Currently, The Plugin is supported on the following features:
 - Subscribe all data streams: EEG, Motion, Performance metric, Device information, Band power,...   
 - Start and Stop a record but only api not examples.
 - Create, load, unload profiles and do training but only api not examples
 
Upcoming feature support will be added :
 - Error handling
 - Update record, delete record
 - Inject marker
 - Support more for trainings.

More See [here](https://github.com/Emotiv/unity-plugin/blob/master/LICENSE)


## Authors

* **TungNguyen**

See also the list of [contributors](https://github.com/Emotiv/unity-plugin/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details


