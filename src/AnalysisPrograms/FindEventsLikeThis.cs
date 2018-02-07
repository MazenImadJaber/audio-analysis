﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TowseyLib;
using AudioAnalysisTools;



namespace AnalysisPrograms
{
    class FindEventsLikeThis
    {

        //Following lines are used for the debug command line.
        //CANETOAD
        //felt  "C:\SensorNetworks\WavFiles\Canetoad\DM420010_128m_00s__130m_00s - Toads.mp3" C:\SensorNetworks\Output\FELT_CaneToad\FELT_CaneToad_Params.txt events.txt
        //GECKO
        //felt "C:\SensorNetworks\WavFiles\Gecko\Suburban_March2010\geckos_suburban_104.mp3"          C:\SensorNetworks\Output\FELT_Gecko\FELT_Gecko_Params.txt FELT_Gecko1
        //felt "C:\SensorNetworks\WavFiles\Gecko\Gecko05012010\DM420008_26m_00s__28m_00s - Gecko.mp3" C:\SensorNetworks\Output\FELT_Gecko\FELT_Gecko_Params.txt FELT_Gecko1
        //KOALA MALE EXHALE
        //felt "C:\SensorNetworks\WavFiles\Koala_Male\Recordings\KoalaMale\LargeTestSet\WestKnoll_Bees_20091103-190000.wav" C:\SensorNetworks\Output\FELT_KoalaMaleExhale\KoalaMaleExhale_Params.txt events.txt
        //felt "C:\SensorNetworks\WavFiles\Koala_Male\SmallTestSet\HoneymoonBay_StBees_20080905-001000.wav" C:\SensorNetworks\Output\FELT_KoalaMaleExhale\KoalaMaleExhale_Params.txt events.txt
        //KOALA MALE FOREPLAY
        //felt "C:\SensorNetworks\WavFiles\Koala_Male\SmallTestSet\HoneymoonBay_StBees_20080905-001000.wav" C:\SensorNetworks\Output\FELT_KoalaMaleForeplay_LargeTestSet\KoalaMaleForeplay_Params.txt events.txt
        //BRIDGE CREEK
        //felt "C:\SensorNetworks\WavFiles\Length1_2_4_8_16mins\BridgeCreek_1min.wav" C:\SensorNetworks\Output\TestWavDuration\DurationTest_Params.txt events.txt
        //CURLEW
        //felt "C:\SensorNetworks\WavFiles\Curlew\Curlew2\Top_Knoll_-_St_Bees_20090517-210000.wav" C:\SensorNetworks\Output\FELT_CURLEW\FELT_CURLEW_Params.txt FELT_Curlew1_Curated2_symbol.txt
        // @"C:\SensorNetworks\WavFiles\Curlew\Curlew_JasonTagged\West_Knoll_Bees_20091102-000000.mp3"
        //CURRAWONG
        //felt "C:\SensorNetworks\WavFiles\Currawongs\Currawong_JasonTagged\West_Knoll_Bees_20091102-003000.wav" C:\SensorNetworks\Output\FELT_Currawong\FELT_Currawong_Params.txt FELT_Currawong2_curatedBinary.txt


        //Keys to recognise identifiers in PARAMETERS - INI file. 
        public static string key_CALL_NAME       = "CALL_NAME";
        public static string key_DO_SEGMENTATION = "DO_SEGMENTATION";
        public static string key_MIN_HZ          = "MIN_HZ";
        public static string key_MAX_HZ          = "MAX_HZ";
        public static string key_FRAME_OVERLAP   = "FRAME_OVERLAP";
        public static string key_SMOOTH_WINDOW   = "SMOOTH_WINDOW";
        public static string key_MIN_DURATION    = "MIN_DURATION";
        public static string key_DYNAMIC_RANGE   = "DYNAMIC_RANGE";
        public static string key_EVENT_THRESHOLD = "EVENT_THRESHOLD";
        public static string key_DRAW_SONOGRAMS  = "DRAW_SONOGRAMS";

        public static string eventsFile = "events.txt";


        public static void Dev(string[] args)
        {
            string title = "# FIND OTHER ACOUSTIC EVENTS LIKE THIS ONE";
            string date  = "# DATE AND TIME: " + DateTime.Now;
            Log.WriteLine(title);
            Log.WriteLine(date);

            Log.Verbosity = 1;
            Segment.CheckArguments(args);


            string recordingPath = args[0];    //the recording
            string iniPath       = args[1];    //parameters / ini file
            string targetFName   = args[2];    //name of target file 

            string targetName = Path.GetFileNameWithoutExtension(targetFName);
            string outputDir  = Path.GetDirectoryName(iniPath) + "\\";
            string opPath     = outputDir + targetName + "_info.txt";
            string matrixPath = outputDir + targetName + "_matrix.txt";
            string imagePath  = outputDir + targetName + "_target.png";
            string binaryTemplatePath  = outputDir + targetFName;
           // string trinaryTemplatePath = outputDir + targetName + "_curatedTrinary.txt";
            
            Log.WriteIfVerbose("# Output folder =" + outputDir);


            //i: GET RECORDING
            AudioRecording recording = new AudioRecording(recordingPath);

            //ii: READ PARAMETER VALUES FROM INI FILE
            var config = new Configuration(iniPath);
            Dictionary<string, string> dict = config.GetTable();
            Dictionary<string, string>.KeyCollection keys = dict.Keys;

            string callName     = dict[key_CALL_NAME];
            double frameOverlap = Double.Parse(dict[key_FRAME_OVERLAP]);
            bool doSegmentation = Boolean.Parse(dict[key_DO_SEGMENTATION]);
            //double dynamicRange = Double.Parse(dict[key_DYNAMIC_RANGE]);      //dynamic range for target events
            double smoothWindow = Double.Parse(dict[key_SMOOTH_WINDOW]);      //before segmentation 
            int minHz = Int32.Parse(dict[key_MIN_HZ]);
            int maxHz = Int32.Parse(dict[key_MAX_HZ]);
            double minDuration    = Double.Parse(dict[key_MIN_DURATION]);     //min duration of event in seconds 
            double eventThreshold = Double.Parse(dict[key_EVENT_THRESHOLD]);  //min score for an acceptable event
            int DRAW_SONOGRAMS    = Int32.Parse(dict[key_DRAW_SONOGRAMS]);    //options to draw sonogram

            Log.WriteIfVerbose("Freq band: {0} Hz - {1} Hz.)", minHz, maxHz);
            Log.WriteIfVerbose("Min Duration: " + minDuration + " seconds");

            //iii: GET THE TARGET
            double[,] targetMatrix = ReadChars2TrinaryMatrix(binaryTemplatePath);
            //double[,] targetMatrix = ReadChars2TrinaryMatrix(trinaryTemplatePath);

            //iv: Find matching events
            //#############################################################################################################################################
            var results = FindMatchingEvents.ExecuteFELT(targetMatrix, recording, doSegmentation, minHz, maxHz, frameOverlap, smoothWindow, eventThreshold, minDuration);
            var sonogram       = results.Item1;
            var matchingEvents = results.Item2;
            var scores         = results.Item3;
            double matchThreshold = results.Item4;
            Log.WriteLine("# Finished detecting events like the target.");
            int count = matchingEvents.Count;
            Log.WriteLine("# Matching Event Count = " + matchingEvents.Count());
            Log.WriteLine("           @ threshold = {0:f3}", matchThreshold);
            //#############################################################################################################################################

            //v: write events count to results info file. 
            double sigDuration = sonogram.Duration.TotalSeconds;
            string fname = Path.GetFileName(recordingPath);
            string str = String.Format("{0}\t{1}\t{2}", fname, sigDuration, count);
            StringBuilder sb = AcousticEvent.WriteEvents(matchingEvents, str);
            FileTools.WriteTextFile(opPath, sb.ToString());


            //draw images of sonograms
            string opImagePath = outputDir + Path.GetFileNameWithoutExtension(recordingPath) + "_matchingEvents.png";
            if (DRAW_SONOGRAMS == 2)
            {
                DrawSonogram(sonogram, opImagePath, matchingEvents, matchThreshold, scores);
            }
            else
            if ((DRAW_SONOGRAMS == 1) && (matchingEvents.Count > 0))
            {
                DrawSonogram(sonogram, imagePath, matchingEvents, matchThreshold, scores);
            }

            Log.WriteLine("# Finished recording:- " + Path.GetFileName(recordingPath));
            Console.ReadLine();
        } //Dev()


        /// <summary>
        /// reads matrix of chars into a trinary symbolisation of a call.
        /// </summary>
        /// <param name="symbolPath"></param>
        /// <returns></returns>
        public static double[,] ReadChars2TrinaryMatrix(string symbolPath)
        {
            List<string> lines = FileTools.ReadTextFile(symbolPath);
            int rows = lines.Count;
            int cols = lines[0].Length;
            var m = new double[rows,cols];
            for (int r = 0; r < rows; r++)
            {
                string line = lines[r];
                for (int c = 0; c < cols; c++)
                    if(line[c] == '+') m[r,c] = 1.0;
                    else if (line[c] == '-') m[r, c] = -1.0;
                    else if (line[c] == '0') m[r, c] = 0.0;
                    else
                    {
                        m[r, c] = 0.0;
                        Log.WriteLine("### WARNING WARNING WARNING! Non-standard CHAR in file: {0}", Path.GetFileName(symbolPath));
                        Log.WriteLine("###     Non-standard CHAR = {0} at position {1},{2}", line[c], r, c);
                    }
            }
            return m;
        }



        public static void DrawSonogram(BaseSonogram sonogram, string path, List<AcousticEvent> predictedEvents, double threshold, double[] scores)
        {
            Log.WriteLine("# Start to draw image of sonogram.");
            bool doHighlightSubband = false; bool add1kHzLines = true;

            using (System.Drawing.Image img = sonogram.GetImage(doHighlightSubband, add1kHzLines))
            using (Image_MultiTrack image = new Image_MultiTrack(img))
            {
                //img.Save(@"C:\SensorNetworks\WavFiles\temp1\testimage1.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                image.AddTrack(Image_Track.GetTimeTrack(sonogram.Duration, sonogram.FramesPerSecond));
                if (scores != null)
                {
                    double normMax = threshold * 4; //so normalised eventThreshold = 0.25
                    for (int i = 0; i < scores.Length; i++)
                    {
                        scores[i] /= normMax;
                        if (scores[i] > 1.0) scores[i] = 1.0;
                        if (scores[i] < 0.0) scores[i] = 0.0;
                    }

                    image.AddTrack(Image_Track.GetScoreTrack(scores, 0.0, 1.0, 0.25));
                }
                image.AddEvents(predictedEvents);
                image.Save(path);
            }
        }

    }//end class
}