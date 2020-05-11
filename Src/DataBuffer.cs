using System;
using System.Collections.Generic;
using System.Collections;
using EmotivUnityPlugin;

/// <summary>
/// Data buffer.
/// </summary>
public class DataBuffer
{   
    /// <summary>
    /// Seting data buffer.
    /// </summary>
    public virtual void SettingBuffer(int winSize, int step, int headerCount) {
        UnityEngine.Debug.Log("SettingBuffer");
    }

    public virtual double[] GetDataFromBuffer(int index)
    {
        return null;
    }

    /// <summary>
    /// Get latest data from buffer.
    /// </summary>
    public virtual double[] GetLatestDataFromBuffer(int index)
    {
        return null;
    }

    /// <summary>
    /// Add data to buffer.
    /// </summary>
    public virtual void AddDataToBuffer(ArrayList data)
    {
        UnityEngine.Debug.Log("AddDataToBuffer");
    }

}