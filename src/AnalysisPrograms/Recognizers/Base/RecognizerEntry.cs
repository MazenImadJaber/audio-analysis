﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="RecognizerEntry.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//   Defines the RecognizerEntry type.
//
// NOTE:  The action type to call a recognizer is "EventRecognizer".
//         The action name should be the first argument on the command line.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AnalysisPrograms.Recognizers.Base
{
    using System;
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;
    using Acoustics.Shared;
    using Acoustics.Shared.ConfigFile;
    using Acoustics.Tools;
    using AnalysisBase;
    using AnalysisBase.Extensions;
    using AudioAnalysisTools;
    using log4net;
    using McMaster.Extensions.CommandLineUtils;
    using Production;
    using Production.Arguments;

    public class RecognizerEntry
    {
        public const string CommandName = "EventRecognizer";
        private const string Description =
            "The entry point for all species or event recognizers.Only to be used on short recordings(< 2 mins)." +
            "This recognizer runs any IEventRecognizer. The recognizer run is based on the " +
            "Identifier field and parsed from the AnalysisName field in the config file of the same name";

        [Command(
            CommandName,
            Description = Description)]
        public class Arguments : SourceConfigOutputDirArguments
        {
            public override Task<int> Execute(CommandLineApplication app)
            {
                RecognizerEntry.Execute(this);

                return this.Ok();
            }
        }

        private static readonly ILog Log = LogManager.GetLogger(nameof(RecognizerEntry));

        [Obsolete("See https://github.com/QutBioacoustics/audio-analysis/issues/134")]
        public static Arguments Dev()
        {
            Log.Info("Runnung Event Recognizer through the RecognizerEntry.Dev() method.");

            // The MULTI-RECOGNISER
            /*
            // Canetoad, Litoria fallax and Limnodynastes convex.
            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\3mile_creek_dam_-_Herveys_Range_1076_248366_20130305_001700_30.wav";
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Frogs\MultiLabel\Gympie_CaneToad_Lnasuta_Lfallax.wav";
            //string outputPath    = @"G:\SensorNetworks\Output\Frogs\Multirecognizer_2016December";
            */

            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\LitoriaWotjulumensisAndRothii\Lrothii_emerald_river_1014_252497_20131216_180244_30_0.wav";
            //string recordingPath = @"D:\SensorNetworks\WavFiles\Frogs\SherynBrodie\Lconvex_Lbicolor_GolfC_20170213_183122.wav";
            string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\RecognizerConfigFiles\Ecosounds.MultiRecognizer.yml";
            string outputPath = @"D:\SensorNetworks\Output\Frogs\TestOfRecognizers-2017August";

            //Ardea insignis (The White-bellied Herron
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Bhutan\Heron_commonCall_downsampled.wav";
            //string recordingPath = @"E:\SensorNetworks\WavFiles\Bhutan\waklaytar_site3_1379_347077_20160409_061730_40_0.wav";
            //string recordingPath = @"E:\SensorNetworks\WavFiles\Bhutan\waklaytar_site3_1379_347093_20160404_061431_130_0.wav";
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Bhutan\Both call types of Heron.wav";
            //string recordingPath = @"E:\SensorNetworks\WavFiles\Bhutan\waklaytar_site3_1379_347103_20160329_133319_130_0.wav";
            //string recordingPath = @"E:\SensorNetworks\WavFiles\Bhutan\waklaytar_site3_1379_347093_20160404_062131_40_0.wav";
            //string outputPath = @"G:\SensorNetworks\Output\Bhutan\";
            //string recordingPath = @"C:\SensorNetworks\WavFiles\TsheringDema\WBH12HOURS-D_20160403_120000_238min.wav";

            //string recordingPath = @"G:\SensorNetworks\WavFiles\Bhutan\SecondDeployment\WBH12HOURS-N_20160426_000010.wav";
            //string outputPath = @"C:\SensorNetworks\Output\TsheringDema";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\RecognizerConfigFiles\Towsey.ArdeaInsignis.yml";

            // Path from Anthony
            // "C:\Users\Administrator\Desktop\Sensors Analysis\ParallelExecutables\2\AnalysisPrograms.exe" audio2csv - source "Y:\Tshering\WBH_Walaytar\201505 - second deployment\Site2_Waklaytar\24Hours WBH_28032016\WBH12HOURS-D_20160403_120000.wav" - config "C:\Users\Administrator\Desktop\Sensors Analysis\Towsey.ArdeaInsignis.Parallel.yml" - output "Y:\Results\2016Oct31-145124\Tshering\WBH_Walaytar\201505 - second deployment\Site2_Waklaytar\24Hours WBH_28032016\WBH12HOURS-D_20160403_120000.wav" - tempdir F:\2 - m True - n

            /*
            // Canetoad
            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\Canetoad\Groote_20160803_151738_Canetoad_LinsPlayback.wav";   // Positive call
            //string recordingPath = @"C:\Work\GitHub\recognizer-tests\tests\species\Rhinella_marina\data\CaneToad_Gympie.wav";         // Positive call
            //string recordingPath = @"C:\Work\GitHub\recognizer-tests\tests\species\Rhinella_marina\data\Lwotjulumensis_trill_bickerton_20131212_214430.wav";    // Positive call
            //string recordingPath = @"C:\Work\GitHub\recognizer-tests\tests\species\Rhinella_marina\data\TruckMotor_20150603_004248.wav"; // Negative call
            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\Canetoad\FalsePositives\FalsePosFromPaul_2015-06-02-031015_downsampled.wav";
            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\Canetoad\FalsePositives\FalsePosFromPaul_2015-06-03-004248_downsampled.wav";
            //string recordingPath = @"C:\SensorNetworks\WavFiles\TestRecordings\CaneToads_rural1_20.mp3";
            //string recordingPath = @"Y:\Groote\2016 March\Emerald River\CardA\Data\EMERALD_20150703_103506.wav";
            //string outputPath = @"C:\SensorNetworks\Output\Frogs\Canetoad\Rural1";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\RecognizerConfigFiles\Towsey.RhinellaMarina.yml";
            */

            /*
            //Crinia remota
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Frogs\CriniaSpecies\EmeraldRiver_CriniaRemota_20140206_032030.wav";
            //string outputPath    = @"G:\SensorNetworks\Output\Frogs\TestOfRecognizers-2016November";
            string configPath    = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.CriniaRemota.yml";
            */

            /*
            //Crinia tinnula
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Frogs\Crinia\CriniaTinnula.wav";
            //string outputPath = @"C:\SensorNetworks\Output\Frogs\TestOfRecognisers-2016October";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Stark.CriniaTinnula.yml";
            */

            /*
            // Cyclorana novaehollandiae
            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\28 Cyclorana novaehollandiae.mp3";
            //string outputPath = @"C:\SensorNetworks\Output\Frogs\TestOfRecognisers-2016Sept\Test";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.CycloranaNovaeholl.yml";
            */

            /*
            // Lewin's Rail  --  Lewinia pectoralis
            //string recordingPath = @"C:\SensorNetworks\WavFiles\LewinsRail\BAC1_20071008-081607.wav";
            //string recordingPath = @"C:\SensorNetworks\WavFiles\LewinsRail\BAC1_20071008-084607.wav";
            //string recordingPath = @"C:\SensorNetworks\WavFiles\LewinsRail\BAC2_20071008-062040.wav";
            //string recordingPath = @"C:\SensorNetworks\WavFiles\LewinsRail\BAC2_20071008-075040.wav";
            //string recordingPath = @"C:\SensorNetworks\WavFiles\LewinsRail\BAC2_20071008-085040.wav";
            //string configPath    = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\RecognizerConfigFiles\Towsey.LewiniaPectoralis.yml";
            //string outputPath     = @"C:\SensorNetworks\Output\LewinsRail\";
            */

            /*
            // LEWIN'S RAIL TEST
            //string recordingPath = @"G:\SensorNetworks\SoftwareTests\UnitTest_Towsey.LewiniaPectoralis\Data\BAC2_20071008-085040.wav";
            //string configPath = @"G:\SensorNetworks\SoftwareTests\UnitTest_Towsey.LewiniaPectoralis\Data\Towsey.LewiniaPectoralis.yml";
            //string outputPath = @"G:\SensorNetworks\SoftwareTests\UnitTest_Towsey.LewiniaPectoralis";
            string recordingPath = @"G:\SensorNetworks\WavFiles\LewinsRail\BAC2_20071008-085040.wav";
            //string recordingPath = @"C:\SensorNetworks\WavFiles\LewinsRail\BAC2_20071008-085040.wav";
            string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\RecognizerConfigFiles\Towsey.LewiniaPectoralis.yml";
            string outputPath = @"C:\SensorNetworks\Output\LewinsRail\Results2017";
            */

            // Limnodynastes convex
            //string recordingPath = @"D:\SensorNetworks\WavFiles\Frogs\LimnodynastesSpecies\10 Limnodynastes convexiusculusMONO.wav";
            //string recordingPath = @"D:\SensorNetworks\WavFiles\Frogs\SherynBrodie\3mile_creek_dam_-_Herveys_Range_1076_248366_20130305_001700_30.wav";
            string recordingPath = @"D:\SensorNetworks\WavFiles\Frogs\SherynBrodie\Lconvex_Paradise_20170206_0220.wav";
            //string outputPath = @"C:\SensorNetworks\Output\Frogs\TestOfRecognizers-2016Sept\LimnoConvex";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\RecognizerConfigFiles\Towsey.LimnodynastesConvex.yml";

            // Litoria bicolor
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\Bickerton\bicolor_bickerton_island_1013_255205_20131211_191621_30_0.wav";
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\Bickerton\bicolor_bickerton_island_1013_255205_20131211_195821_30_0.wav";
            //string outputPath = @"G:\SensorNetworks\Output\Frogs\TestOfRecognizers-2016October\";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\RecognizerConfigFiles\Towsey.LitoriaBicolor.yml";

            // Litoria caerulea Common green tree frog
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\Groote\EmeraldRiver_LitoriaCaerulea_Lrothii_20131223_220522.wav";
            //string outputPath = @"G:\SensorNetworks\Output\Frogs\TestOfRecognizers-2016November";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\RecognizerConfigFiles\Towsey.LitoriaCaerulea.yml";

            // Litoria fallax
            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\53 Litoria fallax.mp3";
            //string outputPath = @"C:\SensorNetworks\Output\Frogs\TestOfRecognizers-2016Sept\Test";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.LitoriaFallax.yml";

            // Litoria nasuta - WORKING ON THIS JUST BEFORE 2016 CHRISTMAS BREAK
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\43 Litoria nasuta.mp3";
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\Groote\EmeraldRiver_LitoriaNasuta_Lbicolor_20131225_223700_30_0.wav";
            //string outputPath    = @"G:\SensorNetworks\Output\Frogs\TestOfRecognizers-2016December";
            //string configPath    = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.LitoriaNasuta.yml";

            // Litoria olongburensis
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\TEST_16000Hz_LitoriaOlongburensis.wav";
            //string outputPath = @"C:\SensorNetworks\Output\Frogs\TestOfRecognizers-2016Sept\Canetoad";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.Canetoad.yml";

            // Litoria rothii.
            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\49 Litoria rothii.mp3";
            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\69 Litoria rothii.mp3";
            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\LitoriaWotjulumensisAndRothii\bickerton_island_1013_255205_20131211_194041_30_0.wav";
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\Bickerton\rothii_bickerton_island_1013_255213_20131212_205130_30_0.wav";
            //string recordingPath = @"D:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\Bickerton\rothii_bickerton_island_1013_255213_20131212_205630_30_0.wav";
            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\LitoriaWotjulumensisAndRothii\Lrothii_emerald_river_1014_252497_20131216_180244_30_0.wav";
            //string outputPath = @"D:\SensorNetworks\Output\Frogs\TestOfRecognizers-2017August\";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.LitoriaRothii.yml";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.CriniaRemota.yml";

            // Litoria rubella
            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\50 Litoria rubella.mp3";
            //string outputPath = @"C:\SensorNetworks\Output\Frogs\TestOfRecognisers-2016Sept\Test";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.LitoriaRubella.yml";

            // Litoria wotjulumensis
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Frogs\LitoriaSpecies\Bickerton\wotjulumensis_bickerton_island_1013_255205_20131211_192951_30_0.wav";
            //string outputPath    = @"G:\SensorNetworks\Output\Frogs\TestOfRecognizers-2016October\";
            //string configPath    = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.LitoriaWatjulumensis.yml";

            // Platyplectrum ornatum
            //string recordingPath = @"E:\SensorNetworks\WavFiles\Frogs\PlatyplectrumSp\p_ornatum_bickerton_island_1013_255599_20140213_214500_30_0.wav";
            //string outputPath = @"E:\SensorNetworks\Output\Frogs\TestOfRecognizers-2016October\";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\RecognizerConfigFiles\Towsey.PlatyplectrumOrnatum.yml";

            // Uperoleia inundata
            //string recordingPath = @"E:\SensorNetworks\WavFiles\Frogs\UperoleiaSp\u_inundata_bickerton_island_1013_255713_20140112_212900_30_0.wav";
            //string recordingPath = @"E:\SensorNetworks\WavFiles\Frogs\UperoleiaSp\u_inundata_bickerton_island_1013_255713_20140112_213030_30_0.wav";
            //string configPath    = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.UperoleiaInundata.yml";
            //string outputPath    = @"E:\SensorNetworks\Output\Frogs\TestOfRecognizers-2016October";

            // Uperoleia lithomoda
            //string recordingPath = @"G:\SensorNetworks\WavFiles\Frogs\UperoleiaSpecies\UperoleiaLithomoda_BickertonIsland_140128_201100.mp3";
            //string outputPath = @"G:\SensorNetworks\Output\Frogs\TestOfRecognizers-2016December";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.UperoleiaLithomoda.yml";

            // Uperoleia mimula
            //string recordingPath = @"C:\SensorNetworks\WavFiles\Frogs\23 Uperoleia mimula.mp3";
            //string outputPath = @"C:\SensorNetworks\Output\Frogs\TestOfRecognisers-2016Sept\Test";
            //string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.UperoleiaMimula.yml";

            /*
            // Fresh water blue cat fish
            string recordingPath = @"C:\SensorNetworks\WavFiles\Freshwater\BlueCatfish_LonePine_ChrisAfterFiltering.wav";
            string recordingPath = @"C:\SensorNetworks\WavFiles\Freshwater\BlueCatfish_LonePine_LeftChannel_First60s.wav";
            string recordingPath = @"C:\SensorNetworks\WavFiles\Freshwater\BlueCatfish_LonePine_LeftChannel.wav";
            string recordingPath = @"C:\SensorNetworks\WavFiles\Freshwater\BlueCatfish_LonePine_ChrisFilteredLeftChFirst60s.wav";
            string outputPath = @"C:\SensorNetworks\Output\FreshWater";
            string configPath = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.IctalurusFurcatus.yml";
            */

            var arguments = new Arguments
            {
                Source = recordingPath.ToFileInfo(),
                Config = configPath.ToFileInfo(),
                Output = outputPath.ToDirectoryInfo(),
            };

            // #########  NOTE: All other parameters are set in the .yml file assigned to configPath variable above.
            return arguments;
        }

        /// <summary>
        /// This entrypoint should be used for testing short files (less than 2 minutes)
        /// </summary>
        public static void Execute(Arguments arguments)
        {
            if (arguments == null)
            {
                arguments = Dev();
            }

            Log.Info("Running event recognizer");

            var sourceAudio = arguments.Source;
            var configFile = arguments.Config;
            var outputDirectory = arguments.Output;

            if (configFile == null)
            {
                throw new FileNotFoundException("No config file argument provided");
            }
            else if (!configFile.Exists)
            {
                Log.Warn($"Config file {configFile.FullName} not found... attempting to resolve config file");
                arguments.Config = configFile = ConfigFile.ResolveConfigFile(configFile.Name, Directory.GetCurrentDirectory().ToDirectoryInfo());
            }

            LoggedConsole.WriteLine("# Recording file:      " + sourceAudio.FullName);
            LoggedConsole.WriteLine("# Configuration file:  " + configFile);
            LoggedConsole.WriteLine("# Output folder:       " + outputDirectory);

            Log.Info("Reading configuration file");
            dynamic configuration = Yaml.Deserialise(configFile);
            string analysisIdentifier = configuration[AnalysisKeys.AnalysisName];

            Log.Info("Attempting to run recognizer: " + analysisIdentifier);

            // find an appropriate event IAnalyzer
            IAnalyser2 recognizer = AnalyseLongRecordings.AnalyseLongRecording.FindAndCheckAnalyser(analysisIdentifier);

            // get default settings
            AnalysisSettings analysisSettings = recognizer.DefaultSettings;

            // convert arguments to analysis settings
            analysisSettings = arguments.ToAnalysisSettings(
                analysisSettings,
                outputIntermediate: true,
                resultSubDirectory: recognizer.Identifier,
                configuration: configuration);

            // Enable this if you want the Config file ResampleRate parameter to work.
            // Generally however the ResampleRate should remain at 22050Hz for all recognizers.
            //analysisSettings.AnalysisTargetSampleRate = (int) configuration[AnalysisKeys.ResampleRate];

            // get transform input audio file - if needed
            Log.Info("Querying source audio file");
            var audioUtilityRequest = new AudioUtilityRequest()
            {
                TargetSampleRate = analysisSettings.AnalysisTargetSampleRate,
            };
            var preparedFile = AudioFilePreparer.PrepareFile(
                arguments.Output,
                arguments.Source,
                MediaTypes.MediaTypeWav,
                audioUtilityRequest,
                arguments.Output);

            var source = preparedFile.SourceInfo.ToSegment();
            var prepared = preparedFile.TargetInfo.ToSegment(FileSegment.FileDateBehavior.None);
            var segmentSettings = new SegmentSettings<FileInfo>(
                analysisSettings,
                source,
                (analysisSettings.AnalysisOutputDirectory, analysisSettings.AnalysisTempDirectory),
                prepared);

            if (preparedFile.TargetInfo.SampleRate.Value != analysisSettings.AnalysisTargetSampleRate)
            {
                Log.Warn("Input audio sample rate does not match target sample rate");
            }

            // Execute a pre analyzer hook
            recognizer.BeforeAnalyze(analysisSettings);

            // execute actual analysis - output data will be written
            Log.Info("Running recognizer: " + analysisIdentifier);
            AnalysisResult2 results = recognizer.Analyze(analysisSettings, segmentSettings);

            // run summarize code - output data can be written
            Log.Info("Running recognizer summary: " + analysisIdentifier);
            recognizer.SummariseResults(
                analysisSettings,
                source,
                results.Events,
                results.SummaryIndices,
                results.SpectralIndices,
                new[] { results });

            //Log.Info("Recognizer run, saving extra results");
            // TODO: Michael, output anything else as you wish.

            Log.Debug("Clean up temporary files");
            if (source.Source.FullName != prepared.Source.FullName)
            {
                prepared.Source.Delete();
            }

            int eventCount = results?.Events?.Length ?? 0;
            Log.Info($"Number of detected events: {eventCount}");
            Log.Success(recognizer.Identifier + " recognizer has completed");
        }
    }
}