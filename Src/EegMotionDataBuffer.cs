using System;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Buffer for eeg data or motion data .
    /// </summary>
    public class EegMotionDataBuffer : DataBuffer 
    {
        BufferStream[] bufHi;

        public enum DataType : int {
            EEG, MOTION
        }

        List<Channel_t> _channels = new List<Channel_t>();
        DataType _dataType = DataType.EEG;

        public List<Channel_t> DataChannels { get => _channels; set => _channels = value; }

        public void Clear() {
            
            if (bufHi != null) {
                Array.Clear(bufHi, 0, bufHi.Length);
                bufHi = null;
            }
        }
        public void SetChannels( JArray channelList) 
        {
            _channels.Add(Channel_t.CHAN_TIME_SYSTEM);
            foreach(var item in channelList) {
                string chanStr = item.ToString();
                if (chanStr != "MARKERS") // remove MARKERS chan from eeg header
                    _channels.Add(ChannelStringList.StringToChannel(chanStr));
            }
        }
        public void SetDataType(DataType type)
        {
            _dataType = type;
        }

        public override void SettingBuffer(int winSize, int step, int headerCount) 
        {
            int buffSize;
            if (_dataType == DataType.EEG)
                buffSize = headerCount; // include "TIMESTAMP", exclude MARKERS channel
            else 
                buffSize = headerCount + 1; // include "TIMESTAMP" channel

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
            if (data.Count > _channels.Count) {
                UnityEngine.Debug.Log("AddDataToBuffer: data contain markers channels.");
            }


            for (int i=0 ; i <  _channels.Count; i++) {
                if (data[i] != null) {
                    double eegData = Convert.ToDouble(data[i]);
                    bufHi[i].AppendData(eegData);
                }
            }
            
        }

        // Event handler
        public void OnDataReceived(object sender, ArrayList data) {
            AddDataToBuffer(data);
        }

        public override double[] GetDataFromBuffer(int index) 
        {
            return bufHi[index].NextWithRemoval();
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

        public double[] GetAllDataFromBuffer(int index)
        {
            List<double> dataList = new List<double>();
            double[] nextSegment = null;
            do {
                nextSegment = GetDataFromBuffer(index);
                if(nextSegment != null)
                    dataList.AddRange(nextSegment);
            }
            while (nextSegment != null);
            return dataList.ToArray();
        }

        // get data both eeg and motion data
        public double[] GetData(Channel_t channel) 
        {
            if (channel == Channel_t.CHAN_FLEX_CMS || channel == Channel_t.CHAN_FLEX_DRL)
                return null;
            
            try
            {
                return GetAllDataFromBuffer(GetChanIndex(channel));
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.Log(" exception " + e.Message + " index " + GetChanIndex(channel) + " chan " + (int)channel + " buffSize " + bufHi.Length);
                return null;
            }

            
        }

        public int GetBufferSize() 
        {
            if(bufHi[3] == null)
                return 0;

            return bufHi[3].GetBufSize(); // get buffer size of AF3
        }

        public int GetChanIndex(Channel_t chan) 
        {
            int chanIndex = _channels.IndexOf(chan);
            return (int)chanIndex;
        }

        public void PrintEEgData()
        {
            double[] eeg = GetData(Channel_t.CHAN_AF3);
            UnityEngine.Debug.Log("======PrintEEgData: AF3: size: " 
                                + eeg.Length + " [0]: " + eeg[0].ToString() );
        }
    }
}

