﻿using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pivet.Data
{
    public delegate void ProgressHandler(ProgressEvent evt);

    public class ProgressEvent : EventArgs
    {
        public double Progress { get; set; }

    }

    public interface IDataProcessor
    {
        //int LoadItems(OracleConnection conn, FilterConfig filters, int modifyThreshold, VersionState versionState);
        int LoadItems(OracleConnection conn, FilterConfig filters);
        void ProcessDeletes(string rootFolder);
        List<ChangedItem> SaveToDisk(string rootFolder);

        event ProgressHandler ProgressChanged;
        
        string ItemName { get; }

        string ProcessorID { get; }
    }
}
