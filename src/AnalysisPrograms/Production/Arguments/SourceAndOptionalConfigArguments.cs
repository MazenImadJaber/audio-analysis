// <copyright file="SourceAndOptionalConfigArguments.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AnalysisPrograms.Production.Arguments
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using Acoustics.Shared.ConfigFile;
    using log4net;
    using McMaster.Extensions.CommandLineUtils;

    public abstract class SourceAndOptionalConfigArguments
        : SourceArguments
    {
        private static readonly ILog Log = LogManager.GetLogger(nameof(SourceAndOptionalConfigArguments));

        [Option(
            Description = "The path to the config file. If not found a default config file will be loaded.")]
        [LegalFilePath]
        public string Config { get; set; }

        public FileInfo ResolveConfigFile<T>()
        {
            if (this.Config.IsNullOrEmpty())
            {
                Log.Info("Using default config file since no config file path was provided");
                return ConfigFile.Default<T>();
            }
            else if (File.Exists(this.Config))
            {
                return this.Config.ToFileInfo();
            }
            else
            {
                Log.Warn($"Config file `{this.Config}` not found... attempting to resolve config file");

                return ConfigFile.Resolve(this.Config, Directory.GetCurrentDirectory().ToDirectoryInfo());
            }
        }
    }
}