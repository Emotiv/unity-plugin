using UnityEngine;
using System.Collections;
using System.IO;
using System;

namespace EmotivUnityPlugin
{
    /// <summary>
    /// Logger handler: print log at file with format
    /// Not apply for unity editor mode.
    /// </summary>
    public class MyLogger : ILogger
    {
        static readonly object _object = new object();
        private FileStream m_FileStream;
        private StreamWriter m_StreamWriter;
        private ILogHandler m_DefaultLogHandler = Debug.unityLogger.logHandler;
        public static MyLogger Instance { get; } = new MyLogger();

        public ILogHandler logHandler { get; set; }
        public bool logEnabled { get; set; }
        public LogType filterLogType { get; set; }
        public bool saveToFile { get; set; }
        public bool showConsoleLog { get; set; }

        private MyLogger()
        {
            logHandler = this;
            logEnabled = true;
            filterLogType = LogType.Log;
            saveToFile = true; // Default to saving logs to files
            showConsoleLog = true; // Default to showing logs in the console
        }

        /// <summary>
        /// Initial logger handler
        /// </summary>
        public void Init(string prefixFileName ,  bool saveToFile)
        {
            this.saveToFile = saveToFile;

            if (saveToFile)
            {
                string dateTimeStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string fileName = prefixFileName + "Log_" + dateTimeStr + ".txt";

                string logPath = Config.LogDirectory;
                string filePath = Path.Combine(logPath, fileName);
                m_FileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                m_StreamWriter = new StreamWriter(m_FileStream);
            }

            // Replace the default debug log handler
            UnityEngine.Debug.unityLogger.logHandler = this;
        }

        public void Log(LogType logType, object message)
        {
            if (logEnabled && logType <= filterLogType)
            {
                logHandler.LogFormat(logType, null, "{0}", message);
            }
        }

        public void Log(LogType logType, object message, UnityEngine.Object context)
        {
            if (logEnabled && logType <= filterLogType)
            {
                logHandler.LogFormat(logType, context, "{0}", message);
            }
        }

        public void Log(LogType logType, string tag, object message)
        {
            if (logEnabled && logType <= filterLogType)
            {
                logHandler.LogFormat(logType, null, "{0}: {1}", tag, message);
            }
        }

        public void Log(LogType logType, string tag, object message, UnityEngine.Object context)
        {
            if (logEnabled && logType <= filterLogType)
            {
                logHandler.LogFormat(logType, context, "{0}: {1}", tag, message);
            }
        }

        public void Log(object message)
        {
            Log(LogType.Log, message);
        }

        public void Log(string tag, object message)
        {
            Log(LogType.Log, tag, message);
        }

        public void Log(string tag, object message, UnityEngine.Object context)
        {
            Log(LogType.Log, tag, message, context);
        }

        public void LogWarning(string tag, object message)
        {
            Log(LogType.Warning, tag, message);
        }

        public void LogWarning(string tag, object message, UnityEngine.Object context)
        {
            Log(LogType.Warning, tag, message, context);
        }

        public void LogError(string tag, object message)
        {
            Log(LogType.Error, tag, message);
        }

        public void LogError(string tag, object message, UnityEngine.Object context)
        {
            Log(LogType.Error, tag, message, context);
        }

        public void LogException(Exception exception)
        {
            LogException(exception, null);
        }

        public void LogException(Exception exception, UnityEngine.Object context)
        {
            if (logEnabled && LogType.Exception <= filterLogType)
            {
                logHandler.LogException(exception, context);
            }
        }

        public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
        {
            lock (_object)
            {
                LogType myLogType = logType;
                string type = "";
                switch (logType)
                {
                    case LogType.Log:
                        type = "info";
                        break;
                    case LogType.Warning:
                        type = "warning";
                        myLogType = LogType.Log;
                        break;
                    case LogType.Error:
                        type = "error";
                        myLogType = LogType.Log;
                        break;
                    case LogType.Exception:
                        type = "exception";
                        break;
                    default:
                        type = "info";
                        break;
                }

                string newFormat;
                object[] tmpArgs;

                if (args.Length > 1 && args[0].ToString() == "CortexLog")
                {
                    newFormat = "{0}"; // log from cortex side
                    tmpArgs = new object[1];
                    tmpArgs[0] = args[1]; // get second element of args for message
                }
                else
                {
                    newFormat = "[{0}][unity " + type + "   ] {1}"; // log from unity side
                    string dtNow = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffzzz");
                    tmpArgs = new object[2];
                    tmpArgs[0] = dtNow;
                    tmpArgs[1] = args[0]; // only get first element of args
                }

                if (saveToFile)
                {
                    m_StreamWriter.WriteLine(String.Format(newFormat, tmpArgs));
                    m_StreamWriter.Flush();
                }

                if (showConsoleLog)
                {
                    m_DefaultLogHandler.LogFormat(myLogType, context, newFormat, tmpArgs);
                }
            }
        }

        public bool IsLogTypeAllowed(LogType logType)
        {
            return logEnabled && logType <= filterLogType;
        }

        public void LogFormat(LogType logType, string format, params object[] args)
        {
            if (IsLogTypeAllowed(logType))
            {
                lock (_object)
                {
                    LogFormat(logType, null, format, args);
                }
            }
        }
    }
}