using System;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Dev data buffer.
    /// </summary>
    public class DevDataBuffer : DataBuffer 
    {

        BufferStream[] bufHi;

        // dev channels list: timestamp, battery, signal strength, and EEG channels
        List<Channel_t> _devChannels = new List<Channel_t>();

        double BATTERY_MAX = (double)BatteryLevel.LEVEL_4;
        bool _hasBatteryPercent = false; // has battery percentage range [0-100]. From Cortex v2.7.0, we add battery percentage to dev stream

        public double Battery
        {
            get {
                if (_hasBatteryPercent)
                    return GetContactQuality(Channel_t.CHAN_BATTERY_PERCENT);
                else
                    return GetContactQuality(Channel_t.CHAN_BATTERY);
            }
        }

        public double SignalStrength
        {
            get {
                return GetContactQuality(Channel_t.CHAN_SIGNAL_STRENGTH);
            }
        }

        public double BatteryMax
        {
            get {
                return BATTERY_MAX;
            }        
        }

        public void Clear() {
            
            if (bufHi != null) {
                Array.Clear(bufHi, 0, bufHi.Length);
                bufHi = null;
            }
        }

        public void SetChannels(JArray devChannels) 
        {
            _devChannels.Add(Channel_t.CHAN_TIME_SYSTEM);
            _devChannels.Add(Channel_t.CHAN_BATTERY);
            _devChannels.Add(Channel_t.CHAN_SIGNAL_STRENGTH);
            foreach(var item in devChannels){
                string chanStr = item.ToString();

                if (chanStr == "BatteryPercent") {
                    _hasBatteryPercent = true;
                    _devChannels.Add(Channel_t.CHAN_BATTERY_PERCENT);
                }
                else if (chanStr != "Battery" &&  chanStr != "Signal") { // added above
                    _devChannels.Add(ChannelStringList.StringToChannel(chanStr));
                }
            }
        }

        public override void SettingBuffer(int winSize, int step, int headerCount) 
        {
            int buffSize = headerCount + 1; // include "TIMESTAMP"
            bufHi = new BufferStream[buffSize];
            for (int i = 0; i < buffSize; i++)
            {
                if (bufHi[i] == null){
                    bufHi[i] = new BufferStream(winSize, step);
                }
                else {
                    bufHi[i].Reset();
                    bufHi[i].WindowSize = winSize;
                    bufHi[i].StepSize = step;
                }
            }
        }

        public override void AddDataToBuffer(ArrayList data)
        {
            for (int i = 0; i < data.Count; i++){
                double devData = Convert.ToDouble(data[i]);
                bufHi[i].AppendData(devData);
            }
        }
        
        // Event handler
        public void OnDevDataReceived(object sender, ArrayList data) {
            // UnityEngine.Debug.Log("DevDataBuffer: OnDevDataReceived = " + data);
            AddDataToBuffer(data);
        }

        public override double[] GetDataFromBuffer(int index)
        {
            return bufHi[(int)index].NextWithRemoval();
        }
        
        public override double[] GetLatestDataFromBuffer(int index)
        {
            double[] nextSegment = null;
            double[] lastSegment = null;
            do
            {
                lastSegment = nextSegment;
                nextSegment = GetDataFromBuffer(index);
            }
            while (nextSegment != null);
            return lastSegment;
        }

        // return -1 if the moment there is no battery come. 
        public double GetContactQuality(Channel_t channel)
        {
            if (channel == Channel_t.CHAN_FLEX_CMS || channel == Channel_t.CHAN_FLEX_DRL) {
                return 0;
            }
            double[] chanData;
            chanData = GetLatestDataFromBuffer(GetChanIndex(channel));

            if (chanData != null) {
                return chanData[0];
            } else {
                return -1;
            }
        }

        // get CQ value follow channel id from Dev respond
        public double GetContactQuality(int channelId)
        {
            double[] chanData = GetLatestDataFromBuffer(channelId);
            if (chanData != null) {
                return chanData[0];
            } else {
                return 0;
            }
        }    

        public int GetChanIndex(Channel_t chan) {
            int chanIndex = _devChannels.IndexOf(chan);
            if (chanIndex == -1)
                return -1; // TODO: return error
            // UnityEngine.Debug.Log("GetChanIndex" + chanIndex);
            return (int)chanIndex;
        }

        /// <summary>
        /// Get the buffer size of one data channel to check data ready for retrieving.
        /// </summary> 
        /// <remarks>The data of a channel will start from index 3 after timestamp, battery, signal strength.
        /// So We choose index 3 to get buffersize</remarks>
        public int GetBufferSize() {
            if(bufHi[3] == null)
                return 0;

            return bufHi[3].GetBufSize();
        }

        public void PrintDevData() {
            double cq = (double)GetContactQuality(Channel_t.CHAN_AF3);
            UnityEngine.Debug.Log("======PrintDevData: battery" + Battery.ToString() 
                                + " signal" + SignalStrength.ToString() + " AF3: "+ cq.ToString());
        }

    }
}

