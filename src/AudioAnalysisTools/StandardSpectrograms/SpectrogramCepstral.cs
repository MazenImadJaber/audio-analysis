// <copyright file="SpectrogramCepstral.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AudioAnalysisTools.StandardSpectrograms
{
    using System;
    using System.IO;
    using Acoustics.Tools.Wav;
    using AudioAnalysisTools.DSP;
    using AudioAnalysisTools.WavTools;
    using TowseyLibrary;

    public class SpectrogramCepstral : BaseSonogram
    {
        public SpectrogramCepstral(string configFile, WavReader wav)
            : this(SonogramConfig.Load(configFile), wav)
        {
        }

        public SpectrogramCepstral(SonogramConfig config, WavReader wav)
            : base(config, wav)
        {
        }

        public SpectrogramCepstral(AmplitudeSonogram sg)
            : base(sg.Configuration)
        {
            this.Configuration = sg.Configuration;
            this.DecibelsPerFrame = sg.DecibelsPerFrame;
            this.DecibelsNormalised = sg.DecibelsNormalised;
            this.Duration = sg.Duration;
            this.FrameCount = sg.FrameCount;
            this.DecibelReference = sg.DecibelReference;
            this.MaxAmplitude = sg.MaxAmplitude;
            this.SampleRate = sg.SampleRate;
            this.SigState = sg.SigState;
            this.SnrData = sg.SnrData;
            this.Data = sg.Data;

            //converts amplitude matrix to cepstral sonogram
            this.Make(this.Data);
        }

        public SpectrogramCepstral(AmplitudeSonogram sg, int minHz, int maxHz)
            : this(sg)
        {
            this.DecibelsPerFrame = sg.DecibelsPerFrame;
            this.DecibelsNormalised = sg.DecibelsNormalised;
            this.Duration = sg.Duration;
            this.FrameCount = sg.FrameCount;
            this.DecibelReference = sg.DecibelReference;
            this.MaxAmplitude = sg.MaxAmplitude;
            this.SampleRate = sg.SampleRate;
            this.SigState = sg.SigState;
            this.SnrData = sg.SnrData;

            // sub-band highlighting no longer available
            //this.subBandMinHz = minHz;
            //this.subBandMaxHz = maxHz;
            //double[] noise_subband = BaseSonogram.ExtractModalNoiseSubband(this.SnrData.ModalNoiseProfile, minHz, maxHz, sg.doMelScale,
            //                                                   sonogram.Configuration.FreqBinCount, sonogram.FBinWidth);

            this.Data = SpectrogramTools.ExtractFreqSubband(sg.Data, minHz, maxHz, this.Configuration.DoMelScale, sg.Configuration.FreqBinCount, sg.FBinWidth);

            //converts amplitude matrix to cepstral sonogram
            this.Make(this.Data);
        }

        /// <summary>
        /// Converts amplitude matrix to cepstral sonogram.
        /// </summary>
        /// <param name="amplitudeM">Matrix of amplitude values.</param>
        public override void Make(double[,] amplitudeM)
        {
            var tuple = MakeCepstrogram(this.Configuration, amplitudeM, this.DecibelsNormalised, this.SampleRate);
            this.Data = tuple.Item1;
            this.ModalNoiseProfile = tuple.Item2; //store the full bandwidth modal noise profile
        }

        //##################################################################################################################################

        /// <summary>
        /// NOTE!!!! The decibel array has been normalised in 0 - 1.
        /// </summary>
        protected static Tuple<double[,], double[]> MakeCepstrogram(SonogramConfig config, double[,] matrix, double[] decibels, int sampleRate)
        {
            double[,] m = matrix;
            int nyquist = sampleRate / 2;
            double epsilon = config.epsilon;
            bool includeDelta = config.mfccConfig.IncludeDelta;
            bool includeDoubleDelta = config.mfccConfig.IncludeDoubleDelta;

            //(i) APPLY FILTER BANK
            int bandCount = config.mfccConfig.FilterbankCount;
            bool doMelScale = config.mfccConfig.DoMelScale;
            int ccCount = config.mfccConfig.CcCount;
            int fftBinCount = config.FreqBinCount;  //number of Hz bands = 2^N +1. Subtract DC bin
            int minHz = config.MinFreqBand ?? 0;
            int maxHz = config.MaxFreqBand ?? nyquist;

            Log.WriteIfVerbose("ApplyFilterBank(): Dim prior to filter bank  =" + matrix.GetLength(1));

            //error check that filterBankCount < Number of FFT bins
            if (bandCount > fftBinCount)
            {
                throw new Exception(
                    "## FATAL ERROR in BaseSonogram.MakeCepstrogram():- Can't calculate cepstral coefficients. Filterbank Count > number of FFT bins. (" +
                    bandCount + " > " + fftBinCount + ")\n\n");
            }

            //this is the filter count for full bandwidth 0-Nyquist. This number is trimmed proportionately to fit the required bandwidth.
            m = doMelScale ? MFCCStuff.MelFilterBank(m, bandCount, nyquist, minHz, maxHz) : MFCCStuff.LinearFilterBank(m, bandCount, nyquist, minHz, maxHz);

            Log.WriteIfVerbose("\tDim after filter bank=" + m.GetLength(1) + " (Max filter bank=" + bandCount + ")");

            //(ii) CONVERT AMPLITUDES TO DECIBELS
            m = MFCCStuff.DecibelSpectra(m, config.WindowPower, sampleRate, epsilon); //from spectrogram

            //(iii) NOISE REDUCTION
            var tuple1 = SNR.NoiseReduce(m, config.NoiseReductionType, config.NoiseReductionParameter);
            m = tuple1.Item1;

            //(iv) calculate cepstral coefficients
            m = MFCCStuff.Cepstra(m, ccCount);

            //(v) Normalize Matrix Values
            m = DataTools.normalise(m);

            //(vi) Calculate the full range of MFCC coefficients ie including decibel and deltas, etc
            m = MFCCStuff.AcousticVectors(m, decibels, includeDelta, includeDoubleDelta);
            var tuple2 = Tuple.Create(m, tuple1.Item2);

            // return matrix and full bandwidth modal noise profile
            return tuple2;
        }

        /// <summary>
        /// Returns a Spectrogram and Cepstrogram from the passed recording. These are NOT noise reduced.
        /// however, tuple also returns the modal noise and sub-band modal noise.
        /// </summary>
        public static Tuple<SpectrogramStandard, SpectrogramCepstral, double[], double[]> GetAllSonograms(AudioRecording recording, SonogramConfig sonoConfig, int minHz, int maxHz)
        {
            int sr = recording.SampleRate;
            bool doMelScale = sonoConfig.DoMelScale;
            int ccCount = sonoConfig.mfccConfig.CcCount;
            bool includeDelta = sonoConfig.mfccConfig.IncludeDelta;
            bool includeDoubleDelta = sonoConfig.mfccConfig.IncludeDoubleDelta;
            sonoConfig.SourceFName = recording.BaseName;

            var basegram = new AmplitudeSonogram(sonoConfig, recording.WavReader);
            var sonogram = new SpectrogramStandard(basegram);  //spectrogram has dim[N,257]

            Log.WriteLine("Signal: Duration={0}, Sample Rate={1}", sonogram.Duration, sr);
            Log.WriteLine(
                $"Frames: Size={0}, Count={1}, Duration={2:f1}ms, Overlap={5:f0}%, Offset={3:f1}ms, Frames/s={4:f1}",
                sonogram.Configuration.WindowSize,
                sonogram.FrameCount,
                sonogram.FrameDuration * 1000,
                sonogram.FrameStep * 1000,
                sonogram.FramesPerSecond,
                sonoConfig.WindowOverlap * 100);

            int binCount = (int)(maxHz / sonogram.FBinWidth) - (int)(minHz / sonogram.FBinWidth) + 1;
            Log.WriteLine("Freqs : {0} Hz - {1} Hz. (Freq bin count = {2})", minHz, maxHz, binCount);
            Log.WriteLine("MFCCs : doMelScale=" + doMelScale + ";  ccCount=" + ccCount + ";  includeDelta=" + includeDelta + ";  includeDoubleDelta=" + includeDoubleDelta);

            //CALCULATE MODAL NOISE PROFILE - USER MAY REQUIRE IT FOR NOISE REDUCTION
            double[] modalNoise = sonogram.SnrData.ModalNoiseProfile;

            //extract sub-band modal noise profile
            double[] noiseSubband = SpectrogramTools.ExtractModalNoiseSubband(modalNoise, minHz, maxHz, doMelScale, sonogram.NyquistFrequency, sonogram.FBinWidth);

            // CALCULATE CEPSTRO-GRAM.  //cepstrogram has dim[N,13]
            var cepstrogram = new SpectrogramCepstral(basegram, minHz, maxHz);
            var tuple = Tuple.Create(sonogram, cepstrogram, modalNoise, noiseSubband);
            return tuple;
        }
    } // end class CepstralSonogram

    //##################################################################################################################################
    //##################################################################################################################################

    public class TriAvSonogram : SpectrogramCepstral
    {
        public TriAvSonogram(string configFile, WavReader wav)
            : base(SonogramConfig.Load(configFile), wav)
        {
        }

        public TriAvSonogram(SonogramConfig config, WavReader wav)
            : base(config, wav)
        {
        }

        public override void Make(double[,] amplitudeM)
        {
            this.Data = MakeAcousticVectors(this.Configuration, amplitudeM, this.DecibelsNormalised, this.SampleRate);
        }

        public static double[,] MakeAcousticVectors(SonogramConfig config, double[,] matrix, double[] decibels, int sampleRate)
        {
            //int ccCount = config.mfccConfig.CcCount;
            bool includeDelta = config.mfccConfig.IncludeDelta;
            bool includeDoubleDelta = config.mfccConfig.IncludeDoubleDelta;
            int deltaT = config.DeltaT;

            Log.WriteIfVerbose(" MakeAcousticVectors(matrix, decibels, includeDelta=" + includeDelta + ", includeDoubleDelta=" + includeDoubleDelta + ", deltaT=" + deltaT + ")");
            var tuple = MakeCepstrogram(config, matrix, decibels, sampleRate);
            double[,] m = tuple.Item1;

            //initialize feature vector for template - will contain three acoustic vectors - for T-dT, T and T+dT
            int frameCount = m.GetLength(0);
            int cepstralL = m.GetLength(1);  // length of cepstral vector
            int featurevL = 3 * cepstralL;   // to accomodate cepstra for T-2, T and T+2

            double[,] acousticM = new double[frameCount, featurevL]; //init the matrix of acoustic vectors
            for (int i = deltaT; i < frameCount - deltaT; i++)
            {
                double[] rowTm2 = DataTools.GetRow(m, i - deltaT);
                double[] rowT = DataTools.GetRow(m, i);
                double[] rowTp2 = DataTools.GetRow(m, i + deltaT);

                for (int j = 0; j < cepstralL; j++)
                {
                    acousticM[i, j] = rowTm2[j];
                }

                for (int j = 0; j < cepstralL; j++)
                {
                    acousticM[i, cepstralL + j] = rowT[j];
                }

                for (int j = 0; j < cepstralL; j++)
                {
                    acousticM[i, cepstralL + cepstralL + j] = rowTp2[j];
                }
            }

            return acousticM;
        }
    } //end class AcousticVectorsSonogram : CepstralSonogram
}
