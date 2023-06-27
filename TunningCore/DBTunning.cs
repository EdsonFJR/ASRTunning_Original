using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TunningCore.SQL;
using System.Data;
using TunningUtils;
using System.Reflection;
using System.Globalization;
using System.Web.Script.Serialization;

namespace TunningCore
{
 
    public class DBTunning
    {
        private DataBaseProperties dbProp;

        public DBTunning(DataBaseProperties dbProperties)
        {
            this.dbProp = dbProperties;
        }


        public TunningConfiguration GetTunningConfiguration()
        {
            TunningConfiguration config = GetConfig();
            if (config != null)
            {
                List<RecognitionServer> servers = GetRecognitionServers();
                if (servers != null)
                {
                    config.Servers = servers;
                }
            }
            return config;
        }

        private TunningConfiguration GetConfig()
        {
            SQLDBInterface db = new SQLDBInterface(dbProp);
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT TOP 1 [TaskInterval],[RepositoryPath],[AlternativeRepositoryPath],[Active],[MinimumAvailableSpace],[NTDomainUser],[NTPassword],[Site] FROM [dbo].[TunningConfiguration] WITH(NOLOCK)");
            db.cmm.CommandText = sb.ToString();

            TunningConfiguration config = new TunningConfiguration();
            try
            {
                db.Open();
                IDataReader dr = db.cmm.ExecuteReader(CommandBehavior.CloseConnection);
                if (dr.Read())
                {
                    config.Active = Convert.ToBoolean(dr["Active"]);
                    config.Interval = int.Parse(dr["TaskInterval"].ToString());
                    config.RepositoryPath = dr["RepositoryPath"].ToString();
                    config.AlternativeRepositoryPath = dr["AlternativeRepositoryPath"].ToString();
                    config.MinimumAvailableSpace = Int64.Parse(dr["MinimumAvailableSpace"].ToString());
                    config.NTDomainUser = string.Empty;
                    config.NTPassword = string.Empty;
                    if (dr["NTDomainUser"] != DBNull.Value)
                    {
                       Olos.Utils.EncryptDecrypt dec = new Olos.Utils.EncryptDecrypt();
                       config.NTDomainUser = dec.DecryptText(dr["NTDomainUser"].ToString());
                       if (dr["NTPassword"] != DBNull.Value)
                       {
                           config.NTPassword = dec.DecryptText(dr["NTPassword"].ToString());
                       }
                    }
                    config.Site = dr["Site"].ToString();
                }
                return config;
            }
            catch (Exception ex)
            {
                Logger.LogError("db", string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Empty, ex.Message);
                return null;
            }
            finally
            {
                db.Close();
                db = null;
            }
        }

        private List<RecognitionServer> GetRecognitionServers()
        {
            SQLDBInterface db = new SQLDBInterface(dbProp);
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT [ServerId],[IP],[CurrentTunningPath],[Active],[ServerType] FROM [dbo].[TunningRecognitionServers] WITH(NOLOCK)");
            db.cmm.CommandText = sb.ToString();

            List<RecognitionServer> servers = new List<RecognitionServer>();
            try
            {
                db.Open();
                IDataReader dr = db.cmm.ExecuteReader(CommandBehavior.CloseConnection);
                while (dr.Read())
                {
                    RecognitionServer server = new RecognitionServer();
                    server.Id = int.Parse(dr["ServerId"].ToString());
                    server.IP = dr["IP"].ToString();
                    server.RootPath = dr["CurrentTunningPath"].ToString();
                    server.Active = Convert.ToBoolean(dr["Active"]);
                    server.ServerType = (ASR)int.Parse(dr["ServerType"].ToString());
                    server.IsValid = false;
                    servers.Add(server);
                }
                return servers;
            }
            catch (Exception ex)
            {
                Logger.LogError("db", string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Empty, ex.Message);
                return null;
            }
            finally
            {
                db.Close();
                db = null;
            }
        }

        public List<TunningTask> GetTunnningTasks()
        {
            SQLDBInterface db = new SQLDBInterface(dbProp);
            StringBuilder sb = new StringBuilder();
            sb.Append("SELECT [Id],CONVERT(VARCHAR, [Time], 120) as [Time],[UserId],[DesirableSamples],Convert(VARCHAR,[StartPeriod],120) as [StartPeriod],[Status],[Type],[Values],[Description],[TNATRegex] FROM [dbo].[TunningTask] T (NOLOCK) ");
            sb.Append("        INNER JOIN [dbo].[TunningTaskDetail]  D (NOLOCK)	ON T.Id = D.TaskId WHERE [Status] = 0 ORDER BY [Time] ");
            db.cmm.CommandText = sb.ToString();

            List<TunningTask> tasks = new List<TunningTask>();

            int lastId = 0;
            TunningTask task = null;

            try
            {
                db.Open();
                IDataReader dr = db.cmm.ExecuteReader(CommandBehavior.CloseConnection);
                while (dr.Read())
                {
                    if (lastId != int.Parse(dr["Id"].ToString()))
                    {
                        if (task != null)
                        {
                            task.TaskRegex = GenerateRegex(task.Tasks);
                            tasks.Add(task);
                        }
                        task = new TunningTask();
                        task.Id = int.Parse(dr["Id"].ToString());
                        task.Date = DateTime.ParseExact(dr["Time"].ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        task.UserId = int.Parse(dr["UserId"].ToString());
                        task.DesirableSamples = int.Parse(dr["DesirableSamples"].ToString());
                        task.StartPeriod = DateTime.ParseExact(dr["StartPeriod"].ToString(), "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                        task.Status = (TaskStatus)int.Parse(dr["Status"].ToString());
                        task.Description = dr["Description"].ToString();
                        task.Tasks = new List<TaskDetail>();
                        lastId = task.Id;
                    }

                    TaskDetail taskDetail = new TaskDetail();
                    taskDetail.Type = (TaskType)int.Parse(dr["Type"].ToString());
                    taskDetail.Value = dr["Values"].ToString();
                    taskDetail.TNATRegex = dr["TNATRegex"].ToString();
                    task.Tasks.Add(taskDetail);
                }

                if (task != null)
                {
                    task.TaskRegex = GenerateRegex(task.Tasks);
                    tasks.Add(task);
                }

                return tasks;
            }
            catch (Exception ex)
            {
                Logger.LogError("db", string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Empty, ex.Message);
                return null;
            }
            finally
            {
                db.Close();
                db = null;
            }
        }

/*
        private string GenerateRegex(List<TaskDetail> list)
        {
            string defaultRegex = "";
            if (list != null)
            {
                if (list.Exists(x => x.Type == TaskType.AnyGrammar))
                {
                    defaultRegex = string.Empty;
                }
                else
                {
                    defaultRegex = string.Join("|", list.Select(x => x.Value.Replace("-", @"\-").Replace(".", @"\.").Replace("+", @"\+").Replace("(", @"\(").Replace(")", @"\)".Replace("[", @"\[").Replace("]", @"\]").Replace("{", @"\{").Replace("}", @"\}"))).ToArray());
                }
            }
            return defaultRegex;
        }
*/

        private string GenerateRegex(List<TaskDetail> list)
        {
            string defaultRegex = "";
            string taskSeparator = "";
            if (list != null)
            {
                foreach (TaskDetail item in list)
                {
                    if (item.Type == TaskType.AnyGrammar && string.IsNullOrWhiteSpace(item.TNATRegex))
                    {
                        defaultRegex = "";
                        break;
                    }
                    else if (item.Type == TaskType.AnyGrammar && !string.IsNullOrWhiteSpace(item.TNATRegex))
                    {
                        defaultRegex +=  string.Format("{0}(TNAT=({1})\\b)", taskSeparator, item.TNATRegex);
                    }
                    else if (item.Type == TaskType.SpecificGrammar && string.IsNullOrWhiteSpace(item.TNATRegex))
                    {
                        defaultRegex += string.Format("{0}({1}\\b)", taskSeparator, item.Value.Replace("-", @"\-").Replace(".", @"\.").Replace("+", @"\+").Replace("(", @"\(").Replace(")", @"\)".Replace("[", @"\[").Replace("]", @"\]").Replace("{", @"\{").Replace("}", @"\}")));
                    }
                    else if (item.Type == TaskType.SpecificGrammar && !string.IsNullOrWhiteSpace(item.TNATRegex))
                    {
                        defaultRegex += string.Format("{0}({1}\\b.+TNAT=({2})\\b)", taskSeparator, item.Value.Replace("-", @"\-").Replace(".", @"\.").Replace("+", @"\+").Replace("(", @"\(").Replace(")", @"\)".Replace("[", @"\[").Replace("]", @"\]").Replace("{", @"\{").Replace("}", @"\}")), item.TNATRegex );
                    }
                    taskSeparator = "|";
                }
            }
            return string.IsNullOrWhiteSpace(defaultRegex) ? defaultRegex : string.Format("({0})", defaultRegex); 
        }

        public bool InsertASRInfo(RecognitionInfo entryInfo, TunningTask taskdefinition, string originalPath, string currentPath, DateTime fileDate, string site)
        {
            string nbstJson = string.Empty;
            if (entryInfo.NBestResult != null)
            {
                nbstJson = new JavaScriptSerializer().Serialize(entryInfo.NBestResult);
            }
            SQLDBInterface db = new SQLDBInterface(dbProp);
            StringBuilder sb = new StringBuilder();
            sb.Append("INSERT INTO [dbo].[TunningData] ([Time],[Channel],[ENDR],[RSTT],[RSLT],[SPOK],[Confidence],[WVNM],[OriginalPath],[CurrentPath],[CallId],[Grammar],[Tenant],[Step],[TaskId],[TNAT],[NBST],[NBSTResult])");
            sb.AppendFormat("VALUES ('{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}','{8}','{9}','{10}','{11}','{12}','{13}','{14}','{15}','{16}','{17}') ", fileDate.ToString("yyyy-MM-dd HH:mm:ss.fff"), entryInfo.Channel, entryInfo.EndOfSpeech, entryInfo.Result, entryInfo.ParsedResult, entryInfo.NormalizedRawText, 
                                    entryInfo.Confidence, entryInfo.WaveFile, originalPath, currentPath, entryInfo.CallId, entryInfo.Grammar, site, entryInfo.Step, taskdefinition.Id, entryInfo.TNAT, entryInfo.NBST, nbstJson);
            db.cmm.CommandText = sb.ToString();

            try
            {
                db.Open();
                int rowsAffected = db.cmm.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError("db", string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Empty, ex.Message);
                return false;
            }
            finally
            {
                db.Close();
                db = null;
            }
        }

        public bool UpdateTaskStatus(TunningTask taskdefinition)
        {
            SQLDBInterface db = new SQLDBInterface(dbProp);
            StringBuilder sb = new StringBuilder();
            sb.AppendFormat("UPDATE [TunningTask] SET [Status] = {0} {1} {2} WHERE Id = {3}", (int)taskdefinition.Status, taskdefinition.Status == TaskStatus.Running ? ", StartDate = GETDATE()" : string.Empty,
                taskdefinition.Status != TaskStatus.Running ? ", EndDate = GETDATE() " : string.Empty, taskdefinition.Id);
            db.cmm.CommandText = sb.ToString();

            try
            {
                db.Open();
                int rowsAffected = db.cmm.ExecuteNonQuery();
                return rowsAffected > 0;
            }
            catch (Exception ex)
            {
                Logger.LogError("db", string.Format("Class:{0} Method:{1}", this.GetType().Name, MethodBase.GetCurrentMethod().Name), string.Empty, ex.Message);
                return false;
            }
            finally
            {
                db.Close();
                db = null;
            }
        }
    }
}
