// <copyright file="SourceAndConfigArguments.cs" company="QutEcoacoustics">
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

    public abstract class SourceAndConfigArguments
        : SourceArguments
    {
        private static readonly ILog Log = LogManager.GetLogger(nameof(SourceAndConfigArguments));

        [Argument(
            1,
            Description = "The path to the config file. If not found we will search for the default config file of the same name.")]
        [Required]
        [LegalFilePath]
        public string Config { get; set; }

        public FileInfo ResolveConfigFile()
        {
            if (this.Config.IsNullOrEmpty())
            {
                throw new FileNotFoundException("No config file argument provided");
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