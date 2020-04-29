using System;
using System.Collections.Generic;
using System.Collections;
using EmotivUnityPlugin;

public class DataBuffer
{   
    public virtual void SettingBuffer(int winSize, int step, int headerCount) {
        UnityEngine.Debug.Log("SettingBuffer");
    }

    public virtual double[] GetDataFromBuffer(int index)
    {
        return null;
    }

    public virtual double[] GetLatestDataFromBuffer(int index)
    {
        return null;
    }

        public virtual void AddDataToBuffer(ArrayList data)
    {
        UnityEngine.Debug.Log("AddDataToBuffer");
    }

}