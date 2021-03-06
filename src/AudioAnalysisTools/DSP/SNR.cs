// <copyright file="SNR.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AudioAnalysisTools.DSP
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using StandardSpectrograms;
    using TowseyLibrary;
    using WavTools;

    // IMPORTANT NOTE: If you are converting Hertz to Mel scale, this conversion must be done BEFORE noise reduction

    public enum NoiseReductionType
    {
        None,
        Standard,
        Modal,
        Binary,
        FixedDynamicRange,
        Mean,
        Median,
        LowestPercentile,
        BriggsPercentile,
        ShortRecording,
        FlattenAndTrim,
    }

    public class SNR
    {
        // FractionalBoundForMode is used when estimating the modal noise value in a signal waveform.
        // It is used in method of Lmel et al to estimate the noise in an additive signal model.
        // It sets an upper limit on where the mode is located in the histogram of freq bin values
        public const double FractionalBoundForMode = 0.95;
        public const double FractionalBoundForLowPercentile = 0.2; // used when removing lowest percentile noise from a signal waveform

        /// <summary>
        /// Reference dB levels for a zero signal
        /// Used as minimum bound when normalising dB values.
        /// This value was based on the observation of an actual zero signal on an SM2.
        /// However, SM4 has a lower noise level. Consequently this value lowered to -100 on 7th August 2018.
        /// </summary>
        public const double MinimumDbBoundForZeroSignal = -100;

        /// <summary>
        /// Reference dB levels for recording of very silent environment
        /// Used as minimum bound when normalising dB values.
        /// This value was based on actual recording of cold winter morning (Gympie) using an SM2.
        /// However, SM4 has a lower noise level. Consequently this value lowered to -90 on 7th August 2018.
        /// </summary>
        public const double MinimumDbBoundForEnvironmentalNoise = -90;

        /// <summary>
        /// Reference logEnergies for signal segmentation, energy normalisation etc
        /// MinLogEnergyReference was changed from -6.0 to -8.0 on 6th August 2018 to accommodate signals with extended zero values.
        /// Typical noise value using Jason Wimmer original handheld recorders was = -4.5 = -45dB
        /// Typical noise value for quiet recordings with SM4 = -8.0 or -9.0, i.e. -80 to -90 dB
        /// </summary>
        public const double MinLogEnergyReference = -8.0;
        public const double MaxLogEnergyReference = 0.0;     // = Math.Log10(1.00) which assumes max frame amplitude = 1.0

        // number of noise standard deviations included in noise threshold - determines severity of noise reduction.
        public const double DefaultStddevCount = 0.0;

        //SETS MINIMUM DECIBEL BOUND when removing local backgroundnoise
        public const double DefaultNhBgThreshold = 2.0;

        public const string KeyNoiseReductionType = "NOISE_REDUCTION_TYPE";
        public const string KeyDynamicRange = "DYNAMIC_RANGE";

        /// <summary>
        /// Initializes a new instance of the <see cref="SNR"/> class.
        /// CONSTRUCTOR
        /// This constructor is called once from DSP_Frames.ExtractEnvelopeAndAmplSpectrogram().
        /// </summary>
        /// <param name="signal">signal </param>
        /// <param name="frameIDs">the start and end index of every frame</param>
        public SNR(double[] signal, int[,] frameIDs)
        {
            var logEnergy = CalculateLogEnergyOfsignalFrames(signal, frameIDs);
            this.FrameDecibels = ConvertLogEnergy2Decibels(logEnergy); //convert logEnergy to decibels. Just x10.

            // Calculate fraction of high energy frames PRIOR to noise removal.
            // Need to set a high energy threshold when measuring fraction of high energy frames.
            // IMPORTANT - This value ONLY applies BEFORE noise removal.
            // The value has been chosen somewhat arbitrarily.
            // It is relevant only when doing noise removal using Lamel et al algorithm
            double defaultHighEnergyThresholdInDecibel = -10.0;
            this.FractionOfHighEnergyFrames = CalculateFractionOfHighEnergyFrames(this.FrameDecibels, defaultHighEnergyThresholdInDecibel);

            this.SubtractBackgroundNoise_dB();
            this.NoiseRange = this.MinDb - this.NoiseSubtracted;
            this.MaxReferenceDecibelsWrtNoise = this.MaxDb - this.MinDb; // BEST BECAUSE TAKES NOISE LEVEL INTO ACCOUNT
        }

        public double FractionOfHighEnergyFrames { get; set; }

        /// <summary>
        /// Gets or sets the FrameDecibels array. Calculate as dB[i] = 10 x log-energy of frame[i].
        /// Appears only to be used to determine the fraction of high energy frames.
        /// </summary>
        public double[] FrameDecibels { get; set; }

        public double MinDb { get; set; }

        public double MaxDb { get; set; }

        public double NoiseSubtracted { get; set; }

        /// <summary>
        /// Gets or sets Snr.
        /// SNR = max_dB - Q
        /// that is, max decibels minus the modal noise
        /// This is notation used by Lamel et al.
        /// </summary>
        public double Snr { get; set; }

        // NoiseRange = this.MinDb - this.NoiseSubtracted
        public double NoiseRange { get; set; }

        //MaxReferenceDecibelsWrtNoise = this.MaxDb - this.MinDb
        public double MaxReferenceDecibelsWrtNoise { get; set; }

        //max reference dB wrt modal noise, where modal noise = 0.0dB. Used for normalisaion
        public double[] ModalNoiseProfile { get; set; }

        /// <summary>
        /// subtract background noise to produce a decibels array in which zero dB = modal noise
        /// DOES NOT TRUNCATE BELOW ZERO VALUES.
        /// </summary>
        public void SubtractBackgroundNoise_dB()
        {
            var results = SubtractBackgroundNoiseFromWaveform_dB(this.FrameDecibels, DefaultStddevCount);

            // after subtraction of background, the frame decibels have changed
            this.FrameDecibels = results.NoiseReducedSignal;
            this.NoiseSubtracted = results.NoiseSd; //Q
            this.MinDb = results.MinDb; //min decibels of all frames
            this.MaxDb = results.MaxDb; //max decibels of all frames
            this.Snr = results.Snr; // = max_dB - Q;
        }

        //# END CLASS METHODS ####################################################################################################################################
        //########################################################################################################################################################
        //# START STATIC METHODS TO DO WITH CALCULATIONS OF SIGNAL ENERGY AND DECIBELS############################################################################

        /// <summary>
        /// NormaliseMatrixValues the power values using the passed reference decibel level.
        /// NOTE: This method assumes that the energy values are in decibels and that they have been scaled
        /// so that the modal noise value = 0 dB. Simply truncate all values below this to zero dB
        /// </summary>
        public static double[] NormaliseDecibelArray_ZeroOne(double[] dB, double maxDecibels)
        {
            //NormaliseMatrixValues power between 0.0 decibels and max decibels.
            int length = dB.Length;
            var e = new double[length];
            for (int i = 0; i < length; i++)
            {
                e[i] = dB[i];
                if (e[i] <= 0.0)
                {
                    e[i] = 0.0;
                }
                else
                {
                    e[i] = dB[i] / maxDecibels;
                }

                if (e[i] > 1.0)
                {
                    e[i] = 1.0;
                }
            }

            return e;
        }

        /// <summary>
        /// Root Mean Square (RMS) Normalization
        /// Matrix is assumed to be a spectrogram
        /// Divides the spectrogram values by the RMS in order to adjust for varying levels of signal strength.
        /// </summary>
        public static double[,] RmsNormalization(double[,] matrix)
        {
            double sumOfSquares = 0;
            var normalizedMatrix = new double[matrix.GetLength(0), matrix.GetLength(1)];
            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    sumOfSquares += matrix[i, j] * matrix[i, j];
                }
            }

            double rms = Math.Sqrt(sumOfSquares / (matrix.GetLength(0) * matrix.GetLength(1)));

            for (int i = 0; i < matrix.GetLength(0); i++)
            {
                for (int j = 0; j < matrix.GetLength(1); j++)
                {
                    normalizedMatrix[i, j] = matrix[i, j] / rms;
                }
            }

            return normalizedMatrix;
        }

        /// <summary>
        /// Returns the log(energy) in each frame of the signal.
        /// The energy of a frame is the log of the summed energy of all the samples in the frame.
        /// Normally, if the passed frames are FFT spectra, then would multiply by 2 because spectra are symmetrical about Nyquist.
        /// BUT this method returns the AVERAGE sample energy, which therefore normalises for frame length / sample number.
        /// <para>
        /// Energy normalisation formula taken from Lecture Notes of Prof. Bryan Pellom
        /// Automatic Speech Recognition: From Theory to Practice.
        /// http://www.cis.hut.fi/Opinnot/T-61.184/ September 27th 2004.
        /// </para>
        /// Calculate normalised energy of frame as  energy[i] = logEnergy - maxLogEnergy;
        /// This is same as log10(logEnergy / maxLogEnergy) ie normalised to a fixed maximum energy value.
        /// </summary>
        public static double[] CalculateLogEnergyOfsignalFrames(double[] signal, int[,] frameIDs)
        {
            int frameCount = frameIDs.GetLength(0);

            //window or frame width
            int n = frameIDs[0, 1] + 1;
            var logEnergy = new double[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                // foreach sample in frame
                double sum = 0.0;
                for (int j = 0; j < n; j++)
                {
                    sum += Math.Pow(signal[frameIDs[i, 0] + j], 2); //sum the energy = amplitude squared
                }

                //NormaliseMatrixValues to frame size i.e. average energy per sample
                double e = sum / n;

                //LoggedConsole.WriteLine("e=" + e);
                //if (e > 0.25) LoggedConsole.WriteLine("e > 0.25 = " + e);

                //to guard against log(0) but this should never happen!
                if (e <= double.MinValue)
                {
                    LoggedConsole.WriteLine("DSP.SignalLogEnergy() Warning!!! Zero Energy in frame " + i);

                    // MaxLogEnergyReference = 0.0, i.e. where amplitude = 1.0.
                    logEnergy[i] = MinLogEnergyReference - MaxLogEnergyReference; //NormaliseMatrixValues to absolute scale
                    continue;
                }

                double logE = Math.Log10(e);

                //NormaliseMatrixValues to ABSOLUTE energy value i.e. as defined in header of Sonogram class
                // NOTE: MaxLogEnergyReference = 0.0, i.e. where amplitude = 1.0.
                if (logE < MinLogEnergyReference)
                {
                    logEnergy[i] = MinLogEnergyReference - MaxLogEnergyReference;
                }
                else
                {
                    logEnergy[i] = logE - MaxLogEnergyReference;
                }
            }

            //could alternatively NormaliseMatrixValues to RELATIVE energy value i.e. max frame energy in the current signal
            //double maxEnergy = logEnergy[DataTools.getMaxIndex(logEnergy)];
            //for (int i = 0; i < frameCount; i++) //foreach time step
            //{
            //    logEnergy[i] = ((logEnergy[i] - maxEnergy) * 0.1) + 1.0; //see method header for reference
            //}
            return logEnergy;
        }

        /// <summary>
        /// NOTE: This method is identical to the above one, except that the actual frames are passed
        /// rather than the starts and ends of the frames.
        /// </summary>
        public static double[] CalculateLogEnergyOfsignalFrames(double[,] frames)
        {
            int frameCount = frames.GetLength(0);
            int n = frames.GetLength(1);
            var logEnergy = new double[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                //sum over all samples in frame
                double sum = 0.0;
                for (int j = 0; j < n; j++)
                {
                    //sum the energy = amplitude squared
                    sum += frames[i, j] * frames[i, j];
                }

                //NormaliseMatrixValues to frame size i.e. average energy per sample
                double e = sum / n;

                //to guard against log(0) but this should never happen!
                if (e <= double.MinValue)
                {
                    LoggedConsole.WriteLine("DSP.SignalLogEnergy() Warning!!! Zero Energy in frame " + i);
                    logEnergy[i] = MinLogEnergyReference - MaxLogEnergyReference; //NormaliseMatrixValues to absolute scale
                    continue;
                }

                double logE = Math.Log10(e);

                //NormaliseMatrixValues to ABSOLUTE energy value i.e. as defined in header of Sonogram class
                if (logE < MinLogEnergyReference)
                {
                    logEnergy[i] = MinLogEnergyReference - MaxLogEnergyReference;
                }
                else
                {
                    logEnergy[i] = logE - MaxLogEnergyReference;
                }
            }

            return logEnergy;
        }

        public static double[] Signal2Decibels(double[] signal)
        {
            int length = signal.Length;
            var dB = new double[length];
            for (int i = 0; i < length; i++)
            {
                // 10 times log of amplitude squared
                dB[i] = 20 * Math.Log10(signal[i]);
            }

            return dB;
        }

        public static double[] ConvertLogEnergy2Decibels(double[] logEnergy)
        {
            var dB = new double[logEnergy.Length];
            for (int i = 0; i < logEnergy.Length; i++)
            {
                dB[i] = logEnergy[i] * 10; //Convert log energy to decibels.
            }

            return dB;
        }

        public static double[] DecibelsInSubband(double[,] dBMatrix, int minHz, int maxHz, double freqBinWidth)
        {
            int frameCount = dBMatrix.GetLength(0);
            int minBin = (int)(minHz / freqBinWidth);
            int maxBin = (int)(maxHz / freqBinWidth);
            var db = new double[frameCount];

            // foreach frame
            for (int i = 0; i < frameCount; i++)
            {
                // foreach bin in the bandwidth in frame
                double sum = 0.0;
                for (int j = minBin; j <= maxBin; j++)
                {
                    sum += dBMatrix[i, j]; // sum the dB values
                }

                db[i] = sum;
            }

            return db;
        }

        /// <summary>
        /// returns a spectrogram with reduced number of frequency bins
        /// </summary>
        /// <param name="inSpectro">input spectrogram</param>
        /// <param name="subbandCount">numbre of req bands in output spectrogram</param>
        public static double[,] ReduceFreqBinsInSpectrogram(double[,] inSpectro, int subbandCount)
        {
            int frameCount = inSpectro.GetLength(0);
            int n = inSpectro.GetLength(1);
            var outSpectro = new double[frameCount, subbandCount];
            int binWidth = n / subbandCount;

            // for each frame
            for (int i = 0; i < frameCount; i++)
            {
                int startBin;
                double sum = 0.0;

                // foreach output band EXCEPT THE LAST
                for (int j = 0; j < subbandCount - 1; j++)
                {
                    startBin = j * binWidth;
                    var endBin = startBin + binWidth;

                    // foreach output band sum the spectral values
                    for (int b = startBin; b < endBin; b++)
                    {
                        sum += inSpectro[i, b];
                    }

                    outSpectro[i, j] = sum;
                }

                //now do the top most freq band
                startBin = (subbandCount - 1) * binWidth;

                // foreach output band
                for (int b = startBin; b < n; b++)
                {
                    sum += inSpectro[i, b]; // sum the spectral values
                }

                outSpectro[i, subbandCount - 1] = sum;
            }

            return outSpectro;
        }

        public static List<int[]> SegmentArrayOfIntensityvalues(double[] values, double threshold, int minLength)
        {
            int count = values.Length;
            var events = new List<int[]>();
            bool isHit = false;
            int startId = 0;

            //pass over all elements in array
            for (int i = 0; i < count; i++)
            {
                if (isHit == false && values[i] > threshold)
                {
                    //start of an event
                    isHit = true;
                    startId = i;
                }
                else
                {
                    // check for the end of an event
                    if (isHit && values[i] <= threshold)
                    {
                        //this is end of an event, so initialise it
                        isHit = false;
                        int endId = i;
                        int segmentLength = endId - startId + 1;
                        if (segmentLength < minLength)
                        {
                            continue; //skip events with duration shorter than threshold
                        }

                        var ev = new int[2];
                        ev[0] = startId;
                        ev[1] = endId;
                        events.Add(ev);
                    }
                }
            }

            return events;
        } //end SegmentArrayOfIntensityvalues()

        // #######################################################################################################################################################
        // STATIC METHODS TO DO WITH SUBBAND of a SPECTROGRAM
        // #######################################################################################################################################################

        /// <param name="sonogram">sonogram of signal - values in dB</param>
        /// <param name="minHz">min of freq band to sample</param>
        /// <param name="maxHz">max of freq band to sample</param>
        /// <param name="nyquist">signal nyquist - used to caluclate hz per bin</param>
        /// <param name="smoothDuration">window width (in seconds) to smooth sig intenisty</param>
        /// <param name="framesPerSec">time scale of the sonogram</param>
        public static Tuple<double[], double, double> SubbandIntensity_NoiseReduced(
            double[,] sonogram, int minHz, int maxHz, int nyquist, double smoothDuration, double framesPerSec)
        {
            //A: CALCULATE THE INTENSITY ARRAY
            double[] intensity = CalculateFreqBandAvIntensity(sonogram, minHz, maxHz, nyquist);

            //B: SMOOTH THE INTENSITY ARRAY
            int smoothWindow = (int)Math.Round(framesPerSec * smoothDuration);
            if (smoothWindow != 0 && smoothWindow % 2 == 0)
            {
                smoothWindow += 1; //Convert to odd number for smoothing
            }

            intensity = DataTools.filterMovingAverage(intensity, smoothWindow);

            //C: REMOVE NOISE FROM INTENSITY ARRAY
            double standardDeviationCount = 0.1; // number of noise SDs to calculate noise threshold - determines severity of noise reduction
            BackgroundNoise bgn = SubtractBackgroundNoiseFromSignal(intensity, standardDeviationCount);
            var tuple = Tuple.Create(bgn.NoiseReducedSignal, bgn.NoiseMode, bgn.NoiseSd);
            return tuple;
        }

        /// <summary>
        /// Calculates the mean intensity in a freq band defined by its min and max freq.
        /// THis method averages dB log values incorrectly but it is faster than doing many log conversions.
        /// This method is used to find acoustic events and is accurate enough for the purpose.
        /// </summary>
        public static double[] CalculateFreqBandAvIntensity(double[,] sonogram, int minHz, int maxHz, int nyquist)
        {
            int frameCount = sonogram.GetLength(0);
            int binCount = sonogram.GetLength(1);
            double binWidth = nyquist / (double)binCount;
            int minBin = (int)Math.Round(minHz / binWidth);
            int maxBin = (int)Math.Round(maxHz / binWidth);
            int binCountInBand = maxBin - minBin + 1;
            double[] intensity = new double[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                for (int j = minBin; j < maxBin; j++)
                {
                    intensity[i] += sonogram[i, j];
                }

                intensity[i] /= binCountInBand;
            }

            return intensity;
        }

        /// <summary>
        /// Calculates the average intensity in a freq band having min and max freq,
        /// AND then subtracts average intensity in the side/buffer bands, below and above.
        /// THis method adds dB log values incorrectly but it is faster than doing many log conversions.
        /// This method is used to find acoustic events and is accurate enough for the purpose.
        /// </summary>
        public static double[] CalculateFreqBandAvIntensityMinusBufferIntensity(double[,] sonogramData, int minHz, int maxHz, int bottomHzBuffer, int topHzBuffer, int nyquist)
        {
            var bandIntensity = SNR.CalculateFreqBandAvIntensity(sonogramData, minHz, maxHz, nyquist);
            var bottomSideBandIntensity = SNR.CalculateFreqBandAvIntensity(sonogramData, minHz - bottomHzBuffer, minHz, nyquist);
            var topSideBandIntensity = SNR.CalculateFreqBandAvIntensity(sonogramData, maxHz, maxHz + topHzBuffer, nyquist);

            int frameCount = sonogramData.GetLength(0);
            double[] netIntensity = new double[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                netIntensity[i] = bandIntensity[i] - bottomSideBandIntensity[i] - topSideBandIntensity[i];
            }

            return netIntensity;
        }

        /// <summary>
        /// Calculates the average intensity in a freq band having min and max freq,
        /// AND then subtracts average intensity in the side/buffer bands, below and above.
        /// THis method adds dB log values incorrectly but it is faster than doing many log conversions.
        /// This method is used to find acoustic events and is accurate enough for the purpose.
        /// </summary>
        public static double[] CalculateWhistleIntensity(double[,] sonogramData, int minHz, int maxHz, int bottomHzBuffer, int topHzBuffer, int nyquist)
        {
            var bandIntensity = SNR.CalculateFreqBandAvIntensity(sonogramData, minHz, maxHz, nyquist);
            var bottomSideBandIntensity = SNR.CalculateFreqBandAvIntensity(sonogramData, minHz - bottomHzBuffer, minHz, nyquist);
            var topSideBandIntensity = SNR.CalculateFreqBandAvIntensity(sonogramData, maxHz, maxHz + topHzBuffer, nyquist);

            int frameCount = sonogramData.GetLength(0);
            double[] netIntensity = new double[frameCount];
            for (int i = 0; i < frameCount; i++)
            {
                netIntensity[i] = bandIntensity[i] - bottomSideBandIntensity[i] - topSideBandIntensity[i];
            }

            return netIntensity;
        }

        private static double CalculateFractionOfHighEnergyFrames(double[] dbArray, double dbThreshold)
        {
            int length = dbArray.Length;
            int count = 0;
            for (int i = 0; i < length; i++)
            {
                if (dbArray[i] > dbThreshold)
                {
                    count++;
                }
            }

            return count / (double)length;
        }

        // ########################################################################################################################################################
        // # NEXT FOUR METHODS USED TO CALCULATE SNR OF SHORT RECORDINGS  #########################################################################################
        // # INFO USED FOR XUEYAN QUERIES.                                #########################################################################################
        // ########################################################################################################################################################

        /// <summary>
        /// The calculation of SNR in this method assumes that background noise has already been removed.
        /// That is, the maximum value is with respect to zero.
        /// SNR should be calculated based on power values in decibel units.
        ///     i.e. SNR = 10log(PowerOfSignal / PowerOfNoise);
        ///     or   SNR = 20log(Signal amplitude) - 20log(Noise amplitude);
        ///     If the passed sonogram data is amplitude or energy values (rather than decibel values) then the returned SNR value needs to be appropriately corrected.
        /// </summary>
        public static SnrStatistics CalculateSnrInFreqBand(double[,] sonogramData, int startframe, int frameSpan, int minBin, int maxBin, double threshold)
        {
            int frameCount = sonogramData.GetLength(0);
            double[,] bandSonogram = MatrixTools.Submatrix(sonogramData, 0, minBin, frameCount - 1, maxBin);

            // estimate low energy content independently for each freq bin.
            // estimate from the lowest quintile (20%) of frames in the bin.
            int lowEnergyFrameCount = frameCount / 5;
            int binCountInBand = maxBin - minBin + 1;

            double[,] callMatrix = new double[frameSpan, binCountInBand];

            // loop over all freq bins in the band
            for (int bin = 0; bin < binCountInBand; bin++)
            {
                double[] freqBin = MatrixTools.GetColumn(bandSonogram, bin);
                double[] orderedArray = (double[])freqBin.Clone();
                Array.Sort(orderedArray);

                double sum = 0.0;
                for (int i = 0; i < lowEnergyFrameCount; i++)
                {
                    sum += orderedArray[i];
                }

                double bgnEnergyInBin = sum / lowEnergyFrameCount;

                // NOW get the required time frame
                double[] callBin = DataTools.Subarray(freqBin, startframe, frameSpan);

                // subtract the background noise
                for (int i = 0; i < callBin.Length; i++)
                {
                    callBin[i] -= bgnEnergyInBin;
                }

                MatrixTools.SetColumn(callMatrix, bin, callBin);
            }

            // now calculate SNR from the call matrix
            double snr = 0.0;
            for (int frame = 0; frame < frameSpan; frame++)
            {
                for (int bin = 0; bin < binCountInBand; bin++)
                {
                    if (callMatrix[frame, bin] > snr)
                    {
                        snr = callMatrix[frame, bin];
                    }
                }
            }

            // now calculate % of frames having high energy.
            // only count cells which actually have activity
            double[] frameAverages = new double[frameSpan];
            for (int frame = 0; frame < frameSpan; frame++)
            {
                int count = 0;
                double sum = 0.0;
                for (int bin = 0; bin < binCountInBand; bin++)
                {
                    if (callMatrix[frame, bin] > 0.0)
                    {
                        count++;
                        sum += callMatrix[frame, bin];
                    }
                }

                frameAverages[frame] = sum / count;
            }

            // count the number of spectrogram frames where the energy exceeds the threshold
            double thirdSnr = snr * 0.3333;
            int framesExceedingThreshold = 0;
            int framesExceedingThirdSnr = 0;
            for (int frame = 0; frame < frameSpan; frame++)
            {
                if (frameAverages[frame] > threshold)
                {
                    framesExceedingThreshold++;
                }

                if (frameAverages[frame] > thirdSnr)
                {
                    framesExceedingThirdSnr++;
                }
            }

            var stats = new SnrStatistics
            {
                Threshold = threshold,
                Snr = snr,
                FractionOfFramesExceedingThreshold = framesExceedingThreshold / (double)frameSpan,
                FractionOfFramesExceedingOneThirdSnr = framesExceedingThirdSnr / (double)frameSpan,
            };

            return stats;
        }

        /// <summary>
        /// Calculates the matrix row/column bounds given the real world bounds.
        /// Axis scales are obtained form the passed sonogram instance.
        /// </summary>
        public static SnrStatistics CalculateSnrInFreqBand(BaseSonogram sonogram, TimeSpan startTime, TimeSpan extractDuration, int minHz, int maxHz, double threshold)
        {
            // calculate temporal bounds
            int frameCount = sonogram.Data.GetLength(0);
            double frameDuration = sonogram.FrameDuration;

            //take a bit extra afound the given temporal bounds
            int bufferFrames = (int)Math.Round(0.25 / frameDuration);

            // calculate temporal bounds
            int startFrame = (int)Math.Round(startTime.TotalSeconds / frameDuration) - bufferFrames;
            int frameSpan = (int)Math.Round(extractDuration.TotalSeconds / frameDuration) + bufferFrames;
            if (startFrame < 0)
            {
                startFrame = 0;
            }

            int endframe = startFrame + frameSpan;
            if (endframe >= frameCount)
            {
                frameSpan = frameSpan - (endframe - frameCount) - 1;
            }

            // calculate frequency bounds
            int binCount = sonogram.Data.GetLength(1);
            double binWidth = sonogram.NyquistFrequency / (double)binCount;
            int bufferBins = (int)Math.Round(500 / binWidth);

            int lowFreqBin = (int)Math.Round(minHz / binWidth) - bufferBins;
            int hiFreqBin = (int)Math.Round(maxHz / binWidth) + bufferBins;
            if (lowFreqBin < 0)
            {
                lowFreqBin = 0;
            }

            if (hiFreqBin >= binCount)
            {
                hiFreqBin = binCount - 1;
            }

            SnrStatistics stats = CalculateSnrInFreqBand(sonogram.Data, startFrame, frameSpan, lowFreqBin, hiFreqBin, threshold);
            stats.ExtractDuration = sonogram.Duration;
            if (extractDuration < sonogram.Duration)
            {
                stats.ExtractDuration = extractDuration;
            }

            return stats;
        }

        /// <summary>
        /// This method written 18-09-2014 to process Xueyan's query recordings.
        /// Calculate the SNR statistics for each recording and then write info back to csv file
        /// </summary>
        public static SnrStatistics Calculate_SNR_ShortRecording(FileInfo sourceRecording, Dictionary<string, string> configDict, TimeSpan start, TimeSpan duration, int minHz, int maxHz, double threshold)
        {
            configDict["NoiseReductionType"] = "None";

            // 1) get recording
            var recordingSegment = new AudioRecording(sourceRecording.FullName);
            var sonoConfig = new SonogramConfig(configDict); // default values config

            // 2) get decibel spectrogram
            BaseSonogram sonogram = new SpectrogramStandard(sonoConfig, recordingSegment.WavReader);

            // remove the DC column
            sonogram.Data = MatrixTools.Submatrix(sonogram.Data, 0, 1, sonogram.Data.GetLength(0) - 1, sonogram.Data.GetLength(1) - 1);

            return CalculateSnrInFreqBand(sonogram, start, duration, minHz, maxHz, threshold);
        }

        // ########################################################################################################################################################
        // # START STATIC METHODS TO DO WITH NOISE REDUCTION FROM WAVEFORMS E.g. not spectrograms. See further below for spectrograms #############################
        // ########################################################################################################################################################

        /// <summary>
        /// Calls method to implement the "Adaptive Level Equalisatsion" algorithm of (Lamel et al, 1981)
        /// It has the effect of setting background noise level to 0 dB.
        /// Passed signal array MUST be in deciBels.
        /// ASSUMES an ADDITIVE MODEL with GAUSSIAN NOISE.
        /// Calculates the average and standard deviation of the noise and then calculates a noise threshold.
        /// Then subtracts threshold noise from the signal - so now zero dB = threshold noise
        /// Sets default values for min dB value and the noise threshold. 10 dB is a default used by Lamel et al.
        /// RETURNS: 1) noise reduced decibel array; 2) Q - the modal BG level; 3) min value 4) max value; 5) snr; and 6) SD of the noise
        /// </summary>
        ///
        public static BackgroundNoise SubtractBackgroundNoiseFromWaveform_dB(double[] dBarray, double sdCount)
        {

            // Implements the algorithm in Lamel et al, 1981.
            NoiseRemovalModal.CalculateNoiseUsingLamelsAlgorithm(dBarray, out var minDb, out var maxDb, out var noiseMode, out var noiseSd);

            // subtract noise.
            var threshold = noiseMode + (noiseSd * sdCount);
            double snr = maxDb - threshold;
            double[] dBFrames = SubtractAndTruncate2Zero(dBarray, threshold);
            return new BackgroundNoise()
            {
                NoiseReducedSignal = dBFrames,
                NoiseMode = noiseMode,
                MinDb = minDb,
                MaxDb = maxDb,
                Snr = snr,
                NoiseSd = noiseSd,
                NoiseThreshold = threshold,
            };
        }

        /// <summary>
        /// Calculates and subtracts the background noise value from an array of double.
        /// Used for calculating and removing the background noise and setting baseline = zero.
        /// Implements a MODIFIED version of Lamel et al. They only search in range 10dB above min dB whereas
        /// this method sets upper limit to 66% of range of intensity values.
        /// ASSUMES ADDITIVE MODEL with GAUSSIAN NOISE.
        /// Values below zero set equal to zero.
        /// This method can be called for any array of signal values but is PRESUMED TO BE A WAVEFORM or FREQ BIN OF HISTOGRAM
        /// </summary>
        public static BackgroundNoise SubtractBackgroundNoiseFromSignal(double[] array, double sdCount)
        {
            BackgroundNoise bgn = CalculateModalBackgroundNoiseInSignal(array, sdCount);
            bgn.NoiseReducedSignal = SubtractAndTruncate2Zero(array, bgn.NoiseThreshold);
            return bgn;
        }

        public static BackgroundNoise CalculateModalBackgroundNoiseInSignal(double[] array, double sdCount)
        {
            // create histogram whose width is adjusted to length of signal
            int binCount = array.Length / 4;
            if (binCount > 500)
            {
                binCount = 500;
            }

            double min, max, binWidth;
            int[] histo = Histogram.Histo(array, binCount, out binWidth, out min, out max);
            ////Log.WriteLine("BindWidth = "+ binWidth);

            int smoothingwindow = 3;
            if (binCount > 250)
            {
                smoothingwindow = 5;
            }

            double[] smoothHisto = DataTools.filterMovingAverage(histo, smoothingwindow);
            ////DataTools.writeBarGraph(histo);

            int indexOfMode, indexOfOneStdDev;
            GetModeAndOneStandardDeviation(smoothHisto, out indexOfMode, out indexOfOneStdDev);

            // modal noise level gets symbol Q in Lamel et al.
            double mode = min + ((indexOfMode + 1) * binWidth);
            double noiseSd = (indexOfMode - indexOfOneStdDev) * binWidth; // SD of the noise

            // check for noiseSd = zero which can cause possible division by zero later on
            if (indexOfMode == indexOfOneStdDev)
            {
                noiseSd = binWidth;
            }

            double threshold = mode + (noiseSd * sdCount);
            double snr = max - threshold;
            return new BackgroundNoise()
                       {
                           NoiseReducedSignal = null,
                           NoiseMode = mode,
                           MinDb = min,
                           MaxDb = max,
                           Snr = snr,
                           NoiseSd = noiseSd,
                           NoiseThreshold = threshold,
                       };
        }

        /// <summary>
        /// This is the important part of Lamel's algorithm.
        /// It assumes an additive noise model. That is, it assumes that the passed histogram represents the distribution of values
        /// in a waveform consisting of a signal plus added Gaussian noise.
        /// This method estimates the mean and SD of the noise.
        /// </summary>
        public static void GetModeAndOneStandardDeviation(double[] histo, out int indexOfMode, out int indexOfOneSd)
        {
            // this Constant sets an upper limit on the value returned as the modal noise.
            int upperBoundOfMode = (int)(histo.Length * FractionalBoundForMode);
            indexOfMode = DataTools.GetMaxIndex(histo);
            if (indexOfMode > upperBoundOfMode)
            {
                indexOfMode = upperBoundOfMode;
            }

            //calculate SD of the background noise
            double totalAreaUnderLowerCurve = 0.0;
            for (int i = 0; i <= indexOfMode; i++)
            {
                totalAreaUnderLowerCurve += histo[i];
            }

            indexOfOneSd = indexOfMode;
            double partialSum = 0.0; //sum
            double thresholdSum = totalAreaUnderLowerCurve * 0.68; // 0.68 = area under one standard deviation
            for (int i = indexOfMode; i > 0; i--)
            {
                partialSum += histo[i];
                indexOfOneSd = i;
                if (partialSum > thresholdSum)
                {
                    // we have passed the one SD point
                    break;
                }
            }
        }

        public static double[] SubtractAndTruncate2Zero(double[] inArray, double threshold)
        {
            var outArray = new double[inArray.Length];
            for (int i = 0; i < inArray.Length; i++)
            {
                outArray[i] = inArray[i] - threshold;
                if (outArray[i] < 0.0)
                {
                    outArray[i] = 0.0;
                }
            }

            return outArray;
        }

        public static double[] TruncateNegativeValues2Zero(double[] inArray)
        {
            int length = inArray.Length;
            var outArray = new double[length];

            // foreach row
            for (int i = 0; i < length; i++)
            {
                if (inArray[i] < 0.0)
                {
                    outArray[i] = 0.0;
                }
                else
                {
                    outArray[i] = inArray[i];
                }
            }

            return outArray;
        }

        //########################################################################################################################################################
        //# START STATIC METHODS TO DO WITH CHOICE OF NOISE REDUCTION METHOD FROM SPECTROGRAM ####################################################################
        //########################################################################################################################################################

        /// <summary>
        /// Converts a string interpreted as a key to a NoiseReduction Type.
        /// </summary>
        /// <param name="key">The string to convert.</param>
        /// <returns>A NoiseReductionType enumeration.</returns>
        public static NoiseReductionType KeyToNoiseReductionType(string key)
        {
            NoiseReductionType nrt;
            Enum.TryParse(key, true, out nrt);

            return nrt;
        }

        /// <summary>
        /// Removes noise from a spectrogram. Choice of methods.
        /// Make sure that do MelScale reduction BEFORE applying noise filter.
        /// </summary>
        public static Tuple<double[,], double[]> NoiseReduce(double[,] m, NoiseReductionType nrt, double parameter)
        {
            double[] bgNoiseProfile = null;
            switch (nrt)
            {
                case NoiseReductionType.Standard:
                    {
                        //calculate noise profile - assumes a dB spectrogram.
                        var profile = NoiseProfile.CalculateModalNoiseProfile(m, DefaultStddevCount);

                        // smooth the noise profile
                        bgNoiseProfile = DataTools.filterMovingAverage(profile.NoiseThresholds, 5);

                        // IMPORTANT: parameter = nhBackgroundThreshold
                        m = NoiseReduce_Standard(m, bgNoiseProfile, parameter);
                    }

                    break;
                case NoiseReductionType.Modal:
                    {
                        double sdCount = parameter;
                        var profile = NoiseProfile.CalculateModalNoiseProfile(m, sdCount); //calculate modal profile - any matrix of values
                        bgNoiseProfile = DataTools.filterMovingAverage(profile.NoiseThresholds, 5); //smooth the modal profile
                        m = TruncateBgNoiseFromSpectrogram(m, bgNoiseProfile);
                    }

                    break;
                case NoiseReductionType.Mean:
                    m = NoiseReduce_Mean(m, parameter);
                    break;
                case NoiseReductionType.Median:
                    m = NoiseReduce_Median(m, parameter);
                    break;
                case NoiseReductionType.LowestPercentile:
                    {
                        bgNoiseProfile = NoiseProfile.GetNoiseProfile_fromLowestPercentileFrames(m, (int)parameter);
                        bgNoiseProfile = DataTools.filterMovingAverage(bgNoiseProfile, 5); //smooth the modal profile
                        m = TruncateBgNoiseFromSpectrogram(m, bgNoiseProfile);
                    }

                    break;
                case NoiseReductionType.ShortRecording:
                    {
                        bgNoiseProfile = NoiseProfile.GetNoiseProfile_BinWiseFromLowestPercentileCells(m, (int)parameter);
                        bgNoiseProfile = DataTools.filterMovingAverage(bgNoiseProfile, 5); //smooth the modal profile
                        m = TruncateBgNoiseFromSpectrogram(m, bgNoiseProfile);
                    }

                    break;
                case NoiseReductionType.BriggsPercentile:
                    // Briggs filters twice
                    m = NoiseRemoval_Briggs.NoiseReduction_byDivisionAndSqrRoot(m, (int)parameter);
                    m = NoiseRemoval_Briggs.NoiseReduction_byDivisionAndSqrRoot(m, (int)parameter);
                    break;
                case NoiseReductionType.Binary:
                    {
                        NoiseProfile profile = NoiseProfile.CalculateModalNoiseProfile(m, DefaultStddevCount); //calculate noise profile
                        bgNoiseProfile = DataTools.filterMovingAverage(profile.NoiseThresholds, 7); //smooth the noise profile
                        m = NoiseReduce_Standard(m, bgNoiseProfile, parameter); // parameter = nhBackgroundThreshold
                        m = DataTools.Matrix2Binary(m, 2 * parameter);             //convert to binary with backgroundThreshold = 2*parameter
                    }

                    break;
                case NoiseReductionType.FixedDynamicRange:
                    Log.WriteIfVerbose("\tNoise reduction: FIXED DYNAMIC RANGE = " + parameter); //parameter should have value = 50 dB approx
                    m = NoiseReduce_FixedRange(m, parameter, DefaultStddevCount);
                    break;
                case NoiseReductionType.FlattenAndTrim:
                    Log.WriteIfVerbose("\tNoise reduction: FLATTEN & TRIM: StdDev Count=" + parameter);
                    m = NoiseReduce_FlattenAndTrim(m, parameter);
                    break;
                default:
                    Log.WriteIfVerbose("No noise reduction applied");
                    break;
            }

            return Tuple.Create(m, bgNoiseProfile);
        }

        //########################################################################################################################################################
        //# START STATIC METHODS TO DO WITH NOISE REDUCTION FROM SPECTROGRAMS ####################################################################################
        //########################################################################################################################################################

        /// <summary>
        /// expects a spectrogram in dB values
        /// IMPORTANT: Mel scale conversion should be done before noise reduction
        /// Uses default values for severity of noise reduction and neighbourhood threshold
        /// </summary>
        public static double[,] NoiseReduce_Standard(double[,] matrix)
        {
            //SETS MIN DECIBEL BOUND
            double nhBackgroundThreshold = DefaultNhBgThreshold;

            //calculate modal noise profile
            NoiseProfile profile = NoiseProfile.CalculateModalNoiseProfile(matrix, DefaultStddevCount);

            //smooth the noise profile
            double[] smoothedProfile = DataTools.filterMovingAverage(profile.NoiseThresholds, 7);
            return NoiseReduce_Standard(matrix, smoothedProfile, nhBackgroundThreshold);
        }

        /// <summary>
        /// expects a spectrogram in dB values
        /// </summary>
        public static double[,] NoiseReduce_Standard(double[,] matrix, double[] noiseProfile, double nhBackgroundThreshold)
        {
            double[,] mnr = matrix;
            mnr = TruncateBgNoiseFromSpectrogram(mnr, noiseProfile);
            mnr = RemoveNeighbourhoodBackgroundNoise(mnr, nhBackgroundThreshold);
            return mnr;
        }

        /// <summary>
        /// IMPORTANT: Mel scale conversion should be done before noise reduction
        /// </summary>
        public static double[,] NoiseReduce_FixedRange(double[,] matrix, double dynamicRange, double sdCount)
        {
            NoiseProfile profile = NoiseProfile.CalculateModalNoiseProfile(matrix, sdCount); //calculate modal noise profile
            double[] smoothedProfile = DataTools.filterMovingAverage(profile.NoiseThresholds, 7); //smooth the noise profile
            double[,] mnr = SubtractBgNoiseFromSpectrogram(matrix, smoothedProfile);
            mnr = SetDynamicRange(mnr, 0.0, dynamicRange);
            return mnr;
        }

        public static double[,] NoiseReduce_FlattenAndTrim(double[,] matrix, double stdDevCount)
        {
            int upperPercentileTrim = 95;
            var profile = NoiseProfile.CalculateModalNoiseProfile(matrix, stdDevCount); //calculate modal noise profile
            double[] smoothedProfile = DataTools.filterMovingAverage(profile.NoiseThresholds, 5); //smooth the noise profile

            //double[,] mnr = SNR.SubtractBgNoiseFromSpectrogram(matrix, smoothedProfile);
            double[,] mnr = TruncateBgNoiseFromSpectrogram(matrix, smoothedProfile);
            const int temporalNh = 5;
            const int freqBinNh = 9;
            mnr = SetLocalBounds(mnr, 0, upperPercentileTrim, temporalNh, freqBinNh);
            return mnr;
        }

        /// <summary>
        /// The passed matrix is the decibel spectrogram
        /// </summary>
        public static double[,] NoiseReduce_Mean(double[,] matrix, double nhBackgroundThreshold)
        {
            double[,] mnr = matrix;
            double[] meanNoiseProfile = NoiseProfile.CalculateMeanNoiseProfile(mnr).NoiseMean;
            meanNoiseProfile = DataTools.filterMovingAverage(meanNoiseProfile, width: 3);
            mnr = TruncateBgNoiseFromSpectrogram(mnr, meanNoiseProfile);
            mnr = RemoveNeighbourhoodBackgroundNoise(mnr, nhBackgroundThreshold);
            return mnr;
        }

        /// <summary>
        /// The passed matrix is the decibel spectrogram
        /// </summary>
        public static double[,] NoiseReduce_Median(double[,] matrix, double nhBackgroundThreshold)
        {
            double[,] mnr = matrix;
            var profile = NoiseProfile.CalculateMedianNoiseProfile(mnr);
            double[] medianNoiseProfile = profile.NoiseMedian;
            medianNoiseProfile = DataTools.filterMovingAverage(medianNoiseProfile, width: 3);
            mnr = TruncateBgNoiseFromSpectrogram(mnr, medianNoiseProfile);
            mnr = RemoveNeighbourhoodBackgroundNoise(mnr, nhBackgroundThreshold);
            return mnr;
        }

        // #############################################################################################################################
        // ################################# NOISE REDUCTION METHODS #################################################################

        /// <summary>
        /// Subtracts the supplied noise profile from spectorgram AND sets values less than backgroundThreshold to ZERO.
        /// </summary>
        public static double[,] SubtractAndTruncateNoiseProfile(double[,] matrix, double[] noiseProfile, double backgroundThreshold)
        {
            int rowCount = matrix.GetLength(0);
            int colCount = matrix.GetLength(1);
            double[,] outM = new double[rowCount, colCount]; //to contain noise reduced matrix

            for (int col = 0; col < colCount; col++) //for all cols i.e. freq bins
            {
                for (int y = 0; y < rowCount; y++) //for all rows
                {
                    outM[y, col] = matrix[y, col] - noiseProfile[col];
                    if (outM[y, col] < backgroundThreshold)
                    {
                        outM[y, col] = 0.0;
                    }
                }
            }

            return outM;
        }

        /// <summary>
        /// Subtracts the supplied noise profile from spectorgram AND sets negative values to ZERO.
        /// </summary>
        public static double[,] TruncateBgNoiseFromSpectrogram(double[,] matrix, double[] noiseProfile)
        {
            double backgroundThreshold = 0.0;
            return SubtractAndTruncateNoiseProfile(matrix, noiseProfile, backgroundThreshold);
        }

        /// <summary>
        /// Subtracts the supplied modal noise value for each freq bin BUT DOES NOT set negative values to zero.
        /// </summary>
        public static double[,] SubtractBgNoiseFromSpectrogram(double[,] matrix, double[] noiseProfile)
        {
            int rowCount = matrix.GetLength(0);
            int colCount = matrix.GetLength(1);
            double[,] outM = new double[rowCount, colCount]; //to contain noise reduced matrix

            for (int col = 0; col < colCount; col++)
            {
                for (int y = 0; y < rowCount; y++)
                {
                    outM[y, col] = matrix[y, col] - noiseProfile[col];
                }
            }

            return outM;
        }

        /*
        public static double[,] TruncateNegativeValues2Zero(double[,] m)
        {
            int rows = m.GetLength(0);
            int cols = m.GetLength(1);
            var matrix = new double[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (matrix[r, c] < 0.0)
                    {
                        matrix[r, c] = 0.0;
                    }
                    else
                    {
                        matrix[r, c] = m[r, c];
                    }
                }
            }

            return matrix;
        }
        */

        /// <summary>
        /// sets the dynamic range in dB for a sonogram.
        /// All intensity values are shifted so that the max intensity value = maxDB parameter.
        /// All values which fall below the minDB parameter are then set = to minDB.
        /// </summary>
        /// <param name="m">The spectral sonogram passes as matrix of doubles</param>
        /// <param name="minDb">minimum decibel value</param>
        /// <param name="maxDb">maximum decibel value</param>
        public static double[,] SetDynamicRange(double[,] m, double minDb, double maxDb)
        {
            double minIntensity; // min value in matrix
            double maxIntensity; // max value in matrix
            DataTools.MinMax(m, out minIntensity, out maxIntensity);
            double shift = maxDb - maxIntensity;

            int rowCount = m.GetLength(0);
            int colCount = m.GetLength(1);
            double[,] normM = new double[rowCount, colCount];
            for (int col = 0; col < colCount; col++)
            {
                for (int row = 0; row < rowCount; row++)
                {
                    normM[row, col] = m[row, col] + shift;
                    if (normM[row, col] < minDb)
                    {
                        normM[row, col] = 0;
                    }
                }
            }

            return normM;
        }

        /// <summary>
        /// SetLocalBounds
        /// </summary>
        /// <param name="m">The spectral sonogram passes as matrix of doubles</param>
        /// <param name="minPercentileBound">minimum decibel value</param>
        /// <param name="maxPercentileBound">maximum decibel value</param>
        /// <param name="temporalNh">buffer in temporal dimension</param>
        /// <param name="freqBinNh">buffer in frequency dimension</param>
        public static double[,] SetLocalBounds(double[,] m, int minPercentileBound, int maxPercentileBound, int temporalNh, int freqBinNh)
        {
            int binCount = 100; // histogram width is adjusted to length of signal
            int rowCount = m.GetLength(0);
            int colCount = m.GetLength(1);
            double[,] normM = new double[rowCount, colCount];

            for (int col = freqBinNh; col < colCount - freqBinNh; col++) //for all cols i.e. freq bins
            {
                for (int row = temporalNh; row < rowCount - temporalNh; row++) //for all rows i.e. frames
                {
                    var localMatrix = MatrixTools.Submatrix(m, row - temporalNh, col - freqBinNh, row + temporalNh, col + freqBinNh);
                    double minIntensity, maxIntensity, binWidth;
                    int[] histo = Histogram.Histo(localMatrix, binCount, out binWidth, out minIntensity, out maxIntensity);
                    int lowerBinBound = Histogram.GetPercentileBin(histo, minPercentileBound);
                    int upperBinBound = Histogram.GetPercentileBin(histo, maxPercentileBound);

                    // double lowerBound = minIntensity + (lowerBinBound * binWidth);
                    // double upperBound = minIntensity + (upperBinBound * binWidth);
                    // calculate the range = upperBound - lowerBound
                    //                     = (minIntensity + (upperBinBound * binWidth)) - (minIntensity + (lowerBinBound * binWidth));
                    //                     = (upperBinBound - lowerBinBound) * binWidth;
                    normM[row, col] = (upperBinBound - lowerBinBound) * binWidth;
                }
            }

            return normM;
        }

        /// <summary>
        /// This method sets a sonogram pixel value = minimum value in sonogram if average pixel value in its neighbourhood is less than min+threshold.
        /// Typically would expect min value in sonogram = zero.
        /// </summary>
        /// <param name="matrix">the sonogram</param>
        /// <param name="nhThreshold">user defined threshold. Typically in range 2-4 dB</param>
        public static double[,] RemoveNeighbourhoodBackgroundNoise(double[,] matrix, double nhThreshold)
        {
            if (nhThreshold < 0.000001)
            {
                return matrix;
            }

            int M = 3; // each row is a frame or time instance
            int N = 9; // each column is a frequency bin
            int rNh = M / 2;
            int cNh = N / 2;

            double min;
            double max;
            DataTools.MinMax(matrix, out min, out max);
            nhThreshold += min;

            //int[] h = DataTools.Histo(matrix, 50);
            //DataTools.writeBarGraph(h);

            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            double[,] outM = new double[rows, cols];
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rows; r++)
                {
                    //if (matrix[r, c] <= 70.0) continue;
                    double x = 0.0;

                    //double Xe2 = 0.0;
                    int count = 0;
                    for (int i = r - rNh; i <= r + rNh; i++)
                    {
                        if (i < 0)
                        {
                            continue;
                        }

                        if (i >= rows)
                        {
                            continue;
                        }

                        for (int j = c - cNh; j <= c + cNh; j++)
                        {
                            if (j < 0)
                            {
                                continue;
                            }

                            if (j >= cols)
                            {
                                continue;
                            }

                            count++; //to accomodate edge effects
                            x += matrix[i, j];

                            //Xe2 += (matrix[i, j] * matrix[i, j]);
                            //LoggedConsole.WriteLine(i+"  "+j+"   count="+count);
                            //Console.ReadLine();
                        }
                    }

                    double mean = x / count;

                    //double variance = (Xe2 / count) - (mean * mean);
                    //if ((c<(cols/5))&&(mean < (threshold+1.0))) outM[r, c] = min;
                    //else
                    if (mean < nhThreshold)
                    {
                        outM[r, c] = min;
                    }
                    else
                    {
                        outM[r, c] = matrix[r, c];
                    }

                    //LoggedConsole.WriteLine((outM[r, c]).ToString("F1") + "   " + (matrix[r, c]).ToString("F1") + "  mean=" + mean + "  variance=" + variance);
                    //Console.ReadLine();
                }
            }

            return outM;
        }

        public class BackgroundNoise
        {
            public double[] NoiseReducedSignal { get; set; }

            public double NoiseMode { get; set; }

            public double NoiseSd { get; set; }

            public double NoiseThreshold { get; set; }

            public double MinDb { get; set; }

            public double MaxDb { get; set; }

            public double Snr { get; set; }
        }

        /// <summary>
        /// used to store info about the SNR in a signal using db units
        /// </summary>
        public class SnrStatistics
        {
            /// <summary>
            /// Gets or sets duration of the event under consideration.
            /// It may be shorter or longer than the actual recording we have.
            /// If longer then the event, then duration := recording duration.
            /// Rest was truncated in original data extraction.
            /// </summary>
            public TimeSpan ExtractDuration { get; set; }

            /// <summary>
            /// Gets or sets decibel threshold used to calculate cover and average SNR
            /// </summary>
            public double Threshold { get; set; }

            /// <summary>
            /// Gets or sets maximum dB value in the signal or spectrogram - relative to zero dB background
            /// </summary>
            public double Snr { get; set; }

            /// <summary>
            /// Gets or sets fraction of frames in the call where the average energy exceeds the user specified threshold.
            /// </summary>
            public double FractionOfFramesExceedingThreshold { get; set; }

            /// <summary>
            /// Gets or sets fraction of frames in the call where the average energy exceeds half the calculated SNR.
            /// </summary>
            public double FractionOfFramesExceedingOneThirdSnr { get; set; }
        }
    }
}
