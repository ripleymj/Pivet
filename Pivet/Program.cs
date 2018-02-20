﻿using Newtonsoft.Json;
using Pivet.Data;
using System;
using System.IO;
using System.Linq;

namespace Pivet
{
    class Program
    {
        public static Config GlobalConfig;
        static void Main(string[] args)
        {
            var configFile = "config.json";
            var profileToRun = "";

            if (args.Length > 1)
            {
                for (var x = 0; x < args.Length - 1; x++)
                {
                    if (args[x].ToLower().Equals("-c"))
                    {
                        configFile = args[x + 1];
                    }
                    if (args[x].ToLower().Equals("-p"))
                    {
                        profileToRun = args[x + 1];
                    }
                }
            }

            if (File.Exists(configFile) == false)
            {
                configFile = ConfigBuilder.RunBuilder();

                if (configFile == "")
                {
                    Logger.Error("Pivet cannot run without a configuration file.");
                    return;
                }
            }

            string j = File.ReadAllText(configFile);
            try
            {
                GlobalConfig = JsonConvert.DeserializeObject<Config>(j);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to parse config.json, please validate all required fields are present.");
                Console.ReadKey();
                return;
            }

            Logger.Write($"Config loaded. {GlobalConfig.Environments.Count} Environment(s) found, {GlobalConfig.Profiles.Count} Profile(s) found.");
            Logger.Write("Terminating for now... profile running is coming soon :)");
            
            foreach (var profile in GlobalConfig.Profiles)
            {
                if (profileToRun.Length > 0)
                {
                    if (profile.Name.Equals(profileToRun))
                    {
                        EnvironmentConfig environment = GlobalConfig.Environments.Where(e => e.Name.Equals(profile.EnvironmentName)).FirstOrDefault();
                        if (environment == null)
                        {
                            Logger.Error($"Could not run profile '{profileToRun}', unable to find environment named '{profile.EnvironmentName}'");
                            return;
                        }
                        else
                        {
                            ProfileRunner.Run(profile, environment);
                        }
                    }
                }
                else
                {
                    EnvironmentConfig environment = GlobalConfig.Environments.Where(e => e.Name.Equals(profile.EnvironmentName)).FirstOrDefault();
                    if (environment == null)
                    {
                        Logger.Error($"Could not run profile '{profileToRun}', unable to find environment named '{profile.EnvironmentName}'");
                    }
                    else
                    {
                        ProfileRunner.Run(profile, environment); 
                    }
                }

            }
            Logger.Write("All done!");
            Console.ReadKey();

        }
    }

    internal class Logger
    {
        internal static bool Quiet { get; set; }

        internal static void Write(string str)
        {
            if (!Quiet)
            {
                Console.WriteLine($"[MSG] {str}");
            }
        }

        internal static void Error(string str)
        {
            Console.WriteLine($"[ERR] {str}");
        }
    }
}