﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

using AnalysisBase;
using AudioAnalysisTools;

using TowseyLib;

namespace AnalysisPrograms
{
    using Acoustics.Shared.Extensions;

    using AnalysisPrograms.Production;

    using PowerArgs;

    public class IndicesCsv2Display
    {
        public class Arguments
        {
            [ArgDescription("The source csv file to operate on")]
            [Production.ArgExistingFile(Extension = ".csv")]
            [ArgPosition(0)]
            public FileInfo InputCsv { get; set; }

            // Note: not required
            [ArgDescription("The path to the config file")]
            [Production.ArgExistingFile]
            public FileInfo Config { get; set; }

            [ArgDescription("A file path to write output image to")]
            [ArgNotExistingFile(Extension = ".png")]
            [ArgRequired]
            public FileInfo Output { get; set; }
        }

        //use the following for the command line for the <indicesCsv2Image> task. 
        //indicesCsv2Image  "C:\SensorNetworks\Output\SunshineCoast\Site1\Towsey.MultiAnalyser\DM420036_Towsey.MultiAnalyser.Indices.csv"            "C:\SensorNetworks\Software\AudioAnalysis\AnalysisConfigFiles\Towsey.MultiAnalyser.cfg"  C:\SensorNetworks\Output\SunshineCoast\Site1\Towsey.MultiAnalyser\DM420036_Towsey.MultiAnalyser.IndicesNEW.png
        //indicesCsv2Image  "C:\SensorNetworks\Output\Frogs\ShiloDebugOct2012\IndicesCsv2Image\DM420044_20111020_000000_Towsey.Acoustic.Indices.csv" "C:\SensorNetworks\Software\AudioAnalysis\AnalysisConfigFiles\Towsey.Acoustic.cfg"       C:\SensorNetworks\Output\Frogs\ShiloDebugOct2012\IndicesCsv2Image\DM420044_20111020_000000_Towsey.Acoustic.Indices.png


        // indicesCsv2Image  "C:\SensorNetworks\Output\LSKiwi3\Towsey.Acoustic\TOWER_20100208_204500_Towsey.Acoustic.Indices.csv"  "C:\SensorNetworks\Software\AudioAnalysis\AnalysisConfigFiles\Towsey.Acoustic.cfg"   "C:\SensorNetworks\Output\LSKiwi3\Towsey.Acoustic\TOWER_20100208_204500_Towsey.Acoustic.Indices.png

        private static Arguments Dev()
        {
            throw new NoDeveloperMethodException();
        }

        /// <summary>
        /// Loads a csv file for visualisation and displays TracksImage
        /// </summary>
        /// <param name="arguments"></param>
        /// <exception cref="InvalidOperationException"></exception>
        /// <returns></returns>
        public static void Main(Arguments arguments)
        {
            if (arguments == null)
            {
                arguments = Dev();
            }

            bool verbose = true;

            if (arguments.Config == null)
            {
                LoggedConsole.WriteLine("### WARNING: Config file is not provided - using defaults");
            }

            arguments.Output.CreateParentDirectories();

            if (verbose)
            {
                string date = "# DATE AND TIME: " + DateTime.Now;
                LoggedConsole.WriteLine("# MAKE AN IMAGE FROM A CSV FILE OF INDICES DERIVED FROM AN AUDIO RECORDING");
                LoggedConsole.WriteLine(date);
                LoggedConsole.WriteLine("# Input  audio  file: " + arguments.InputCsv);
                LoggedConsole.WriteLine("# Configuration file: " + arguments.Config);
                LoggedConsole.WriteLine("# Output image  file: " + arguments.Output);
            }

            string analysisIdentifier = null;
            if (arguments.Config.Exists)
            {
                var configuration = new ConfigDictionary(arguments.Config);
                Dictionary<string, string> configDict = configuration.GetTable();
                analysisIdentifier = configDict[Keys.ANALYSIS_NAME];
            }

            var outputDTs = Tuple.Create(new DataTable(), new DataTable() );

            var analysers = AnalysisCoordinator.GetAnalysers(typeof(MainEntry).Assembly);
            IAnalyser analyser = analysers.FirstOrDefault(a => a.Identifier == analysisIdentifier);
            if (analyser == null)
            {
                LoggedConsole.WriteLine("\nWARNING: Analysis name not recognized: " + analysisIdentifier);
                LoggedConsole.WriteLine("\t\t Will construct default image");
                outputDTs = DisplayIndices.ProcessCsvFile(arguments.InputCsv);
            }
            else
            {
                outputDTs = analyser.ProcessCsvFile(arguments.InputCsv, arguments.Config);

            }
            analyser = null;

            //DataTable dtRaw = output.Item1;
            DataTable dt2Display = outputDTs.Item2;
            if (dt2Display == null)
            {
                throw new InvalidOperationException("Data table is null - cannot continue");
            }

            // #########################################################################################################
            // Convert datatable to image
            bool normalisedDisplay = false;
            string fileName = Path.GetFileNameWithoutExtension(arguments.Output.Name);
            string title = String.Format("(c) QUT.EDU.AU - SOURCE:{0};  ", fileName);
            Bitmap tracksImage = DisplayIndices.ConstructVisualIndexImage(dt2Display, title, normalisedDisplay, arguments.Output);
            // #########################################################################################################

            if (tracksImage == null)
            {
                LoggedConsole.WriteLine("\nWARNING: Null image returned from DisplayIndices.ConstructVisualIndexImage(dt2Display, title, normalisedDisplay, imagePath);");
                throw new AnalysisOptionDevilException();
            }

        } // Main();
    } //class
}
