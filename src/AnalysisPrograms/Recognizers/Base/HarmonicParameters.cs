// --------------------------------------------------------------------------------------------------------------------
// <copyright file="HarmonicParameters.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>

namespace AnalysisPrograms.Recognizers.Base
{
    using System;
    using System.Collections.Generic;
    using Acoustics.Shared;
    using AudioAnalysisTools;
    using AudioAnalysisTools.StandardSpectrograms;
    using TowseyLibrary;

    /// <summary>
    /// TODO TODO: THIS METHOD IS WORK IN PROGESS AND CURRENTLY DOES YIELD A SUCCESSFUL RESULT. To BE FURTHER WORKED ON!!.
    /// Parameters needed from a config file to detect the stacked harmonic components of a soundscape.
    /// This can also be used for recognizing the harmonics of non-biological sounds such as from turbines, motor-bikes, compressors and other hi-revving motors.
    /// </summary>
    [YamlTypeTag(typeof(HarmonicParameters))]
    public class HarmonicParameters : CommonParameters
    {
        //ComponentName: Harmonic
        //SpeciesName: Curlew
        //FrameSize: 512
        //FrameStep: 512
        //WindowFunction: HANNING
        //BgNoiseThreshold: 0.0
        //# min and max of the freq band to search
        //MinHertz: 1000
        //MaxHertz: 6500
        //MinDuration: 0.5
        //MaxDuration: 3.0
        //DecibelThreshold: 1.5
        //# Parameters for the finding formants.
        //# duration of DCT in seconds
        //#DctDuration: 0.5
        //# minimum acceptable value of a DCT coefficient
        //DctThreshold: 0.5
        //MinFormantGap: 800
        //MaxFormantGap: 2200

        /// <summary>
        /// Gets or sets the dctThreshold.
        /// </summary>
        public double? DctThreshold { get; set; }

        /// <summary>
        /// Gets or sets the bottom bound of the rectangle. Units are Hertz.
        /// </summary>
        public int? MinFormantGap { get; set; }

        /// <summary>
        /// Gets or sets the the top bound of the rectangle. Units are Hertz.
        /// </summary>
        public int? MaxFormantGap { get; set; }

        //#IntensityThreshold: 0.15
        //# Event threshold - Determines FP / FN trade-off for events.
        //EventThreshold: 0.2

        public static (List<AcousticEvent>, double[]) GetComponentsWithHarmonics(
            SpectrogramStandard sonogram,
            int minHz,
            int maxHz,
            int nyquist,
            double decibelThreshold,
            double dctThreshold,
            double minDuration,
            double maxDuration,
            int minFormantGap,
            int maxFormantGap,
            TimeSpan segmentStartOffset)
        {

            // Event threshold - Determines FP / FN trade-off for events.
            //double eventThreshold = 0.2;

            var sonogramData = sonogram.Data;
            int frameCount = sonogramData.GetLength(0);
            int binCount = sonogramData.GetLength(1);

            double freqBinWidth = nyquist / (double)binCount;
            int minBin = (int)Math.Round(minHz / freqBinWidth);
            int maxBin = (int)Math.Round(maxHz / freqBinWidth);
            int bandWidthBins = maxBin - minBin + 1;

            // extract the sub-band
            double[,] subMatrix = MatrixTools.Submatrix(sonogram.Data, 0, minBin, frameCount - 1, maxBin);

            //ii: DETECT HARMONICS
            // now look for harmonics in search band using the Xcorrelation-DCT method.
            var results = CrossCorrelation.DetectHarmonicsInSonogramMatrix(subMatrix, decibelThreshold);

            // set up score arrays
            double[] dBArray = results.Item1;
            double[] harmonicIntensityScores = results.Item2; //an array of formant intesnity
            int[] maxIndexArray = results.Item3;

            for (int r = 0; r < frameCount; r++)
            {
                if (harmonicIntensityScores[r] < dctThreshold)
                {
                    continue;
                }

                //ignore locations with incorrect formant gap
                int maxId = maxIndexArray[r];
                double freqBinGap = 2 * bandWidthBins / (double)maxId;
                double formantGap = freqBinGap * freqBinWidth;
                if (formantGap < minFormantGap || formantGap > maxFormantGap)
                {
                    harmonicIntensityScores[r] = 0.0;
                }
            }

            // smooth the harmonicIntensityScores array to allow for brief gaps.
            harmonicIntensityScores = DataTools.filterMovingAverageOdd(harmonicIntensityScores, 5);

            //extract the events based on length and threshhold.
            // Note: This method does NOT do prior smoothing of the score array.
            var acousticEvents = AcousticEvent.ConvertScoreArray2Events(
                    harmonicIntensityScores,
                    minHz,
                    maxHz,
                    sonogram.FramesPerSecond,
                    sonogram.FBinWidth,
                    decibelThreshold,
                    minDuration,
                    maxDuration,
                    segmentStartOffset);

            return (acousticEvents, harmonicIntensityScores);
        }
    }
}
