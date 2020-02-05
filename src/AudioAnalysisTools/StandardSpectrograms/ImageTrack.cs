// <copyright file="ImageTrack.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AudioAnalysisTools.StandardSpectrograms
{
    using System;
    using SixLabors.ImageSharp;
    using System.Linq;
    using AudioAnalysisTools.Indices;
    using AudioAnalysisTools.WavTools;
    using SixLabors.Fonts;
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Processing;
    using SixLabors.Primitives;
    using TowseyLibrary;

    public enum TrackType
    {
        none,
        deciBels,
        waveEnvelope,
        segmentation,
        syllables,
        scoreArray,
        similarityScoreList,
        scoreArrayNamed,
        scoreMatrix,
        zeroCrossings,
        hits,
        timeTics,
    }

    public sealed class ImageTrack
    {
        public const int DefaultHeight = 30;       //pixel height of a track
        public const int HeightOfTimeScale = 15;   //pixel height of the top and bottom time scales
        public const int syllablesTrackHeight = 10;   //pixel height of a track
        public const int envelopeTrackHeight = 40;   //pixel height of a track
        public const int scoreTrackHeight = 40;   //pixel height of a track

        public static Color[] TrackColors =
        {
            Color.White, Color.Red, Color.Orange, Color.Cyan, Color.OrangeRed, Color.Pink, Color.Salmon, Color.Tomato, Color.DarkRed, Color.Purple,
                                          /*Color.AliceBlue,*/ /*Color.Aqua, Color.Aquamarine, Color.Azure, Color.Bisque,*/
            Color.Blue, Color.BlueViolet, /*Color.Brown, Color.BurlyWood,*/ Color.CadetBlue, /*Color.Chartreuse,*/
            Color.Chocolate, /*Color.Coral,*/ /*Color.CornflowerBlue,*/ /*Color.Cornsilk,*/ Color.Crimson, Color.DarkBlue,
            Color.DarkCyan, Color.DarkGoldenrod, Color.DarkGray, Color.DarkGreen, Color.DarkKhaki, Color.DarkMagenta,
            Color.DarkOliveGreen, Color.DarkOrange, Color.DarkOrchid, Color.DarkSalmon, Color.DarkSeaGreen,
            Color.DarkSlateBlue, Color.DarkSlateGray, Color.DarkTurquoise, Color.DarkViolet, Color.DeepPink, Color.DeepSkyBlue,
            Color.DimGray, Color.DodgerBlue, Color.Firebrick, Color.ForestGreen, Color.Fuchsia,
            Color.Gainsboro, Color.Gold, Color.Goldenrod, /*Color.Gray,*/ Color.Green, /*Color.GreenYellow,*/
            Color.Honeydew, Color.HotPink, Color.IndianRed, Color.Indigo, /*Color.Khaki,*/ Color.Lavender,
                             /*Color.LavenderBlush,*/ Color.LawnGreen, /*Color.LemonChiffon,*/ Color.Lime,
            Color.LimeGreen, /*Color.Linen,*/ Color.Magenta, Color.Maroon, Color.MediumAquamarine, Color.MediumBlue,
                             /*Color.MediumOrchid,*/ Color.MediumPurple, /*Color.MediumSeaGreen,*/ Color.MediumSlateBlue, Color.MediumSpringGreen,
            Color.MediumTurquoise, Color.MediumVioletRed, Color.MidnightBlue, /*Color.MistyRose,*/ /*Color.Moccasin,*/
            Color.Navy, /*Color.OldLace,*/ Color.Olive, /*Color.OliveDrab,*/
                             /*Color.Orchid, Color.PaleVioletRed, Color.PapayaWhip, */
                             /*Color.PeachPuff,*/ /*Color.Peru,*/ Color.Plum, /*Color.PowderBlue,*/ Color.RosyBrown,
            Color.RoyalBlue, Color.SaddleBrown, /*Color.SandyBrown,*/ Color.SeaGreen, /*Color.Sienna,*/
                             /*Color.Silver,*/ Color.SkyBlue, Color.SlateBlue, /*Color.SlateGray,*/ Color.SpringGreen, Color.SteelBlue,
                             /*Color.Tan,*/ Color.Teal, Color.Thistle, Color.Turquoise, Color.Violet, /*Color.Wheat,*/
                             /*Color.Yellow,*/ Color.YellowGreen,  Color.Black,
        };

        public TrackType TrackType { get; set; }

        public double scoreMin = 0.0; //default min score displayed in score track of image

        public double ScoreMin
        {
            get { return this.scoreMin; } set { this.scoreMin = value; }
        }

        public double scoreMax = 10.0; //default max score displayed in score track of image

        public double ScoreMax
        {
            get { return this.scoreMax; } set { this.scoreMax = value; }
        }

        public double ScoreThreshold { get; set; }

        public int sampleUnit { get; set; }

        public string Name { get; set; }

        public int topOffset { get; set; } //set to track's TOP    pixel row in final image

        public int bottomOffset { get; set; } //set to track's BOTTOM pixel row in final image

        private int height = DefaultHeight;

        public int Height
        {
            get { return this.height; } set { this.height = value; }
        }

        private int[] intData = null; // used to store segmentation state for example
        private double[] doubleData = null;
        private double[,] doubleMatrix = null;

        private TimeSpan timeSpan { get; set; }

        private double timeScale { get; set; } //pixels per second - not actually used from 30-08-2012

        private double[] doubleData1 = null;
        private double[] doubleData2 = null;

        //these params used for segmentation track
        public double SegmentationThreshold_k1 { get; set; }

        public double SegmentationThreshold_k2 { get; set; }

        private int garbageID = 0;

        public int GarbageID
        {
            get
            {
                return this.garbageID;
            }

            set
            {
                this.garbageID = value;
            }
        }

        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data"></param>
        public ImageTrack(TrackType type, int[] data)
        {
            this.TrackType = type;
            this.intData = data;
            this.height = this.SetTrackHeight();

            //if(SonoImage.Verbose)LoggedConsole.WriteLine("\tTrack CONSTRUCTOR: trackType = " + type + "  Data = " + data.ToString());
        }

        public ImageTrack(TrackType type, double[] data)
        {
            this.TrackType = type;
            this.doubleData = data;
            this.height = this.SetTrackHeight();

            //if (SonoImage.Verbose) LoggedConsole.WriteLine("\tTrack CONSTRUCTOR: trackType = " + type + "  Data = " + data.ToString());
        }

        /// <summary>
        /// used for showing the singal envelope track
        /// </summary>
        /// <param name="type"></param>
        /// <param name="data1"></param>
        /// <param name="data2"></param>
        public ImageTrack(TrackType type, double[] data1, double[] data2)
        {
            this.TrackType = type;
            this.doubleData1 = data1;
            this.doubleData2 = data2;
            this.height = this.SetTrackHeight();
        }

        public ImageTrack(TrackType type, double[,] data)
        {
            this.TrackType = type;
            this.doubleMatrix = data;
            this.height = this.SetTrackHeight();
        }

        public ImageTrack(TrackType type, TimeSpan t, double pixelsPerSecond)
        {
            this.TrackType = type;
            this.timeSpan = t;
            this.timeScale = pixelsPerSecond;
            this.height = this.SetTrackHeight();
        }

        public ImageTrack(TrackType type)
        {
            this.TrackType = type;
        }

        public void SetIntArray(int[] data)
        {
            this.intData = data;
        }

        private int SetTrackHeight()
        {
            switch (this.TrackType)
            {
                case TrackType.timeTics:
                    return HeightOfTimeScale;
                case TrackType.syllables:
                    return syllablesTrackHeight;
                case TrackType.scoreArray:
                    return scoreTrackHeight;
                case TrackType.similarityScoreList:
                    return scoreTrackHeight;
                case TrackType.scoreArrayNamed:
                    return scoreTrackHeight;
                case TrackType.deciBels:
                    return DefaultHeight;
                case TrackType.waveEnvelope:
                    return envelopeTrackHeight;
                case TrackType.segmentation:
                    return DefaultHeight;
                case TrackType.scoreMatrix:
                    return DefaultHeight;
                default:
                    return DefaultHeight;
            }
        }

        public void DrawTrack(Image<Rgb24> bmp)
        {
            //Log.WriteIfVerbose("\tDrawing track type =" + TrackType);
            switch (this.TrackType)
            {
                case TrackType.timeTics:
                    this.DrawTimeTrack(bmp);    //time scale track
                    break;
                case TrackType.deciBels:
                    this.DrawDecibelTrack(bmp); //frame energy track
                    break;
                case TrackType.waveEnvelope:
                    this.DrawWaveEnvelopeTrack(bmp); //signal envelope track
                    break;
                case TrackType.segmentation:
                    this.DrawSegmentationTrack(bmp); //segmentation track
                    break;
                case TrackType.syllables:
                    this.DrawSyllablesTrack(bmp);
                    break;
                case TrackType.scoreArray:
                    this.DrawScoreArrayTrack(bmp);  //add a score track
                    break;
                case TrackType.similarityScoreList:
                    this.DrawSimilarityScoreTrack(bmp);  //add a score track
                    break;
                case TrackType.scoreArrayNamed:
                    this.DrawNamedScoreArrayTrack(bmp);  //add a score track
                    break;
                case TrackType.scoreMatrix:
                    this.DrawScoreMatrixTrack(bmp);  //add a score track
                    break;
                default:
                    Log.WriteLine("WARNING******** !!!! ImageTrack.DrawTrack():- TRACKTYPE NOT DEFINED");
                    break;
            }

            // none, energy, syllables, scoreArray, scoreMatrix, zeroCrossings, hits
            //if ((title != null) && (title.Length != 0)) DrawTrackTitle(bmp, title);  //add a score track
        }

        /// adds title to bottom of bmp which is assume to be a track.
        public Image<Rgb24> DrawTrackTitle(Image<Rgb24> bmp, string title)
        {
            bmp.Mutate(g =>
            {
                g.DrawText(title, SystemFonts.CreateFont("Tahoma", 8), Color.Black, new PointF(10, bmp.Height - 2));
            });
            return bmp;
        }

        /// <summary>
        /// paints a track of symbol colours derived from symbol ID
        /// </summary>
        public Image<Rgb24> DrawSyllablesTrack(Image<Rgb24> bmp)
        {
            int bmpWidth = bmp.Width;

            //int bmpHt = bmp.Height;
            Color gray = Color.LightGray;
            Color white = Color.White;

            //Color red = Color.Red;
            if (this.intData == null || this.intData.Length == 0)
            {
                LoggedConsole.WriteLine("#####WARNING!! AddScoreArrayTrack(Image<Rgb24> bmp):- Integer data does not exists!");
                return bmp;
            }

            int bottom = this.topOffset + this.height - 1;
            for (int x = 0; x < Math.Min(bmp.Width, this.intData.Length); x++)
            {
                Color col = TrackColors[this.intData[x]];
                if (this.intData[x] == 0)
                {
                    col = white;
                }

                if (this.intData[x] == this.garbageID)
                {
                    col = gray;
                }

                for (int z = 0; z < this.height; z++)
                {
                    bmp[x, this.topOffset + z] = col;  //add in hits
                }

                bmp[x, bottom] = Color.Black;
            }

            return bmp;
        }

        public Image<Rgb24> DrawNamedScoreArrayTrack(Image<Rgb24> bmp)
        {
            this.DrawScoreArrayTrack(bmp);
            int length = bmp.Width;
            bmp.Mutate(g =>
            {
                var font = SystemFonts.CreateFont("Arial", 10);
                g.DrawText(this.Name, font, Color.Red, new PointF(10, this.topOffset));
                g.DrawText(this.Name, font, Color.Red, new PointF(length / 2, this.topOffset));
                g.DrawText(this.Name, font, Color.Red, new PointF(length - 80, this.topOffset));
            });

            return bmp;
        }

        /// <summary>
        /// Displays a score track, normalised to min and max of the data. max=approx 8-16.
        /// </summary>
        public Image<Rgb24> DrawScoreArrayTrack(Image<Rgb24> bmp)
        {
            // LoggedConsole.WriteLine("DRAW SCORE TRACK: image ht=" + bmp.Height + "  topOffset = " + this.topOffset + "   bottomOffset =" + this.bottomOffset);
            if (this.doubleData == null)
            {
                return bmp;
            }

            int dataLength = this.doubleData.Length;
            double range = this.scoreMax - this.scoreMin;

            //next two lines are for subsampling if the score array is compressed to fit smaller image width.
            double subSample = dataLength / (double)bmp.Width;
            if (subSample < 1.0)
            {
                subSample = 1;
            }

            Color gray = Color.FromRgb(235, 235, 235); // use as background
            int baseLine = this.topOffset + this.height - 2;

            //int length = (bmpWidth <= doubleData.Length) ? bmpWidth : doubleData.Length;
            //for (int w = 0; w < length; w++)
            for (int w = 0; w < bmp.Width; w++)
            {
                int start = (int)Math.Round(w * subSample);
                int end = (int)Math.Round((w + 1) * subSample);
                if (end >= this.doubleData.Length)
                {
                    continue;
                }

                int location = 0;

                //find max value in sub-sample - if there is a sub-sample
                double subsampleMax = -double.MaxValue;
                for (int x = start; x < end; x++)
                {
                    if (subsampleMax < this.doubleData[x])
                    {
                        subsampleMax = this.doubleData[x];
                    }

                    location = x;
                }

                double fraction = (subsampleMax - this.scoreMin) / range;
                int id = this.Height - 1 - (int)(this.Height * fraction);
                if (id < 0)
                {
                    id = 0;
                }
                else if (id > this.Height)
                {
                    id = this.Height; // impose bounds
                }

                //paint white and leave a black vertical histogram bar
                //for (int z = 0; z < id; z++)
                //{
                //    bmp.SetPixel(w, this.topOffset + z, gray); // background
                //}

                for (int z = id; z < this.height; z++)
                {
                    bmp[w, this.topOffset + z] = Color.Black; // draw the score bar
                }

                bmp[w, baseLine] = Color.Black; // draw base line
            }

            //add in horizontal threshold significance line
            double f = (this.ScoreThreshold - this.scoreMin) / range;
            int lineID = this.Height - 1 - (int)(this.Height * f);
            if (lineID < 0)
            {
                return bmp;
            }

            if (lineID > this.Height)
            {
                return bmp;
            }

            for (int x = 0; x < bmp.Width; x++)
            {
                bmp[x, this.topOffset + lineID] = Color.Lime;
            }

            return bmp;
        }

        /// <summary>
        /// Displays a score track, normalised to min and max of the data. max=approx 8-16.
        /// </summary>
        public Image<Rgb24> DrawSimilarityScoreTrack(Image<Rgb24> bmp)
        {
            //LoggedConsole.WriteLine("DRAW SCORE TRACK: image ht=" + bmp.Height + "  topOffset = " + topOffset + "   botOffset =" + bottomOffset);
            if (this.doubleData == null)
            {
                return bmp;
            }

            int dataLength = this.doubleData.Length;
            double range = this.scoreMax - this.scoreMin;

            //next two lines are for subsampling if the score array is compressed to fit smaller image width.
            double subSample = dataLength / (double)bmp.Width;

            // 13 = neighbourhoodLenght
            if (subSample < 1.0)
            {
                subSample = 13;
            }

            Color gray = Color.FromRgb(235, 235, 235); // use as background
            int baseLine = this.topOffset + this.height - 2;

            //int length = (bmpWidth <= doubleData.Length) ? bmpWidth : doubleData.Length;
            //for (int w = 0; w < length; w++)

            for (int w = 0; w < bmp.Width; w += 13)
            {
                var location = w;
                double fraction = 0.0;
                if (w / 13 < dataLength)
                {
                    fraction = (this.doubleData[w / 13] - this.scoreMin) / range;
                }
                else
                {
                    fraction = 0.0;
                }

                int id = this.Height - 1 - (int)(this.Height * fraction);
                if (id < 0)
                {
                    id = 0;
                }
                else if (id > this.Height)
                {
                    id = this.Height; // impose bounds
                }

                //paint white and leave a black vertical histogram bar
                for (int z = 0; z < id; z++)
                {
                    bmp[w, this.topOffset + z] = gray; // background
                }

                for (int z = id; z < this.height; z++)
                {
                    bmp[w, this.topOffset + z] = Color.Black; // draw the score bar
                }

                bmp[w, baseLine] = Color.Black; // draw base line
            }

            //add in horizontal threshold significance line
            double f = (this.ScoreThreshold - this.scoreMin) / range;
            int lineID = this.Height - 1 - (int)(this.Height * f);
            if (lineID < 0)
            {
                return bmp;
            }

            if (lineID > this.Height)
            {
                return bmp;
            }

            for (int x = 0; x < bmp.Width; x++)
            {
                bmp[x, this.topOffset + lineID] = Color.Lime;
            }

            return bmp;
        }

        /// <summary>
        /// This method assumes that the passed data array is of values, min=0.0, max = approx 8-16.
        /// </summary>
        public Image<Rgb24> DrawScoreMatrixTrack(Image<Rgb24> bmp)
        {
            int bmpWidth = bmp.Width;
            int dataLength = this.intData.Length;
            int subSample = (int)Math.Round((double)(dataLength / bmp.Width));
            if (subSample < 1)
            {
                subSample = 1;
            }

            Color gray = Color.LightGray;
            Color white = Color.White;
            Color red = Color.Red;

            int numberOfScoreTracks = this.doubleMatrix.GetLength(0);
            double[] scores = new double[numberOfScoreTracks];
            for (int x = 0; x < bmpWidth; x++)
            {
                //transfer scores
                for (int y = 0; y < numberOfScoreTracks; y++)
                {
                    scores[y] = this.doubleMatrix[y, x];
                }

                int maxIndex = DataTools.GetMaxIndex(scores);

                int id = this.Height - 1 - (int)(this.Height * scores[maxIndex] / this.ScoreMax);
                if (id < 0)
                {
                    id = 0;
                }
                else if (id > this.Height)
                {
                    id = this.Height;
                }

                //paint white and leave a black vertical histogram bar
                for (int z = 0; z < id; z++)
                {
                    bmp[x, this.topOffset + z]  = white;
                }

                for (int z = id; z < this.Height; z++)
                {
                    bmp[x, this.topOffset + z] = TrackColors[maxIndex + 15];
                }
            }

            //add in horizontal threshold significance line
            double max = 2 * this.ScoreThreshold;
            if (max < this.ScoreMax)
            {
                max = this.ScoreMax;
            }

            int lineID = (int)(this.Height * (1 - (this.ScoreThreshold / max)));
            for (int x = 0; x < bmpWidth; x++)
            {
                bmp[x, this.topOffset + lineID] = gray;
            }

            return bmp;
        }

        /// <summary>
        /// assumes that max signal value = 1.0 and min sig value = -1.0 i.e. wav file values
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public Image<Rgb24> DrawWaveEnvelopeTrack(Image<Rgb24> bmp)
        {
            int halfHeight = this.height / 2;
            Color c = Color.FromRgb(10, 200, 255);

            for (int w = 0; w < bmp.Width; w++)
            {
                int minID = halfHeight + (int)Math.Round(this.doubleMatrix[0, w] * halfHeight);

                //minID = halfHeight + (int)Math.Round(-1.0 * halfHeight);
                int maxID = halfHeight + (int)Math.Round(this.doubleMatrix[1, w] * halfHeight) - 1;
                for (int z = minID; z <= maxID; z++)
                {
                    bmp[w, this.bottomOffset - z] = c;
                }

                bmp[w, this.topOffset + halfHeight] = c; //set zero line in case it was missed
                if (this.doubleMatrix[0, w] < -0.99)
                {
                    bmp[w, this.bottomOffset - 1] = Color.OrangeRed;
                    bmp[w, this.bottomOffset - 2] = Color.OrangeRed;
                    bmp[w, this.bottomOffset - 3] = Color.OrangeRed;
                }
                else
                    if (this.doubleMatrix[1, w] > 0.99)
                    {
                        bmp[w, this.topOffset] = Color.OrangeRed;
                        bmp[w, this.topOffset + 1] = Color.OrangeRed;
                        bmp[w, this.topOffset + 2] = Color.OrangeRed;
                    }

                //bmp.SetPixel(w, this.topOffset, Color.OrangeRed);
                //bmp.SetPixel(w, this.bottomOffset - 1, Color.OrangeRed);
            }

            return bmp;
        }

        /// <summary>
        /// adds time track to a sonogram at the vertical position determined by topOffset.
        /// </summary>
        /// <param name="bmp"></param>
        /// <param name="topOffset"></param>
        /// <returns></returns>
        public Image<Rgb24> DrawTimeTrack(Image<Rgb24> bmp)
        {
            int width = bmp.Width;

            var timeTrack = DrawTimeTrack(this.timeSpan, width);
            bmp.Mutate(g => { g.DrawImage(timeTrack, 0, this.topOffset); });

            return bmp;
        }

        /// <summary>
        /// This method assumes that the passed decibel array has been normalised
        /// Also requires values to be set for SegmentationThreshold_k1 and SegmentationThreshold_k2
        /// </summary>
        public Image<Rgb24> DrawSegmentationTrack(Image<Rgb24> bmp)
        {
            Image<Rgb24> track = DrawSegmentationTrack(this.doubleData, this.intData, this.SegmentationThreshold_k1, this.SegmentationThreshold_k2, bmp.Width);
            if (track == null)
            {
                LoggedConsole.WriteErrorLine("Cannot draw Segmentation Track due to null data");
                return bmp;
            }

            bmp.Mutate(g => { g.DrawImage(track, 0, this.topOffset); });

            return bmp;
        }

        public Image<Rgb24> DrawDecibelTrack(Image<Rgb24> bmp)
        {
            Image<Rgb24> track = DrawDecibelTrack(this.doubleData, bmp.Width, this.SegmentationThreshold_k1, this.SegmentationThreshold_k2);
            bmp.Mutate(g => { g.DrawImage(track, 0, this.topOffset); });

            return bmp;
        }

        //###########################################################################################################################################
        //#### STATIC METHODS BELOW HERE TO DRAW TRACK ##############################################################################################
        //###########################################################################################################################################

        public static Image DrawWaveEnvelopeTrack(AudioRecording recording, int imageWidth)
        {
            //int height = ImageTrack.DefaultHeight;
            double[,] envelope = recording.GetWaveForm(imageWidth);

            Image envelopeImage = DrawWaveEnvelopeTrack(envelope);
            return envelopeImage;
        }

        /// <summary>
        /// assumes that max signal value = 1.0 and min sig value = -1.0 i.e. wav file values
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static Image<Rgb24> DrawWaveEnvelopeTrack(double[,] envelope)
        {
            int height = DefaultHeight;
            int halfHeight = DefaultHeight / 2;
            Color colour = Color.FromRgb(10, 200, 255); // pale blue
            int width = envelope.GetLength(1);
            var bmp = new Image<Rgb24>(width, height);

            for (int w = 0; w < width; w++)
            {
                int minID = halfHeight + (int)Math.Round(envelope[0, w] * halfHeight);

                //minID = halfHeight + (int)Math.Round(-1.0 * halfHeight);
                int maxID = halfHeight + (int)Math.Round(envelope[1, w] * halfHeight) - 1;
                for (int z = minID; z <= maxID; z++)
                {
                    bmp[w, height - z - 1] = colour;
                }

                // set zero line in case it was missed
                bmp[w, halfHeight] = colour;

                // if clipped values
                if (envelope[0, w] < -0.99)
                {
                    bmp[w, height - 1] = Color.OrangeRed;
                    bmp[w, height - 2] = Color.OrangeRed;
                    bmp[w, height - 3] = Color.OrangeRed;
                }
                else
                    if (envelope[1, w] > 0.99)
                    {
                        bmp[w, 0] = Color.OrangeRed;
                        bmp[w, 1] = Color.OrangeRed;
                        bmp[w, 2] = Color.OrangeRed;
                    }

                //bmp[w, this.topOffset, Color.OrangeRed);
                //bmp[w, this.bottomOffset - 1, Color.OrangeRed);
            }

            return bmp;
        }

        /// <summary>
        /// This method assumes that the passed decibel array has been normalised
        /// </summary>
        public static Image<Rgb24> DrawDecibelTrack(double[] data, int imageWidth, double segmentationThreshold_k1, double segmentationThreshold_k2)
        {
            int dataLength = data.Length;
            int subSample = (int)Math.Round((double)(dataLength / imageWidth));
            if (subSample < 1)
            {
                subSample = 1;
            }

            var bmp = new Image<Rgb24>(imageWidth, DefaultHeight);
            bmp.Mutate(g => {
                g.FillRectangle(new SolidBrush(Color.White), 0, 0, imageWidth, DefaultHeight);
            });

            for (int w = 0; w < imageWidth; w++)
            {
                int start = w * subSample;
                int end = (w + 1) * subSample;
                double max = -double.MaxValue;
                int location = 0;
                for (int x = start; x < end; x++)
                {
                    if (max < data[x])
                    {
                        max = data[x];
                    }

                    location = x;
                }

                double norm = data[location];
                int id = DefaultHeight - 1 - (int)(DefaultHeight * norm);
                if (id < 0)
                {
                    id = 0;
                }
                else if (id > DefaultHeight)
                {
                    id = DefaultHeight;
                }

                //paint white and leave a black vertical bar
                //for (int z = 0; z < id; z++) bmp.SetPixel(w, z, Color.White); // draw bar by drawing in white backgorund
                for (int z = id; z < DefaultHeight; z++)
                {
                    bmp[w, z] = Color.Black;
                }
            }

            //display vocalisation thresholds used to determine endpoints
            Color[] stateColors = { Color.White, Color.Green, Color.Red };
            double v1 = segmentationThreshold_k1;
            int k1 = DefaultHeight - (int)(DefaultHeight * v1);
            double v2 = segmentationThreshold_k2;
            int k2 = DefaultHeight - (int)(DefaultHeight * v2);
            if (v1 < 0.0 || v1 > 1.0)
            {
                return bmp; //thresholds are illegal so stop now.
            }

            if (v2 < 0.0 || v2 > 1.0)
            {
                return bmp;
            }

            //calculate location of the segmentation threshold lines
            int y1 = k1;
            if (y1 >= bmp.Height)
            {
                y1 = bmp.Height - 1;
            }

            int y2 = k2;
            if (y2 >= bmp.Height)
            {
                y2 = bmp.Height - 1;
            }

            Pen orangePen = new Pen(Color.Orange, 1);
            Pen limePen = new Pen(Color.Lime, 1);

            bmp.Mutate(g =>
            {
                g.DrawLine(orangePen, 0, y1, imageWidth, y1); //threshold lines
                g.DrawLine(limePen, 0, y2, imageWidth, y2); //threshold lines
            });

            return bmp;
        }

        public static Image<Rgb24> DrawSegmentationTrack(BaseSonogram sg, double segmentationThreshold_k1, double segmentationThreshold_k2, int imageWidth)
        {
            Image<Rgb24> track = DrawSegmentationTrack(sg.DecibelsNormalised, sg.SigState, segmentationThreshold_k1, segmentationThreshold_k2, sg.FrameCount);
            return track;
        }

        public static Image<Rgb24> DrawSegmentationTrack(double[] data, int[] stateData, double segmentationThreshold_k1, double segmentationThreshold_k2, int imageWidth)
        {
            if (data == null)
            {
                return null;
            }

            Image<Rgb24> segmentBmp = DrawDecibelTrack(data, imageWidth, segmentationThreshold_k1, segmentationThreshold_k2);
            int dataLength = data.Length;
            int subSample = (int)Math.Round((double)(dataLength / imageWidth));
            if (subSample < 1)
            {
                subSample = 1;
            }

            var stateBmp = new Image<Rgb24>(imageWidth, 4);

            //display vocalisation state and thresholds used to determine endpoints
            Color[] stateColors = { Color.White, Color.Green, Color.Red };

            for (int x = 0; x < stateBmp.Width; x++)
            {
                int location = x * subSample;
                if (location > dataLength - 1)
                {
                    continue;
                }

                Color col = stateColors[stateData[location]];
                stateBmp[x, 0] = col;
                stateBmp[x, 1] = col;
                stateBmp[x, 2] = col;
                stateBmp[x, 3] = col;
            }

            // surround the whole by a frame
            segmentBmp.Mutate(g => {
                g.DrawImage(stateBmp, 0, 1);
                g.DrawRectangle(new Pen(Color.Black, 1), 0, 0, imageWidth, DefaultHeight);
            });

            return segmentBmp;
        }

        /// <summary>
        /// ASSUME that passed decibel array has been normalised
        /// </summary>
        /// <param name="sg"></param>
        /// <returns></returns>
        public static ImageTrack GetDecibelTrack(BaseSonogram sg)
        {
            var track = new ImageTrack(TrackType.deciBels, sg.DecibelsNormalised);
            return track;
        }

        /// <summary>
        /// ASSUME that passed decibel array has been normalised
        /// </summary>
        /// <param name="sg"></param>
        /// <returns></returns>
        public static ImageTrack GetSegmentationTrack(BaseSonogram sg)
        {
            var track = new ImageTrack(TrackType.segmentation, sg.DecibelsNormalised);
            track.intData = sg.SigState;
            track.SegmentationThreshold_k1 = EndpointDetectionConfiguration.K1Threshold / sg.DecibelReference;
            track.SegmentationThreshold_k2 = EndpointDetectionConfiguration.K2Threshold / sg.DecibelReference;
            return track;
        }

        public static ImageTrack GetWavEnvelopeTrack(AudioRecording ar, int imageWidth)
        {
            double[,] envelope = ar.GetWaveForm(imageWidth);

            return GetWavEnvelopeTrack(envelope, imageWidth);
        }

        public static ImageTrack GetWavEnvelopeTrack(double[,] envelope, int imageWidth)
        {
            var track = new ImageTrack(TrackType.waveEnvelope, envelope);
            return track;
        }

        public static ImageTrack GetTimeTrack(TimeSpan t, double pixelsPerSecond)
        {
            var track = new ImageTrack(TrackType.timeTics, t, pixelsPerSecond);

            //int height = track.Height;
            return track;
        }

        public static ImageTrack GetSyllablesTrack(int[] SyllableIDs, int garbageID)
        {
            var track = new ImageTrack(TrackType.syllables, SyllableIDs);
            track.GarbageID = garbageID;
            return track;
        }

        public static ImageTrack GetScoreTrack(double[] scores, double? scoreMin, double? scoreMax, double? scoreThreshold)
        {
            var track = new ImageTrack(TrackType.scoreArray, scores);
            track.ScoreThreshold = scoreThreshold ?? 0;
            track.ScoreMin = scoreMin ?? 0;
            track.ScoreMax = scoreMax ?? 0;
            return track;
        }

        public static ImageTrack GetSimilarityScoreTrack(double[] scores, double? scoreMin, double? scoreMax, double? scoreThreshold, int neighbourhoodLength)
        {
            var track = new ImageTrack(TrackType.similarityScoreList, scores);
            track.ScoreThreshold = scoreThreshold ?? 0;
            track.ScoreMin = scoreMin ?? 0;
            track.ScoreMax = scoreMax ?? 0;
            track.sampleUnit = neighbourhoodLength;
            return track;
        }

        public static ImageTrack GetNamedScoreTrack(double[] scores, double? scoreMin, double? scoreMax, double? scoreThreshold, string name)
        {
            var track = new ImageTrack(TrackType.scoreArrayNamed, scores);
            track.ScoreThreshold = scoreThreshold ?? 0;
            track.ScoreMin = scoreMin ?? 0;
            track.ScoreMax = scoreMax ?? 0;
            track.Name = name;
            return track;
        }

        public static ImageTrack GetScoreTrack(int[] scores, int? scoreMin, int? scoreMax, int? scoreThreshold)
        {
            //convert integers to doubles
            var dscores = new double[scores.Length];
            for (int x = 0; x < scores.Length; x++)
            {
                dscores[x] = scores[x];
            }

            return GetScoreTrack(dscores, (double)scoreMin, (double)scoreMax, (double)scoreThreshold);
        }

        /// <summary>
        /// used to draw richness indices
        /// </summary>
        /// <param name="scores"></param>
        /// <param name="scoreMin"></param>
        /// <param name="scoreMax"></param>
        /// <param name="scoreThreshold"></param>
        /// <returns></returns>
        public static void DrawScoreTrack(Image<Rgb24> bmp, double[] array, int yOffset, int trackHeight, double minVal, double maxVal, double threshold, string title)
        {
            Color[] grayScale = ImageTools.GrayScale();
            int imageWidth = array.Length;
            double range = maxVal - minVal;
            for (int x = 0; x < imageWidth; x++) //for pixels in the line
            {
                // NormaliseMatrixValues and bound the value - use min bound, max and 255 image intensity range
                double value = (array[x] - minVal) / range;
                int c = 255 - (int)Math.Floor(255.0 * value); //original version
                if (c < threshold)
                {
                    c = 0;
                }
                else
                if (c >= 256)
                {
                    c = 255;
                }

                Color col = grayScale[c];
                for (int y = 0; y < trackHeight; y++)
                {
                    bmp[x, yOffset + y] = col;
                }

                bmp[x, yOffset] = grayScale[0]; //draw upper boundary
            }//end over all pixels

            bmp.Mutate(g =>
            {
                g.DrawText(title, SystemFonts.CreateFont("Arial", 10), Color.White, new PointF(imageWidth + 5, yOffset));
            });
        }

        /// <summary>
        /// used to draw score track or any array of values
        /// </summary>
        /// <param name="scores"></param>
        /// <param name="scoreMin"></param>
        /// <param name="scoreMax"></param>
        /// <param name="scoreThreshold"></param>
        /// <returns></returns>
        public static Image<Rgb24> DrawGrayScaleScoreTrack(double[] array, double minVal, double maxVal, double threshold, string title)
        {
            int trackHeight = IndexDisplay.DefaultTrackHeight;
            Color[] grayScale = ImageTools.GrayScale();
            int imageWidth = array.Length;
            Image<Rgb24> bmp = new Image<Rgb24>(imageWidth, trackHeight);

            double range = maxVal - minVal;
            for (int x = 0; x < imageWidth; x++) //for pixels in the line
            {
                // NormaliseMatrixValues and bound the value - use min bound, max and 255 image intensity range
                double value = (array[x] - minVal) / range;
                int c = 255 - (int)Math.Floor(255.0 * value); //original version
                if (c < threshold)
                {
                    c = 0;
                }
                else
                    if (c >= 256)
                {
                    c = 255;
                }

                Color col = grayScale[c];
                for (int y = 0; y < trackHeight; y++)
                {
                    bmp[x, y] = col;
                }

                bmp[x, 0] = grayScale[0]; //draw upper boundary
            }//end over all pixels

            bmp.Mutate(g =>
            {
                g.DrawText(title, SystemFonts.CreateFont("Tahoma", 8), Color.White, new PointF(imageWidth + 5, 0));
            });

            return bmp;
        }

        /// <summary>
        /// used to draw score track of an array of values
        /// The values in array MUST lie in [0,1].
        /// </summary>
        /// <param name="scores"></param>
        /// <param name="scoreMin"></param>
        /// <param name="scoreMax"></param>
        /// <param name="scoreThreshold"></param>
        /// <returns></returns>
        public static Image<Rgb24> DrawBarScoreTrack(double[] order, double[] array, int trackWidth, double threshold, string title)
        {
            int trackHeight = IndexDisplay.DefaultTrackHeight;

            Color[] grayScale = ImageTools.GrayScale();

            //int imageWidth = array.Length;
            Image<Rgb24> bmp = new Image<Rgb24>(trackWidth, trackHeight);
            bmp.Mutate(g => {
                g.Clear(grayScale[240]);
            });

            for (int i = 0; i < array.Length; i++) //for pixels in the line
            {
                int x = (int)order[i];
                double value = array[i];
                if (value > 1.0)
                {
                    value = 1.0; //expect normalised data
                }

                int barHeight = (int)Math.Round(value * trackHeight);
                for (int y = 0; y < barHeight; y++)
                {
                    bmp[x, trackHeight - y - 1] = Color.Black;
                }

                bmp[x, 0] = Color.Gray; //draw upper boundary
            }//end over all pixels

            int endWidth = trackWidth - array.Length;
            var font = SystemFonts.CreateFont("Arial", 9);
            bmp.Mutate(g =>
            {
                g.FillRectangle(new SolidBrush(Color.Black), array.Length + 1, 0, endWidth, trackHeight);
                g.DrawText(title, font, Color.White, new PointF(array.Length + 5, 2));
            });
            return bmp;
        }

        /// <summary>
        /// used to draw coloured score track or any array of values
        /// </summary>
        /// <param name="scores"></param>
        /// <param name="scoreMin"></param>
        /// <param name="scoreMax"></param>
        /// <param name="scoreThreshold"></param>
        /// <returns></returns>
        public static Image<Rgb24> DrawColourScoreTrack(double[] order, double[] array, int trackWidth, int trackHeight, double threshold, string title)
        {
            Color[] colorScale = { Color.LightGray, Color.Gray, Color.Orange, Color.Red, Color.Purple };
            Image<Rgb24> bmp = new Image<Rgb24>(trackWidth, trackHeight);
            bmp.Mutate(g => { g.Clear(Color.FromRgb(240, 240, 240)); });

            //double range = maxVal - minVal;
            for (int i = 0; i < array.Length; i++) //for pixels in the line
            {
                int x = (int)order[i];

                // NormaliseMatrixValues and bound the value - use min bound, max and 255 image intensity range
                //double value = (array[i] - minVal) / range;
                double value = array[i];
                int barHeight = (int)Math.Round(value * trackHeight);
                int colourIndex = (int)Math.Floor(value * colorScale.Length * 0.99);
                for (int y = 0; y < barHeight; y++)
                {
                    bmp[x, trackHeight - y - 1] = colorScale[colourIndex];
                }

                bmp[x, 0] = Color.Gray; //draw upper boundary
            }//end over all pixels

            int endWidth = trackWidth - array.Length;
            var font = SystemFonts.CreateFont("Arial", 9);
            bmp.Mutate(g =>
            {
                g.FillRectangle(Brushes.Solid(Color.Black), array.Length + 1, 0, endWidth, trackHeight);
                g.DrawText(title, font, Color.White, new PointF(array.Length + 5, 2));
            });

            return bmp;
        }

        public static void DrawScoreTrack(Image<Rgb24> bmp, double[] array, int yOffset, int trackHeight, double threshold, string title)
        {
            double minVal;
            double maxVal;
            DataTools.MinMax(array, out minVal, out maxVal);
            DrawScoreTrack(bmp, array, yOffset, trackHeight, minVal, maxVal, threshold, title);
        }

        public static Image<Rgb24> DrawGrayScaleScoreTrack(double[] array, int trackHeight, double threshold, string title)
        {
            double minVal;
            double maxVal;
            DataTools.MinMax(array, out minVal, out maxVal);
            Image<Rgb24> bitmap = DrawGrayScaleScoreTrack(array, minVal, maxVal, threshold, title);
            return bitmap;
        }

        // mark of time scale according to scale.
        public static Image<Rgb24> DrawTitleTrack(int trackWidth, int trackHeight, string title)
        {
            Image<Rgb24> bmp = new Image<Rgb24>(trackWidth, trackHeight);
            bmp.Mutate(g =>
            {
                g.Clear(Color.Black);
                Pen pen = new Pen(Color.White, 1);

                g.DrawLine(new Pen(Color.Gray, 1), 0, 0, trackWidth, 0); //draw upper boundary

                //g.DrawLine(pen, duration + 1, 0, trackWidth, 0);
                
                g.DrawText(title, SystemFonts.CreateFont("Tahoma", 9), Color.Wheat, new PointF(4, 3));
            });
            return bmp;
        }

        /// <summary>
        /// IMPORTANT: THIS TIME SCALE METHOD WAS REWORKED ON 23 June 2015 and on 3 August 2015.
        /// IT POSSIBLY CONTAINS BUGS THAT WILL NEED TO BE FIXED FOR ZOOMING SPECTROGRAMS
        /// It is possible that rounding the tic marks to 'nice' numbers is not a good idea.
        ///
        /// Returns a bitmap of a time scale.
        /// Interval between tic marks is calculated automatically.
        /// This method is used for long duration spectrograms.
        /// It could be generalised for any time track.
        /// </summary>
        /// <param name="fullDuration">time span of entire time track to be drawn</param>
        /// <param name="dateTime">date and time at start of the track </param>
        /// <param name="trackWidth">X pixel dimension</param>
        /// <param name="trackHeight">Y pixel dimension</param>
        /// <returns></returns>
        public static Image<Rgb24> DrawTimeTrack(TimeSpan fullDuration, DateTimeOffset? dateTime, int trackWidth, int trackHeight)
        {
            // if null date time then just send back relative
            if (dateTime == null)
            {
                return DrawTimeRelativeTrack(fullDuration, trackWidth, trackHeight);
            }

            DateTime startDate = ((DateTimeOffset)dateTime).DateTime.Date;

            Image<Rgb24> bmp = new Image<Rgb24>(trackWidth, trackHeight);
            bmp.Mutate(g => { g.Clear(Color.Black); });

            double xAxisPixelDurationInMilliseconds = fullDuration.TotalMilliseconds / trackWidth;

            TimeSpan startTimeAbs = TimeSpan.Zero;
            DateTimeOffset dto = (DateTimeOffset)dateTime;
            startTimeAbs = dto.TimeOfDay;

            // round start time to nearest second or minute depending on the scale. If low resolution, round to nearest minute
            int roundedStartSeconds = (int)Math.Ceiling(startTimeAbs.TotalSeconds);
            TimeSpan roundedStartTime = TimeSpan.FromSeconds(roundedStartSeconds);
            if (xAxisPixelDurationInMilliseconds > 1000)
            {
                int roundedStartMinutes = (int)Math.Round(startTimeAbs.TotalMinutes);
                roundedStartTime = TimeSpan.FromMinutes(roundedStartMinutes);
            }

            int roundedStartHours = roundedStartTime.Hours;

            TimeSpan ticStartTime = TimeSpan.FromHours(roundedStartHours);
            TimeSpan ticStartOffset = ticStartTime - roundedStartTime;
            int pixelStartOffset = (int)(ticStartOffset.TotalMilliseconds / xAxisPixelDurationInMilliseconds);

            TimeSpan xAxisTicInterval = CalculateGridInterval(fullDuration, trackWidth);

            Pen whitePen = new Pen(Color.White, 1);
            Pen grayPen = new Pen(Color.Gray, 1);
            Font stringFont = SystemFonts.CreateFont("Arial", 10);

            int rows = bmp.Height;
            int cols = bmp.Width;

            // for columns, draw in X-axis lines
            bmp.Mutate(g =>
            {
                int xPixelInterval =
                    (int)Math.Round(xAxisTicInterval.TotalMilliseconds / xAxisPixelDurationInMilliseconds);
                int halfInterval = xPixelInterval / 2;
                int halfheight = trackHeight / 3;
                for (int x = 0; x < cols - pixelStartOffset; x++)
                {
                    if (x % xPixelInterval == 0)
                    {
                        int tickPosition = x + pixelStartOffset;
                        g.DrawLine(whitePen, tickPosition, 0, tickPosition, trackHeight);
                        g.DrawLine(whitePen, tickPosition + halfInterval, 0, tickPosition + halfInterval, halfheight);

                        TimeSpan elapsedTimeSpan =
                            TimeSpan.FromMilliseconds(xAxisPixelDurationInMilliseconds * tickPosition);

                        TimeSpan absoluteTS = roundedStartTime + elapsedTimeSpan;
                        TimeSpan roundedTimeSpan = TimeSpan.FromSeconds(Math.Round(absoluteTS.TotalSeconds));
                        string timeStr = "0000";
                        if (xAxisPixelDurationInMilliseconds <= 1000)
                        {
                            timeStr = $"{roundedTimeSpan}";
                        }
                        else if (roundedTimeSpan.Hours == 0.0 && roundedTimeSpan.Minutes == 0.0)
                        {
                            g.DrawLine(whitePen, tickPosition + 1, 0, tickPosition + 1, trackHeight);
                            if (tickPosition > 0)
                            {
                                g.DrawLine(whitePen, tickPosition - 1, 0, tickPosition - 1, trackHeight);
                            }

                            if (startDate.Year > 2000)
                            {
                                DateTime today = startDate + roundedTimeSpan;
                                timeStr = $"{today.ToShortDateString()}";
                            }
                        }
                        else
                        {
                            timeStr = $"{roundedTimeSpan.Hours:d2}{roundedTimeSpan.Minutes:d2}h";
                        }

                        g.DrawText(timeStr, stringFont, Color.White, new PointF(tickPosition, 3)); //draw time
                    }
                }

                g.DrawLine(whitePen, 0, 0, trackWidth, 0); //draw upper boundary

                //g.DrawLine(whitePen, 0, trackHeight - 1, trackWidth, trackHeight - 1);//draw lower boundary
                g.DrawLine(grayPen, 0, trackHeight - 1, trackWidth, trackHeight - 1); //draw lower boundary
            });

            return bmp;
        }

        /// <summary>
        /// Like the above method but adds a label at end displaying units of time.
        /// </summary>
        public static Image<Rgb24> DrawTimeTrack(TimeSpan fullDuration, TimeSpan startOffset, TimeSpan ticInterval, int trackWidth, int trackHeight, string title)
        {
            Image<Rgb24> bmp = new Image<Rgb24>(trackWidth, trackHeight);
            bmp.Mutate(g =>
            {
                g.Clear(Color.Black);

                TimeSpan gridInterval = CalculateGridInterval(fullDuration, trackWidth);

                int hour;
                int min = (int)startOffset.TotalMinutes - 1;
                var XaxisScale = gridInterval.TotalMinutes;
                Pen whitePen = new Pen(Color.White, 1);
                Font stringFont = SystemFonts.CreateFont("Arial", 9);

                for (int x = 0; x < trackWidth; x++) //for pixels in the line
                {
                    min++;
                    if (min % XaxisScale != 0)
                    {
                        continue;
                    }

                    g.DrawLine(whitePen, x, 0, x, trackHeight);
                    hour = (int)Math.Round(min / XaxisScale);
                    if (hour >= 24)
                    {
                        min = 0;
                        hour = 0;
                    }

                    g.DrawText(hour.ToString(), stringFont, Color.White, new PointF(x + 2, 1)); //draw time
                } //end over all pixels

                g.DrawLine(whitePen, 0, 0, trackWidth, 0); //draw upper boundary
                g.DrawLine(whitePen, 0, trackHeight - 1, trackWidth, trackHeight - 1); //draw lower boundary
                g.DrawLine(whitePen, trackWidth, 0, trackWidth, trackHeight - 1); //draw right end boundary

                g.DrawText(title, stringFont, Color.White, new PointF(trackWidth - 30, 2));

            });

            return bmp;
        }

        public static Image<Rgb24> DrawTimeRelativeTrack(TimeSpan fullDuration, int trackWidth, int trackHeight)
        {
            Image<Rgb24> bmp = new Image<Rgb24>(trackWidth, trackHeight);
            bmp.Mutate(g =>
            {
                g.Clear(Color.Black);

                double xAxisPixelDurationInMilliseconds = fullDuration.TotalMilliseconds / trackWidth;

                TimeSpan startTime = TimeSpan.Zero;

                TimeSpan xAxisTicInterval = CalculateGridInterval(fullDuration, trackWidth);

                Pen whitePen = new Pen(Color.White, 1);
                Pen grayPen = new Pen(Color.Gray, 1);
                Font stringFont = SystemFonts.CreateFont("Arial", 8);

                int rows = bmp.Height;
                int cols = bmp.Width;

                // draw first time entry
                string time = "HHmm";
                if (xAxisPixelDurationInMilliseconds < 60000)
                {
                    g.DrawText(time, stringFont, Color.White, new PointF(0, 3)); //draw time
                }
                else
                {
                    g.DrawText("Hours", stringFont, Color.White, new PointF(0, 3)); //draw time
                }

                // for columns, draw in X-axis lines
                int xPixelInterval =
                    (int)Math.Round(xAxisTicInterval.TotalMilliseconds / xAxisPixelDurationInMilliseconds);
                int halfInterval = xPixelInterval / 2;
                int halfheight = trackHeight / 3;
                for (int x = 1; x < cols; x++)
                {
                    if (x % halfInterval == 0)
                    {
                        g.DrawLine(whitePen, x, 0, x, halfheight);
                    }

                    if (x % xPixelInterval == 0)
                    {
                        int tickPosition = x;
                        g.DrawLine(whitePen, tickPosition, 0, tickPosition, trackHeight);
                        TimeSpan elapsedTimeSpan =
                            TimeSpan.FromMilliseconds(xAxisPixelDurationInMilliseconds * tickPosition);
                        if (xAxisPixelDurationInMilliseconds <= 1000)
                        {
                            time = $"{elapsedTimeSpan}";
                        }
                        else if (xAxisPixelDurationInMilliseconds < 60000)
                        {
                            time = $"{elapsedTimeSpan.Hours:d2}{elapsedTimeSpan.Minutes:d2}";
                        }
                        else
                        {
                            time = $"{elapsedTimeSpan.TotalHours:f0}";
                        }

                        g.DrawText(time, stringFont, Color.White, new PointF(tickPosition, 2)); //draw time
                    }
                }

                g.DrawLine(whitePen, 0, 0, trackWidth, 0); //draw upper boundary
                g.DrawLine(grayPen, 0, trackHeight - 1, trackWidth, trackHeight - 1); //draw lower boundary
            });

            return bmp;
        }

        public static TimeSpan CalculateGridInterval(TimeSpan totalDuration, int width)
        {
            double pixelDuration = totalDuration.TotalMinutes / width;
            double[] gridIntervals = { 0.08333333, 0.1666666, 0.333333, 0.5, 1.0, 2.0, 4.0, 8.0, 15.0, 30.0, 60.0 }; //minutes

            double pixelInterval = 0;
            foreach (double gridInterval in gridIntervals)
            {
                if (pixelInterval > 51)
                {
                    break;
                }

                pixelInterval = gridInterval / pixelDuration;
            }

            TimeSpan ts = TimeSpan.FromMinutes(pixelInterval * pixelDuration);
            return ts;
        }

        // mark off Y-axis 12 month time scale.
        public static Image<Rgb24> DrawYearScaleVertical(int offset, int trackHeight)
        {
            int trackWidth = 30;
            Image<Rgb24> bmp = new Image<Rgb24>(trackWidth, trackHeight);
            bmp.Mutate(g =>
            {
                g.Clear(Color.Black);

                int daysInYear = 366;
                double interval = daysInYear / (double)12;

                string[] months =
                    { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

                Pen whitePen = new Pen(Color.White, 1);

                //Pen grayPen = new Pen(Color.Gray);
                Font stringFont = SystemFonts.CreateFont("Arial", 9);

                for (int i = 0; i < 12; i++) //for pixels in the line
                {
                    int Y = (int)Math.Round(offset + (i * interval));

                    //if (offset % XaxisScale != 0) continue;
                    g.DrawLine(whitePen, Y, 0, Y, trackHeight);

                    //hour = offset / XaxisScale;
                    //if (hour >= 24)
                    //{
                    //    offset = 0;
                    //    hour = 0;
                    //}
                    g.DrawLine(whitePen, 0, Y, trackWidth, Y);
                    g.DrawText(months[i], stringFont, Color.White, new PointF(1, Y + 6)); //draw time
                } // end over all pixels

                g.DrawLine(whitePen, 0, daysInYear + offset, trackWidth, daysInYear + offset);

                //g.DrawLine(whitePen, 0, offset, trackWidth, offset);          //draw lower boundary
                // g.DrawLine(whitePen, duration, 0, duration, trackHeight - 1);//draw right end boundary

                // g.DrawString(title, stringFont, Brushes.White, new PointF(duration + 4, 3));
            });
            
            return bmp;
        }

        // mark off X-axis 12 month time scale.
        public static Image<Rgb24> DrawYearScale_horizontal(int trackWidth, int trackHeight)
        {
            Image<Rgb24> bmp = new Image<Rgb24>(trackWidth, trackHeight);
            bmp.Mutate(g =>
            {
                g.Clear(Color.Black);

                int daysInYear = 366;
                double interval = daysInYear / (double)12;

                string[] months =
                    { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

                Pen whitePen = new Pen(Color.White, 1);

                //Pen grayPen = new Pen(Color.Gray);
                Font stringFont = SystemFonts.CreateFont("Arial", 9);

                for (int i = 0; i < 12; i++)
                {
                    int X = (int)Math.Round(i * interval);
                    g.DrawLine(whitePen, X, 0, X, trackHeight);

                    //g.DrawLine(whitePen, X, 0, X, trackWidth, Y);
                    g.DrawText(months[i], stringFont, Color.White, new PointF(X + 2, 2)); //draw time
                } // end over all pixels

                g.DrawLine(whitePen, 0, daysInYear, trackWidth, daysInYear);

                //g.DrawLine(whitePen, 0, offset, trackWidth, offset);          //draw lower boundary
                // g.DrawLine(whitePen, duration, 0, duration, trackHeight - 1);//draw right end boundary

                // g.DrawString(title, stringFont, Brushes.White, new PointF(duration + 4, 3));
            });

            return bmp;
        }

        /// <summary>
        /// returns array of bytes that is the gray scale colour to use.
        /// </summary>
        public static byte[] GetXaxisTicLocations(int width, TimeSpan timeSpan)
        {
            byte[] ba = new byte[width]; //byte array
            double secondsPerPixel = timeSpan.TotalSeconds / width;

            // assume that the time scale starts at zero
            ba[0] = 2;
            ba[1] = 2;

            for (int x = 2; x < width - 1; x++)
            {
                double elapsedTime = x * secondsPerPixel;
                double mod1sec = elapsedTime % 1.0;

                // double mod2sec  = elapsedTime %  2.0;
                // double mod5sec = elapsedTime % 5.0;
                double mod10sec = elapsedTime % 10.0;
                if (mod10sec < secondsPerPixel)
                {
                    // put a major tic i.e. double black line
                    ba[x] = 2;
                    ba[x + 1] = 2;
                }
                else

                    //if (mod2sec <= secondsPerPixel)
                    //{
                    //    ba[x] = (byte)0;
                    //}
                    //else

                    // minor tic
                    if (mod1sec <= secondsPerPixel)
                    {
                        ba[x] = 1;
                    }
            }

            return ba; //byte array
        }

        public static Image<Rgb24> DrawTimeTrack(TimeSpan duration, int width)
        {
            int height = HeightOfTimeScale;
            Pen blackPen = new Pen(Color.Black, 1);
            Pen grayPen = new Pen(Color.DarkGray, 1);
            var bgBrush = new SolidBrush(Color.FromRgb(240, 240, 240));

            DateTime start = new DateTime(0);
            double secondsPerPixel = duration.TotalSeconds / width;

            byte[] hScale = GetXaxisTicLocations(width, duration);

            var bmp = new Image<Rgb24>(width, height);
            var font = SystemFonts.CreateFont("Tahoma", 8);

            bmp.Mutate(g =>
            {
                g.FillRectangle(bgBrush, 0, 0, width, height);

                // mark the tics
                for (int x = 0; x < width; x++)
                {
                    if (hScale[x] == 0)
                    {
                        continue;
                    }

                    if (hScale[x] == 1) // minor tic
                    {
                        g.DrawLine(grayPen, x, 0, x, height - 1);
                    }
                    else
                    {
                        if (hScale[x] == 2) // major tic
                        {
                            g.DrawLine(blackPen, x, 0, x, height - 1);
                        }
                    }
                } //end of adding time grid

                // add time tic labels - where to place label depends on scale of the time axis.
                int oneSecondInterval = (int)Math.Round(1 / secondsPerPixel);
                int interval = oneSecondInterval;
                if (interval < 50)
                {
                    interval *= 10;
                }

                int prevLocation = -interval - 1;
                for (int x = 1; x < width; x++)
                {
                    if (hScale[x] == 0)
                    {
                        continue;
                    }

                    //if ((hScale[x] == 2) && (x > (prevLocation + interval)))
                    if (hScale[x] == 2 && hScale[x - 1] == 2)
                    {
                        int secs = (int)Math.Round(x * secondsPerPixel);
                        TimeSpan span = new TimeSpan(0, 0, secs);
                        g.DrawText(span.ToString(), font, Color.Black, new PointF(x, 0));
                        prevLocation = x;
                    }
                }

                g.DrawLine(blackPen, 0, 0, width, 0);
                g.DrawLine(blackPen, 0, height - 1, width, height - 1);
            });

            return bmp;
        }
    }// end  class ImageTrack
}
