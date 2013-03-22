﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AcousticEventDetection.cs" company="MQUTeR">
//   -
// </copyright>
// <summary>
//   Defines the AcousticEventDetection type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Dong.Felt
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Text;

    using AnalysisBase;

    using AudioAnalysisTools;

    using TowseyLib;
    using Acoustics.Shared.Extensions;

    /// <summary>
    /// The acoustic event detection.
    /// </summary>
    public class POI
    {
        //  pick the highest point in the spectrogram
       //var amplitudeSpectrogram = new AmplitudeSonogram(config, recording.GetWavReader());

        /// <summary>
        /// AudioToSpectrogram.  
        /// </summary>
        /// <param name="wavFilePath">
        /// </param>
        /// <param name="recording">
        /// The recording.
        /// </param>
        /// <returns>
        /// </returns>
        public static SpectralSonogram AudioToSpectrogram(string wavFilePath, out AudioRecording recording)
        {
            recording = new AudioRecording(wavFilePath);
            var config = new SonogramConfig { NoiseReductionType = NoiseReductionType.NONE };
            var spectrogram = new SpectralSonogram(config, recording.GetWavReader());
            return spectrogram;
        }

        /// <summary>
        /// Reduce the pink noise. 
        /// the result is binary spectrogram.
        /// </summary>
        /// <param name="spectralSonogram">
        /// The spectral sonogram.
        /// </param>
        /// <param name="threshold">
        /// The threshold. ranging from 3.0 to 10.0 - decible value
        /// </param>
        public static double[,] NoiseReductionToBinarySpectrogram(SpectralSonogram spectralSonogram, double threshold, bool makeBinary = false)
        {
            double[] modalNoise = SNR.CalculateModalNoise(spectralSonogram.Data);
            double[,] matrix = spectralSonogram.Data;

            int rowCount = matrix.GetLength(0);
            int colCount = matrix.GetLength(1);
            double[,] outMatrix = new double[rowCount, colCount]; 

            for (int col = 0; col < colCount; col++) //for all cols i.e. freq bins
            {
                for (int y = 0; y < rowCount; y++) //for all rows
                {
                    outMatrix[y, col] = matrix[y, col] - modalNoise[col];
                    if (outMatrix[y, col] < threshold)
                    {
                        outMatrix[y, col] = 0.0;
                    }
                    else
                    {
                        if (makeBinary)
                        {
                            outMatrix[y, col] = 1.0;
                        }
                    }
                } 
            }
            return outMatrix;
            //spectralSonogram.Data = outM;

            //var imageResult = new Image_MultiTrack(spectralSonogram.GetImage(false, false));
            //imageResult.Save("C:\\Test recordings\\Test6.png");

        }

        /// <summary>
        /// The pick local maximum.
        /// </summary>
        /// <param name="m">
        /// The m. matrix
        /// </param>
        /// <param name="neighborWindowSize">
        /// The neighbor window size. It should be odd number 
        /// </param>
        /// <returns>
        /// The <see cref="List"/>.
        /// </returns>
        public static List<Point> PickLocalMaximum(double[,] m, int neighborWindowSize)
        {
            Contract.Requires(neighborWindowSize % 2 == 1, "Neighbourhood window size must be odd");
            Contract.Requires(neighborWindowSize >= 1, "Neighbourhood window size must be at least 1");

            int centerOffset = -(int)(neighborWindowSize / 2.0);

            var results = new List<Point>();
            
            for (int row = 0; row < m.GetLength(1); row++)
            {
                for (int col = 0; col < m.GetLength(0); col++)
                {
                    // assume local maxium
                    double localMaximum = m[col, row];
                    bool maximum = true;

                    // check if it really is the local maximum in the neighbourhood
                    for (int i = centerOffset; i < neighborWindowSize; i++)
                    {
                        for (int j = centerOffset; j < neighborWindowSize; j++)
                        {
                            if (m.PointIntersect(col + j, row + i))
                            {
                                var current = m[col + j, row + i];

                                // don't check the middle point
                                if (localMaximum <= current && !(i == 0 && j ==0))
                                {
                                    // actually not a local maximum
                                    maximum = false;
                                }
                            }
                        }
                    }

                    // iff it is indeed the local maximum, then add it
                    if (maximum)
                    {
                        results.Add(new Point(col, row));
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// To generate a binary spectrogram, a amplitudeThreshold is required
        /// Above the threshold, its amplitude value will be assigned to MAX (black), otherwise to MIN (white)
        /// Side affect: An image is saved
        /// Side affect: the original AmplitudeSonogram is modified
        /// </summary>
        /// <returns>
        /// </returns>
        public void GenerateBinarySpectrogram(AmplitudeSonogram amplitudeSpectrogram, double amplitudeThreshold)
        {
             var spectrogramAmplitudeMatrix = amplitudeSpectrogram.Data;
             ////var maximumOfFrame = (int)Math.Round(amplitudeSpectrogram.FrameDuration / amplitudeSpectrogram.FramesPerSecond);
             ////var maximumOfFrequencyBin = (int)Math.Round(amplitudeSpectrogram.NyquistFrequency / amplitudeSpectrogram.FBinWidth);
             for (int i = 0; i < spectrogramAmplitudeMatrix.GetLength(0); i++)
             {
                 for (int j = 0; j < spectrogramAmplitudeMatrix.GetLength(1); j++)
                 {
                     if (spectrogramAmplitudeMatrix[i, j] > amplitudeThreshold) // by default it will be 0.028
                         spectrogramAmplitudeMatrix[i, j] = 1;
                     else
                         spectrogramAmplitudeMatrix[i, j] = 0;
                 }
             }

             var imageResult = new Image_MultiTrack(amplitudeSpectrogram.GetImage(false, true));
             imageResult.Save("C:\\Test recordings\\Test4.png");
        }

        /// <summary>
        /// PeakAmplitudeDetection applies a window of n seconds, starting every n seconds, to the recording. The window detects points
        /// that have a peakAmplitude value.
        /// </summary>
        /// <param name="amplitudeSpectrogram">An input amplitude sonogram</param>
        /// <param name="slideWindowDuation"></param>
        /// <param name="minFreq">for slideWindowMinFrequencyBin</param>
        /// <param name="maxFreq"> for slideWindowMaxFrequencyBin</param>
        /// <returns></returns>
        public string PeakAmplitudeDetection(AmplitudeSonogram amplitudeSpectrogram, int minFreq, int maxFreq, double slideWindowDuation = 2.0)
         {
             var spectrogramAmplitudeMatrix = amplitudeSpectrogram.Data;
          
             var numberOfWindows = (int)(amplitudeSpectrogram.Duration.Seconds / slideWindowDuation);  // by default SlideWindowDuation = 2.0 sec
             
            // get the offset frame of Window
             var slideWindowFrameOffset = (int)Math.Round(slideWindowDuation * amplitudeSpectrogram.FramesPerSecond); 

             var slideWindowminFreqBin = (int)Math.Round(minFreq / amplitudeSpectrogram.FBinWidth); 
             var slideWindowmaxFreqBin = (int)Math.Round(maxFreq / amplitudeSpectrogram.FBinWidth);

             var peakPoints = new Tuple<Point, double>[numberOfWindows];

             for (int windowIndex = 0; windowIndex < numberOfWindows; windowIndex++)
             {
                 var currentMaximum = double.NegativeInfinity;

                 // scan along frames
                 for (int i = 0; i < (windowIndex + 1) * slideWindowFrameOffset; i++)
                 {
                      // scan through bins
                     for (int j = slideWindowminFreqBin; j < slideWindowmaxFreqBin; j++)
                      {
                          if (spectrogramAmplitudeMatrix[i, j] > currentMaximum)
                          {
                              peakPoints[windowIndex] = Tuple.Create(new Point(i, j), spectrogramAmplitudeMatrix[i, j]);
                          }
                      }
                  }
              }

              var outputPoints = string.Empty;
              foreach (var point in peakPoints)
              {
                  if (point != null)
                  {
                      outputPoints += string.Format("Point found at x:{0}, y:{1}, value: {2}\n", point.Item1.X, point.Item1.Y, point.Item2);
                  }
              }

            return outputPoints;
            ////Log.Info("Found points: \n" + outputPoints);
         }

        //const double customizedTime = 15.0;
         //var events = new List<AcousticEvent>() { 
         //new AcousticEvent(peakPoints[0].Item1.X / amplitudeSpectrogram.FramesPerSecond, customizedTime / amplitudeSpectrogram.FramesPerSecond, peakPoints[0].Item1.Y * 43 - 15,peakPoints[0].Item1.Y*43),   
         //new AcousticEvent(peakPoints[1].Item1.X / amplitudeSpectrogram.FramesPerSecond, customizedTime / amplitudeSpectrogram.FramesPerSecond, peakPoints[1].Item1.Y * 43 - 15,peakPoints[1].Item1.Y*43),
         //  //new AcousticEvent(11.0,2.0,500,1000),
         //  //new AcousticEvent(14.0,2.0,500,1000),
         //  //new AcousticEvent(17.0,2.0,500,1000),
         //};
         //foreach (var e in events)
         //{
         //     e.BorderColour = AcousticEvent.DEFAULT_BORDER_COLOR;
         //}
         //var image = new Image_MultiTrack(amplitudeSpectrogram.GetImage(false, true));
         //image.AddTrack(Image_Track.GetTimeTrack(amplitudeSpectrogram.Duration, amplitudeSpectrogram.FramesPerSecond));
         //image.AddTrack(Image_Track.GetSegmentationTrack(amplitudeSpectrogram));
         //image.AddEvents(events, amplitudeSpectrogram.NyquistFrequency, amplitudeSpectrogram.Configuration.FreqBinCount, amplitudeSpectrogram.FramesPerSecond);

        //image.Save("C:\\Test recordings\\Test5.png");    
        /// <summary>
        /// Make fake acoustic events randomly. 
        /// </summary>
        /// <param name="numberOfFakes">
        /// The number Of Fakes.
        /// </param>
        /// <param name="minTime">
        /// The min Time.
        /// </param>
        /// <param name="minFrequency">
        /// The min Frequency.
        /// </param>
        /// <param name="maxTime">
        /// The max Time.
        /// </param>
        /// <param name="maxFrequency">
        /// The max Frequency.
        /// </param>
        /// <returns>
        /// The <see>
        ///         <cref>AcousticEvent[]</cref>
        ///     </see>
        ///     .
        /// </returns>
        public AcousticEvent[] MakeFakeAcousticEvents(int numberOfFakes, double minTime, double minFrequency, double maxTime, double maxFrequency)
        {
            Contract.Requires(numberOfFakes > 0);

            var rand = new Random();
            var duration = maxTime - minTime;

            var events = new AcousticEvent[numberOfFakes];
            for (int index = 0; index < numberOfFakes; index++)
            {
                events[index] = new AcousticEvent(
                    rand.NextDouble() * minTime, 
                    rand.NextDouble() * duration, 
                    minFrequency * rand.NextDouble(),
                    maxFrequency * rand.NextDouble());
            }

            return events;

            //var recording = new AudioRecording(wavFilePath);
            //var events = new List<AcousticEvent>() { 
            //    new AcousticEvent(5.0,2.0,500,1000),   
            //    new AcousticEvent(8.0,2.0,500,1000),
            //    new AcousticEvent(11.0,2.0,500,1000),
            //    new AcousticEvent(14.0,2.0,500,1000),
            //    new AcousticEvent(17.0,2.0,500,1000),
            //};
            //foreach (var e in events)
            //{
            //    e.BorderColour = AcousticEvent.DEFAULT_BORDER_COLOR;
            //}
            ////generate spectrogram
            //var config = new SonogramConfig { NoiseReductionType = NoiseReductionType.NONE };
            //var spectrogram = new SpectralSonogram(config, recording.GetWavReader());

            //var image = new Image_MultiTrack(spectrogram.GetImage(false, true));
            //image.AddTrack(Image_Track.GetTimeTrack(spectrogram.Duration, spectrogram.FramesPerSecond));
            ////image.AddTrack(Image_Track.GetWavEnvelopeTrack(sonogram, image.sonogramImage.Width));
            //image.AddTrack(Image_Track.GetSegmentationTrack(spectrogram));
            //image.AddEvents(events, spectrogram.NyquistFrequency, spectrogram.Configuration.FreqBinCount, spectrogram.FramesPerSecond);
            //image.Save("C:\\Test recordings\\Test1.png");

        }

        /// <summary>
        /// Draw a line on the spectrogram
        /// 
        /// Side affect: writes image to disk
        /// </summary>
        /// <param name="wavFilePath"></param>
         public void DrawLine(string wavFilePath,double startTime,double endTime, int minFrequency,int maxFrequency)
         {
             var recording = new AudioRecording(wavFilePath);
             var config = new SonogramConfig(); 
             var amplitudeSpectrogram = new SpectralSonogram(config, recording.GetWavReader());
             var spectrogramAmplitudeMatrix = amplitudeSpectrogram.Data;

             int minFrame = (int)Math.Round(startTime * amplitudeSpectrogram.FramesPerSecond );
             int maxFrame = (int)Math.Round(endTime * amplitudeSpectrogram.FramesPerSecond );

             int minFrequencyBin = (int)Math.Round(minFrequency / amplitudeSpectrogram.FBinWidth);
             int maxFrequencyBin = (int)Math.Round(maxFrequency / amplitudeSpectrogram.FBinWidth); 

             for (int i = minFrame ; i < maxFrame; i++)
             {
                 for (int j = minFrequencyBin; j < maxFrequencyBin; j++)
                 {
                     spectrogramAmplitudeMatrix[i, j] = 1;
                 }
             }
             var imageResult = new Image_MultiTrack(amplitudeSpectrogram.GetImage(false, true));
             imageResult.Save("C:\\Test recordings\\Test2.png");
         }

            //  not necessary part - you shouldn't ever work on recordings that aren't in 22050Hz
            //if (recording.SampleRate != 22050)
            //{
            //    recording.ConvertSampleRate22kHz();
            //}

        private static readonly SonogramConfig standardConfig = new SonogramConfig();

        /// <summary>
        /// Draw a box on a customerized frequency and time range
        /// Side affect: writes file to disk
        /// </summary>
        /// <param name="wavFilePath"></param>
         public void DrawCustomizedBox(string wavFilePath)
         {
             Contract.Requires(!string.IsNullOrWhiteSpace(wavFilePath));
             Contract.Requires(File.Exists(wavFilePath));

             var recording = new AudioRecording(wavFilePath);
             var amplitudeSpectrogram = new AmplitudeSonogram(standardConfig, recording.GetWavReader());
             var spectrogramAmplitudeMatrix = amplitudeSpectrogram.Data;
             const int MinFreq = 2000;
             const int MaxFreq = 3500;
             int minFreqBin = (int)Math.Round(MinFreq / amplitudeSpectrogram.FBinWidth);
             int maxFreqBin = (int)Math.Round(MaxFreq / amplitudeSpectrogram.FBinWidth);

             const int StartTime = 16;
             const int EndTime = 22;
             int minFrameNum = (int)Math.Round(StartTime * amplitudeSpectrogram.FramesPerSecond);
             int maxFrameNum = (int)Math.Round(EndTime * amplitudeSpectrogram.FramesPerSecond);

             for (int i = minFrameNum; i < maxFrameNum; i++)
             {
                 spectrogramAmplitudeMatrix[i, minFreqBin] = 1;
                 spectrogramAmplitudeMatrix[i, maxFreqBin] = 1;
             }
             for (int j = minFreqBin; j < maxFreqBin; j++)
             {
                 spectrogramAmplitudeMatrix[minFrameNum, j] = 1;
                 spectrogramAmplitudeMatrix[maxFrameNum, j] = 1;
             }

             var imageResult = new Image_MultiTrack(amplitudeSpectrogram.GetImage(false, true));
             imageResult.Save("C:\\Test recordings\\Test3.png");
         }
         // print configure dictionary
         //string printMessage = analysisSettings.ConfigDict["my_custom_setting"];
         //Log.Info(printMessage);
    }
}

/*





*/