﻿// <copyright file="UnsupervisedFeatureLearningTest.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AnalysisPrograms
{
    using System;
    using System.Collections.Generic;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Accord.MachineLearning;
    using Accord.Math;
    using Acoustics.Shared.Csv;
    using AudioAnalysisTools.DSP;
    using AudioAnalysisTools.StandardSpectrograms;
    using AudioAnalysisTools.WavTools;
    using McMaster.Extensions.CommandLineUtils;
    using NeuralNets;
    using Production.Arguments;
    using TowseyLibrary;

    public class MahnooshSandpit
    {
        public const string CommandName = "MahnooshSandpit";

        public void Execute(Arguments arguments)
        {
            LoggedConsole.WriteLine("feature extraction process");

            var inputDir = @"D:\Mahnoosh\Liz\"; //@"C:\Users\kholghim\Mahnoosh\UnsupervisedFeatureLearning\"; //
            var resultDir = Path.Combine(inputDir, "FeatureLearning");
            var inputPath = Path.Combine(inputDir, "PatchSamplingSegments");
            var trainSetPath = Path.Combine(inputDir, "TrainSet");
            var testSetPath = Path.Combine(inputDir, "TestSet");
            var outputMelImagePath = Path.Combine(resultDir, "MelScaleSpectrogram.png");
            var outputNormMelImagePath = Path.Combine(resultDir, "NormalizedMelScaleSpectrogram.png");
            var outputNoiseReducedMelImagePath = Path.Combine(resultDir, "NoiseReducedMelSpectrogram.png");
            var outputReSpecImagePath = Path.Combine(resultDir, "ReconstrcutedSpectrogram.png");
            var outputClusterImagePath = Path.Combine(resultDir, "Clusters.bmp");

            // +++++++++++++++++++++++++++++++++++++++++++++++++patch sampling from 1-min recordings

            // check whether there is any file in the folder/subfolders
            if (Directory.GetFiles(inputPath, "*", SearchOption.AllDirectories).Length == 0)
            {
                throw new ArgumentException("The folder of recordings is empty...");
            }

            // get the nyquist value from the first wav file in the folder of recordings
            int nq = new AudioRecording(Directory.GetFiles(inputPath, "*.wav")[0]).Nyquist;

            int nyquist = nq; // 11025;
            int frameSize = 1024;
            int finalBinCount = 128; // 256; // 100; // 40; // 200; //
            int hertzInterval = 1000;
            FreqScaleType scaleType = FreqScaleType.Mel;
            var freqScale = new FrequencyScale(scaleType, nyquist, frameSize, finalBinCount, hertzInterval);
            var fst = freqScale.ScaleType;

            var sonoConfig = new SonogramConfig
            {
                WindowSize = frameSize,
                // since each 24 frames duration is equal to 1 second
                WindowOverlap = 0.1028,
                DoMelScale = (scaleType == FreqScaleType.Mel) ? true : false,
                MelBinCount = (scaleType == FreqScaleType.Mel) ? finalBinCount : frameSize / 2,
                NoiseReductionType = NoiseReductionType.None,
            };

            // MinFreBin and MaxFreqBin to work with arbitrary frequency bin bounds.
            // The default value is minFreqBin = 1 and maxFreqBin = finalBinCount.
            // For any other arbitrary frequency bin bounds, these two parameters need to be manually set .

            // Black Rail call is between 1000 Hz and 3000 Hz, which is mapped to Mel value [1000, 1876]
            // Hence, we only work with freq bins between [40, 76]
            int minFreqBin = 40;
            int maxFreqBin = 80; //76;
            int numFreqBand = 1;
            int patchWidth = (maxFreqBin - minFreqBin + 1) / numFreqBand; //finalBinCount / numFreqBand;
            int patchHeight = 1; // 2; // 4; // 16; // 6; // Frame size
            int numRandomPatches = 80; //20; //2; //10; //100; // 40; //  30; //  500; //
            // int fileCount = Directory.GetFiles(folderPath, "*.wav").Length;

            // Define variable number of "randomPatch" lists based on "numFreqBand"
            Dictionary<string, List<double[,]>> randomPatchLists = new Dictionary<string, List<double[,]>>();
            for (int i = 0; i < numFreqBand; i++)
            {
                randomPatchLists.Add(string.Format("randomPatch{0}", i.ToString()), new List<double[,]>());
            }

            List<double[,]> randomPatches = new List<double[,]>();

            /*
            foreach (string filePath in Directory.GetFiles(folderPath, "*.wav"))
            {
                FileInfo f = filePath.ToFileInfo();
                if (f.Length == 0)
                {
                    Debug.WriteLine(f.Name);
                }
            }
            */
            double[,] inputMatrix;

            foreach (string filePath in Directory.GetFiles(inputPath, "*.wav"))
            {
                FileInfo fileInfo = filePath.ToFileInfo();

                // process the wav file if it is not empty
                if (fileInfo.Length != 0)
                {
                    var recording = new AudioRecording(filePath);
                    sonoConfig.SourceFName = recording.BaseName;

                    var sonogram = new SpectrogramStandard(sonoConfig, recording.WavReader);

                    // DO RMS NORMALIZATION
                    sonogram.Data = SNR.RmsNormalization(sonogram.Data);

                    // DO NOISE REDUCTION
                    // sonogram.Data = SNR.NoiseReduce_Median(sonogram.Data, nhBackgroundThreshold: 2.0);
                    sonogram.Data = PcaWhitening.NoiseReduction(sonogram.Data);

                    // check whether the full band spectrogram is needed or a matrix with arbitrary freq bins
                    if (minFreqBin != 1 || maxFreqBin != finalBinCount)
                    {
                        inputMatrix = PatchSampling.GetArbitraryFreqBandMatrix(sonogram.Data, minFreqBin, maxFreqBin);
                    }
                    else
                    {
                        inputMatrix = sonogram.Data;
                    }

                    // creating matrices from different freq bands of the source spectrogram
                    List<double[,]> allSubmatrices = PatchSampling.GetFreqBandMatrices(inputMatrix, numFreqBand);

                    // Second: selecting random patches from each freq band matrix and add them to the corresponding patch list
                    int count = 0;
                    while (count < allSubmatrices.Count)
                    {
                        randomPatchLists[$"randomPatch{count.ToString()}"].Add(PatchSampling.GetPatches(allSubmatrices.ToArray()[count], patchWidth, patchHeight, numRandomPatches, PatchSampling.SamplingMethod.Random).ToMatrix());
                        count++;
                    }
                }
            }

            foreach (string key in randomPatchLists.Keys)
            {
                randomPatches.Add(PatchSampling.ListOf2DArrayToOne2DArray(randomPatchLists[key]));
            }

            // convert list of random patches matrices to one matrix
            int numberOfClusters = 16; //128; //20; // 256; //500; // 128; // 64; // 32; // 10; // 50;
            List<double[][]> allBandsCentroids = new List<double[][]>();
            List<KMeansClusterCollection> allClusteringOutput = new List<KMeansClusterCollection>();

            for (int i = 0; i < randomPatches.Count; i++)
            {
                double[,] patchMatrix = randomPatches[i];

                // Apply PCA Whitening
                var whitenedSpectrogram = PcaWhitening.Whitening(patchMatrix);

                // Do k-means clustering
                var clusteringOutput = KmeansClustering.Clustering(whitenedSpectrogram.Reversion, numberOfClusters);
                // var clusteringOutput = KmeansClustering.Clustering(patchMatrix, noOfClusters, pathToClusterCsvFile);

                // writing centroids to a csv file
                // note that Csv.WriteToCsv can't write data types like dictionary<int, double[]> (problems with arrays)
                // I converted the dictionary values to a matrix and used the Csv.WriteMatrixToCsv
                // it might be a better way to do this
                string pathToClusterCsvFile = Path.Combine(resultDir, "ClusterCentroids" + i.ToString() + ".csv");
                var clusterCentroids = clusteringOutput.ClusterIdCentroid.Values.ToArray();
                Csv.WriteMatrixToCsv(pathToClusterCsvFile.ToFileInfo(), clusterCentroids.ToMatrix());
                //Csv.WriteToCsv(pathToClusterCsvFile.ToFileInfo(), clusterCentroids);

                // sorting clusters based on size and output it to a csv file
                Dictionary<int, double> clusterIdSize = clusteringOutput.ClusterIdSize;
                int[] sortOrder = KmeansClustering.SortClustersBasedOnSize(clusterIdSize);

                // Write cluster ID and size to a CSV file
                string pathToClusterSizeCsvFile = Path.Combine(resultDir, "ClusterSize" + i.ToString() + ".csv");
                Csv.WriteToCsv(pathToClusterSizeCsvFile.ToFileInfo(), clusterIdSize);

                // Draw cluster image directly from clustering output
                List<KeyValuePair<int, double[]>> list = clusteringOutput.ClusterIdCentroid.ToList();
                double[][] centroids = new double[list.Count][];

                for (int j = 0; j < list.Count; j++)
                {
                    centroids[j] = list[j].Value;
                }

                allBandsCentroids.Add(centroids);
                allClusteringOutput.Add(clusteringOutput.Clusters);

                List<double[,]> allCentroids = new List<double[,]>();
                for (int k = 0; k < centroids.Length; k++)
                {
                    // convert each centroid to a matrix in order of cluster ID
                    // double[,] cent = PatchSampling.ArrayToMatrixByColumn(centroids[i], patchWidth, patchHeight);
                    // OR: in order of cluster size
                    double[,] cent = MatrixTools.ArrayToMatrixByColumn(centroids[sortOrder[k]], patchWidth, patchHeight);

                    // normalize each centroid
                    double[,] normCent = DataTools.normalise(cent);

                    // add a row of zero to each centroid
                    double[,] cent2 = PatchSampling.AddRow(normCent);

                    allCentroids.Add(cent2);
                }

                // concatenate all centroids
                double[,] mergedCentroidMatrix = PatchSampling.ListOf2DArrayToOne2DArray(allCentroids);

                // Draw clusters
                // int gridInterval = 1000;
                // var freqScale = new FrequencyScale(FreqScaleType.Mel, nyquist, frameSize, finalBinCount, gridInterval);

                var clusterImage = ImageTools.DrawMatrixWithoutNormalisation(mergedCentroidMatrix);
                clusterImage.RotateFlip(RotateFlipType.Rotate270FlipNone);
                // clusterImage.Save(outputClusterImagePath, ImageFormat.Bmp);

                var outputClusteringImage = Path.Combine(resultDir, "ClustersWithGrid" + i.ToString() + ".bmp");
                // Image bmp = ImageTools.ReadImage2Bitmap(filename);
                // FrequencyScale.DrawFrequencyLinesOnImage((Bitmap)clusterImage, freqScale, includeLabels: false);
                clusterImage.Save(outputClusteringImage);
            }

            //+++++++++++++++++++++++++++++++++++++++++++++++++++++Processing and generating features for the target recordings
            //var recording2Path = Path.Combine(trainSetPath, "SM304264_0+1_20160421_054539_29-30min.wav"); // an example from the train set
            //var recording2Path = Path.Combine(testSetPath, "SM304264_0+1_20160423_054539_29-30min.wav"); // an example from the test set
            // check whether there is any file in the folder/subfolders
            if (Directory.GetFiles(testSetPath, "*", SearchOption.AllDirectories).Length == 0)
            {
                throw new ArgumentException("The folder of recordings is empty...");
            }

            //*****
            // lists of features for all processing files
            // the key is the file name, and the value is the features for different bands
            Dictionary<string, List<double[,]>> allFilesMeanFeatureVectors = new Dictionary<string, List<double[,]>>();
            Dictionary<string, List<double[,]>> allFilesMaxFeatureVectors = new Dictionary<string, List<double[,]>>();
            Dictionary<string, List<double[,]>> allFilesStdFeatureVectors = new Dictionary<string, List<double[,]>>();
            

            foreach (string filePath in Directory.GetFiles(testSetPath, "*.wav"))
            {
                FileInfo fileInfo = filePath.ToFileInfo();

                // process the wav file if it is not empty
                if (fileInfo.Length != 0)
                {
                    var recording = new AudioRecording(filePath);
                    sonoConfig.SourceFName = recording.BaseName;

                    var sonogram = new SpectrogramStandard(sonoConfig, recording.WavReader);

                    // DO RMS NORMALIZATION
                    sonogram.Data = SNR.RmsNormalization(sonogram.Data);

                    // DO NOISE REDUCTION
                    // sonogram.Data = SNR.NoiseReduce_Median(sonogram.Data, nhBackgroundThreshold: 2.0);
                    sonogram.Data = PcaWhitening.NoiseReduction(sonogram.Data);

                    // check whether the full band spectrogram is needed or a matrix with arbitrary freq bins
                    if (minFreqBin != 1 || maxFreqBin != finalBinCount)
                    {
                        inputMatrix = PatchSampling.GetArbitraryFreqBandMatrix(sonogram.Data, minFreqBin, maxFreqBin);
                    }
                    else
                    {
                        inputMatrix = sonogram.Data;
                    }

                    // creating matrices from different freq bands of the source spectrogram
                    List<double[,]> allSubmatrices2 = PatchSampling.GetFreqBandMatrices(inputMatrix, numFreqBand);
                    double[][,] matrices2 = allSubmatrices2.ToArray();
                    List<double[,]> allSequentialPatchMatrix = new List<double[,]>();
                    for (int i = 0; i < matrices2.GetLength(0); i++)
                    {
                        int rows = matrices2[i].GetLength(0);
                        int columns = matrices2[i].GetLength(1);
                        var sequentialPatches = PatchSampling.GetPatches(matrices2[i], patchWidth, patchHeight, (rows / patchHeight) * (columns / patchWidth), PatchSampling.SamplingMethod.Sequential);
                        allSequentialPatchMatrix.Add(sequentialPatches.ToMatrix());
                    }

                    // +++++++++++++++++++++++++++++++++++Feature Transformation
                    // to do the feature transformation, we normalize centroids and
                    // sequential patches from the input spectrogram to unit length
                    // Then, we calculate the dot product of each patch with the centroids' matrix

                    List<double[][]> allNormCentroids = new List<double[][]>();
                    for (int i = 0; i < allBandsCentroids.Count; i++)
                    {
                        // double check the index of the list
                        double[][] normCentroids = new double[allBandsCentroids.ToArray()[i].GetLength(0)][];
                        for (int j = 0; j < allBandsCentroids.ToArray()[i].GetLength(0); j++)
                        {
                            normCentroids[j] = ART_2A.NormaliseVector(allBandsCentroids.ToArray()[i][j]);
                        }

                        allNormCentroids.Add(normCentroids);
                    }

                    List<double[][]> allFeatureTransVectors = new List<double[][]>();
                    for (int i = 0; i < allSequentialPatchMatrix.Count; i++)
                    {
                        double[][] featureTransVectors = new double[allSequentialPatchMatrix.ToArray()[i].GetLength(0)][];
                        for (int j = 0; j < allSequentialPatchMatrix.ToArray()[i].GetLength(0); j++)
                        {
                            // normalize each patch to unit length
                            var normVector = ART_2A.NormaliseVector(allSequentialPatchMatrix.ToArray()[i].ToJagged()[j]);
                            featureTransVectors[j] = allNormCentroids.ToArray()[i].ToMatrix().Dot(normVector);
                        }

                        allFeatureTransVectors.Add(featureTransVectors);
                    }

                    // +++++++++++++++++++++++++++++++++++Feature Transformation

                    // +++++++++++++++++++++++++++++++++++Temporal Summarization
                    // The resolution to generate features is 1 second
                    // Each 24 single-frame patches form 1 second
                    // for each 24 patch, we generate 3 vectors of mean, std, and max
                    // The pre-assumption is that each input spectrogram is 1 minute

                    // store features of different bands in lists
                    List<double[,]> allMeanFeatureVectors = new List<double[,]>();
                    List<double[,]> allMaxFeatureVectors = new List<double[,]>();
                    List<double[,]> allStdFeatureVectors = new List<double[,]>();

                    // number of frames needs to be concatenated to form 1 second. Each 24 frames make 1 second.
                    int numFrames = (24 / patchHeight) * 60; //24 / patchHeight; //

                    foreach (var freqBandFeature in allFeatureTransVectors)
                    {
                        List<double[]> meanFeatureVectors = new List<double[]>();
                        List<double[]> maxFeatureVectors = new List<double[]>();
                        List<double[]> stdFeatureVectors = new List<double[]>();
                        int c = 0;
                        while (c + numFrames < freqBandFeature.GetLength(0))
                        {
                            // First, make a list of patches that would be equal to the needed resolution (1 scond, 60 second, etc.)
                            List<double[]> sequencesOfFramesList = new List<double[]>();
                            for (int i = c; i < c + numFrames; i++)
                            {
                                sequencesOfFramesList.Add(freqBandFeature[i]);
                            }

                            List<double> mean = new List<double>();
                            List<double> std = new List<double>();
                            List<double> max = new List<double>();
                            double[,] sequencesOfFrames = sequencesOfFramesList.ToArray().ToMatrix();
                            // int len = sequencesOfFrames.GetLength(1);

                            // Second, calculate mean, max, and standard deviation of six vectors element-wise
                            for (int j = 0; j < sequencesOfFrames.GetLength(1); j++)
                            {
                                double[] temp = new double[sequencesOfFrames.GetLength(0)];
                                for (int k = 0; k < sequencesOfFrames.GetLength(0); k++)
                                {
                                    temp[k] = sequencesOfFrames[k, j];
                                }

                                mean.Add(AutoAndCrossCorrelation.GetAverage(temp));
                                std.Add(AutoAndCrossCorrelation.GetStdev(temp));
                                max.Add(temp.GetMaxValue());
                            }

                            meanFeatureVectors.Add(mean.ToArray());
                            maxFeatureVectors.Add(max.ToArray());
                            stdFeatureVectors.Add(std.ToArray());
                            c += numFrames;
                        }

                        allMeanFeatureVectors.Add(meanFeatureVectors.ToArray().ToMatrix());
                        allMaxFeatureVectors.Add(maxFeatureVectors.ToArray().ToMatrix());
                        allStdFeatureVectors.Add(stdFeatureVectors.ToArray().ToMatrix());
                    }

                    //*****
                    // the keys of the following dictionaries contain file name
                    // and their values are a list<double[,]> which the list.count is 
                    // equal to the number of freq bands defined as an user-defined parameter.
                    // the 2D-array is the feature vectors.
                    allFilesMeanFeatureVectors.Add(fileInfo.Name, allMeanFeatureVectors);
                    allFilesMaxFeatureVectors.Add(fileInfo.Name, allMaxFeatureVectors);
                    allFilesStdFeatureVectors.Add(fileInfo.Name, allStdFeatureVectors);

                    // +++++++++++++++++++++++++++++++++++Temporal Summarization

                    /*
                    // ++++++++++++++++++++++++++++++++++Writing features to file
                    // First, concatenate mean, max, std for each second.
                    // Then write to CSV file.

                    for (int j = 0; j < allMeanFeatureVectors.Count; j++)
                    {
                        // write the features of each pre-defined frequency band into a separate CSV file
                        var outputFeatureFile = Path.Combine(resultDir, "FeatureVectors-" + j.ToString() + fileInfo.Name + ".csv");

                        // creating the header for CSV file
                        List<string> header = new List<string>();
                        for (int i = 0; i < allMeanFeatureVectors.ToArray()[j].GetLength(1); i++)
                        {
                            header.Add("mean" + i.ToString());
                        }

                        for (int i = 0; i < allStdFeatureVectors.ToArray()[j].GetLength(1); i++)
                        {
                            header.Add("std" + i.ToString());
                        }

                        for (int i = 0; i < allMaxFeatureVectors.ToArray()[j].GetLength(1); i++)
                        {
                            header.Add("max" + i.ToString());
                        }

                        // concatenating mean, std, and max vector together for each 1 second
                        List<double[]> featureVectors = new List<double[]>();
                        for (int i = 0; i < allMeanFeatureVectors.ToArray()[j].ToJagged().GetLength(0); i++)
                        {
                            List<double[]> featureList = new List<double[]>
                            {
                                allMeanFeatureVectors.ToArray()[j].ToJagged()[i],
                                allMaxFeatureVectors.ToArray()[j].ToJagged()[i],
                                allStdFeatureVectors.ToArray()[j].ToJagged()[i],
                            };
                            double[] featureVector = DataTools.ConcatenateVectors(featureList);
                            featureVectors.Add(featureVector);
                        }

                        // writing feature vectors to CSV file
                        using (StreamWriter file = new StreamWriter(outputFeatureFile))
                        {
                            // writing the header to CSV file
                            foreach (var entry in header.ToArray())
                            {
                                file.Write(entry + ",");
                            }

                            file.Write(Environment.NewLine);

                            foreach (var entry in featureVectors.ToArray())
                            {
                                foreach (var value in entry)
                                {
                                    file.Write(value + ",");
                                }

                                file.Write(Environment.NewLine);
                            }
                        }
                    }
                    */
                    /*
                    // Reconstructing the target spectrogram based on clusters' centroids
                    List<double[,]> convertedSpec = new List<double[,]>();
                    int columnPerFreqBand = sonogram2.Data.GetLength(1) / numFreqBand;
                    for (int i = 0; i < allSequentialPatchMatrix.Count; i++)
                    {
                        double[,] reconstructedSpec2 = KmeansClustering.ReconstructSpectrogram(allSequentialPatchMatrix.ToArray()[i], allClusteringOutput.ToArray()[i]);
                        convertedSpec.Add(PatchSampling.ConvertPatches(reconstructedSpec2, patchWidth, patchHeight, columnPerFreqBand));
                    }

                    sonogram2.Data = PatchSampling.ConcatFreqBandMatrices(convertedSpec);

                    // DO DRAW SPECTROGRAM
                    var reconstructedSpecImage = sonogram2.GetImageFullyAnnotated(sonogram2.GetImage(), "RECONSTRUCTEDSPECTROGRAM: " + freqScale.ScaleType.ToString(), freqScale.GridLineLocations);
                    reconstructedSpecImage.Save(outputReSpecImagePath, ImageFormat.Png);
                    */
                }
            }

            //*****
            // ++++++++++++++++++++++++++++++++++Writing features to one file
            // First, concatenate mean, max, std for each second.
            // Then, write the features of each pre-defined frequency band into a separate CSV file.

            var filesName = allFilesMeanFeatureVectors.Keys.ToArray();
            var meanFeatures = allFilesMeanFeatureVectors.Values.ToArray();
            var maxFeatures = allFilesMaxFeatureVectors.Values.ToArray();
            var stdFeatures = allFilesStdFeatureVectors.Values.ToArray();

            // The number of elements in the list shows the number of freq bands
            // the size of each element in the list shows the number of files processed to generate feature for.
            // the dimensions of the matrix shows the number of feature vectors generated for each file and the length of feature vector
            var allMeans = new List<double[][,]>();
            var allMaxs = new List<double[][,]>();
            var allStds = new List<double[][,]>();

            // looping over freq bands
            for (int i = 0; i < meanFeatures[0].Count; i++)
            {
                var means = new List<double[,]>();
                var maxs = new List<double[,]>();
                var stds = new List<double[,]>();

                // looping over all files
                for (int k = 0; k < meanFeatures.Length; k++)
                {
                    means.Add(meanFeatures[k].ToArray()[i]);
                    maxs.Add(maxFeatures[k].ToArray()[i]);
                    stds.Add(stdFeatures[k].ToArray()[i]);
                }

                allMeans.Add(means.ToArray());
                allMaxs.Add(maxs.ToArray());
                allStds.Add(stds.ToArray());
            }

            // each element of meanFeatures array is a list of features for different frequency bands.
            // looping over the number of freq bands
            for (int i = 0; i < allMeans.ToArray().GetLength(0); i++)
            {
                // creating output feature file based on the number of freq bands
                var outputFeatureFile = Path.Combine(resultDir, "FeatureVectors-" + i.ToString() + ".csv");

                // creating the header for CSV file
                List<string> header = new List<string>();
                header.Add("file name");
                for (int j = 0; j < allMeans.ToArray()[i][0].GetLength(1); j++)
                {
                    header.Add("mean" + j.ToString());
                }

                for (int j = 0; j < allStds.ToArray()[i][0].GetLength(1); j++)
                {
                    header.Add("std" + j.ToString());
                }

                for (int j = 0; j < allMaxs.ToArray()[i][0].GetLength(1); j++)
                {
                    header.Add("max" + j.ToString());
                }

                var csv = new StringBuilder();
                string content = string.Empty;
                foreach (var entry in header.ToArray())
                {
                    content += entry.ToString() + ",";
                }

                csv.AppendLine(content);

                var allFilesFeatureVectors = new Dictionary<string, double[,]>();

                // looping over files
                for (int j = 0; j < allMeans.ToArray()[i].GetLength(0); j++)
                {
                    // concatenating mean, std, and max vector together for the pre-defined resolution
                    List<double[]> featureVectors = new List<double[]>();
                    for (int k = 0; k < allMeans.ToArray()[i][j].ToJagged().GetLength(0); k++)
                    {
                        List<double[]> featureList = new List<double[]>
                        {
                            allMeans.ToArray()[i][j].ToJagged()[k],
                            allMaxs.ToArray()[i][j].ToJagged()[k],
                            allStds.ToArray()[i][j].ToJagged()[k],
                        };
                        double[] featureVector = DataTools.ConcatenateVectors(featureList);
                        featureVectors.Add(featureVector);
                    }

                    allFilesFeatureVectors.Add(filesName[j], featureVectors.ToArray().ToMatrix());
                }

                // writing feature vectors to CSV file
                foreach (var entry in allFilesFeatureVectors)
                {
                    content = string.Empty;
                    content += entry.Key.ToString() + ",";
                    foreach (var cent in entry.Value)
                    {
                        content += cent.ToString() + ",";
                    }

                    csv.AppendLine(content);
                }

                File.WriteAllText(outputFeatureFile, csv.ToString());
            }
            //*****
        }

        [Command(
            CommandName,
            Description = "Temporary entry point for unsupervised feature learning")]
        public class Arguments : SubCommandBase
        {
            public override Task<int> Execute(CommandLineApplication app)
            {
                var instance = new MahnooshSandpit();
                instance.Execute(this);
                return this.Ok();
            }
        }
    }
}
