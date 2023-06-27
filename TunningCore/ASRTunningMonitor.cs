using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TunningUtils;
using Olos.Utils;


namespace TunningCore
{

    public class ASRTunningMonitor
    {
        public DBTunning tunningDatabase;
        private TunningConfiguration configuration;

        private Dictionary<int, Task> tunningTasks = new Dictionary<int, Task>();
        private CancellationTokenSource cancellationToken = new CancellationTokenSource(); 
        private Task taskLoadConfiguration;
        private DateTime lastReloadConfig;
        private bool keepRunning = false;
        /*
        Task campaignTask = Task.Factory.StartNew(() => bllCampaign.DoWorkCampaign(item, _cancellationToken.Token));
                                        //_listOfTasks.Add(campaignTask);
                                        _listOfTasks.Add(item.Id, campaignTask);
                    int lastReaload = Environment.TickCount - MIN_RELOAD_INTERVAL;
        */



        public void LoadConfiguration()
        { 
             //BLLServices _bllServices = new Olos.DataBase.BussinessLogicLayer.BLLServices(null); ;
             DataBaseProperties configdb;
             DataBaseProperties tunningdb;

             Olos.Utils.EncryptDecrypt dec = new Olos.Utils.EncryptDecrypt();
             configdb = new DataBaseProperties()
             {
                 ServerName = dec.DecryptText(ConfigurationManager.AppSettings["SQLServerName"]), 
                 DatabaseName = dec.DecryptText(ConfigurationManager.AppSettings["SQLDataBaseName"]), 
                 UserName = dec.DecryptText(ConfigurationManager.AppSettings["SQLUserName"]), 
                 Password = dec.DecryptText(ConfigurationManager.AppSettings["SQLPassword"]), 
                 ApplicationName = ConfigurationManager.AppSettings["ApplicationName"] 
             };

             tunningdb = new DataBaseProperties()
             {
                 ServerName = dec.DecryptText(ConfigurationManager.AppSettings["SQLTunningServerName"]), 
                 DatabaseName = dec.DecryptText(ConfigurationManager.AppSettings["SQLTunningDataBaseName"]), 
                 UserName = dec.DecryptText(ConfigurationManager.AppSettings["SQLTunningUserName"]), 
                 Password = dec.DecryptText(ConfigurationManager.AppSettings["SQLTunningPassword"]), 
                 ApplicationName = ConfigurationManager.AppSettings["ApplicationName"] 
             };

             
             tunningDatabase = new DBTunning(tunningdb);
             configuration = tunningDatabase.GetTunningConfiguration();
            


        }
        
        public void Start()
        {
            LoadConfiguration();
            Thread newTasks = new Thread(new ThreadStart(CheckNewTasks));
            keepRunning = true;
            newTasks.Start();


        }
        public void Stop()
        {
            keepRunning = false;


        }

        public ASRTunningMonitor()
        {

        }


        private void CheckNewTasks()
        {
            const int MIN_INTERVAL = 30000;
            const int MAX_INTERVAL = 60000;

            while (keepRunning)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                try
                {
                    List<TunningTask> tasks = tunningDatabase.GetTunnningTasks();
                    if (configuration.Active && configuration.Servers.Count > 0)
                    {
                        foreach(TunningTask item in tasks)
                        {
                            if (!tunningTasks.ContainsKey(item.Id))
                            {
                                ASRTask asrTask = new ASRTask(item, tunningDatabase, configuration);
                                Task newtask = Task.Factory.StartNew(() => asrTask.DoWork(item, cancellationToken.Token));
                                tunningTasks.Add(item.Id, newtask);
                            }
                        }
                    }
                   
                }
                catch (Exception ex)
                {
                    Logger.LogError("asrControl", string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Empty, string.Format("Message:{0} StackTrace:{1}", ex.Message, ex.StackTrace));
                }

                sw.Stop();
                if (sw.Elapsed.TotalMilliseconds < MIN_INTERVAL)
                {
                    Thread.Sleep(MIN_INTERVAL - (int)sw.Elapsed.TotalMilliseconds);
                }
            }
        }







    }



}
