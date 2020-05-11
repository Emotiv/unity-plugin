using System;
using System.Collections.Generic;
using System.Collections;
using Newtonsoft.Json.Linq;

namespace EmotivUnityPlugin
{

    /// <summary>
    /// Performance metric data buffer .
    /// </summary>
    public class PMDataBuffer : DataBuffer
    {
        BufferStream[] bufHi;   // high rate buffer

        const int InvalidValue = -1; // for null data value when is poor EEG signal quality 

        List<string> _pmList = new List<string>(); // performance metric lists

        public List<string> PmList { get => _pmList; set => _pmList = value; }

        public int SetChannels(JArray pmLists) 
        {
            string timestamp = ChannelStringList.ChannelToString(Channel_t.CHAN_TIME_SYSTEM);
            int count = 1;
            PmList.Add(timestamp);
            foreach(var item in pmLists){
                // exclude Active flag
                string chanStr = item.ToString();
                if (!chanStr.Contains(".isActive")) {
                    PmList.Add(chanStr);
                    ++count;
                }
            }
            return count;
        }

        public void Clear() {
            
            if (bufHi != null) {
                Array.Clear(bufHi, 0, bufHi.Length);
                bufHi = null;
            }
        }

        public override void SettingBuffer(int winSize, int step, int headerCount) {
            int buffSize = headerCount;
            bufHi  = new BufferStream[buffSize];
            // UnityEngine.Debug.Log("PM Setting Buffer size" + bufHi.Length);
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

        // event handler
        public void OnPMDataReceived(object sender, ArrayList data)
        {
            AddDataToBuffer(data);
        }

        public override void AddDataToBuffer(ArrayList data)
        {
            int i = 0;
            foreach (var ele in data) {
                // ignore active flag
                if (Utils.IsNumericType(ele))
                {
                    try
                    {
                        double pmData = Convert.ToDouble(ele);
                        bufHi[i].AppendData(pmData);
                        i++;
                    }
                    catch (System.Exception e)
                    {
                    UnityEngine.Debug.LogError(e.Message + " index " + i + " value " +ele.ToString());
                    break;
                    }
                    
                }
            }
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

        public double GetData(string label)
        {
            int index = GetLabelIndex(label);
            if (index == -1) {
                UnityEngine.Debug.LogError(" Invalid label: " + label);
                return InvalidValue;
            }
            double[] chanData = GetLatestDataFromBuffer(index);
            if (chanData != null) {
                return chanData[0];
            } else {
                return InvalidValue;
            }
        }

        public int GetLabelIndex(string chan) 
        {
            int chanIndex = _pmList.IndexOf(chan);
            return (int)chanIndex;
        }

        public int GetBufferSize() 
        {
            if(bufHi[1] == null)
                return 0;

            return bufHi[1].GetBufSize(); // buff size of "boredom"
        }
    }
}


