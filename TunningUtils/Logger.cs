using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Olos.FileLogger;
using Olos.FileLoggerMgr;
using System.Configuration;

namespace TunningUtils
{
    public static class Logger
    {
        static FileLoggerManager logger = new FileLoggerManager();

        static Logger()
        {
            String loggerFileName = ConfigurationManager.AppSettings["LogPath"];
            if (String.IsNullOrEmpty(loggerFileName))
            {
                loggerFileName = @"C:\Olos\Logs\LogASR\";
            }
            if (!loggerFileName.EndsWith("\\"))
            {
                loggerFileName += "\\";
            }

            logger.FileName = loggerFileName + "LogASRTunning.txt";
            logger.LogInfoEnabled = Convert.ToBoolean(ConfigurationManager.AppSettings["AdditionalLogEnabled"]);
            logger.MaxLines = 10000;
        }

        public static void LogMessage(string module, string codeReference, string dataReference, string message)
        {
            logger.LogInfo(string.Format("{0}\t{1}\t{2}\t{3}", module, codeReference, dataReference, message));
        }

        public static void LogError(string module, string codeReference, string dataReference, string message)
        {
            logger.LogError(string.Format("{0}\t{1}\t{2}\t{3}", module, codeReference, dataReference, message));
        }

        public static void Close()
        {
            logger.CloseFile();
        }

        public static void Stop()
        {
            logger.CloseFile();
            logger.Stop();
            logger = null;
        }


        public static bool LogInfoEnabled
        {
            get { return logger.LogInfoEnabled; }
            set { logger.LogInfoEnabled = value; }
        }

    }
}
