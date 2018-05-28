﻿// <copyright file="PatchSampling.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AudioAnalysisTools.DSP
{
    using System;
    using System.Collections.Generic;
    using Accord.Math;
    using TowseyLibrary;

    public static class PatchSampling
    {
        /// <summary>
        /// sample a set of patches ("sequential" or "random" or "overlapped random") from a spectrogram
        /// in "sequential" mode, it generates non-overlapping patches from the whole input matrix, and
        /// in this case the "numOfPatches" can be simply set to zero.
        /// However, in "random" mode, the method requires an input for the "numOfPatches" parameter.
        /// </summary>

        /// <summary>
        /// The sampling method.
        /// </summary>
        public enum SamplingMethod
        {
            /// <summary>
            /// Sequential patches.
            /// </summary>
            Sequential = 0,

            /// <summary>
            /// Random Patches.
            /// </summary>
            Random = 1,

            /// <summary>
            /// Overlapping Random Patches.
            /// </summary>
            OverlappedRandom = 2,
        }

        public static double[][] GetPatches(double[,] spectrogram, int patchWidth, int patchHeight, int numberOfPatches, SamplingMethod samplingMethod)
        {
            List<double[]> patches = new List<double[]>();
            if (samplingMethod == SamplingMethod.Sequential)
            {
                patches = GetSequentialPatches(spectrogram, patchWidth, patchHeight);
            }
            else
            {
                if (samplingMethod == SamplingMethod.Random)
                {
                    patches = GetRandomPatches(spectrogram, patchWidth, patchHeight, numberOfPatches);
                }
                else
                {
                    if (samplingMethod == SamplingMethod.OverlappedRandom)
                    {
                       patches = GetOverlappedRandomPatches(spectrogram, patchWidth, patchHeight, numberOfPatches);
                    }
                }
            }

            return patches.ToArray();
        }

        /// <summary>
        /// converts a set of patches to a matrix of original size after applying pca.
        /// the assumption here is that the input matrix is a sequential non-overlapping patches.
        /// </summary>
        public static double[,] ConvertPatches(double[,] whitenedPatches, int patchWidth, int patchHeight, int colSize)
        {
            int ht = whitenedPatches.GetLength(0);
            double[][] patches = whitenedPatches.ToJagged();
            List<double[,]> allPatches = new List<double[,]>();

            for (int row = 0; row < ht; row++)
            {
                allPatches.Add(MatrixTools.ArrayToMatrixByColumn(patches[row], patchWidth, patchHeight));
            }

            double[,] matrix = ConcatenateGridOfPatches(allPatches, colSize, patchWidth, patchHeight);

            return matrix;
        }

        /// <summary>
        /// construct the original matrix from a list of sequential patches
        /// all vectors in list are of the same length
        /// </summary>
        public static double[,] ConcatenateGridOfPatches(List<double[,]> list, int colSize, int patchWidth, int patchHeight)
        {
            double[][,] arrayOfPatches = list.ToArray();

            // number of patches
            int rows = list.Count;
            int numberOfItemsInRow = colSize / patchWidth; 
            int numberOfItemsInColumn = rows / numberOfItemsInRow;
            double[,] matrix = new double[numberOfItemsInColumn * patchHeight, numberOfItemsInRow * patchWidth];

            // the number of patches in each row of the matrix
            for (int i = 0; i < numberOfItemsInColumn; i++)
            {
                // the number of patches in each column of the matrix
                for (int j = 0; j < numberOfItemsInRow; j++)
                {

                    // the id of patch is equal to (i * numberOfItemsInRow) + j
                    for (int r = 0; r < list[(i * numberOfItemsInRow) + j].GetLength(0); r++)
                    {
                        for (int c = 0; c < list[(i * numberOfItemsInRow) + j].GetLength(1); c++)
                        {
                            matrix[r + (i * patchHeight), c + (j * patchWidth)] = arrayOfPatches[(i * numberOfItemsInRow) + j][r, c];
                        }
                    }
                }
            }

            return matrix;
        }

        /// <summary>
        /// converts a spectrogram matrix to submatrices by dividing the column of input matrix to
        /// different freq bands with equal size. Output submatrices have same number of rows and same number 
        /// of columns. numberOfBands as an input parameter indicates how many output bands are needed.
        /// </summary>
        public static List<double[,]> GetFreqBandMatrices(double[,] matrix, int numberOfBands)
        {
            List<double[,]> allSubmatrices = new List<double[,]>();
            int cols = matrix.GetLength(1); // number of freq bins
            int rows = matrix.GetLength(0);
            int newCol = cols / numberOfBands;

            int bandId = 0;
            while (bandId < numberOfBands)
            {
                double[,] m = new double[rows, newCol];
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < newCol; j++)
                    {
                        m[i, j] = matrix[i, j + (newCol * bandId)];
                    }
                }

                allSubmatrices.Add(m);
                bandId++;
            }

            // Note that if we want the first 1/4 as the lower band, the second and third 1/4 as the mid,
            // and the last 1/4 is the upper freq band, we need to use the commented part of the code.
            /*
            double[,] minFreqBandMatrix = new double[rows, newCol];
            double[,] maxFreqBandMatrix = new double[rows, newCol];

            // Note that I am not aware of any faster way to copy a part of 2D-array
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < newCol; j++)
                {
                    minFreqBandMatrix[i, j] = matrix[i, j];
                }
            }

            allSubmatrices.Add(minFreqBandMatrix);

            if (numberOfBands == 3)
            {
                double[,] midFreqBandMatrix = new double[rows, newCol * 2];
                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < newCol * 2; j++)
                    {
                        midFreqBandMatrix[i, j] = matrix[i, j + newCol];
                    }
                }

                allSubmatrices.Add(midFreqBandMatrix);
            }
            else
            {
                if (numberOfBands == 4)
                {
                    double[,] mid1FreqBandMatrix = new double[rows, newCol];
                    double[,] mid2FreqBandMatrix = new double[rows, newCol];
                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < newCol; j++)
                        {
                            mid1FreqBandMatrix[i, j] = matrix[i, j + newCol];
                        }
                    }

                    allSubmatrices.Add(mid1FreqBandMatrix);

                    for (int i = 0; i < rows; i++)
                    {
                        for (int j = 0; j < newCol; j++)
                        {
                            mid2FreqBandMatrix[i, j] = matrix[i, j + newCol * 2];
                        }
                    }

                    allSubmatrices.Add(mid2FreqBandMatrix);
                }
            }

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < newCol; j++)
                {
                    maxFreqBandMatrix[i, j] = matrix[i, j + newCol * 3];
                }
            }

            allSubmatrices.Add(maxFreqBandMatrix);
            */
            return allSubmatrices;
        }

        /// <summary>
        /// concatenate submatrices column-wise into one matrix, i.e., the number of rows for the output matrix
        /// is equal to the number of rows of each of the frequency band matrices.
        /// </summary>
        public static double[,] ConcatFreqBandMatrices(List<double[,]> submatrices)
        {
            // The assumption here is all frequency band matrices have the same number of columns.
            int columnSize = submatrices.Count * submatrices[0].GetLength(1);
            int rowSize = submatrices[0].GetLength(0);
            double[,] matrix = new double[rowSize, columnSize];
            int count = 0;
            while (count < submatrices.Count)
            {
                DoubleSquareArrayExtensions.AddToArray(matrix, submatrices[count], DoubleSquareArrayExtensions.MergingDirection.Column, submatrices[count].GetLength(1) * count);
                count++;
            }

            // If we have frequency band matrices with different number of columns,
            // Then the below commented code need to be used.
            /*
            double[][,] submat = submatrices.ToArray();
            int colSize = 0;
            for (int i = 0; i < submat.Length; i++)
            {
                colSize = colSize + submat[i].GetLength(1);
            }

            // storing the number of rows of each submatrice in an array
            int[] noRows = new int[submat.Length];
            for (int i = 0; i < submat.Length; i++)
            {
                noRows[i] = submat[i].GetLength(0);
            }

            // find the max number of rows from noRows array
            int maxRows = noRows.Max();

            double[,] matrix = new double[maxRows, colSize];

            // might be better way to do this
            AddToArray(matrix, submat[0], "column");
            AddToArray(matrix, submat[1], "column", submat[0].GetLength(1));
            AddToArray(matrix, submat[2], "column", submat[0].GetLength(1) + submat[1].GetLength(1));
            */

            return matrix;
        }

        /// <summary>
        /// convert a list of patch matrices to one matrix
        /// </summary>
        public static double[,] ListOf2DArrayToOne2DArray(List<double[,]> listOfPatchMatrices)
        {
            int numberOfPatches = listOfPatchMatrices[0].GetLength(0);
            double[,] allPatchesMatrix = new double[listOfPatchMatrices.Count * numberOfPatches, listOfPatchMatrices[0].GetLength(1)];
            for (int i = 0; i < listOfPatchMatrices.Count; i++)
            {
                var m = listOfPatchMatrices[i];
                if (m.GetLength(0) != numberOfPatches)
                {
                    throw new ArgumentException("All arrays must be the same length");
                }

                DoubleSquareArrayExtensions.AddToArray(allPatchesMatrix, m, DoubleSquareArrayExtensions.MergingDirection.Row, i * m.GetLength(0));
            }

            return allPatchesMatrix;
        }

        /// <summary>
        /// Adding a row of zero/one to 2D array
        /// </summary>
        public static double[,] AddRow(double[,] matrix)
        {
            double[,] newMatrix = new double[matrix.GetLength(0) + 1, matrix.GetLength(1)];
            double[] newArray = new double[matrix.GetLength(1)];

            int minX = matrix.GetLength(0);
            int minY = matrix.GetLength(1);

            // copying the original matrix to a new matrix (row by row)

            for (int i = 0; i < minX; ++i)
            {
                Array.Copy(matrix, i * matrix.GetLength(1), newMatrix, i * matrix.GetLength(1), minY);
            }

            // creating an array of "1.0" or "0.0"
            for (int j = 0; j < matrix.GetLength(1); j++)
            {
                newArray[j] = 1.0;
            }

            //convert the new array to a matrix
            double[,] matrix2 = MatrixTools.ArrayToMatrixByColumn(newArray, newArray.Length, 1);
            int minX2 = matrix2.GetLength(0);
            int minY2 = matrix2.GetLength(1);

            // copying the array of one or zero to the last row of the new matrix
            for (int i = 0; i < minX2; ++i)
            {
                Array.Copy(matrix2, i * matrix2.GetLength(1), newMatrix, minX * minY, minY2);
            }

            return newMatrix;
        }

        /// <summary>
        /// Generate non-overlapping sequential patches from a <paramref name="matrix"/>
        /// </summary>
        private static List<double[]> GetSequentialPatches(double[,] matrix, int patchWidth, int patchHeight)
        {
            List<double[]> patches = new List<double[]>();

            int rows = matrix.GetLength(0);
            int columns = matrix.GetLength(1);
            for (int r = 0; r < rows / patchHeight; r++)
            {
                for (int c = 0; c < columns / patchWidth; c++)
                {
                    double[,] submatrix = MatrixTools.Submatrix(matrix, r * patchHeight, c * patchWidth,
                        (r * patchHeight) + patchHeight - 1, (c * patchWidth) + patchWidth - 1);

                    // convert a matrix to a vector by concatenating columns and
                    // store it to the array of vectors
                    patches.Add(MatrixTools.Matrix2Array(submatrix));
                }
            }

            return patches;
        }

        /// <summary>
        /// Generate non-overlapping random patches from a matrix
        /// </summary>
        private static List<double[]> GetRandomPatches(double[,] matrix, int patchWidth, int patchHeight, int numberOfPatches)
        {
            int seed = 100;
            Random randomNumber = new Random(seed);
            List<double[]> patches = new List<double[]>();

            int rows = matrix.GetLength(0);
            int columns = matrix.GetLength(1);
            for (int i = 0; i < numberOfPatches; i++)
            {
                // selecting a random number from the height of the matrix
                int rowRandomNumber = randomNumber.Next(0, rows - patchHeight);

                // selecting a random number from the width of the matrix
                int columnRandomNumber = randomNumber.Next(0, columns - patchWidth);
                double[,] submatrix = MatrixTools.Submatrix(matrix, rowRandomNumber, columnRandomNumber,
                    rowRandomNumber + patchHeight - 1, columnRandomNumber + patchWidth - 1);

                // convert a matrix to a vector by concatenating columns and
                // store it to the array of vectors
                patches.Add(MatrixTools.Matrix2Array(submatrix));
            }

            return patches;
        }

        /// <summary>
        /// Generate overlapped random patches from a matrix
        /// </summary>
        private static List<double[]> GetOverlappedRandomPatches(double[,] matrix, int patchWidth, int patchHeight, int numberOfPatches)
        {
            int seed = 100;
            Random randomNumber = new Random(seed);
            List<double[]> patches = new List<double[]>();

            int rows = matrix.GetLength(0);
            int columns = matrix.GetLength(1);
            int no = 0;
            while (no < numberOfPatches)
            {
                // First select a random patch
                // selecting a random number from the height of the matrix
                int rowRandomNumber = randomNumber.Next(0, rows - patchHeight);

                // selecting a random number from the width of the matrix
                int columnRandomNumber = randomNumber.Next(0, columns - patchWidth);
                double[,] submatrix = MatrixTools.Submatrix(matrix, rowRandomNumber, columnRandomNumber,
                    rowRandomNumber + patchHeight - 1, columnRandomNumber + patchWidth - 1);

                // convert a matrix to a vector by concatenating columns and
                // store it to the array of vectors
                patches.Add(MatrixTools.Matrix2Array(submatrix));
                no++;

                // shifting the row by one
                // note that if we select full band patches, then we don't need to shift the column.
                rowRandomNumber = rowRandomNumber + 1;

                // Second, slide the patch window (rInt+1) to select the next patch
                double[,] submatrix2 = MatrixTools.Submatrix(matrix, rowRandomNumber, columnRandomNumber,
                    rowRandomNumber + patchHeight - 1, columnRandomNumber + patchWidth - 1);
                patches.Add(MatrixTools.Matrix2Array(submatrix2));
                no++;

                // The below commented code can be used when shifting the row by three
                /*
                rInt = rInt + 2;
                // Second, slide the patch window (rowRandomNumber+1) to select the next patch
                double[,] submatrix3 = MatrixTools.Submatrix(spectrogram, rowRandomNumber, columnRandomNumber,
                    rowRandomNumber + patchHeight - 1, columnRandomNumber + patchWidth - 1);
                patches.Add(MatrixTools.MatrixToArray(submatrix3));
                no++;
                */
            }

            return patches;
        }
    }
}