// <copyright file="ContentVisualization.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AudioAnalysisTools.ContentDescriptionTools
{
    using System;
    using System.Collections.Generic;
    using SixLabors.ImageSharp;
    using System.Drawing.Drawing2D;
    using System.IO;
    using AudioAnalysisTools.DSP;
    using AudioAnalysisTools.StandardSpectrograms;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Processing;
    using TowseyLibrary;

    public static class ContentVisualization
    {
        public static Image DrawLdfcSpectrogramWithContentScoreTracks(Image ldfcSpectrogram, List<Plot> contentScores)
        {
            int plotHeight = 30;
            var imageList = new List<Image>
            {
                ldfcSpectrogram,
            };

            if (contentScores != null)
            {
                foreach (var plot in contentScores)
                {
                    var image = plot.DrawAnnotatedPlot(plotHeight);
                    imageList.Add(image);
                }
            }

            return ImageTools.CombineImagesVertically(imageList);
        }

        /// <summary>
        /// Can be used for visual checking and debugging purposes.
        /// </summary>
        public static void DrawNormalisedIndexMatrices(DirectoryInfo dir, string baseName, Dictionary<string, double[,]> dictionary)
        {
            var list = new List<Image>();
            foreach (string key in ContentSignatures.IndexNames)
            {
                var bmp = ImageTools.DrawReversedMatrixWithoutNormalisation(dictionary[key]);

                // need to rotate spectrogram to get correct orientation.
                bmp.RotateFlip(RotateFlipType.Rotate270FlipNone);

                // draw grid lines and add axis scales
                var xAxisPixelDuration = TimeSpan.FromSeconds(60);
                var fullDuration = TimeSpan.FromTicks(xAxisPixelDuration.Ticks * bmp.Width);
                var freqScale = new FrequencyScale(11025, 512, 1000);
                SpectrogramTools.DrawGridLinesOnImage((Image<Rgb24>)bmp, TimeSpan.Zero, fullDuration, xAxisPixelDuration, freqScale);
                const int trackHeight = 20;
                var recordingStartDate = default(DateTimeOffset);
                var timeBmp = ImageTrack.DrawTimeTrack(fullDuration, recordingStartDate, bmp.Width, trackHeight);
                var array = new Image[2];
                array[0] = bmp;
                array[1] = timeBmp;
                var image = ImageTools.CombineImagesVertically(array);

                // add a header to the spectrogram
                var header = new Image<Rgb24>(image.Width, 20);
                Graphics g = Graphics.FromImage(header);
                g.Clear(Color.LightGray);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                g.DrawString(key, new Font("Tahoma", 9), Brushes.Black, 4, 4);
                list.Add(ImageTools.CombineImagesVertically(new List<Image>(new[] { header, image })));
            }

            // save the image - the directory for the path must exist
            var path = Path.Combine(dir.FullName, baseName + "__Towsey.Acoustic.GreyScaleImages.png");
            var indexImage = ImageTools.CombineImagesInLine(list);
            indexImage?.Save(path);
        }
    }
}
