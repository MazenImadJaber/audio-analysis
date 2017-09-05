// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpectrogramZoomingConfig.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//   Defines the SpectrogramZoomingConfig type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AudioAnalysisTools.LongDurationSpectrograms
{
    using System;

    using Indices;

    public class SpectrogramZoomingConfig : IIndexPropertyReferenceConfiguration
    {
        public SpectrogramZoomingConfig()
        {
        }

        public string IndexPropertiesConfig { get; set; }

        /// <summary>
        /// Gets or sets an optional reference to a config that defines
        /// the style for drawing LD spectrograms.
        /// </summary>
        public LdSpectrogramConfig LdSpectrogramConfig { get; set; } = new LdSpectrogramConfig();

        /// <summary>
        /// Gets or sets a value indicating whether or not to render images using distributions (rather than index properties)
        /// </summary>
        public bool UseDistributionsForNormalization { get; set; } = false;

        public double LowerNormalizationBoundForDecibelSpectrograms { get; set; } = -100;

        /// <summary>
        /// Gets or sets MaxTilePerSuperTile.
        /// Controls how large the super tiles are.
        /// </summary>
        [Obsolete]
        public int MaxTilesPerSuperTile { get; set; }

        public double SpectralFrameDuration { get; set; }

        /// <summary>
        /// Get or sets the number of zoom levels to render for standard FFT spectrogram images.
        /// Should contain about 1-3 steps following an inverse base-2 power distribution
        /// </summary>
        public double[] SpectralFrameScale { get; set; } = { 0.7, 0.04, 0.02 };

        /// <summary>
        /// Get or sets the number of zoom levels for index based images.
        /// Should contain about 11-12 steps following an inverse base-2 power distribution
        /// </summary>
        public double[] SpectralIndexScale { get; set; } = { 60, 24, 12, 6, 2, 1, 0.6, 0.2 };

        public int TileWidth { get; set; }

        public string TilingProfile { get; set; }

        public double UpperNormalizationBoundForDecibelSpectrograms { get; set; } = -30;

        public int ScalingFactorSpectralFrame(double scaleValueSecondsPerPixel)
        {
            var scaleFactor = (int)Math.Round(scaleValueSecondsPerPixel / this.SpectralFrameDuration);
            return scaleFactor;
        }

        public int ScalingFactorSpectralIndex(double scaleValueSecondsPerPixel, double indexCalculationDuration)
        {
            var scaleFactor = (int)Math.Round(scaleValueSecondsPerPixel / indexCalculationDuration);
            return scaleFactor;
        }

        public double SuperTileCount(TimeSpan recordingDuration, double scaleValueSecondsPerPixel)
        {
            TimeSpan supertileDuration =
                TimeSpan.FromSeconds(this.TileWidth * scaleValueSecondsPerPixel * this.MaxTilesPerSuperTile);
            double count = recordingDuration.TotalMilliseconds / supertileDuration.TotalMilliseconds;
            return count;
        }

        public int SuperTileWidthDefault()
        {
            return this.TileWidth * this.MaxTilesPerSuperTile;
        }

        ///// <summary>
        ///// returns fractional tile count generated by a recording at any one scale
        ///// </summary>
        //public double TileCount(TimeSpan recordingDuration, double scaleValueSecondsPerPixel)
        //{
        //    TimeSpan tileDuration = TimeSpan.FromSeconds(this.TileWidth * scaleValueSecondsPerPixel);
        //    double count = recordingDuration.TotalMilliseconds / tileDuration.TotalMilliseconds;
        //    return count;
        //}
        //
        //public TimeSpan TimePerSuperTile(double scaleValueSecondsPerPixel)
        //{
        //    return TimeSpan.FromSeconds(this.TileWidth * scaleValueSecondsPerPixel * this.MaxTilesPerSuperTile);
        //}
        //
        //public TimeSpan TimePerTile(double scaleValueSecondsPerPixel)
        //{
        //    return TimeSpan.FromSeconds(this.TileWidth * scaleValueSecondsPerPixel);
        //}
    }
}