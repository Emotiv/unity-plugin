using UnityEngine;
using System.Collections;
using System.IO;
using System;
using EmotivUnityPlugin;

/// <summary>
/// Logger handler: print log at file with format
/// Not apply for unity editor mode.
/// </summary>
public class Logger : ILogHandler
{
    static readonly object _object = new object();
    private FileStream m_FileStream;
    private StreamWriter m_StreamWriter;
    private ILogHandler m_DefaultLogHandler = Debug.unityLogger.logHandler;
    public static Logger Instance {get;} = new Logger();

    /// <summary>
    /// Initial logger handler
    /// </summary>
    public void Init()
    {
        #if !UNITY_EDITOR
        string dateTimeStr  = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName     = Config.AppName + "Log_" + dateTimeStr + ".txt";

        string logPath      = Utils.GetLogPath();
        string filePath     = Path.Combine(logPath, fileName);

        m_FileStream    = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        m_StreamWriter  = new StreamWriter(m_FileStream);
        // Replace the default debug log handler
        UnityEngine.Debug.unityLogger.logHandler = this;
        #endif
    }

    
    public void LogException(Exception exception, UnityEngine.Object context)
    {
        // m_DefaultLogHandler.LogException(exception, context);
        LogFormat(LogType.Exception, context, "", exception.Message);
    }

    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        lock(_object) 
        {
            string type = "";
            switch (logType)
            {
                case LogType.Log: {
                    type = "Step";
                    break;
                }
                case LogType.Warning: {
                    type = "Warning";
                    break;
                }
                case LogType.Error: {
                    type = "Error";
                    break;
                }
                default: {
                    type = "Exception";
                    break;
                }
            }
            // Change format
            string newFormat = "{0}:  " + type + ":  {1}";
            string dtNow = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
            int len = args.Length + 1;
            object[] tmpArgs = new object[2];
            tmpArgs[0]  = dtNow;
            tmpArgs[1]  = args[0]; // only get first element of args
            m_StreamWriter.WriteLine(String.Format(newFormat, tmpArgs));
            m_StreamWriter.Flush();
            m_DefaultLogHandler.LogFormat(logType, context, newFormat, tmpArgs);
            
        }
    }
}