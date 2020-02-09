// <copyright file="FileRenamer.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AnalysisPrograms
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Acoustics.Shared;
    using Acoustics.Tools.Audio;
    using log4net;
    using McMaster.Extensions.CommandLineUtils;
    using McMaster.Extensions.CommandLineUtils.Abstractions;
    using Production;
    using Production.Arguments;

    public class FileRenamer
    {
        public const string CommandName = "FileRenamer";

        [Command(
            CommandName,
            Description = "[UNMAINTAINED] Renames files based on modified and created date.")]
        public class Arguments : SubCommandBase
        {
            [Option(
                CommandOptionType.SingleValue,
                Description = "The directory containing audio files.")]
            [DirectoryExists]
            [Required]
            public virtual DirectoryInfo InputDir { get; set; }

            [Option(
                CommandOptionType.SingleValue,
                Description = "Specify the timezone (e.g. '+1000', '-0700').",
                ShortName = "z")]
            [Required]
            public TimeSpan Timezone { get; set; }

            [Option(Description = "Whether to recurse into subfolders.")]
            public bool Recursive { get; set; }

            [Option(
                Description = "Only print rename actions, don't actually rename files.",
                ShortName = "n")]
            public bool DryRun { get; set; }

            protected override ValidationResult OnValidate(ValidationContext context, CommandLineContext appContext)
            {
                return base.OnValidate(context, appContext);
            }

            public override Task<int> Execute(CommandLineApplication app)
            {
                FileRenamer.Execute(this);
                return this.Ok();
            }
        }

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public static void Execute(Arguments arguments)
        {
            if (arguments == null)
            {
                throw new NoDeveloperMethodException();
            }

            var validExtensions = new[] { ".wav", ".mp3", ".wv", ".ogg", ".wma" };
            var searchOption = arguments.Recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var dir = arguments.InputDir;
            var searchpattern = "*.*";

            var files = dir
                .EnumerateFiles(searchpattern, searchOption)
                .Where(f => validExtensions.Contains(f.Extension.ToLowerInvariant()))
                .OrderBy(f => f.Name)
                .ToArray();

            var parsedTimeZone =
                DateTimeOffset.Parse(
                    new DateTime(DateTime.Now.Ticks, DateTimeKind.Unspecified).ToString("O") + arguments.Timezone)
                    .Offset;
            var newFileNames = StartParallel(files, arguments.DryRun, parsedTimeZone);

            // print out mapping of original file name to new file name
            // only include file names that have changed
            for (var i = 0; i < newFileNames.Length; i++)
            {
                var originalName = files[i].FullName;
                var newName = newFileNames[i];

                if (originalName != newName)
                {
                    Log.InfoFormat("{0}, {1}", originalName, newName);
                }
            }

            Log.Info("Finished.");
        }

        /// <summary>
        /// Determine new files names and rename if not a dry run.
        /// </summary>
        /// <param name="files">Array of files.</param>
        /// <param name="isDryRun">Dry run or not.</param>
        /// <param name="timezone">Timezone string to use.</param>
        /// <returns>Array of file names in same order.</returns>
        private static string[] StartParallel(FileInfo[] files, bool isDryRun, TimeSpan timezone)
        {
            var count = files.Count();
            var results = new string[count];

            Parallel.ForEach(
                files,
                new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount },
                (item, state, index) =>
                {
                    var item1 = item;
                    var index1 = index;

                    var fileName = item1.Name;

                    var isDateTimeInFileName = FileDateHelpers.FileNameContainsDateTime(fileName);

                    if (isDateTimeInFileName)
                    {
                        results[index1] = item1.FullName;
                    }
                    else
                    {
                        results[index1] = Path.Combine(item1.DirectoryName, GetNewName(item1, timezone));

                        if (!isDryRun)
                        {
                            File.Move(item1.FullName, results[index1]);
                        }
                    }
                });

            return results;
        }

        private static string GetNewName(FileInfo file, TimeSpan timezone)
        {
            var fileName = file.Name;
            var fileLength = file.Length;
            var lastModified = file.LastWriteTime;
            var mediaType = MediaTypes.GetMediaType(file.Extension);

            var audioUtility = new MasterAudioUtility();
            var info = audioUtility.Info(file);
            var duration = info.Duration.HasValue ? info.Duration.Value : TimeSpan.Zero;

            var recordingStart = lastModified - duration;

            // some tweaking to get nice file names - round the minutes of last mod and duration
            // ticks are in 100-nanosecond intervals

            //var modifiedRecordingStart = lastModified.Round(TimeSpan.FromSeconds(15))
            //                             - duration.Round(TimeSpan.FromSeconds(15));

            //// DateTime rounded = new DateTime(((now.Ticks + 25000000) / 50000000) * 50000000);

            ////var roundedTotalSeconds = Math.Round(mediaFile.RecordingStart.TimeOfDay.TotalSeconds);
            ////var modifiedRecordingStart = mediaFile.RecordingStart.Date.AddSeconds(roundedTotalSeconds);

            var dateWithOffset = new DateTimeOffset(recordingStart, timezone);
            var dateTime = dateWithOffset.ToUniversalTime().ToString(AppConfigHelper.StandardDateFormatUtc);
            var ext = fileName.Substring(fileName.LastIndexOf('.') + 1).ToLowerInvariant();

            var prefix = fileName.Substring(0, fileName.LastIndexOf('.'));
            var result = string.Format("{0}_{1}.{2}", prefix, dateTime, ext);

            return result;
        }
    }
}
