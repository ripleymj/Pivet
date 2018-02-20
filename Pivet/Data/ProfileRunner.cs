﻿using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Pivet.Data.Connection;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pivet.Data
{
    class ProfileRunner
    {
        static double lastProgress;
        public static Tuple<bool,string> Run(ProfileConfig profile, EnvironmentConfig config)
        {
            OracleConnection _conn;
            List<IDataProcessor> Processors = new List<IDataProcessor>();
            VersionState versionState;

            Stopwatch sw = new Stopwatch();
            sw.Start();
            Logger.Write($"Processing Environment '{config.Name}'");

            /* ensure root directory exists */
            Directory.CreateDirectory(profile.OutputFolder);

            VersionController versionController = new VersionController();
            versionController.InitRepository(profile.OutputFolder, profile.Repository);

            /* First thing is to get DB connection */
            var connectionProvider = config.Connection.Provider;
            Logger.Write("Getting database connection...");
            var providerType = Type.GetType("Pivet.Data.Connection." + connectionProvider + "Connection");
            if (providerType == null)
            {
                Logger.Write("Unable to find the specified DB provider.");
                return new Tuple<bool, string>(false, "Unable to find the specified DB provider.");
            }

            var dbProvider = Activator.CreateInstance(providerType) as IConnectionProvider;

            Dictionary<string, string> dbParams = new Dictionary<string, string>();
            dbProvider.SetParameters(config.Connection);
            var connectionResult = dbProvider.GetConnection();

            if (connectionResult.Item2 == false)
            {
                Logger.Write("Error connecting to database: " + connectionResult.Item3);
                return new Tuple<bool, string>(false, "Error connecting to database: " + connectionResult.Item3);
            }

            _conn = connectionResult.Item1;
            Logger.Write("Connected to Database.");

            /* Update our Version.txt file to track various version numbers */
            if (File.Exists(Path.Combine(profile.OutputFolder, "version.txt")))
            {
                versionState = JsonConvert.DeserializeObject<VersionState>(File.ReadAllText(Path.Combine(profile.OutputFolder, "version.txt")));
            }
            else
            {
                versionState = new VersionState();
            }

            versionState.UpdateAndSaveFromDB(_conn, profile.OutputFolder);

            /* run each processor */
            foreach (var provider in profile.DataProviders)
            {
                Type dataProvType = Type.GetType("Pivet.Data.Processors." + provider + "Processor");
                IDataProcessor processor = Activator.CreateInstance(dataProvType) as IDataProcessor;
                if (processor == null)
                {
                    Logger.Write("Could not find the data processor: " + provider);
                } else
                {
                    processor.ProgressChanged += Processor_ProgressChanged;
                    Processors.Add(processor);
                    //int itemCount = processor.LoadItems(_conn, config.Filters, config.ModifyThreshold,versionState);
                    int itemCount = processor.LoadItems(_conn, profile.Filters, versionState);
                    Logger.Write("Found " + itemCount + " " + provider + " Definitions");
                }
            }

            Logger.Write("Definitions collected.");

            Logger.Write("Processing deleted items.");
            List<ChangedItem> deletedItems = new List<ChangedItem>();
            foreach(var p in Processors)
            {
                deletedItems.AddRange(p.ProcessDeletes(profile.OutputFolder));
            }
            
            foreach(var item in deletedItems)
            {
                /* delete the file if on disk */
                if (File.Exists(item.FilePath))
                {
                    File.Delete(item.FilePath);
                }
            }

            Logger.Write("Processing new/changed items.");
            List<ChangedItem> changedItems = new List<ChangedItem>();
            foreach (var p in Processors)
            {
                Console.WriteLine($"Saving {p.GetType().Name.Replace("Processor","")} Definitions..." );
                Console.WriteLine();
                changedItems.AddRange(p.SaveToDisk(profile.OutputFolder));
                Console.CursorLeft = 0;
                Processor_ProgressChanged(new ProgressEvent() { Progress = 100 });
            }

            Logger.Write("Definitions saved to disk.");

            Logger.Write("Processing RawData Entries (" + profile.RawData.Count + ")");

            /* TODO: Process RawData Entries */

            foreach (RawDataEntry item in profile.RawData)
            {
                RawDataProcessor proc = new RawDataProcessor(_conn, profile.OutputFolder, item, profile.Filters.Prefixes);
                proc.ProgressChanged += Processor_ProgressChanged;
                changedItems.AddRange(proc.Process());

            }

            versionController.ProcessChanges(changedItems);
            sw.Stop();
            Logger.Write("Environment processed in: " + sw.Elapsed.TotalSeconds + " seconds");
            return new Tuple<bool, string>(true, "");
        }

        private static void Processor_ProgressChanged(ProgressEvent evt)
        {
            if (lastProgress != evt.Progress)
            {
                Console.CursorLeft = 0;
                Console.CursorTop--;

                Console.WriteLine("Progress: " + string.Format("{0:N2}%", evt.Progress));
                lastProgress = evt.Progress;
            }

        }
    }
}