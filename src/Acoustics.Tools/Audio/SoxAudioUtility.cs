namespace Acoustics.Tools.Audio
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using log4net;
    using Shared;

    /// <summary>
    /// Soxi (sound exchange information) Audio utility.
    /// </summary>
    public class SoxAudioUtility : AbstractAudioUtility, IAudioUtility
    {
        private readonly bool enableShortNameHack;
        /*
         * Some things to test out/try:
         * stat - audio stats
         * stats - audio stats
         * spectrogram - create an image (has options to change)
         * trim - segment audio
         * fir - FFT with fir coefficients
         */

        /// <summary>
        /// Initializes a new instance of the <see cref="SoxAudioUtility"/> class.
        /// </summary>
        /// <param name="soxExe">
        /// The exe file.
        /// </param>
        /// <exception cref="FileNotFoundException">
        /// Could not find exe.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="soxExe"/> is <c>null</c>.
        /// </exception>
        public SoxAudioUtility(FileInfo soxExe)
        {
            this.CheckExe(soxExe, "sox");
            this.ExecutableModify = soxExe;
            this.ExecutableInfo = soxExe;
            this.ResampleQuality = SoxResampleQuality.VeryHigh;

            this.TemporaryFilesDirectory = TempFileHelper.TempDir();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SoxAudioUtility"/> class.
        /// </summary>
        /// <param name="soxExe">
        /// The exe file.
        /// </param>
        /// <param name="temporaryFilesDirectory">
        /// Which directory should hold temporary files.
        /// </param>
        /// <param name="enableShortNameHack">
        /// Whether or not filenames with unicode characters should be shortened
        /// to 8.3 filenames on Windows.
        /// </param>
        /// <exception cref="FileNotFoundException">
        /// Could not find exe.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="soxExe"/> is <c>null</c>.
        /// </exception>
        public SoxAudioUtility(FileInfo soxExe, DirectoryInfo temporaryFilesDirectory, bool enableShortNameHack = true)
        {
            this.enableShortNameHack = enableShortNameHack;
            this.CheckExe(soxExe, "sox");
            this.ExecutableModify = soxExe;
            this.ExecutableInfo = soxExe;
            this.ResampleQuality = SoxResampleQuality.VeryHigh;

            this.TemporaryFilesDirectory = temporaryFilesDirectory;
        }

        /// <summary>
        /// Resample quality.
        /// </summary>
        public enum SoxResampleQuality
        {
            /// <summary>
            /// −q
            /// bandwidth: n/a
            /// Rej dB: ~30 @ Fs/4
            /// Typical Use: playback on ancient hardware.
            /// </summary>
            Quick = 0,

            /// <summary>
            /// −l
            /// bandwidth: 80%
            /// Rej dB: 100
            /// Typical Use: playback on old hardware.
            /// </summary>
            Low = 1,

            /// <summary>
            /// −m
            /// bandwidth: 95%
            /// Rej dB: 100
            /// Typical Use: audio playback.
            /// </summary>
            Medium = 2,

            /// <summary>
            /// −h
            /// bandwidth: 125
            /// Rej dB: 125
            /// Typical Use: 16-bit mastering (use with dither).
            /// </summary>
            High = 3,

            /// <summary>
            /// −v
            /// bandwidth: 95%
            /// Rej dB: 175
            /// Typical Use: 24-bit mastering.
            /// </summary>
            VeryHigh = 4
        }

        /// <summary>
        /// Gets ResampleQuality.
        /// </summary>
        public SoxResampleQuality? ResampleQuality { get; private set; }

        /// <summary>
        /// Gets the valid source media types.
        /// </summary>
        protected override IEnumerable<string> ValidSourceMediaTypes => new[] { MediaTypes.MediaTypeWav, MediaTypes.MediaTypeMp3, MediaTypes.MediaTypeFlacAudio };

        /// <summary>
        /// Gets the invalid source media types.
        /// </summary>
        protected override IEnumerable<string> InvalidSourceMediaTypes => null;

        /// <summary>
        /// Gets the valid output media types.
        /// </summary>
        protected override IEnumerable<string> ValidOutputMediaTypes => new[] { MediaTypes.MediaTypeWav, MediaTypes.MediaTypeMp3 };

        /// <summary>
        /// Gets the invalid output media types.
        /// </summary>
        protected override IEnumerable<string> InvalidOutputMediaTypes => null;

        /// <summary>
        /// The construct modify args.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="output">
        /// The output.
        /// </param>
        /// <param name="request">
        /// The request.
        /// </param>
        /// <returns>
        /// The System.String.
        /// </returns>
        protected override string ConstructModifyArgs(FileInfo source, FileInfo output, AudioUtilityRequest request)
        {
            // resample - sox specific
            var resampleQuality = this.ResampleQuality.HasValue
                                      ? "-" + GetResampleQuality(this.ResampleQuality.Value)
                                      : string.Empty;

            // allow aliasing/imaging above the pass-band: -a
            // steep filter: -s

            // resample
            string rate = string.Empty;
            string repeatable = string.Empty;
            if (request.TargetSampleRate.HasValue)
            {
                var targetSampleRateHz = request.TargetSampleRate.Value.ToString(CultureInfo.InvariantCulture);
                rate = $"rate {resampleQuality} -s -a {targetSampleRateHz}";
                // the -R forces sox to run in repeatable mode. It makes SoX use the same seed for the random number generator
                repeatable = " -R ";
            }

            var remix = FormatChannelSelection(request);

            // offsets
            var trim = string.Empty;
            if (request.OffsetStart.HasValue && !request.OffsetEnd.HasValue)
            {
                trim = "trim " + request.OffsetStart.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }
            else if (!request.OffsetStart.HasValue && request.OffsetEnd.HasValue)
            {
                trim = "trim 0 " + request.OffsetEnd.Value.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }
            else if (request.OffsetStart.HasValue && request.OffsetEnd.HasValue)
            {
                var delta = request.OffsetEnd.Value.TotalSeconds - request.OffsetStart.Value.TotalSeconds;
                trim = FormattableString.Invariant($"trim {request.OffsetStart.Value.TotalSeconds} {delta}");
            }

            var bandpass = string.Empty;
            if (request.BandPassType != BandPassType.None)
            {
                switch (request.BandPassType)
                {
                    case BandPassType.Sinc:
                        bandpass += FormattableString.Invariant(
                            $"sinc {request.BandpassLow.Value / 1000}k-{request.BandpassHigh.Value / 1000}k");
                        break;
                    case BandPassType.Bandpass:
                        double width = request.BandpassHigh.Value - request.BandpassLow.Value;
                        var center = request.BandpassLow.Value + (width / 2.0);
                        bandpass += FormattableString.Invariant(
                            $"bandpass { center / 1000 }k { width / 1000 }k");
                        break;
                    case BandPassType.None:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // example
            // remix down to 1 channel, medium resample quality using steep filter with target sample rate of 11025hz
            // sox input.wav output.wav remix - rate -m -s 11025

            // −q, −−no−show−progress
            // Run in quiet mode when SoX wouldn’t otherwise do so. This is the opposite of the −S option.

            // SoX will encode complex files in the WAVE_FORMAT_EXTENSIBLE encoding by default - even if the source file way not WAVE_FORMAT_EXTENSIBLE.
            // Since we largely don't care about any of the advanced features provided by WAVE_FORMAT_EXTENSIBLE  we're going to tell sox to always output
            // WAVE_FORMAT_PCM.
            // More info:
            // https://sourceforge.net/p/sox/mailman/message/5863667/
            // http://www-mmsp.ece.mcgill.ca/documents/audioformats/wave/wave.html
            // http://sox.sourceforge.net/soxformat.html
            string forceOutput = string.Empty;
            if (MediaTypes.GetMediaType(output.Extension) == MediaTypes.MediaTypeWav)
            {
                forceOutput = "-t wavpcm ";
            }

            return $"{repeatable} -q -V4 \"{this.FixFilename(source)}\" {forceOutput}\"{output.FullName}\" {trim} {rate} {remix} {bandpass}";
        }

        private static string FormatChannelSelection(AudioUtilityRequest request)
        {
            /*
             * Where a range of channels is specified, the channel numbers to the left and right of the hyphen are
             * optional and default to 1 and to the number of input channels respectively. Thus
             *    sox input.wav output.wav remix −
             * performs a mix-down of all input channels to mono.
            */

            var remix = string.Empty;
            var mixDown = request.MixDownToMono.HasValue && request.MixDownToMono.Value;
            if (mixDown && request.Channels.NotNull())
            {
                // select a subset of channels
                remix = "remix " + string.Join(",", request.Channels);
            }
            else if (mixDown)
            {
                // mix down to mono
                remix = "remix -";
            }
            else if (request.Channels.NotNull())
            {
                // get channels but don't mix down
                remix = "remix " + string.Join(" ", request.Channels);
            }

            return remix;
        }

        /// <summary>
        /// The construct info args.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <returns>
        /// The System.String.
        /// </returns>
        protected override string ConstructInfoArgs(FileInfo source)
        {
            string args = $" --info -V4 \"{this.FixFilename(source)}\"";
            return args;
        }

        /// <summary>
        /// The get info.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        /// <param name="process">
        /// The process.
        /// </param>
        /// <returns>
        /// The Acoustics.Tools.AudioUtilityInfo.
        /// </returns>
        protected override AudioUtilityInfo GetInfo(FileInfo source, ProcessRunner process)
        {
            IEnumerable<string> errorlines = process.ErrorOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            // if no lines, or any line contains "no handler for file extension", return empty
            if (errorlines.Any(l => l.Contains("no handler for file extension")))
            {
                return new AudioUtilityInfo();
            }

            IEnumerable<string> lines = process.StandardOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

            var result = new AudioUtilityInfo();
            result.SourceFile = source;

            foreach (var line in lines)
            {
                var firstColon = line.IndexOf(':');

                if (firstColon > -1)
                {
                    var key = line.Substring(0, firstColon).Trim();
                    var value = line.Substring(firstColon + 1).Trim();

                    if (key == "Duration")
                    {
                        var values = value.Split('=', '~');

                        // duration
                        result.RawData.Add(key, values[0].Trim());

                        // sample count
                        result.RawData.Add("Samples", values[1].Replace("samples", string.Empty).Trim());

                        // approx. CDDA sectors
                        result.RawData.Add("CDDA sectors", values[2].Replace("CDDA sectors", string.Empty).Trim());
                    }
                    else
                    {
                        result.RawData.Add(key, value);
                    }
                }
            }

            // parse info info class
            const string keyDuration = "Duration";
            const string keySamples = "Samples";
            const string keyBitRate = "Bit Rate";
            const string keySampleRate = "Sample Rate";
            const string keyChannels = "Channels";
            const string keyPrecision = "Precision";

            if (result.RawData.ContainsKey(keySampleRate))
            {
                result.SampleRate = this.ParseIntStringWithException(result.RawData[keySampleRate], "sox.samplerate");
            }

            if (result.RawData.ContainsKey(keyDuration))
            {
                
                var sampleCount = result.RawData[keySamples];

                // most precise and efficient calculation, duration from sample count
                if (sampleCount.IsNotEmpty() && result.SampleRate.HasValue)
                {
                    var samples = this.ParseLongStringWithException(sampleCount, "Samples");
                    var duration = Math.Round((decimal)samples.Value / result.SampleRate.Value, 6, MidpointRounding.AwayFromZero);
                    result.Duration = TimeSpan.FromSeconds((double)duration);
                }
                else
                {
                    // less precise and problematic for very long recordings (because of timespan parsing)
                    // but still faster than another invocation
                    var stringDuration = result.RawData[keyDuration];
                    var formats = new[]
                    {
                        @"h\:mm\:ss\.ff", @"hh\:mm\:ss\.ff", @"h:mm:ss.ff",
                        @"hh:mm:ss.ff",
                    };

                    if (TimeSpan.TryParseExact(stringDuration.Trim(), formats, CultureInfo.InvariantCulture,
                        out var duration))
                    {
                        result.Duration = duration;
                    }
                    else
                    {
                        // last resort: ask sox specifically for the duration with another invocation
                        // AT: this case used to always happen even if it was not necessary
                        var extra = this.Duration(source);
                        if (extra.HasValue)
                        {
                            result.Duration = extra.Value;
                        }
                    }
                }
            }

            if (result.RawData.ContainsKey(keyChannels))
            {
                result.ChannelCount = this.ParseIntStringWithException(result.RawData[keyChannels], "sox.channels");
            }

            if (result.RawData.ContainsKey(keyPrecision))
            {
                result.BitsPerSample = this.ParseIntStringWithException(result.RawData[keyPrecision].Replace("-bit", string.Empty).Trim(), "sox.precision");
                if (result.BitsPerSample < 1)
                {
                    result.BitsPerSample = null;
                }
            }

            result.MediaType = GetMediaType(result.RawData, source.Extension);

            if (result.RawData.ContainsKey(keyBitRate))
            {
                var stringValue = result.RawData[keyBitRate];

                int magnitude = 1;

                if (stringValue.Contains("k"))
                {
                    stringValue = stringValue.Replace("k", string.Empty);
                    magnitude = 1000;
                }

                if (stringValue.Contains("M"))
                {
                    stringValue = stringValue.Replace("M", string.Empty);
                    magnitude = 1_000_000;
                }

                if (stringValue.Contains("G") || stringValue.Contains("T"))
                {
                    throw new NotSupportedException(
                        $"The file {source.FullName} reported a bit rate of {stringValue} which is not supported");
                }

                var value = double.Parse(stringValue, CultureInfo.InvariantCulture);

                value = value * magnitude;

                result.BitsPerSecond = Convert.ToInt32(value);

                // Further investigation reveals the 'error' I was chasing down was in fact the difference
                // between the FormatBitRate (averaged inc. header) and the StreamBitRate.
                // For long files this difference approaches 0, for short files the difference is significant
                //if (result.MediaType == MediaTypes.MediaTypeWav)
                //{
                //    // deal with inaccuracy - calculate it a second way
                //    var estimatedBitRate = result.SampleRate * result.ChannelCount * result.BitsPerSample;
                //    int roundedBitRate = (int)((double)estimatedBitRate).RoundToSignficantDigits(3);
                //    if (roundedBitRate != result.BitsPerSecond.Value)
                //    {
                //        throw new InvalidOperationException(
                //            $"SoxAudioUtlity could not accurately predict BitPerSecond. Parsed BitsPerSecond: {result.BitsPerSecond}, predicted: {estimatedBitRate}");
                //    }
                //
                //    // use estimated bit rate
                //    result.BitsPerSecond = estimatedBitRate;
                //}
            }

            return result;
        }

        protected override void CheckRequestValid(FileInfo source, string sourceMimeType, FileInfo output, string outputMediaType, AudioUtilityRequest request)
        {
            AudioUtilityInfo info = null;

            if (request?.BitDepth != null)
            {
                const string message = "Haven't added support for changing bit depth in" + nameof(SoxAudioUtility);
                throw new BitDepthOperationNotImplemented(message);
            }

            // check that if output is mp3, the bit rate and sample rate are set valid amounts.
            if (request != null && outputMediaType == MediaTypes.MediaTypeMp3)
            {
                if (request.TargetSampleRate.HasValue)
                {
                    // sample rate is set - check it
                    this.CheckMp3SampleRate(request.TargetSampleRate.Value);
                }
                else
                {
                    // sample rate is not set, get it from the source file
                    info = this.Info(source);
                    if (!info.SampleRate.HasValue)
                    {
                        throw new ArgumentException("Sample rate for output mp3 may not be correct, as sample rate is not set, and cannot be determined from source file.");
                    }

                    this.CheckMp3SampleRate(info.SampleRate.Value);
                }
            }

            if (request != null && request.Channels.NotNull())
            {
                if (request.Channels.Length == 0)
                {
                    throw new ChannelSelectionOperationNotImplemented("Sox utility cannot choose 0 channels");
                }

                int max = request.Channels.Max();
                int min = request.Channels.Min();
                info = info ?? this.Info(source);
                if (max > info.ChannelCount || min < 1)
                {
                    var msg = $"Requested channel number was out of range. Requested channel {max} but there are only {info.ChannelCount} channels in {source}.";
                    throw new ChannelNotAvailableException(nameof(request.Channels), request.Channels.ToCommaSeparatedList(), msg);
                }
            }
        }

        private static string GetResampleQuality(SoxResampleQuality rq)
        {
            switch (rq)
            {
                case SoxResampleQuality.Quick:
                    return "q";
                case SoxResampleQuality.Low:
                    return "l";
                case SoxResampleQuality.Medium:
                    return "m";
                case SoxResampleQuality.High:
                    return "h";
                case SoxResampleQuality.VeryHigh:
                    return "v";
                default:
                    return "m";
            }
        }

        private static TimeSpan Parse(string timeToParse)
        {
            try
            {
                string hours = timeToParse.Substring(0, 2);
                string minutes = timeToParse.Substring(3, 2);
                string seconds = timeToParse.Substring(6, 2);
                string fractions = timeToParse.Substring(9, 2);

                return new TimeSpan(
                    0, int.Parse(hours), int.Parse(minutes), int.Parse(seconds), int.Parse(fractions) * 10);
            }
            catch
            {
                return TimeSpan.Zero;
            }
        }

        private string FixFilename(FileInfo file)
        {
            var path = file.FullName;
            if (!this.enableShortNameHack)
            {
                return path;
            }

            if (!AppConfigHelper.IsWindows)
            {
                return path;
            }

            if (!PathUtils.HasUnicodeOrUnsafeChars(path))
            {
                return path;
            }

            var shortPath = PathUtils.GetShortFilename(path);
            this.Log.Trace(
                $"SoX unicode path bug avoided by shortening '{path}' to '{shortPath}'");

            return shortPath;
        }

        private AudioUtilityInfo SoxStats(FileInfo source)
        {
            /*
             * −w name
             * Window: Hann (default), Hamming, Bartlett, Rectangular or Kaiser
             *
             *
            sox "I:\Projects\QUT\QutSensors\sensors-trunk\QutSensors.Test\TestData\FemaleKoala MaleKoala.wav" -n
             * stat stats trim 0 60 spectrogram -m -r -l -w Bartlett -X 45 -y 257 -o
             * "I:\Projects\QUT\QutSensors\sensors-trunk\QutSensors.Test\TestData\FemaleKoala MaleKoala.png"
             * stats stat
             *
             * sox "I:\Projects\QUT\QutSensors\sensors-trunk\QutSensors.Test\TestData\FemaleKoala MaleKoala.wav" -n stat stats trim 0 60 spectrogram -m -r -l -w Bartlett -X 45 -y 257 -o "I:\Projects\QUT\QutSensors\sensors-trunk\QutSensors.Test\TestData\FemaleKoala MaleKoala.png" stats stat

sox "I:\Projects\QUT\QutSensors\sensors-trunk\QutSensors.Test\TestData\GParrots_JB2_20090607-173000.wav_minute_8.wav" -n trim 0 10 noiseprof | sox "I:\Projects\QUT\QutSensors\sensors-trunk\QutSensors.Test\TestData\GParrots_JB2_20090607-173000.wav_minute_8.wav" "I:\Projects\QUT\QutSensors\sensors-trunk\QutSensors.Test\TestData\GParrots_JB2_20090607-173000.wav_minute_8-reduced.wav" noisered

sox "I:\Projects\QUT\QutSensors\sensors-trunk\QutSensors.Test\TestData\GParrots_JB2_20090607-173000.wav_minute_8-reduced.wav" -n spectrogram -m -r -l -w Bartlett -X 45 -y 257 -o "I:\Projects\QUT\QutSensors\sensors-trunk\QutSensors.Test\TestData\GParrots_JB2_20090607-173000.wav_minute_8-reduced.png" stats stat

I:\Projects\QUT\QutSensors\sensors-trunk\Extra Assemblies\sox>sox "I:\Projects\QUT\QutSensors\sensors-trunk\QutSensors.Test\TestData\FemaleKoala MaleKoala.wav" -n trim 0 60  spectrogram -m -r -l -w Bartlett -X 45 -y 257 -o "I:\Projects\QUT\QutSensors\sensors-trunk\QutSensors.Test\TestData\FemaleKoal
a MaleKoala.png" -z 180 -q 100 stats stat noiseprof
             *
             * Could also do this for every minute of recording, using trim <start seconds> <end seconds> and looping.
            */

            this.CanProcess(source, null, null);

            IEnumerable<string> lines;
            using (var process = new ProcessRunner(this.ExecutableInfo.FullName))
            {
                string args = "\"" + source.FullName + "\" -n stat stats";

                this.RunExe(process, args, source.DirectoryName);

                if (this.Log.IsDebugEnabled)
                {
                    this.Log.Debug("Source " + this.BuildFileDebuggingOutput(source));
                }

                lines = process.ErrorOutput.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
            }

            // if no lines, or any line contains "no handler for file extension", return empty
            if (!lines.Any() || lines.Any(l => l.Contains("no handler for file extension")))
            {
                return new AudioUtilityInfo();
            }

            //

            // first 15 are split by colon (:)
            var statOutputRaw = lines.Take(15).Select(l => l.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries).Select(i => i.Trim()));
            var statOutput = statOutputRaw.Select(i => new KeyValuePair<string, string>(i.First(), i.Skip(1).First()));

            var results = statOutput.ToDictionary(item => item.Key, item => item.Value);

            lines = lines.Skip(15);

            // if there is a line that starts with 'Overall' (after being trimed), then count the number of words.
            // next 11 may have 1 value, may have more than one
            var isMoreThanOneChannel = lines.First().Trim().Contains("Overall");

            if (isMoreThanOneChannel)
            {
                var header = lines.First();
                var headerNames = header.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var numValues = headerNames.Count();

                lines = lines.Skip(1);

                for (var index = 0; index < 11; index++)
                {
                    string[] currentLine = lines.First().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                    string keyName = lines.First();
                    var tempHeaderCount = numValues;

                    while (tempHeaderCount > 0)
                    {
                        keyName = keyName.Substring(0, keyName.LastIndexOf(' ')).Trim();
                        tempHeaderCount--;
                    }

                    for (var headerIndex = 0; headerIndex < numValues; headerIndex++)
                    {
                        var value = currentLine[currentLine.Length - 1 - headerIndex];
                        var channelName = headerNames[numValues - 1 - headerIndex];
                        results.Add(keyName + " " + channelName, value);
                    }

                    lines = lines.Skip(1);
                }

            }

            // next 4 always 1 value
            foreach (var line in lines)
            {
                var index = line.Trim().LastIndexOf(' ');
                var keyName = line.Substring(0, index).Trim();
                var value = line.Substring(index).Trim();

                results.Add(keyName, value);
            }

            return new AudioUtilityInfo { RawData = results };
        }

        private TimeSpan? Duration(FileInfo source)
        {
            IEnumerable<string> lines;
            using (var process = new ProcessRunner(this.ExecutableInfo.FullName))
            {
                string args = " --info -D \"" + source.FullName + "\"";

                this.RunExe(process, args, source.DirectoryName);

                if (this.Log.IsDebugEnabled)
                {
                    this.Log.Debug("Source " + this.BuildFileDebuggingOutput(source));
                }

                IEnumerable<string> errorlines = process.ErrorOutput.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);

                // if no lines, or any line contains "no handler for file extension", return empty
                if (errorlines.Any(l => l.Contains("no handler for file extension")))
                {
                    return null;
                }

                lines = process.StandardOutput.Split(new[] {'\n', '\r'}, StringSplitOptions.RemoveEmptyEntries);
            }

            return TimeSpan.FromSeconds(this.ParseDoubleStringWithException(lines.FirstOrDefault(), "Duration").Value);
        }

        private static string GetMediaType(Dictionary<string, string> rawData, string extension)
        {
            var ext = extension.Trim('.');

            // separate stream and format
            var formats = rawData.Where(item => item.Key.Contains("Sample Encoding"));

            foreach (var item in formats)
            {
                switch (item.Value)
                {
                    case "MPEG audio (layer I, II or III)":
                        return MediaTypes.MediaTypeMp3;
                    case "Vorbis":
                        return MediaTypes.MediaTypeOggAudio;
                    case "16-bit Signed Integer PCM":
                        return MediaTypes.MediaTypeWav;
                    case "16-bit WavPack":
                        return MediaTypes.MediaTypeWavpack;
                    case "16-bit FLAC":
                        return MediaTypes.MediaTypeFlacAudio;
                    default:
                        return null;
                }
            }

            return null;
        }
    }
}
