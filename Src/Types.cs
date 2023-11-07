using System;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json.Linq;
using System.Runtime.Serialization;

namespace EmotivUnityPlugin
{
    public enum ContactQualityValue {
        NO_SIGNAL = 0,
        VERY_BAD,
        POOR,
        FAIR,
        GOOD
    };

    public enum BatteryLevel {
        LEVEL_INIT = -1, // while battery is not updated yet. or in some cases, has error from firmware
        LEVEL_0   = 0,
        LEVEL_1   = 1,
        LEVEL_2   = 2,
        LEVEL_3   = 3,
        LEVEL_4   = 4,
    };

    public enum Channel_t : int
    {
        // flex sensors with id follow  raw data id
        CHAN_FLEX_LA, CHAN_FLEX_LB, CHAN_FLEX_LC, CHAN_FLEX_LD,
        CHAN_FLEX_LE, CHAN_FLEX_LF, CHAN_FLEX_LG, CHAN_FLEX_LH, CHAN_FLEX_LJ, CHAN_FLEX_LK,
        CHAN_FLEX_LL, CHAN_FLEX_LM, CHAN_FLEX_LN, CHAN_FLEX_LO, CHAN_FLEX_LP, CHAN_FLEX_LQ,
        CHAN_FLEX_RA, CHAN_FLEX_RB, CHAN_FLEX_RC, CHAN_FLEX_RD, CHAN_FLEX_RE, CHAN_FLEX_RF,
        CHAN_FLEX_RG, CHAN_FLEX_RH, CHAN_FLEX_RJ, CHAN_FLEX_RK, CHAN_FLEX_RL, CHAN_FLEX_RM,
        CHAN_FLEX_RN, CHAN_FLEX_RO, CHAN_FLEX_RP, CHAN_FLEX_RQ,

        // non-flexData channels
        CHAN_FLEX_CMS, CHAN_FLEX_DRL,

        // EEG channels
        CHAN_Cz,  CHAN_FCz, CHAN_Fz,  CHAN_Afz, CHAN_Fpz, CHAN_Fp1, CHAN_AF3, CHAN_AF7, CHAN_F9,   CHAN_F7,  CHAN_F5,   CHAN_F3,
        CHAN_F1,  CHAN_FC1, CHAN_C1,  CHAN_C3,  CHAN_FC3, CHAN_FC5, CHAN_FT7, CHAN_FT9, CHAN_T7,   CHAN_C5,  CHAN_TP9,  CHAN_TP7,
        CHAN_CP5, CHAN_CP3, CHAN_CP1, CHAN_P1,  CHAN_P3,  CHAN_P5,  CHAN_P7,  CHAN_P9,  CHAN_PO9,  CHAN_PO7, CHAN_PO3,  CHAN_O1,
        CHAN_O9,  CHAN_CPz, CHAN_Pz,  CHAN_POz, CHAN_Oz,  CHAN_Iz,  CHAN_O10, CHAN_O2,  CHAN_PO4,  CHAN_PO8, CHAN_PO10, CHAN_P10,
        CHAN_P8,  CHAN_P6,  CHAN_P4,  CHAN_P2,  CHAN_CP2, CHAN_CP4, CHAN_CP6, CHAN_TP8, CHAN_TP10, CHAN_C6,  CHAN_T8,   CHAN_FT10,
        CHAN_FT8, CHAN_FC6, CHAN_FC4, CHAN_C4,  CHAN_C2,  CHAN_FC2, CHAN_F2,  CHAN_F4,  CHAN_F6,   CHAN_F8,  CHAN_F10,  CHAN_AF8,
        CHAN_AF4, CHAN_Fp2, CHAN_CTM1, CHAN_CTM2,

        CHAN_COUNTER,       CHAN_INTERPOLATED, CHAN_RAW_CQ,      CHAN_CQ_OVERALL ,  CHAN_MARKER, CHAN_MARKER_HARDWARE,
        CHAN_TIME_SYSTEM,   CHAN_MARKER_RANGE, CHAN_MARKER_TYPE, CHAN_MARKER_TITLE, CHAN_RESERVED,
        CHAN_FLEX_HIGH_BIT, CHAN_BATTERY, CHAN_SIGNAL_STRENGTH, CHAN_BATTERY_PERCENT, CHAN_UNKNOWN,

        // Motion channels
        CHAN_COUNTER_MEMS, CHAN_INTERPOLATED_MEMS,
        CHAN_Q0,    CHAN_Q1,    CHAN_Q2,    CHAN_Q3,
        CHAN_GYROX, CHAN_GYROY, CHAN_GYROZ,
        CHAN_ACCX,  CHAN_ACCY,  CHAN_ACCZ,
        CHAN_MAGX,  CHAN_MAGY,  CHAN_MAGZ
    }

    public enum BandPowerType
    {
        Thetal = 0,
        Alpha,
        BetalL,
        BetalH,
        Gamma
    };

    public static class ChannelStringList
    {
        public static Dictionary<Channel_t, string> list = new Dictionary<Channel_t, string>() {
            {Channel_t.CHAN_FLEX_CMS, "CMS"}, {Channel_t.CHAN_FLEX_DRL, "DRL"}, {Channel_t.CHAN_FLEX_LA, "LA"}, {Channel_t.CHAN_FLEX_LB, "LB"},
            {Channel_t.CHAN_FLEX_LC, "LC"},   {Channel_t.CHAN_FLEX_LD, "LD"},   {Channel_t.CHAN_FLEX_LE, "LE"}, {Channel_t.CHAN_FLEX_LF, "LF"},
            {Channel_t.CHAN_FLEX_LG, "LG"},   {Channel_t.CHAN_FLEX_LH, "LH"},   {Channel_t.CHAN_FLEX_LJ, "LJ"}, {Channel_t.CHAN_FLEX_LK, "LK"},
            {Channel_t.CHAN_FLEX_LL, "LL"},   {Channel_t.CHAN_FLEX_LM, "LM"},   {Channel_t.CHAN_FLEX_LN, "LN"}, {Channel_t.CHAN_FLEX_LO, "LO"},
            {Channel_t.CHAN_FLEX_LP, "LP"},   {Channel_t.CHAN_FLEX_LQ, "LQ"},   {Channel_t.CHAN_FLEX_RA, "RA"}, {Channel_t.CHAN_FLEX_RB, "RB"},
            {Channel_t.CHAN_FLEX_RC, "RC"},   {Channel_t.CHAN_FLEX_RD, "RD"},   {Channel_t.CHAN_FLEX_RE, "RE"}, {Channel_t.CHAN_FLEX_RF, "RF"},
            {Channel_t.CHAN_FLEX_RG, "RG"},   {Channel_t.CHAN_FLEX_RH, "RH"},   {Channel_t.CHAN_FLEX_RJ, "RJ"}, {Channel_t.CHAN_FLEX_RK, "RK"},
            {Channel_t.CHAN_FLEX_RL, "RL"},   {Channel_t.CHAN_FLEX_RM, "RM"},   {Channel_t.CHAN_FLEX_RN, "RN"}, {Channel_t.CHAN_FLEX_RO, "RO"},
            {Channel_t.CHAN_FLEX_RP, "RP"},   {Channel_t.CHAN_FLEX_RQ, "RQ"},

            {Channel_t.CHAN_Cz, "Cz"},     {Channel_t.CHAN_FCz, "FCz"}, {Channel_t.CHAN_Fz, "Fz"},     {Channel_t.CHAN_Afz, "Afz"},
            {Channel_t.CHAN_Fpz, "Fpz"},   {Channel_t.CHAN_Fp1, "Fp1"}, {Channel_t.CHAN_AF3, "AF3"},   {Channel_t.CHAN_AF7, "AF7"},
            {Channel_t.CHAN_F9, "F9"},     {Channel_t.CHAN_F7, "F7"},   {Channel_t.CHAN_F5, "F5"},     {Channel_t.CHAN_F3, "F3"},
            {Channel_t.CHAN_F1, "F1"},     {Channel_t.CHAN_FC1, "FC1"}, {Channel_t.CHAN_C1, "C1"},     {Channel_t.CHAN_C3, "C3"},
            {Channel_t.CHAN_FC3, "FC3"},   {Channel_t.CHAN_FC5, "FC5"}, {Channel_t.CHAN_FT7, "FT7"},   {Channel_t.CHAN_FT9, "FT9"},
            {Channel_t.CHAN_T7, "T7"},     {Channel_t.CHAN_C5, "C5"},   {Channel_t.CHAN_TP9, "TP9"},   {Channel_t.CHAN_TP7, "TP7"},
            {Channel_t.CHAN_CP5, "CP5"},   {Channel_t.CHAN_CP3, "CP3"}, {Channel_t.CHAN_CP1, "CP1"},   {Channel_t.CHAN_P1, "P1"},
            {Channel_t.CHAN_P3, "P3"},     {Channel_t.CHAN_P5, "P5"},   {Channel_t.CHAN_P7, "P7"},     {Channel_t.CHAN_P9, "P9"},
            {Channel_t.CHAN_PO9, "PO9"},   {Channel_t.CHAN_PO7, "PO7"}, {Channel_t.CHAN_PO3, "PO3"},   {Channel_t.CHAN_O1, "O1"},
            {Channel_t.CHAN_O9, "O9"},     {Channel_t.CHAN_CPz, "CPz"}, {Channel_t.CHAN_Pz, "Pz"},     {Channel_t.CHAN_POz, "POz"},
            {Channel_t.CHAN_Oz, "Oz"},     {Channel_t.CHAN_Iz, "Iz"},   {Channel_t.CHAN_O10, "O10"},   {Channel_t.CHAN_O2, "O2"},
            {Channel_t.CHAN_PO4, "PO4"},   {Channel_t.CHAN_PO8, "PO8"}, {Channel_t.CHAN_PO10, "PO10"}, {Channel_t.CHAN_P10, "P10"},
            {Channel_t.CHAN_P8, "P8"},     {Channel_t.CHAN_P6, "P6"},   {Channel_t.CHAN_P4, "P4"},     {Channel_t.CHAN_P2, "P2"},
            {Channel_t.CHAN_CP2, "CP2"},   {Channel_t.CHAN_CP4, "CP4"}, {Channel_t.CHAN_CP6, "CP6"},   {Channel_t.CHAN_TP8, "TP8"},
            {Channel_t.CHAN_TP10, "TP10"}, {Channel_t.CHAN_C6, "C6"},   {Channel_t.CHAN_T8, "T8"},     {Channel_t.CHAN_FT10, "FT10"},
            {Channel_t.CHAN_FT8, "FT8"},   {Channel_t.CHAN_FC6, "FC6"}, {Channel_t.CHAN_FC4, "FC4"},   {Channel_t.CHAN_C4, "C4"},
            {Channel_t.CHAN_C2, "C2"},     {Channel_t.CHAN_FC2, "FC2"}, {Channel_t.CHAN_F2, "F2"},     {Channel_t.CHAN_F4, "F4"},
            {Channel_t.CHAN_F6, "F6"},     {Channel_t.CHAN_F8, "F8"},   {Channel_t.CHAN_F10, "F10"},   {Channel_t.CHAN_AF8, "AF8"},
            {Channel_t.CHAN_AF4, "AF4"},   {Channel_t.CHAN_Fp2, "Fp2"}, {Channel_t.CHAN_CTM1, "CTM1"}, {Channel_t.CHAN_CTM2, "CTM2"},
            {Channel_t.CHAN_RAW_CQ, "RAW_CQ"}, {Channel_t.CHAN_CQ_OVERALL, "OVERALL"}, {Channel_t.CHAN_FLEX_HIGH_BIT, "HighBitFlex"},

            {Channel_t.CHAN_TIME_SYSTEM, "TIMESTAMP"},
            {Channel_t.CHAN_COUNTER, "COUNTER"},
            {Channel_t.CHAN_COUNTER_MEMS, "COUNTER_MEMS"},
            {Channel_t.CHAN_INTERPOLATED, "INTERPOLATED"},
            {Channel_t.CHAN_INTERPOLATED_MEMS, "INTERPOLATED_MEMS"},
            {Channel_t.CHAN_Q0, "Q0"},       {Channel_t.CHAN_Q1, "Q1"},       {Channel_t.CHAN_Q2, "Q2"},      {Channel_t.CHAN_Q3, "Q3"},
            {Channel_t.CHAN_GYROX, "GYROX"}, {Channel_t.CHAN_GYROY, "GYROY"}, {Channel_t.CHAN_GYROZ, "GYROZ"},
            {Channel_t.CHAN_ACCX, "ACCX"},   {Channel_t.CHAN_ACCY, "ACCY"},   {Channel_t.CHAN_ACCZ, "ACCZ"},
            {Channel_t.CHAN_MAGX, "MAGX"},   {Channel_t.CHAN_MAGY, "MAGY"},   {Channel_t.CHAN_MAGZ, "MAGZ"},

            {Channel_t.CHAN_BATTERY, "BATTERY"},
            {Channel_t.CHAN_RESERVED, "RESERVED"},
            {Channel_t.CHAN_MARKER, "MARKER"},
            {Channel_t.CHAN_MARKER_HARDWARE, "MARKER_HARDWARE"},
            {Channel_t.CHAN_MARKER_RANGE, "MarkerValueInt"},
            {Channel_t.CHAN_MARKER_TYPE, "MarkerType"},
            {Channel_t.CHAN_MARKER_TITLE, "MarkerIndex"}
        };

        public static Channel_t StringToChannel(string chanStr) {
            foreach (KeyValuePair<Channel_t, string> kvp in list){
                if (kvp.Value == chanStr){
                    return kvp.Key;
                }
            }
            return Channel_t.CHAN_UNKNOWN;
        }
        public static string ChannelToString(Channel_t chan){
            foreach (KeyValuePair<Channel_t, string> kvp in list){
                if (kvp.Key == chan){
                    return kvp.Value;
                }
            }
            return "CHAN_UNKNOWN";
        }
    }

    public enum ConnectionType {
        CONN_TYPE_DONGLE,
        CONN_TYPE_USB_CABLE,
        CONN_TYPE_USB_CHARGING,
        CONN_TYPE_EXTENDER, // Extender pass throught mode
        CONN_TYPE_BTLE,
        CONN_TYPE_FILE_EDF,
        CONN_TYPE_FILE_CSV,
        CONN_TYPE_FILE_EED,
        CONN_TYPE_WEBSOCKET,
        CONN_TYPE_UNKNOWN = -1
    };

    public enum HeadsetTypes {
        HEADSET_TYPE_UNKNOWN,
        HEADSET_TYPE_INSIGHT,
        HEADSET_TYPE_INSIGHT2,
        HEADSET_TYPE_EPOC_STD,
        HEADSET_TYPE_EPOC_PLUS,
        HEADSET_TYPE_EPOC_FLEX,
        HEADSET_TYPE_EPOC_X,
        HEADSET_TYPE_MN8,
        HEADSET_TYPE_XTRODE,
        HEADSET_TYPE_FLEX2
    };

    // From old code. keep it here for now. will use Channel_t soon
    // This also related to BrainVisualControl.cs line 113
    // This define used on almost of process in this app. not easy to change
    public enum Channels
    {
        AF3=0, F7, F3, FC5, T7, P7, O1, O2, P8, T8, FC6, F4, F8, AF4,
        CMS, DRL
    };

    public enum Channels_Epoc_EEG
    {
        TIME_STAMP, COUNTER, INTERPOLATED, AF3, F7, F3, FC5, T7, P7, O1,
        O2, P8, T8, FC6, F4, F8, AF4, RAW_CQ, CHAN_MARKER_HARDWARE, MARKER_SOFTWARE
    };

    public enum Channels_Insight_EEG
    {
        TIME_STAMP, COUNTER, INTERPOLATED, AF3, T7, Pz, T8, AF4, RAW_CQ, CHAN_MARKER_HARDWARE, MARKER_SOFTWARE
    };
    
    public enum Channels_Motion_v1
    {
        TIME_STAMP, COUNTER, INTERPOLATED, 
        GYROX, GYROY, GYROZ,
        ACCX, ACCY, ACCZ, 
        MAGX, MAGY, MAGZ
    };

    public enum Channels_Motion_v2
    {
        Q0, Q1, Q2, Q3,
        ACCX, ACCY, ACCZ,
        MAGX, MAGY, MAGZ
    }

    // TODO: using headsetTypeToString
    public struct HeadsetNames
    {
        public static string epoc       = "EPOC";
        public static string epoc_plus  = "EPOCPLUS";
        public static string insight    = "INSIGHT";
        public static string insight2   = "INSIGHT2";
        public static string epoc_x     = "EPOCX";
        public static string mn8        = "MN8";
        public static string epoc_flex  = "EPOCFLEX";
        public static string xtrode  = "BGX";
        public static string flex2  = "FLEX2";
    }

    public struct HeadsetConnectionStatus
    {
        public static string DISCOVERED  = "discovered";
        public static string CONNECTING  = "connecting";
        public static string CONNECTED   = "connected";
    }

    public enum ConnectToCortexStates : int {
        Service_connecting, 
        EmotivApp_NotFound,
        // Connected, // after connected to Cortex, we go to Login
        Login_waiting,
        Login_notYet,
        Authorizing,
        Authorize_failed,
        Authorized, 
        LicenseExpried,
        License_HardLimited
    }

    public class License
    {
        public License(JToken licObj) {
            if (licObj["billingFrom"] != null) {
                string timeStr = licObj["billingFrom"].ToString();
                if (!string.IsNullOrEmpty(timeStr))
                    billingFrom = Utils.StringToIsoDateTime(timeStr);
            }
            if (licObj["billingTo"] != null) {
                string timeStr = licObj["billingTo"].ToString();
                if (!string.IsNullOrEmpty(timeStr))
                    billingFrom = Utils.StringToIsoDateTime(timeStr);
            }
            if (licObj["hardLimitTime"] != null) {
                string timeStr = licObj["hardLimitTime"].ToString();
                if (!string.IsNullOrEmpty(timeStr))
                    hardLimitTime = Utils.StringToIsoDateTime(timeStr);
            }
            if (licObj["softLimitTime"] != null) {
                string timeStr = licObj["softLimitTime"].ToString();
                if (!string.IsNullOrEmpty(timeStr))
                    softLimitTime = Utils.StringToIsoDateTime(timeStr);
            }
            if (licObj["validFrom"] != null) {
                string timeStr = licObj["validFrom"].ToString();
                if (!string.IsNullOrEmpty(timeStr))
                    validFrom = Utils.StringToIsoDateTime(timeStr);
            }
            if (licObj["validTo"] != null) {
                string timeStr = licObj["validTo"].ToString();
                if (!string.IsNullOrEmpty(timeStr))
                    validTo = Utils.StringToIsoDateTime(timeStr);
            }

            expired         = (bool)licObj["expired"];

            JArray appArr   = (JArray)licObj["applications"];
            foreach(var ele in appArr) {
                applications.Add(ele.ToString());
            }

            JArray licArr   = (JArray)licObj["scopes"];
            foreach(var ele in licArr) {
                scopes.Add(ele.ToString());
            }
            licenseId       = licObj["licenseId"].ToString();
            licenseName     = licObj["licenseName"].ToString();
            localQuota      = (int)licObj["localQuota"];
            seatCount       = (int)licObj["seatCount"];
            sessionCount    = (int)licObj["sessionCount"];
            totalDebit      = (int)licObj["totalDebit"];
        }
        public DateTime billingFrom;
        public DateTime billingTo;
        public DateTime hardLimitTime;
        public DateTime softLimitTime;
        public DateTime validFrom;
        public DateTime validTo;
        public bool expired = false;
        public List<string> applications = new List<string>();
        public string licenseId = "";
        public string licenseName = "";
        public int localQuota = 0;
        public List<string> scopes = new List<string>();
        public int seatCount = 0;
        public int sessionCount = 0;
        public int totalDebit = 0;
    }

    // contain data and time of data. For example, login time and user login or token and time for token
    [Serializable()]
    public class UserDataInfo : ISerializable
    {
        public UserDataInfo(double time = 0, string token = "", string emotivId = "") {
            LastLoginTime   = time;
            CortexToken     = token;
            EmotivId        = emotivId;
        }
        public double LastLoginTime { get; set; }
        public string CortexToken { get; set;}
        public string EmotivId { get; set;}

        public UserDataInfo(SerializationInfo info, StreamingContext ctxt) {
            LastLoginTime   = (double)info.GetValue("lastLoginTime", typeof(double));
            CortexToken     = (string)info.GetValue("cortexToken", typeof(string));
            EmotivId        = (string)info.GetValue("emotivId", typeof(string));
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("lastLoginTime", LastLoginTime);
            info.AddValue("cortexToken", CortexToken);
            info.AddValue("emotivId", EmotivId);
        }
    }

    public class Record
    {
        private string _uuid;
        private string _applicationId;
        private string _licenseId;
        private string _title;
        private string _description;
        private string _startDateTime;
        private string _endDateTime;
        private JArray _markers;
        private List<string> _tags;

        // Properties
        public string Uuid
        {
            get {
                return _uuid;
            }

            set {
                _uuid = value;
            }
        }

        public string ApplicationId
        {
            get {
                return _applicationId;
            }

            set {
                _applicationId = value;
            }
        }

        public string LicenseId
        {
            get {
                return _licenseId;
            }

            set {
                _licenseId = value;
            }
        }

        public string Title
        {
            get {
                return _title;
            }

            set {
                _title = value;
            }
        }

        public string Description
        {
            get {
                return _description;
            }

            set {
                _description = value;
            }
        }

        public string StartDateTime
        {
            get {
                return _startDateTime;
            }

            set {
                _startDateTime = value;
            }
        }

        public string EndDateTime
        {
            get {
                return _endDateTime;
            }

            set {
                _endDateTime = value;
            }
        }

        public JArray Markers
        {
            get {
                return _markers;
            }

            set {
                _markers = value;
            }
        }

        public List<string> Tags
        {
            get {
                return _tags;
            }

            set
            {
                _tags = value;
            }
        }
        //Constructor
        public Record()
        {
        }
        public Record(JObject obj)
        {
            _uuid          = (string)obj["uuid"];
            _licenseId     = (string)obj["licenseId"];
            _applicationId = (string)obj["applicationId"];
            _title         = (string)obj["title"];
            _description   = (string)obj["description"];
            _startDateTime = (string)obj["startDatetime"];
            _endDateTime   = (string)obj["endDatetime"];
            _markers       = (JArray)obj["markers"];
            _tags          = obj["tags"].ToObject<List<string>>();
        }
        public void PrintOut()
        {
            UnityEngine.Debug.Log("id: " + _uuid + ", title: " + _title + ", startDatetime: " + _startDateTime + ", endDatetime: " + _endDateTime);
        }
    }

    public enum SessionStatus
    {
        Opened = 0,
        Activated = 1,
        Closed = 2
    }

    // Event for subscribe  and unsubscribe
    public class MultipleResultEventArgs
    {
        public MultipleResultEventArgs(JArray successList, JArray failList)
        {
            SuccessList = successList;
            FailList = failList;
        }
        public JArray SuccessList { get; set; }
        public JArray FailList { get; set; }
    }

    // Event for createSession and updateSession
    public class SessionEventArgs
    {
        public SessionEventArgs(string sessionId, string status, string appId, string headsetId)
        {
            SessionId       = sessionId;
            ApplicationId   = appId;
            HeadsetId       = headsetId;
            if (status == "opened")
                Status = SessionStatus.Opened;
            else if (status == "activated")
                Status = SessionStatus.Activated;
            else
                Status = SessionStatus.Closed;
        }
        public string SessionId { get; set; }
        public SessionStatus Status { get; set; }
        public string ApplicationId { get; set; }
        public string HeadsetId { get; set; }
    }
    public class StreamDataEventArgs
    {
        public StreamDataEventArgs(string sid, ArrayList data, string streamName)
        {
            Sid  = sid;
            Data = data;
            StreamName = streamName;
        }
        public string Sid { get; private set; } // subscription id
        public ArrayList Data { get; private set; }
        public string StreamName { get; private set; }
    }
    public class ErrorMsgEventArgs
    {
        public ErrorMsgEventArgs(int code, string messageError, string methodName)
        {
            Code            = code;
            MessageError    = messageError;
            MethodName      = methodName;
        }
        public int Code { get; set; }
        public string MessageError { get; set; }
        public string MethodName { get; set; }
    }

    // event to inform about headset connect
    public class HeadsetConnectEventArgs
    {
        public HeadsetConnectEventArgs(bool isSuccess, string message, string headsetId)
        {
            IsSuccess   = isSuccess;
            Message     = message;
            HeadsetId   = headsetId;
        }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string HeadsetId { get; set; }
    }

    // metal command data object
    public class MentalCommandEventArgs
    {
        public MentalCommandEventArgs(double time, string act, double pow)
        {
            Time    = time;
            Act     = act;
            Pow     = pow;
        }
        public double Time { get; set; }
        public string Act { get; set; }
        public double Pow { get; set; }
    }
    // Facial expression data object
    public class FacEventArgs
    {
        public FacEventArgs(double time, string eyeAct, 
                                         string uAct, double uPow,
                                         string lAct, double lPow )
        {
            Time     = time;
            EyeAct   = eyeAct;
            UAct     = uAct;
            UPow     = uPow;
            LAct     = lAct;
            LPow     = lPow;
        }
        public double Time { get; set; }
        public string EyeAct { get; set; }  //  action of the eyes.
        public string UAct { get; set; }    // upper face action.
        public double UPow { get; set; }    // Power of the upper face action. Zero means "low power", 1 means "high power".
        public string LAct { get; set; }    // The lower face action.
        public double LPow { get; set; }    // Power of the lower face action. Zero means "low power", 1 means "high power".
    }

    // Sys events data object
    public class SysEventArgs
    {
        public SysEventArgs(double time, string detection, string eventMsg)
        {
            Time            = time;
            Detection       = detection;
            EventMessage    = eventMsg;
        }
        public double Time { get; set; }
        public string Detection { get; set; }
        public string EventMessage { get; set; }
    }

    // Detection information
    public class DetectionInfo
    {
        public DetectionInfo(string detection) {
            DetectionName   = detection;
            Actions     = new List<string>();
            Controls    = new List<string>();
            Events      = new List<string>();
            Signature   = new List<string>();
        }
        public string DetectionName { get;}
        public List<string> Actions { get; set; }
        public List<string> Controls { get; set; }
        public List<string> Events { get; set; }
        public List<string> Signature { get; set; }
    }
    
}