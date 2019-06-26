// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PteropusSpecies.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//   This is a template recognizer for the Australian Flying Fox.
//   Since there are several species, this project is started using only the generic name for Flying Foxes.

// Proposed algorithm has 8 steps
// 1. Break long recordings into one-minute segments.
// 2. Convert each segment to a spectrogram.
// 3. Obtain a noise profile for each segment. This is to be used later to remove insect chorusing.
// 4. Scan the one-minute waveform and select "spike maxima" whose amplitude exceeds a decibel threshold, D.
// 5. Extract a single frame (say 512 samples) centred on each spike and convert to a spike spectrum.
// 6. Subtract the noise profile from the spike spectrum.
// 7. Smooth the remaining spectrum.
// 8. Look for evenly spaced harmonics in the smoothed spectrum.
// Typically the lowest harmonic will lie between 1200 Hz and 3000 Hz and the higher ones evenly spaced.
// This is the tricky bit due to variability but may work to use auto-correlation.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AnalysisPrograms.Recognizers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Acoustics.Shared;
    using Acoustics.Shared.ConfigFile;
    using Acoustics.Tools.Wav;
    using AnalysisBase;
    using AnalysisBase.ResultBases;
    using AnalysisPrograms.Recognizers.Base;
    using AudioAnalysisTools;
    using AudioAnalysisTools.DSP;
    using AudioAnalysisTools.Indices;
    using AudioAnalysisTools.StandardSpectrograms;
    using AudioAnalysisTools.WavTools;
    using log4net;
    using TowseyLibrary;

    /// <summary>
    /// This is a template recognizer for species of Flying Fox, Pteropus species
    /// </summary>
    internal class PteropusSpecies : RecognizerBase
    {
        public override string Author => "Towsey";

        public override string SpeciesName => "PteropusSpecies";

        public override string Description => "[STATUS DESCRIPTION] Detects acoustic events for species of Flying Fox, Pteropus species";

        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Summarize your results. This method is invoked exactly once per original file.
        /// </summary>
        public override void SummariseResults(
            AnalysisSettings settings,
            FileSegment inputFileSegment,
            EventBase[] events,
            SummaryIndexBase[] indices,
            SpectralIndexBase[] spectralIndices,
            AnalysisResult2[] results)
        {
            // No operation - do nothing. Feel free to add your own logic.
            base.SummariseResults(settings, inputFileSegment, events, indices, spectralIndices, results);
        }

        /// <summary>
        /// Do your analysis. This method is called once per segment (typically one-minute segments).
        /// </summary>
        /// <param name="audioRecording">one minute of audio recording.</param>
        /// <param name="configuration">config file.</param>
        /// <param name="segmentStartOffset">when recording starts.</param>
        /// <param name="getSpectralIndexes">not sure what this is.</param>
        /// <param name="outputDirectory">where the recogniser results can be found.</param>
        /// <param name="imageWidth"> assuming ????.</param>
        /// <returns>recogniser results.</returns>
        public override RecognizerResults Recognize(AudioRecording audioRecording, Config configuration, TimeSpan segmentStartOffset, Lazy<IndexCalculateResult[]> getSpectralIndexes, DirectoryInfo outputDirectory, int? imageWidth)
        {
            RecognizerResults results = Gruntwork(audioRecording, configuration, outputDirectory, segmentStartOffset);

            return results;
        }

        /// <summary>
        /// THis method does the work.
        /// </summary>
        /// <param name="audioRecording">the recording.</param>
        /// <param name="configuration">the config file.</param>
        /// <param name="outputDirectory">where results are to be put.</param>
        /// <param name="segmentStartOffset">where one segment is located in the total recording.</param>
        /// <returns>a list of events.</returns>
        internal static RecognizerResults Gruntwork(AudioRecording audioRecording, Config configuration, DirectoryInfo outputDirectory, TimeSpan segmentStartOffset)
        {
            // get the common properties
            string speciesName = configuration[AnalysisKeys.SpeciesName] ?? "<no species>";
            string abbreviatedSpeciesName = configuration[AnalysisKeys.AbbreviatedSpeciesName] ?? "<no.sp>";
            int minHz = configuration.GetIntOrNull(AnalysisKeys.MinHz) ?? 500;
            int maxHz = configuration.GetIntOrNull(AnalysisKeys.MaxHz) ?? 8000;

            //var samples = audioRecording.WavReader.Samples;
            //double minDuration = configuration.GetIntOrNull(AnalysisKeys.MinDuration) ?? 0.1;
            //double maxDuration = configuration.GetIntOrNull(AnalysisKeys.MaxDuration) ?? 0.5;
            var neighbourhoodDuration = TimeSpan.FromSeconds(0.05);

            double decibelsIntensityThreshold = configuration.GetDoubleOrNull(AnalysisKeys.NoiseBgThreshold) ?? 12.0;
            double intensityNormalisationMax = 3 * decibelsIntensityThreshold;
            var eventThreshold = decibelsIntensityThreshold / intensityNormalisationMax;

            //######################
            //2.Convert each segment to a spectrogram.
            //double noiseReductionParameter = configuration.GetDoubleOrNull(AnalysisKeys.NoiseBgThreshold) ?? 0.1;

            // make a spectrogram
            var sonoConfig = new SonogramConfig
            {
                WindowSize = 512,
                NoiseReductionType = NoiseReductionType.Standard,
                NoiseReductionParameter = configuration.GetDoubleOrNull(AnalysisKeys.NoiseBgThreshold) ?? 0.0,
            };
            sonoConfig.WindowOverlap = 0.0;

            // now construct the standard decibel spectrogram WITH noise removal, and look for LimConvex
            // get frame parameters for the analysis
            var sonogram = (BaseSonogram)new SpectrogramStandard(sonoConfig, audioRecording.WavReader);
            var intensityArray = SNR.CalculateFreqBandAvIntensity(sonogram.Data, minHz, maxHz, sonogram.NyquistFrequency);

            //var data = sonogram.Data;
            //var intensityArray = MatrixTools.GetRowAverages(data);
            intensityArray = DataTools.NormaliseInZeroOne(intensityArray, 0, intensityNormalisationMax);
            var plot = new Plot(speciesName, intensityArray, eventThreshold);
            var plots = new List<Plot> { plot };

            //iii: CONVERT decibel SCORES TO ACOUSTIC EVENTS
            var acousticEvents = AcousticEvent.GetEventsAroundMaxima(
                intensityArray,
                segmentStartOffset,
                minHz,
                maxHz,
                sonogram.FramesPerSecond,
                sonogram.FBinWidth,
                eventThreshold,
                neighbourhoodDuration);

            // ######################################################################
            acousticEvents.ForEach(ae =>
            {
                ae.SpeciesName = speciesName;
                ae.SegmentDurationSeconds = audioRecording.Duration.TotalSeconds;
                ae.SegmentStartSeconds = segmentStartOffset.TotalSeconds;
                ae.Name = abbreviatedSpeciesName;
            });

            //var sonoImage = sonogram.GetImageFullyAnnotated("Test");
            var sonoImage = SpectrogramTools.GetSonogramPlusCharts(sonogram, acousticEvents, plots, null);

            //var opPath =
            //    outputDirectory.Combine(
            //        FilenameHelpers.AnalysisResultName(
            //            Path.GetFileNameWithoutExtension(recording.BaseName), speciesName, "png", "DebugSpectrogram"));
            string imageFilename = audioRecording.BaseName + ".png";
            sonoImage.Save(Path.Combine(outputDirectory.FullName, imageFilename));

            return new RecognizerResults()
            {
                Events = acousticEvents,
                Hits = null,
                ScoreTrack = null,
                Plots = plots,
                Sonogram = sonogram,
            };
        }

        /*
            // example method
            private static List<AcousticEvent> RunFemaleProfile(configuration, rest of arguments)
            {
                const string femaleProfile = "Female";
                Config currentProfile = ConfigFile.GetProfile(configuration, femaleProfile);
                Log.Info($"Analyzing profile: {femaleProfile}");

                // extract parameters
                int minHz = (int)configuration[AnalysisKeys.MinHz];

                // ...

                // run the algorithm
                List<AcousticEvent> acousticEvents;
                Oscillations2012.Execute(All the correct parameters, minHz);

                // augment the returned events
                acousticEvents.ForEach(ae =>
                {
                    ae.SpeciesName = speciesName;
                    ae.Profile = femaleProfile;
                    ae.AnalysisIdealSegmentDuration = recordingDuration;
                    ae.Name = abbreviatedSpeciesName;
                });

                return acousticEvents;
            }
            */
    }
}