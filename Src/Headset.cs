using Newtonsoft.Json.Linq;
using System.Collections;
using System;
using UnityEngine;

namespace EmotivUnityPlugin
{
    public class Headset
    {
        private string         _headsetID;
        private string         _status;
        private string         _serialId;
        private string         _firmwareVersion;
        private string         _dongleSerial;
        private ArrayList      _sensors;
        private ArrayList      _motionSensors;
        private JObject        _settings;
        private ConnectionType _connectedBy;
        private HeadsetTypes   _headsetType;
        private string         _mode;

        // Contructor
        public Headset()
        {
        }
        public Headset (JObject jHeadset)
        {
            HeadsetID = (string)jHeadset["id"];

            if (HeadsetID.Contains(HeadsetNames.epoc_plus))
            {
                HeadsetType = HeadsetTypes.HEADSET_TYPE_EPOC_PLUS;
            }
            else if (HeadsetID.Contains(HeadsetNames.epoc_flex))
            {
                HeadsetType = HeadsetTypes.HEADSET_TYPE_EPOC_FLEX;
            }
            else if (HeadsetID.Contains(HeadsetNames.epoc_x))
            {
                HeadsetType = HeadsetTypes.HEADSET_TYPE_EPOC_X;
            }
            else if (HeadsetID.Contains(HeadsetNames.insight2))
            {
                HeadsetType = HeadsetTypes.HEADSET_TYPE_INSIGHT2;
            }
            else if (HeadsetID.Contains(HeadsetNames.insight))
            {
                HeadsetType = HeadsetTypes.HEADSET_TYPE_INSIGHT;
            }
            else if (HeadsetID.Contains(HeadsetNames.mn8))
            {
                HeadsetType = HeadsetTypes.HEADSET_TYPE_MN8;
            }
            else if (HeadsetID.Contains(HeadsetNames.xtrode))
            {
                HeadsetType = HeadsetTypes.HEADSET_TYPE_XTRODE;
            }
            else if (HeadsetID.Contains(HeadsetNames.epoc))
            {
                HeadsetType = HeadsetTypes.HEADSET_TYPE_EPOC_STD;
            }
            else if (HeadsetID.Contains(HeadsetNames.flex2))
            {
                HeadsetType = HeadsetTypes.HEADSET_TYPE_FLEX2;
            }

            Status = (string)jHeadset["status"];
            FirmwareVersion = (string)jHeadset["firmware"];
            DongleSerial = (string)jHeadset["dongle"];
            Sensors = new ArrayList();
            
            foreach (JToken sensor in (JArray)jHeadset["sensors"])
            {
                Sensors.Add(sensor.ToString());
            }
            MotionSensors = new ArrayList();
            foreach (JToken sensor in (JArray)jHeadset["motionSensors"])
            {
                MotionSensors.Add(sensor.ToString());
            }
            Mode = (string)jHeadset["mode"];
            string cnnBy = (string)jHeadset["connectedBy"];
            if (cnnBy == "dongle") {
                HeadsetConnection = ConnectionType.CONN_TYPE_DONGLE;
            }
            else if (cnnBy == "bluetooth") {
                HeadsetConnection = ConnectionType.CONN_TYPE_BTLE;
            }
            else if (cnnBy == "extender") {
                HeadsetConnection = ConnectionType.CONN_TYPE_EXTENDER;
            }
            else if (cnnBy == "usb cable") {
                HeadsetConnection = ConnectionType.CONN_TYPE_USB_CABLE;
            }
            else {
                HeadsetConnection = ConnectionType.CONN_TYPE_UNKNOWN;
            }
            Settings = (JObject)jHeadset["settings"];
        }

        // Properties
        public string HeadsetID
        {
            get {
                return _headsetID;
            }

            set {
                _headsetID = value;
            }
        }

        public HeadsetTypes HeadsetType
        {
            get {
                return _headsetType;
            }

            set {
                _headsetType = value;
            }
        }

        public string Status
        {
            get {
                return _status;
            }

            set {
                _status = value;
            }
        }

        public string SerialId
        {
            get {
                return _serialId;
            }

            set {
                _serialId = value;
            }
        }

        public string FirmwareVersion
        {
            get {
                return _firmwareVersion;
            }

            set {
                _firmwareVersion = value;
            }
        }

        public string DongleSerial
        {
            get {
                return _dongleSerial;
            }

            set {
                _dongleSerial = value;
            }
        }

        public ArrayList Sensors
        {
            get {
                return _sensors;
            }

            set {
                _sensors = value;
            }
        }

        public ArrayList MotionSensors
        {
            get {
                return _motionSensors;
            }

            set {
                _motionSensors = value;
            }
        }

        public JObject Settings
        {
            get {
                return _settings;
            }

            set {
                _settings = value;
            }
        }

        public ConnectionType HeadsetConnection
        {
            get {
                return _connectedBy;
            }

            set {
                _connectedBy = value;
            }
        }

        public string Mode
        {
            get {
                return _mode;
            }

            set {
                _mode = value;
            }
        }
    }
}
