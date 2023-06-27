using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TunningCore;
using TunningUtils;

namespace ConsoleApplication1
{
    class Program
    {

        public class Teste
        {
            public string Value { get; set; }
            public string InstallmentsQuantity { get; set; }
            public string TotalValue { get; set; }
            public string AdjustedValue { get; set; }
            public string PercentDiscount { get; set; }
        }


        static void Main(string[] args)
        {

            IFormatProvider theCultureInfo = new System.Globalization.CultureInfo("en-US", false);
            DateTime teste = DateTime.ParseExact(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), "yyyy-MM-dd HH:mm:ss", theCultureInfo);


            
            ASRTunningMonitor engine = new ASRTunningMonitor();
            engine.Start();
            Console.WriteLine("Inicializando");

            while (Console.ReadLine() != "ESC" )
            {

            }
            Console.WriteLine("Finalizando");
            engine.Stop();

        }
    }
}

