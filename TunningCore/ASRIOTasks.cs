using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;

namespace TunningCore
{
    public static class ASRIOTasks
    {

        public static Int64 GetFreeSpaceFromNetworkShare(string serverName, string sharedFolder, string domain_user, string pass)
        {
            Int64 freeSpace = 0;

            List<string> shares = new List<string>();
            // do not use ConnectionOptions to get shares from local machine
            ConnectionOptions connectionOptions = new ConnectionOptions();
            if (!string.IsNullOrWhiteSpace(domain_user) && !string.IsNullOrWhiteSpace(pass))
            {
                connectionOptions.Username = domain_user;
                connectionOptions.Password = pass;
                connectionOptions.Impersonation = ImpersonationLevel.Impersonate;
            }

            ManagementScope scope = new ManagementScope("\\\\" + serverName + "\\root\\CIMV2", connectionOptions);
            bool connected = false;
            try
            {
                scope.Connect();
                connected = true;
            }
            catch
            {
                freeSpace = -1;
            }

            if (connected)
            {
                freeSpace = -2;
                ManagementObjectSearcher worker = new ManagementObjectSearcher(scope, new ObjectQuery("select Path from win32_share where Name = '" + sharedFolder + "'"));

                string localDrive = string.Empty;
                foreach (ManagementObject share in worker.Get())
                {
                    localDrive = share["Path"].ToString().Substring(0, 2); //Get Local Drive
                }

                if (!string.IsNullOrWhiteSpace(localDrive))
                {
                    freeSpace = -3;
                    //ObjectQuery query = new ObjectQuery("select * from Win32_LogicalDisk WHERE DriveType = 3 ");
                    ObjectQuery query = new ObjectQuery("select * from Win32_LogicalDisk WHERE DeviceID = '" + localDrive + "'");
                    ManagementObjectSearcher search = new ManagementObjectSearcher(scope, query);

                    foreach (ManagementObject o in search.Get())
                    {
                        freeSpace = Int64.Parse(o.Properties["FreeSpace"].Value.ToString());
                    }
                }
            }
            return freeSpace;
        }


    }
}
