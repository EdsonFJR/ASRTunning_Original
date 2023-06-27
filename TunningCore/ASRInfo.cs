using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TunningCore
{
    public enum RSTT : int
    {
        serr = 0, //A system error occurred.
        lowconf = 1, //There was an n-best result (including any possible decoys), but it was below the setting of the confidencelevel parameter.
        maxc = 2, //The maximum CPU time was reached (swirec_max_cpu_time).
        nomatch = 3, //There was no recognition match, and no n-best result.
        ok = 4, //Recognition was successful. There is an n-best result.
        stop = 5 //The recognizer received a stop request.
    }

    public enum ENDR : int
    {
        ctimeout = 0, //The end of speech was detected (completetimeout was triggered).
        eeos = 1, //External end of speech. The audio sample sent to the recognizer was labeled as the last sample.
        itimeout = 2, //Normal end of speech.
        maxs = 3, //The maximum speech time was reached (maxspeechtimeout).
        nobos = 4 //No beginning of speech detected.
    }

    public enum RENR : int
    {
        count = 0, //The maximum sentences were reached. (The max is determined by internal algorithms; this is not swirec_max_sentences.)
        err = 1, //A system error occurred.
        maxc = 2, //The maximum CPU time was reached.
        maxsrch = 3, //The recognizer’s maximum allowed search time was reached.
        maxsent = 4, //The number of sentences tried.
        ok = 5, //Recognition was successful. There is an n-best result.
        prun = 6, //Stopped generating the n-best list. This can occur whether or not there were any n-best entries returned. One cause is that the pruning threshold was exceeded (swirec_state_beam). But typically, it simply means that there were no more hypotheses to consider. For example, this happens if requesting an n-best size of n but the grammar has fewer than n choices. It will also happen if the recognizer has found a compelling acoustic match so that all the other hypotheses are pruned in the first pass search.
        stop = 7 //The recognizer received a stop request.
    }

    public class NBestResult
    {
        public string RSLT { get; set; }   //RSLT Sample TAG Return {meaning:doubt}
        //public string RAWT{ get; set; }  //RAWT
        public string SPOK { get; set; }  //SPOK
        public int CONF { get; set; } //CONF

    }

    public class RecognitionInfo
    {
        public string Time { get; set; } //TIME
        public RSTT Result { get; set; } //RSTT (
        public string Channel { get; set; }  //CHAN
        public ENDR EndOfSpeech { get; set; }  //ENDR
        public string ParsedResult { get; set; }   //RSLT Sample TAG Return {meaning:doubt}
        public string RawText { get; set; }  //RAWT
        public string NormalizedRawText { get; set; }  //SPOK
        public int Confidence { get; set; } //CONF
        public List<NBestResult> NBestResult { get; set; } 
        public string WaveFile { get; set; } //WVNM
        public string CallId { get; set; } //SESS == OlosPatern ==> CallId.Grammar
        public string Grammar { get; set; }  //SESS == OlosPatern ==> CallId.Grammar
        public RENR EndOfRecognition { get; set; } //RENR
        public string Tenant { get; set; }  //Usado para separar Sites (config.site)
        public string Step { get; set; }   //STEP
        public string TNAT { get; set; }  //TNAT
        public int NBST { get; set; }  //NBST
        public RecognitionInfo()
        {
            this.NBestResult = new List<NBestResult>();
        }

    }

    public enum TaskStatus : int
    {
        Queued = 0,
        Running = 1,
        PartialCompleted = 2,
        Completed = 3,
        Cancelled = 4,
        Error = 5,
        NoAvailableSpace = 6,
        NoSamplesFound = 7
    } 

    public class TunningTask
    {
        public int Id { get; set; }
        public DateTime Date { get; set; }
        public int UserId { get; set; }
        public int DesirableSamples { get; set; }
        public string Description { get; set; }
        public TaskStatus Status { get; set; }
        public DateTime StartPeriod { get; set; }
        public List<TaskDetail> Tasks { get; set; }
        public string TaskRegex { get; set; }

    }

    public class TaskDetail
    {
        public TaskType Type { get; set; }
        public string Value { get; set; }
        public string TNATRegex { get; set; }
    }

    public enum TaskType : int
    {
        AnyGrammar = 0,
        SpecificGrammar = 1
    }
    public enum ASR : int
    {
        Nuance = 0,
        Verbio = 1
    }

    public class TunningConfiguration
    {
        public int Interval { get; set; }
        public string RepositoryPath { get; set; }
        public string AlternativeRepositoryPath { get; set; }
        public Int64 MinimumAvailableSpace { get; set; }
        public bool Active { get; set; }
        public string NTDomainUser { get; set; }
        public string NTPassword { get; set; }
        public string Site { get; set; }
        public List<RecognitionServer> Servers { get; set; }
    }


    public class RecognitionServer
    {
        public int Id { get; set; }
        public string IP { get; set; }
        public string RootPath { get; set; }
        public bool Active { get; set; }
        public ASR ServerType { get; set; }
        public bool IsValid { get; set; }

    }

    public class DataBaseProperties
    {
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ApplicationName { get; set; }
    }
}
