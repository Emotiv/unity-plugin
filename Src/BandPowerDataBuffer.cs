using System;
using System.Collections.Generic;
using System.Collections;
using EmotivUnityPlugin;
using Newtonsoft.Json.Linq;



/// <summary>
/// Band power data buffer.
/// </summary>
public class BandPowerDataBuffer : DataBuffer
{
    BufferStream[] bufHi;   // high rate buffer

    List<string> _bandPowerList = new List<string>();

     public static Dictionary<BandPowerType, string> BandPowerMap = new Dictionary<BandPowerType, string>() {
                                                                        {BandPowerType.Thetal, "theta"},
                                                                        {BandPowerType.Alpha,  "alpha"},
                                                                        {BandPowerType.BetalL, "betaL"},
                                                                        {BandPowerType.BetalH, "betaH"},
                                                                        {BandPowerType.Gamma,  "gamma"}
                                                                        };

    public List<string> BandPowerList { get => _bandPowerList; set => _bandPowerList = value; }

    public void SetChannels(JArray bandPowerLists) 
    {
        string timestamp = ChannelStringList.ChannelToString(Channel_t.CHAN_TIME_SYSTEM);
        _bandPowerList.Add(timestamp);
        foreach(var item in bandPowerLists){
            string chanStr = item.ToString();
            _bandPowerList.Add(chanStr);
        }
    }

    public void Clear() {
        
        if (bufHi != null) {
            Array.Clear(bufHi, 0, bufHi.Length);
            bufHi = null;
        }
    }

    public override void SettingBuffer(int winSize, int step, int headerCount) {
        int buffSize = headerCount + 1; // include "TIMESTAMP"
        bufHi  = new BufferStream[buffSize];
        UnityEngine.Debug.Log("POW Setting Buffer size" + bufHi.Length);
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
    public void OnBandPowerReceived(object sender, ArrayList data)
    {
        // UnityEngine.Debug.Log("OnBandPowerReceived " + data[2].ToString() + " at " + Utils.GetEpochTimeNow().ToString());
        AddDataToBuffer(data);
    }

    public override void AddDataToBuffer(ArrayList data)
    {
        for (int i=0 ; i < data.Count; i++) {
            if (data[i] != null) {
                double powerData = Convert.ToDouble(data[i]);
                bufHi[i].AppendData(powerData);
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

    public double GamaPower(Channel_t channel)
    {
        if (channel == Channel_t.CHAN_FLEX_CMS || channel == Channel_t.CHAN_FLEX_DRL)
            return 0;

        double[] chanData = GetLatestDataFromBuffer(GetPowerIndex(channel, BandPowerType.Gamma));
        if (chanData != null) {
            return chanData[0];
        } else {
            return 0;
        }
    }
    public double ThetalPower(Channel_t channel)
    {
        if (channel == Channel_t.CHAN_FLEX_CMS || channel == Channel_t.CHAN_FLEX_DRL)
            return 0;

        double[] chanData = GetLatestDataFromBuffer(GetPowerIndex(channel, BandPowerType.Thetal));
        if (chanData != null) {
            return chanData[0];
        }
        else {
            return 0;
        }

    }
    public double AlphaPower(Channel_t channel)
    {
        if (channel == Channel_t.CHAN_FLEX_CMS || channel == Channel_t.CHAN_FLEX_DRL)
            return 0;
        
        try
        {
            double[] chanData = GetLatestDataFromBuffer(GetPowerIndex(channel, BandPowerType.Alpha));
            if (chanData != null) {
                return chanData[0];
            } else {
                return 0;
            }
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.Log("Exception :" + e.Message + " chan " + channel + " index " + GetPowerIndex(channel, BandPowerType.Alpha));
            return 0;
        }
    }

    public double BetalLPower(Channel_t channel)
    {
        if (channel == Channel_t.CHAN_FLEX_CMS || channel == Channel_t.CHAN_FLEX_DRL)
            return 0;

        double[] chanData = GetLatestDataFromBuffer(GetPowerIndex(channel, BandPowerType.BetalL));
        if (chanData != null) {
            return chanData[0];
        } else {
            return 0;
        }
    }
    public double BetalHPower(Channel_t channel)
    {
        if (channel == Channel_t.CHAN_FLEX_CMS || channel == Channel_t.CHAN_FLEX_DRL) {
            return 0;
        }

        double[] chanData = GetLatestDataFromBuffer(GetPowerIndex(channel, BandPowerType.BetalH));
        if (chanData != null) {
            return chanData[0];
        } else {
            return 0;
        }
    }

    public void PrintPower(BandPowerType powerType){
        
        // TODO: Check correct data
        double power = 0;
        if (powerType == BandPowerType.Alpha){
            power = AlphaPower(Channel_t.CHAN_AF3);
        }
        else if (powerType == BandPowerType.Gamma) {
            power = GamaPower(Channel_t.CHAN_AF3);
        }
        UnityEngine.Debug.Log("======PrintPower: type" + (int)powerType + " AF3: "+ power.ToString());
    }

    public int GetPowerIndex(Channel_t channel, BandPowerType powerType) {
        if (channel == Channel_t.CHAN_TIME_SYSTEM)
            return 0;
        string chanStr  = ChannelStringList.ChannelToString(channel);
        string powStr   = BandPowerMap[powerType];
        int chanIndex = _bandPowerList.IndexOf(chanStr+ "/" + powStr); // TODO check chanIndex = -1
        return chanIndex;
    }

    public int GetBufferSize() 
    {
        if(bufHi[2] == null)
            return 0;

        return bufHi[2].GetBufSize(); // get buffer size of "AF3/alpha"
    }
}
