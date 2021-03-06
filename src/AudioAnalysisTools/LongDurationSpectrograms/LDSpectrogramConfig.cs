// --------------------------------------------------------------------------------------------------------------------
// <copyright file="LDSpectrogramConfig.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace AudioAnalysisTools.LongDurationSpectrograms
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Acoustics.Shared.ConfigFile;
    using Newtonsoft.Json;
    using YamlDotNet.Serialization;

    /// <summary>
    /// CONFIG CLASS FOR the class LDSpectrogramRGB.
    /// </summary>
    public class LdSpectrogramConfig : Config
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LdSpectrogramConfig"/> class.
        /// CONSTRUCTOR.
        /// </summary>
        public LdSpectrogramConfig()
        {
            // default values
            this.ColorMap1 = LDSpectrogramRGB.DefaultColorMap1;
            this.ColorMap2 = LDSpectrogramRGB.DefaultColorMap2;
            this.ColourFilter = SpectrogramConstants.BACKGROUND_FILTER_COEFF;
            this.BlueEnhanceParameter = 0.0;
            this.XAxisTicInterval = SpectrogramConstants.X_AXIS_TIC_INTERVAL;
            this.FreqScale = "Linear";
            this.YAxisTicInterval = 1000;
        }

        /// <summary>
        /// Gets or sets the type of freq scale.
        /// # Eventual options will be: Linear, Mel, Linear62Octaves31Nyquist11025, Linear125Octaves30Nyquist11025, Octaves24Nyquist32000, Linear125Octaves28Nyquist32000
        /// # Only "Linear", "Linear125Octaves6Tones28Nyquist11025", "Linear125Octaves7Tones28Nyquist32000" work at present.
        /// </summary>
        public string FreqScale { get; set; }

        /// <summary>
        /// Gets or sets parameter to manipulate the color map and appearance of the false-colour spectrogram.
        /// </summary>
        public string ColorMap1 { get; set; }

        /// <summary>
        /// Gets or sets parameter to manipulate the colour map and appearance of the false-colour spectrogram
        /// Pass two color maps because two maps convey more information.
        /// </summary>
        public string ColorMap2 { get; set; }

        /// <summary>
        /// Gets or sets value of the color filter.
        /// Its value must be less than 1.0. Good value is 0.75.
        /// </summary>
        public double? ColourFilter { get; set; }

        /// <summary>
        /// Gets or sets value of the blue enhancement parameter.
        /// Its value must be in 0.0 to 1.0. Current suggested value is 0.5.
        /// </summary>
        public double? BlueEnhanceParameter { get; set; }

        /// <summary>
        /// Gets or sets the default XAxisTicInterval.
        /// The default assumes one minute spectra i.e. 60 per hour
        /// But as of January 2015, this is not fixed. The user can adjust
        ///  the tic interval to be appropriate to the time scale of the spectrogram.
        /// May 2017: XAxisTicIntervalSeconds is the new configuration option!.
        /// </summary>
        [YamlIgnore]
        [JsonIgnore]
        public TimeSpan XAxisTicInterval
        {
            get => TimeSpan.FromSeconds(this.XAxisTicIntervalSeconds);
            set => this.XAxisTicIntervalSeconds = value.TotalSeconds;
        }

        /// <summary>
        /// Gets or sets the default XAxisTicIntervalSeconds.
        /// The default assumes one minute spectra i.e. 60 per hour
        /// But as of January 2015, this is not fixed. The user can adjust
        ///  the tic interval to be appropriate to the time scale of the spectrogram.
        /// May 2017: Now measured in seconds and usage XAxisTicIntervalSeconds is preferred.
        /// </summary>
        public double XAxisTicIntervalSeconds { get; set; }

        /// <summary>
        /// Gets or sets YAxisTicInterval in Hertz.
        /// The vertical spacing between horizontal grid lines for the y-Axis
        /// mark 1 kHz intervals.
        /// </summary>
        public int YAxisTicInterval { get; set; }

        /// <summary>
        /// In seconds, the horizontal spacing between vertical grid lines for the x-Axis.
        /// </summary>
        public double CalculateYAxisTickInterval(double sampleRate, double frameWidth)
        {
                double freqBinWidth = sampleRate / frameWidth;
                return (int)Math.Round(this.YAxisTicInterval / freqBinWidth);
        }

        /// <summary>
        /// READS A YAML CONFIG FILE into a Config variable and then transfers all values into the appropriate config class.
        /// </summary>
        /// <returns>
        /// The <see cref="LdSpectrogramConfig"/>.
        /// </returns>
        public static LdSpectrogramConfig ReadYamlToConfig(FileInfo path)
        {
            var config = ConfigFile.Deserialize<LdSpectrogramConfig>(path);
            return config;
        }

        /// <summary>
        /// NOTE: As of August 2015, we are using EVN (event count) in both spectrograms because CVR (cover) is too highly correlated with POW.
        /// NOTE: As of May 2017, PMN replaces POW.
        /// </summary>
        public static LdSpectrogramConfig GetDefaultConfig()
        {
            return new LdSpectrogramConfig();
        }

        /// <summary>
        /// Gets a default config for long-duration false-color spectrograms.
        /// But allows caller to substitute custom color maps.
        /// </summary>
        public static LdSpectrogramConfig GetDefaultConfig(string colorMap1, string colorMap2)
        {
            var ldSpectrogramConfig = GetDefaultConfig();
            ldSpectrogramConfig.ColorMap1 = colorMap1;
            ldSpectrogramConfig.ColorMap2 = colorMap2;
            return ldSpectrogramConfig;
        }

        public static string[] GetKeys(string colorMap1, string colorMap2)
        {
            var keys = new List<string>();
            if (colorMap1 != null && colorMap1.Length == 11)
            {
                string[] codes = colorMap1.Split('-');
                foreach (string str in codes)
                {
                    keys.Add(str);
                }

                codes = colorMap2.Split('-');
                foreach (string str in codes)
                {
                    if (!keys.Contains(str))
                    {
                        keys.Add(str);
                    }
                }
            }

            return keys.ToArray();
        }

        public static string[] GetKeys(string colorMap)
        {
            string[] keys = null;
            if (colorMap != null && colorMap.Length == 11)
            {
                keys = colorMap.Split('-');
            }

            return keys;
        }

        public string[] GetKeys() => GetKeys(this.ColorMap1, this.ColorMap2);
    }
}