# <a id="release-notes"></a>Release Notes

## Version 3.7 (November 2023)
### Added
- Support new headset refreshing flow where App need to to call ScanHeadsets() to start headset scanning.

## Version 2.7 2(10 July 2021)
### Added
- Support injectMarker and updateMarker to EEG data stream.

## Version 2.7 0(17 Apr 2021)

### Added
- Support data parsing for new channel BatteryPercent of "dev" stream which is new from Cortex version 2.7.0.

### Fixed
- Fixed issue parsing "Markers" channels from eeg data stream. Actually, we exclude "Markers" data from data buffer
- Fixed issue sometime can not add new method \_methodForRequestId map at CortexClient.cs

## Version 2.4 (12 May 2020)
For the moment the following features are supported:
- Subscribe to all data streams: EEG, Motion, Device information, Band power, detections, etc.
- Create a record and stop a record
- Create, load and unload profiles
- Perform Mental Commands and Facial Expression training

