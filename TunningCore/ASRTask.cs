using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using TunningUtils;
using System.Reflection;
using System.Diagnostics;
using System.Configuration;
using System.Web.Script.Serialization;

namespace TunningCore
{
    public class ASRTask
    {
        private TunningTask taskdefinition;
        private DBTunning db;
        private TunningConfiguration config;
        private bool running;
        private int foundSamples = 0; 

        public ASRTask(TunningTask task, DBTunning dbobj, TunningConfiguration defaultconfig )
        {
            taskdefinition = task;
            db = dbobj;
            config = defaultconfig;
            running = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        internal void DoWork(TunningTask item, CancellationToken cancellationToken)
        {
            Logger.LogMessage(string.Format("task {0}", taskdefinition.Id), string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Empty, "Starting Task");
            Stopwatch sw = new Stopwatch();
            sw.Start();

            DateTime startTime = item.StartPeriod;
            //DateTime endTime = startTime.AddMinutes(30);
            Dictionary<string, FileInfo[]> dirLogFiles = new Dictionary<string,FileInfo[]>();
            Dictionary<string, List<RecognitionInfo>> filteredLogs = new Dictionary<string, List<RecognitionInfo>>();
            taskdefinition.Status = TaskStatus.Running;
            int estimatedFiles = 0;


            string insertSQLTask = string.Empty;
            List<string> insertSQLData = new List<string>();
            string pathForReplication = ConfigurationManager.AppSettings["PathForReplication"] ?? config.RepositoryPath;

            bool updateTask = db.UpdateTaskStatus(taskdefinition);

            try
            {
                List<RecognitionServer> activedServers = config.Servers.Where(x => x.Active == true).ToList();

                if (activedServers == null || activedServers.Count == 0)
                {
                    taskdefinition.Status = TaskStatus.Error;
                    running = false;
                }

                while (running && foundSamples < taskdefinition.DesirableSamples && !cancellationToken.IsCancellationRequested && startTime < DateTime.Now)
                {
                    DateTime searchHour = startTime;
                    foreach (RecognitionServer recServer in activedServers)
                    {
                        string directoryName = string.Empty;
                        string searchMaskLogFiles = "----NO SEARCH----";
                        switch (recServer.ServerType)
                        {
                            case ASR.Nuance:
                                directoryName = string.Format(@"\\{0}\{1}\{2}\{3}{4}\{5}\{6}", recServer.IP, recServer.RootPath, searchHour.ToString("yyyy"), searchHour.ToString("MM"), TranslateMonth(searchHour.ToString("MM")), searchHour.ToString("dd"), searchHour.ToString("HH"));
                                searchMaskLogFiles = "*-LOG";
                                break;
                            case ASR.Verbio: //TO-DO:Pattern is not defined yet
                                directoryName = string.Format(@"\\{0}\{1}\{2}\{3}{4}\{5}\{6}", recServer.IP, recServer.RootPath, searchHour.ToString("yyyy"), searchHour.ToString("MM"), TranslateMonth(searchHour.ToString("MM")), searchHour.ToString("dd"), searchHour.ToString("HH"));
                                searchMaskLogFiles = "*-LOG";
                                break;
                        }

                        if (Directory.Exists(directoryName))
                        {
                            DirectoryInfo dirInfo = new DirectoryInfo(directoryName);

                            FileInfo[] newfiles = dirInfo.EnumerateFiles(searchMaskLogFiles, SearchOption.AllDirectories)
                                   .AsParallel()
                                   .ToArray();
                                   //.Where(fi => fi.CreationTime >= startTime).ToArray(); //Aparentemente o CreationTime nas máquinas em espanhol

                            List<RecognitionInfo> asrInfo = new List<RecognitionInfo>();
                            foreach (FileInfo filelog in newfiles)
                            {
                                List<RecognitionInfo> recInfo = GetFilteredASRInfoFromFile(filelog);
                                if (recInfo.Count > 0)
                                {
                                    asrInfo.AddRange(recInfo);
                                    estimatedFiles++;
                                }

                                //To avoid a huge process, we have to limit 
                                if (taskdefinition.DesirableSamples * 3 < estimatedFiles)
                                {
                                    break;
                                }
                            }

                            filteredLogs.Add(directoryName, asrInfo);
                            foundSamples += asrInfo.Count;
                        }
                        else
                        {
                            Logger.LogMessage(string.Format("task {0}", taskdefinition.Id), string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Format("Directory not Exists or is unacessible:{0} ", directoryName), "Source Repository Configuration");
                        }
                    }
                    startTime = searchHour.AddHours(1);
                }

                if (foundSamples > 0)
                {
                    string serverRep = "localhost";
                    string sharedFolder = "Nuance";

                    string[] repository = config.RepositoryPath.Split(new char[] { '\\' }, StringSplitOptions.RemoveEmptyEntries);
                    if (repository.Length == 2)
                    {
                        serverRep = repository[0];
                        sharedFolder = repository[1];
                    }

                    Logger.LogMessage(string.Format("task {0}", taskdefinition.Id), string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Format("serverRep:{0} sharedFolder:{1} NTDomainUser:{2} NTPassword:{3} Samples:{4} ", serverRep, sharedFolder, config.NTDomainUser, config.NTPassword, foundSamples), "Repository Configuration");

                    Int64 availableSpace = config.MinimumAvailableSpace + 100000000;
                    try
                    {
                        availableSpace = ASRIOTasks.GetFreeSpaceFromNetworkShare(serverRep, sharedFolder, config.NTDomainUser, config.NTPassword);
                    }
                    catch
                    {
                        if (!Convert.ToBoolean(ConfigurationManager.AppSettings["IgnoreWmiAuthentication"]))
                        {
                            availableSpace = 0;
                        }
                    }

                    Logger.LogMessage(string.Format("task {0}", taskdefinition.Id), string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Format("availableSpace:{0}  ", availableSpace), "WMI AvailableSpace");

                    Int64 tasksize = 0;
                    int savedSamples = 0;

                    insertSQLTask += string.Format("INSERT INTO [dbo].[TunningTask] ([Id],[Time],[UserId],[DesirableSamples],[Description],[StartPeriod],[Status],[CreationDate],[StartDate],[EndDate],[Tenant]) VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}'); ", item.Id, item.Date.ToString("yyyy-MM-dd HH:mm:ss"), item.UserId, item.DesirableSamples, item.Description, item.StartPeriod.ToString("yyyy-MM-dd HH:00:00"), "#STATUS#", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "#ENDDATE#", config.Site);
                    foreach (TaskDetail td in item.Tasks)
                    {
                        insertSQLTask += string.Format("INSERT INTO [dbo].[TunningTaskDetail] ([TaskId],[Type],[Values],[Tenant], [TNATRegex]) VALUES ('{0}','{1}','{2}','{3}','{4}'); ", item.Id, (int)td.Type, td.Value, config.Site, td.TNATRegex);
                    }

                    foreach (KeyValuePair<string, List<RecognitionInfo>> entry in filteredLogs)
                    {
                        foreach (RecognitionInfo entryInfo in entry.Value)
                        {
                            string sourceFile = string.Format(@"{0}\{1}", entry.Key, entryInfo.WaveFile);
                            FileInfo fi = new FileInfo(sourceFile);
                            if (File.Exists(sourceFile))
                            {
                                string destDirectory = string.Format(@"{0}\{1}\{2}\{3}\{4}\{5}\{6}", config.RepositoryPath, config.Site, item.Id, fi.CreationTime.ToString("yyyy"), fi.CreationTime.ToString("MM"), fi.CreationTime.ToString("dd"), fi.CreationTime.ToString("HH"));
                                bool dirExist = false;
                                if (!Directory.Exists(destDirectory))
                                {
                                    Directory.CreateDirectory(destDirectory);
                                    Thread.Sleep(100);
                                    if (Directory.Exists(destDirectory))
                                    {
                                        dirExist = true;
                                    }
                                }
                                else
                                {
                                    dirExist = true;
                                }

                                if (!dirExist)
                                {
                                    taskdefinition.Status = TaskStatus.Error;
                                    running = false;
                                    break;
                                }

                                bool saved = db.InsertASRInfo(entryInfo, taskdefinition, entry.Key, destDirectory, fi.CreationTime, config.Site);
                                if (saved)
                                {
                                    File.Copy(sourceFile, string.Format(@"{0}\{1}", destDirectory, entryInfo.WaveFile), true);
                                    tasksize += fi.Length;
                                    taskdefinition.Status = TaskStatus.PartialCompleted;
                                    savedSamples++;
                                    string nbstJson = string.Empty;
                                    if (entryInfo.NBestResult != null)
                                    {
                                        nbstJson = new JavaScriptSerializer().Serialize(entryInfo.NBestResult);
                                    }
                                    insertSQLData.Add(string.Format("INSERT INTO [dbo].[TunningData] ([Time],[Channel],[ENDR],[RSTT],[RSLT],[SPOK],[Confidence],[WVNM],[OriginalPath],[CurrentPath],[CallId],[Grammar],[Tenant],[Step],[TaskId],[TNAT],[NBST],[NBSTResult]) VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}', '{6}', '{7}', '{8}', '{9}', '{10}', '{11}', '{12}', '{13}', '{14}','{15}','{16}','{17}') ", fi.CreationTime.ToString("yyyy-MM-dd HH:mm:ss.fff"), entryInfo.Channel, entryInfo.EndOfSpeech, entryInfo.Result, entryInfo.ParsedResult, entryInfo.NormalizedRawText,
                                                            entryInfo.Confidence, entryInfo.WaveFile, entry.Key, destDirectory.Replace(config.RepositoryPath, pathForReplication), entryInfo.CallId, entryInfo.Grammar, config.Site, entryInfo.Step, taskdefinition.Id, entryInfo.TNAT, entryInfo.NBST, nbstJson));
                                }
                                if (savedSamples >= taskdefinition.DesirableSamples)
                                {
                                    taskdefinition.Status = TaskStatus.Completed;
                                    break;
                                }
                                if (availableSpace - tasksize < config.MinimumAvailableSpace)
                                {
                                    taskdefinition.Status = TaskStatus.NoAvailableSpace;
                                    Logger.LogMessage(string.Format("task {0}", taskdefinition.Id), string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Format("Available:{0} TaskSize:{1} MinConfiguration:{2}", availableSpace, tasksize, config.MinimumAvailableSpace), "No Space");
                                    running = false;
                                    break;
                                }
                            }
                            if (savedSamples >= taskdefinition.DesirableSamples )
                            {
                                taskdefinition.Status = TaskStatus.Completed;
                                break;
                            }
                            if (availableSpace - tasksize < config.MinimumAvailableSpace)
                            {
                                taskdefinition.Status = TaskStatus.NoAvailableSpace;
                                running = false;
                                Logger.LogMessage(string.Format("task {0}", taskdefinition.Id), string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Format("Available:{0} TaskSize:{1} MinConfiguration:{2}", availableSpace, tasksize, config.MinimumAvailableSpace), "No Space");
                                break;
                            }
                        }
                    }

                }
                else
                {
                    taskdefinition.Status = TaskStatus.NoSamplesFound;
                }

            }
            catch(Exception ex)
            {
                taskdefinition.Status = TaskStatus.Error;
                Logger.LogError(string.Format("task {0}", taskdefinition.Id), string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Empty, ex.Message);

            }

            if (cancellationToken.IsCancellationRequested)
            {
                taskdefinition.Status = TaskStatus.Cancelled;
            }

            insertSQLTask = insertSQLTask.Replace("#STATUS#", ((int)taskdefinition.Status).ToString()).Replace("#ENDDATE#", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));

            updateTask = db.UpdateTaskStatus(taskdefinition);

            try
            {
                CreateSQLFile(string.Format(@"{0}\{1}", config.RepositoryPath, config.Site), config.Site + "_" + item.Id.ToString("0000000") + ".sql", insertSQLTask, insertSQLData);
            }
            catch { }
            sw.Stop();
            Logger.LogMessage(string.Format("task {0}", taskdefinition.Id), string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Format("Tempo:{0}s Status:{1}", sw.Elapsed.TotalSeconds, taskdefinition.Status), "Task Ended");

        }                                                                                                   

        public string CreateSQLFile(string sqlPath, string fileName, string task,  List<string> data)
        {
 
            if (!Directory.Exists(sqlPath.TrimEnd('\\') )) Directory.CreateDirectory(sqlPath.TrimEnd('\\') );

            string fullFileName = sqlPath.TrimEnd('\\') + "\\" + fileName;

            StreamWriter sw = File.CreateText(fullFileName);

            sw.WriteLine(task);
            foreach (string item in data)
            {
                sw.WriteLine(item);
            }
            sw.Close();

            return fullFileName;
        }

        private List<RecognitionInfo> GetFilteredASRInfoFromFile(FileInfo filelog)
        {
            bool hasRecognition = false;
            List<RecognitionInfo> recList = new List<RecognitionInfo>();
            using (StreamReader sr = File.OpenText(filelog.FullName))
            {
                string s = String.Empty;
                while ((s = sr.ReadLine()) != null)
                {
                    if (s.Contains("EVNT=SWIrcnd|RSTT=ok") || s.Contains("EVNT=SWIrcnd|RSTT=lowconf"))
                    {
                        if (string.IsNullOrWhiteSpace(taskdefinition.TaskRegex) || Regex.IsMatch(s, taskdefinition.TaskRegex, RegexOptions.IgnoreCase ))
                        {
                            RecognitionInfo ri = ParseRecognitionLine(s);
                            recList.Add(ri);
                            hasRecognition = true;
                        }
                    }
                }
            }
            return recList;
        }

        private RecognitionInfo ParseRecognitionLine(string item)
        {
            RecognitionInfo ri = new RecognitionInfo();
            NBestResult nbstObj = new NBestResult();
            string[] recInfo = item.Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries);

            for (int i = 0; i < recInfo.Length; i++)
            {
                string info = recInfo[i].Substring(0, recInfo[i].IndexOf('=')).ToUpper();
                switch (info)
                {
                    case "RSTT":
                        ri.Result = (RSTT)Enum.Parse(typeof(RSTT), recInfo[i].Remove(0, 5), true);
                        break;
                    case "TIME":
                        ri.Time = recInfo[i].Remove(0, 5);
                        break;
                    case "CHAN":
                        ri.Channel = recInfo[i].Remove(0, 5);
                        break;
                    case "ENDR":
                        ri.EndOfSpeech = (ENDR)Enum.Parse(typeof(ENDR), recInfo[i].Remove(0, 5), true);
                        break;
                    case "RSLT":
                        if (string.IsNullOrWhiteSpace(ri.ParsedResult))
                        {
                            ri.ParsedResult = recInfo[i].Remove(0, 5);
                        }
                        nbstObj = new NBestResult();
                        nbstObj.RSLT = recInfo[i].Remove(0, 5);
                        break;
                    case "RAWT":
                        if (string.IsNullOrWhiteSpace(ri.RawText))
                        {
                            ri.RawText = recInfo[i].Remove(0, 5);
                        }
                        //nbstObj.RAWT = recInfo[i].Remove(0, 5);
                        break;
                    case "SPOK":
                        if (string.IsNullOrWhiteSpace(ri.NormalizedRawText))
                        {
                            ri.NormalizedRawText = recInfo[i].Remove(0, 5);
                        }
                        nbstObj.SPOK = recInfo[i].Remove(0, 5);
                        break;
                    case "CONF":
                        if (ri.Confidence <= 0)
                        {
                            ri.Confidence = int.Parse(recInfo[i].Remove(0, 5));
                        }
                        nbstObj.CONF = int.Parse(recInfo[i].Remove(0, 5));
                        ri.NBestResult.Add(nbstObj);
                        break;
                    case "WVNM":
                        ri.WaveFile = recInfo[i].Remove(0, 5);
                        break;
                    case "SESS":
                        string session = recInfo[i].Remove(0, 5);
                        string[] strSession = session.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                        if (strSession.Length > 1)
                        {
                            ri.CallId = strSession[0];
                            ri.Grammar = strSession[1];
                        }
                        else
                        {
                            //Se alguém não maniver o padrão de CallId.Grammar (tento extrair tudo junto CallIdGrammar)...Erro Aconteceu na Agiplan
                            if (strSession[0].Length > 16 )
                            {
                                ri.CallId = strSession[0].Substring(0, 16);
                                ri.Grammar = strSession[0].Remove(0, 16);
                            }
                            else
                            {
                                ri.CallId = strSession[0];
                            }
                        }
                     
                        break;
                    case "RENR":
                        ri.EndOfRecognition = (RENR)Enum.Parse(typeof(RENR), recInfo[i].Remove(0, 5), true);
                        break;
                    case "STEP":
                        ri.Step = recInfo[i].Remove(0, 5);
                        break;
                    case "TNAT":
                        ri.TNAT = recInfo[i].Remove(0, 5);
                        break;
                    case "NBST":
                        int nbst = 0;
                        int.TryParse(recInfo[i].Remove(0, 5), out nbst);
                        ri.NBST = nbst;
                        break;
                }
            }
            return ri;
        }

        private string TranslateMonth(string month)
        {
            string strMonth = "";
            switch (month)
            {
                case "01":
                    strMonth = "January";
                    break;
                case "02":
                    strMonth = "February";
                    break;
                case "03":
                    strMonth = "March";
                    break;
                case "04":
                    strMonth = "April";
                    break;
                case "05":
                    strMonth = "May";
                    break;
                case "06":
                    strMonth = "June";
                    break;
                case "07":
                    strMonth = "July";
                    break;
                case "08":
                    strMonth = "August";
                    break;
                case "09":
                    strMonth = "September";
                    break;
                case "10":
                    strMonth = "October";
                    break;
                case "11":
                    strMonth = "November";
                    break;
                case "12":
                    strMonth = "December";
                    break;
            }
            return strMonth;


        }

    }
}
