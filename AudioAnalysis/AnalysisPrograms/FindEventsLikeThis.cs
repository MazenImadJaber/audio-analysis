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
        //felt  "C:\SensorNetworks\WavFiles\Canetoad\DM420010_128m_00s__130m_00s - Toads.mp3" C:\SensorNetworks\Output\FELT_CaneToad\FOELT_CaneToad_Params.txt events.txt
        //GECKO
        //felt "C:\SensorNetworks\WavFiles\Gecko\Suburban_March2010\geckos_suburban_104.mp3"          C:\SensorNetworks\Output\FELT_Gecko\FELT_Gecko_Params.txt events.txt
        //felt "C:\SensorNetworks\WavFiles\Gecko\Gecko05012010\DM420008_26m_00s__28m_00s - Gecko.mp3" C:\SensorNetworks\Output\FELT_Gecko\FELT_Gecko_Params.txt events.txt
        //KOALA MALE EXHALE
        //felt "C:\SensorNetworks\WavFiles\Koala_Male\Recordings\KoalaMale\LargeTestSet\WestKnoll_Bees_20091103-190000.wav" C:\SensorNetworks\Output\FELT_KoalaMaleExhale\KoalaMaleExhale_Params.txt events.txt
        //felt "C:\SensorNetworks\WavFiles\Koala_Male\SmallTestSet\HoneymoonBay_StBees_20080905-001000.wav" C:\SensorNetworks\Output\FELT_KoalaMaleExhale\KoalaMaleExhale_Params.txt events.txt
        //KOALA MALE FOREPLAY
        //felt "C:\SensorNetworks\WavFiles\Koala_Male\SmallTestSet\HoneymoonBay_StBees_20080905-001000.wav" C:\SensorNetworks\Output\FELT_KoalaMaleForeplay_LargeTestSet\KoalaMaleForeplay_Params.txt events.txt
        //BRIDGE CREEK
        //felt "C:\SensorNetworks\WavFiles\Length1_2_4_8_16mins\BridgeCreek_1min.wav" C:\SensorNetworks\Output\TestWavDuration\DurationTest_Params.txt events.txt




        //Keys to recognise identifiers in PARAMETERS - INI file. 
        //public static string key_FILE_EXT        = "FILE_EXT";
        public static string key_DO_SEGMENTATION = "DO_SEGMENTATION";
        public static string key_MIN_HZ = "MIN_HZ";
        public static string key_MAX_HZ = "MAX_HZ";
        public static string key_FRAME_OVERLAP = "FRAME_OVERLAP";
        public static string key_SMOOTH_WINDOW = "SMOOTH_WINDOW";
        //public static string key_DCT_THRESHOLD = "DCT_THRESHOLD";
        //public static string key_MIN_OSCIL_FREQ = "MIN_OSCIL_FREQ";
        //public static string key_MAX_OSCIL_FREQ = "MAX_OSCIL_FREQ";
        public static string key_MIN_DURATION = "MIN_DURATION";
        //public static string key_MAX_DURATION = "MAX_DURATION";
        public static string key_EVENT_THRESHOLD = "EVENT_THRESHOLD";
        public static string key_DRAW_SONOGRAMS = "DRAW_SONOGRAMS";

        public static string eventsFile = "events.txt";


        public static void Dev(string[] args)
        {
            string title = "# FIND OTHER ACOUSTIC EVENTS LIKE THIS ONE";
            string date  = "# DATE AND TIME: " + DateTime.Now;
            Log.WriteLine(title);
            Log.WriteLine(date);

            Log.Verbosity = 1;
            Segment.CheckArguments(args);


            string recordingPath = args[0];
            string iniPath = args[1];
            string outputDir = Path.GetDirectoryName(iniPath) + "\\";
            string opFName = args[2];
            string opPath = outputDir + opFName;
            Log.WriteIfVerbose("# Output folder =" + outputDir);


            //READ PARAMETER VALUES FROM INI FILE
            var config = new Configuration(iniPath);
            Dictionary<string, string> dict = config.GetTable();
            Dictionary<string, string>.KeyCollection keys = dict.Keys;

            bool doSegmentation = Boolean.Parse(dict[key_DO_SEGMENTATION]);
            int minHz = Int32.Parse(dict[key_MIN_HZ]);
            int maxHz = Int32.Parse(dict[key_MAX_HZ]);
            double frameOverlap = Double.Parse(dict[key_FRAME_OVERLAP]);
            double smoothWindow = Double.Parse(dict[key_SMOOTH_WINDOW]);       //duration of DCT in seconds 
            double minDuration = Double.Parse(dict[key_MIN_DURATION]);       //min duration of event in seconds 
            double eventThreshold = Double.Parse(dict[key_EVENT_THRESHOLD]);  //min score for an acceptable event
            int DRAW_SONOGRAMS = Int32.Parse(dict[key_DRAW_SONOGRAMS]);      //options to draw sonogram

            Log.WriteIfVerbose("Freq band: {0} Hz - {1} Hz.)", minHz, maxHz);
            Log.WriteIfVerbose("Min Duration: " + minDuration + " seconds");

            //#############################################################################################################################################
            var results = Execute(recordingPath, doSegmentation, minHz, maxHz, frameOverlap, smoothWindow, eventThreshold, minDuration);
            Log.WriteLine("# Finished detecting events like taqrget.");
            //#############################################################################################################################################

            var sonogram = results.Item1;
            var predictedEvents = results.Item2;
            var intensity = results.Item3;
            Log.WriteLine("# Event Count = " + predictedEvents.Count());

            //write event count to results file. 
            double sigDuration = sonogram.Duration.TotalSeconds;
            string fname = Path.GetFileName(recordingPath);
            int count = predictedEvents.Count;
            //string str = String.Format("#RecordingName\tDuration(sec)\t#Ev\tCompT(ms)\t%hiFrames\n{0}\t{1}\t{2}\t{3}\t{4}\n", fname, sigDuration, count, analysisDuration.TotalMilliseconds, pcHIF);
            //StringBuilder sb = new StringBuilder(str);
            //StringBuilder sb = new StringBuilder();
            string str = String.Format("{0}\t{1}\t{2}", fname, sigDuration, count);
            StringBuilder sb = AcousticEvent.WriteEvents(predictedEvents, str);
            FileTools.WriteTextFile(opPath, sb.ToString());


            //draw images of sonograms
            string imagePath = outputDir + Path.GetFileNameWithoutExtension(recordingPath) + ".png";
            if (DRAW_SONOGRAMS == 2)
            {
                DrawSonogram(sonogram, imagePath, predictedEvents, eventThreshold, intensity);
            }
            else
                if ((DRAW_SONOGRAMS == 1) && (predictedEvents.Count > 0))
                {
                    DrawSonogram(sonogram, imagePath, predictedEvents, eventThreshold, intensity);
                }

            Log.WriteLine("# Finished recording:- " + Path.GetFileName(recordingPath));
            Console.ReadLine();
        } //Dev()





        public static System.Tuple<BaseSonogram, List<AcousticEvent>, double[]> Execute(string wavPath,
            bool doSegmentation, int minHz, int maxHz, double frameOverlap, double smoothWindow, double eventThreshold, double minDuration)
        {
            //i: GET RECORDING
            AudioRecording recording = new AudioRecording(wavPath);
            if (recording.SampleRate != 22050) recording.ConvertSampleRate22kHz();
            int sr = recording.SampleRate;

            //ii: MAKE SONOGRAM
            Log.WriteLine("Start sonogram.");
            SonogramConfig sonoConfig = new SonogramConfig(); //default values config
            sonoConfig.WindowOverlap = frameOverlap;
            sonoConfig.SourceFName = recording.FileName;
            BaseSonogram sonogram = new SpectralSonogram(sonoConfig, recording.GetWavReader());
            recording.Dispose();
            Log.WriteLine("Signal: Duration={0}, Sample Rate={1}", sonogram.Duration, sr);
            Log.WriteLine("Frames: Size={0}, Count={1}, Duration={2:f1}ms, Overlap={5:f0}%, Offset={3:f1}ms, Frames/s={4:f1}",
                                       sonogram.Configuration.WindowSize, sonogram.FrameCount, (sonogram.FrameDuration * 1000),
                                      (sonogram.FrameOffset * 1000), sonogram.FramesPerSecond, frameOverlap);
            int binCount = (int)(maxHz / sonogram.FBinWidth) - (int)(minHz / sonogram.FBinWidth) + 1;
            Log.WriteIfVerbose("Freq band: {0} Hz - {1} Hz. (Freq bin count = {2})", minHz, maxHz, binCount);

            //Log.WriteIfVerbose("DctDuration=" + dctDuration + "sec.  (# frames=" + (int)Math.Round(dctDuration * sonogram.FramesPerSecond) + ")");

            //iii: DO SEGMENTATION
            double thresholdSD = 0.1;              //Set threshold to 1/5th of a standard deviation of the background noise.
            double maxDuration = Double.MaxValue;  //Do not constrain maximum length of events.
            var tuple = AcousticEvent.GetSegmentationEvents((SpectralSonogram)sonogram, doSegmentation, minHz, maxHz, smoothWindow, thresholdSD, minDuration, maxDuration);
            var segmentEvents = tuple.Item1;
            var intensity     = tuple.Item5;

            //iv DETECT EVENTS LIKE TARGET
            var tuple2 = FindEvents.Execute((SpectralSonogram)sonogram, segmentEvents, minHz, maxHz, eventThreshold, minDuration);
            List<AcousticEvent> predictedEvents = tuple.Item1;

            return System.Tuple.Create(sonogram, predictedEvents, intensity);

        }//end CaneToadRecogniser


        public static void DrawSonogram(BaseSonogram sonogram, string path,
                                        List<AcousticEvent> predictedEvents, double eventThreshold, double[] intensity)
        {
            Log.WriteLine("# Start to draw image of sonogram.");
            bool doHighlightSubband = false; bool add1kHzLines = true;
            //double maxScore = 50.0; //assumed max posisble oscillations per second

            using (System.Drawing.Image img = sonogram.GetImage(doHighlightSubband, add1kHzLines))
            using (Image_MultiTrack image = new Image_MultiTrack(img))
            {
                //img.Save(@"C:\SensorNetworks\WavFiles\temp1\testimage1.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);
                image.AddTrack(Image_Track.GetTimeTrack(sonogram.Duration));
                image.AddTrack(Image_Track.GetSegmentationTrack(sonogram));
                //image.AddTrack(Image_Track.GetScoreTrack(scores, 0.0, 1.0, eventThreshold));
                //double maxScore = 100.0;
                //image.AddSuperimposedMatrix(hits, maxScore);
                if (intensity != null)
                {
                    double min, max;
                    DataTools.MinMax(intensity, out min, out max);
                    double threshold_norm = eventThreshold / max; //min = 0.0;
                    intensity = DataTools.normalise(intensity);
                    image.AddTrack(Image_Track.GetScoreTrack(intensity, 0.0, 1.0, eventThreshold));
                }
                image.AddEvents(predictedEvents);
                image.Save(path);
            }
        }

    }//end class
}