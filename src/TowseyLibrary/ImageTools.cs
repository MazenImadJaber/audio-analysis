// <copyright file="ImageTools.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace TowseyLibrary
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using Acoustics.Shared.Extensions;
    using AForge.Imaging.Filters;
    using ColorMine.ColorSpaces;
    using MathNet.Numerics.LinearAlgebra;

    public enum Kernal
    {
        LowPass,
        HighPass1,
        HighPass2,
        VerticalLine,
        HorizontalLine3,
        HorizontalLine5,
        DiagLine1,
        DiagLine2,
        GaussianBlur5,
        Grid2,
        Grid3,
        Grid4,
        Grid2Wave,
        Grid3Wave, //grid filters
        Laplace1,
        Laplace2,
        Laplace3,
        Laplace4,
        Erroneous,
        SobelX,
        SobelY,
    }

    public class ImageTools
    {

        // this is a list of predefined colors in the Color class.
        private static string[] ColorNames = new[]
        {
            "AliceBlue", "AntiqueWhite", "Aqua", "Aquamarine", "Azure", "Beige", "Bisque", "Black", "BlanchedAlmond", "Blue", "BlueViolet",
            "Brown", "BurlyWood", "CadetBlue", "Chartreuse", "Chocolate", "Coral", "CornflowerBlue", "Cornsilk", "Crimson", "Cyan",
            "DarkBlue", "DarkCyan", "DarkGoldenrod", "DarkGray", "DarkGreen", "DarkKhaki", "DarkMagenta", "DarkOliveGreen", "DarkOrange",
            "DarkOrchid", "DarkRed", "DarkSalmon", "DarkSeaGreen", "DarkSlateBlue", "DarkSlateGray", "DarkTurquoise", "DarkViolet",
            "DeepPink", "DeepSkyBlue", "DimGray", "DodgerBlue", "Firebrick", "FloralWhite", "ForestGreen", "Fuchsia", "Gainsboro",
            "GhostWhite", "Gold", "Goldenrod", "Gray", "Green", "GreenYellow", "Honeydew", "HotPink", "IndianRed", "Indigo", "Ivory", "Khaki",
            "Lavender", "LavenderBlush", "LawnGreen", "LemonChiffon", "LightBlue", "LightCoral", "LightCyan", "LightGoldenrodYellow",
            "LightGray", "LightGreen", "LightPink", "LightSalmon", "LightSeaGreen", "LightSkyBlue", "LightSlateGray", "LightSteelBlue",
            "LightYellow", "Lime", "LimeGreen", "Linen", "Magenta", "Maroon", "MediumAquamarine", "MediumBlue", "MediumOrchid",
            "MediumPurple", "MediumSeaGreen", "MediumSlateBlue", "MediumSpringGreen", "MediumTurquoise", "MediumVioletRed",
            "MidnightBlue", "MintCream", "MistyRose", "Moccasin", "NavajoWhite", "Navy", "OldLace", "Olive", "OliveDrab", "Orange",
            "OrangeRed", "Orchid", "PaleGoldenrod", "PaleGreen", "PaleTurquoise", "PaleVioletRed", "PapayaWhip", "PeachPuff", "Peru",
            "Pink", "Plum", "PowderBlue", "Purple", "Red", "RosyBrown", "RoyalBlue", "SaddleBrown", "Salmon", "SandyBrown", "SeaGreen",
            "SeaShell", "Sienna", "Silver", "SkyBlue", "SlateBlue", "SlateGray", "Snow", "SpringGreen", "SteelBlue", "Tan", "Teal",
            "Thistle", "Tomato", /*"Transparent",*/"Turquoise", "Violet", "Wheat", "White", "WhiteSmoke", "Yellow", "YellowGreen",
        };

        public static Color[] Colors =
        {
            Color.AliceBlue, Color.AntiqueWhite, Color.Aqua, Color.Aquamarine, Color.Azure, Color.Beige, Color.Bisque, Color.Black,
            Color.BlanchedAlmond, Color.Blue, Color.BlueViolet, Color.Brown, Color.BurlyWood, Color.CadetBlue, Color.Chartreuse,
            Color.Chocolate, Color.Coral, Color.CornflowerBlue, Color.Cornsilk, Color.Crimson, Color.Cyan, Color.DarkBlue,
            Color.DarkCyan, Color.DarkGoldenrod, Color.DarkGray, Color.DarkGreen, Color.DarkKhaki, Color.DarkMagenta,
            Color.DarkOliveGreen, Color.DarkOrange, Color.DarkOrchid, Color.DarkRed, Color.DarkSalmon, Color.DarkSeaGreen,
            Color.DarkSlateBlue, Color.DarkSlateGray, Color.DarkTurquoise, Color.DarkViolet, Color.DeepPink, Color.DeepSkyBlue,
            Color.DimGray, Color.DodgerBlue, Color.Firebrick, Color.FloralWhite, Color.ForestGreen, Color.Fuchsia,
            Color.Gainsboro, Color.GhostWhite, Color.Gold, Color.Goldenrod, Color.Gray, Color.Green, Color.GreenYellow,
            Color.Honeydew, Color.HotPink, Color.IndianRed, Color.Indigo, Color.Ivory, Color.Khaki, Color.Lavender,
            Color.LavenderBlush, Color.LawnGreen, Color.LemonChiffon, Color.LightBlue, Color.LightCoral, Color.LightCyan,
            Color.LightGoldenrodYellow, Color.LightGray, Color.LightGreen, Color.LightPink, Color.LightSalmon,
            Color.LightSeaGreen, Color.LightSkyBlue, Color.LightSlateGray, Color.LightSteelBlue, Color.LightYellow, Color.Lime,
            Color.LimeGreen, Color.Linen, Color.Magenta, Color.Maroon, Color.MediumAquamarine, Color.MediumBlue,
            Color.MediumOrchid, Color.MediumPurple, Color.MediumSeaGreen, Color.MediumSlateBlue, Color.MediumSpringGreen,
            Color.MediumTurquoise, Color.MediumVioletRed, Color.MidnightBlue, Color.MintCream, Color.MistyRose, Color.Moccasin,
            Color.NavajoWhite, Color.Navy, Color.OldLace, Color.Olive, Color.OliveDrab, Color.Orange, Color.OrangeRed,
            Color.Orchid, Color.PaleGoldenrod, Color.PaleGreen, Color.PaleTurquoise, Color.PaleVioletRed, Color.PapayaWhip,
            Color.PeachPuff, Color.Peru, Color.Pink, Color.Plum, Color.PowderBlue, Color.Purple, Color.Red, Color.RosyBrown,
            Color.RoyalBlue, Color.SaddleBrown, Color.Salmon, Color.SandyBrown, Color.SeaGreen, Color.SeaShell, Color.Sienna,
            Color.Silver, Color.SkyBlue, Color.SlateBlue, Color.SlateGray, Color.Snow, Color.SpringGreen, Color.SteelBlue,
            Color.Tan, Color.Teal, Color.Thistle, Color.Tomato, /*Color.Transparent,*/ Color.Turquoise, Color.Violet, Color.Wheat,
            Color.White, Color.WhiteSmoke, Color.Yellow, Color.YellowGreen,
        };

        public static Color[] DarkColors =
        { /*Color.AliceBlue,*/ /*Color.Aqua, Color.Aquamarine, Color.Azure, Color.Bisque,*/
            Color.Black, Color.Blue, Color.BlueViolet, /*Color.Brown, Color.BurlyWood,*/ Color.CadetBlue, /*Color.Chartreuse,*/
            Color.Chocolate, /*Color.Coral,*/ /*Color.CornflowerBlue,*/ /*Color.Cornsilk,*/ Color.Crimson, Color.Cyan, Color.DarkBlue,
            Color.DarkCyan, Color.DarkGoldenrod, Color.DarkGray, Color.DarkGreen, Color.DarkKhaki, Color.DarkMagenta,
            Color.DarkOliveGreen, Color.DarkOrange, Color.DarkOrchid, Color.DarkRed, Color.DarkSalmon, Color.DarkSeaGreen,
            Color.DarkSlateBlue, Color.DarkSlateGray, Color.DarkTurquoise, Color.DarkViolet, Color.DeepPink, Color.DeepSkyBlue,
            Color.DimGray, Color.DodgerBlue, Color.Firebrick, Color.ForestGreen, Color.Fuchsia,
            Color.Gainsboro, Color.Gold, Color.Goldenrod, /*Color.Gray,*/ Color.Green, /*Color.GreenYellow,*/
            Color.Honeydew, Color.HotPink, Color.IndianRed, Color.Indigo, /*Color.Khaki,*/ Color.Lavender,
                             /*Color.LavenderBlush,*/ Color.LawnGreen, /*Color.LemonChiffon,*/ Color.Lime,
            Color.LimeGreen, /*Color.Linen,*/ Color.Magenta, Color.Maroon, Color.MediumAquamarine, Color.MediumBlue,
                             /*Color.MediumOrchid,*/ Color.MediumPurple, /*Color.MediumSeaGreen,*/ Color.MediumSlateBlue, Color.MediumSpringGreen,
            Color.MediumTurquoise, Color.MediumVioletRed, Color.MidnightBlue, /*Color.MistyRose,*/ /*Color.Moccasin,*/
            Color.Navy, /*Color.OldLace,*/ Color.Olive, /*Color.OliveDrab,*/ Color.Orange, Color.OrangeRed,
                             /*Color.Orchid, Color.PaleVioletRed, Color.PapayaWhip, */
                             /*Color.PeachPuff,*/ /*Color.Peru,*/ Color.Pink, Color.Plum, /*Color.PowderBlue,*/ Color.Purple, Color.Red, Color.RosyBrown,
            Color.RoyalBlue, Color.SaddleBrown, Color.Salmon, /*Color.SandyBrown,*/ Color.SeaGreen, /*Color.Sienna,*/
                             /*Color.Silver,*/ Color.SkyBlue, Color.SlateBlue, /*Color.SlateGray,*/ Color.SpringGreen, Color.SteelBlue,
                             /*Color.Tan,*/ Color.Teal, Color.Thistle, Color.Tomato, Color.Turquoise, Color.Violet, /*Color.Wheat,*/
                             /*Color.Yellow,*/ Color.YellowGreen,
        };

        private static double[,] lowPassKernal =
        {
        {
            0.1, 0.1, 0.1,
        },
        {
                                                0.1, 0.2, 0.1,
                                            },
        {
                                                0.1, 0.1, 0.1,
                                            },
        };

        private static double[,] highPassKernal1 =
        {
        {
            -1.0, -1.0, -1.0,
        },
        {
            -1.0, 9.0, -1.0,
        },
        {
            -1.0, -1.0, -1.0,
        },
    };

        private static double[,] highPassKernal2 =
        {
        {
            -0.3, -0.3, -0.3, -0.3, -0.3,
        },
        {
                                                -0.3, -0.3, -0.3, -0.3, -0.3,
                                            },
        {
                                                -0.3, -0.3,  9.7, -0.3, -0.3,
                                            },
        {
                                                -0.3, -0.3, -0.3, -0.3, -0.3,
                                            },
        {
                                                -0.3, -0.3, -0.3, -0.3, -0.3,
                                            },
        };

        private static double[,] vertLineKernal =
        {
        {
            -0.5, 1.0, -0.5,
        },
        {
            -0.5, 1.0, -0.5,
        },
        {
            -0.5, 1.0, -0.5,
        },
    };

        private static double[,] horiLineKernal3 =
        {
        {
            -0.5, -0.5, -0.5,
        },
        {
            1.0, 1.0, 1.0,
        },
        {
            -0.5, -0.5, -0.5,
        },
    };

        private static double[,] horiLineKernal5 =
        {
        {
            -0.5, -0.5, -0.5, -0.5, -0.5,
        },
        {
            1.0, 1.0, 1.0, 1.0, 1.0,
        },
        {
            -0.5, -0.5, -0.5, -0.5, -0.5,
        },
    };

        private static double[,] diagLineKernal1 =
        {
        {
            2.0, -1.0, -1.0,
        },
        {
            -1.0, 2.0, -1.0,
        },
        {
            -1.0, -1.0, 2.0,
        },
    };

        private static double[,] diagLineKernal2 =
        {
        {
            -1.0, -1.0, 2.0,
        },
        {
            -1.0, 2.0, -1.0,
        },
        {
            2.0, -1.0, -1.0,
        },
    };

        private static double[,] Laplace1Kernal =
        {
        {
            0.0, -1.0, 0.0,
        },
        {
            -1.0, 4.0, -1.0,
        },
        {
            0.0, -1.0, 0.0,
        },
    };

        private static double[,] Laplace2Kernal =
        {
        {
            -1.0, -1.0, -1.0,
        },
        {
            -1.0, 8.0, -1.0,
        },
        {
            -1.0, -1.0, -1.0,
        },
    };

        private static double[,] Laplace3Kernal =
        {
        {
            1.0, -2.0, 1.0,
        },
        {
            -2.0, 4.0, -2.0,
        },
        {
            1.0, -2.0, 1.0,
        },
    };

        private static double[,] Laplace4Kernal =
        {
        {
            -1.0, -1.0, -1.0,
        },
        {
            -1.0, 9.0, -1.0,
        },
        {
            -1.0, -1.0, -1.0,
        },
    }; //subtracts original

        private static double[,] grid2 =
        {
        {
            -0.5, 1.0, -1.0, 1.0, -1.0, 1.0, -0.5,
        },
        {
                                                -0.5, 1.0, -1.0, 1.0, -1.0, 1.0, -0.5,
                                            },

//                                            { -0.5, 1.0, -1.0, 1.0, -1.0, 1.0, -0.5},
//                                            { -0.5, 1.0, -1.0, 1.0, -1.0, 1.0, -0.5},
//                                            { -0.5, 1.0, -1.0, 1.0, -1.0, 1.0, -0.5},
//                                            { -0.5, 1.0, -1.0, 1.0, -1.0, 1.0, -0.5},
        {
                                                -0.5, 1.0, -1.0, 1.0, -1.0, 1.0, -0.5,
                                            },
        };

        //static double[,] grid2Wave =      { { -0.5, 1.0, -1.5, 2.0, -1.5, 1.0, -0.5},
        //                                    { -0.5, 1.0, -1.5, 2.0, -1.5, 1.0, -0.5},
        //                                    { -0.5, 1.0, -1.5, 2.0, -1.5, 1.0, -0.5}};
        private static double[,] grid3 =
        {
        {
            -0.5, 1.0, -0.5, -0.5, 1.0, -0.5, -0.5, 1.0, -0.5,
        },
        {
                                                -0.5, 1.0, -0.5, -0.5, 1.0, -0.5, -0.5, 1.0, -0.5,
                                            },
        {
                                                -0.5, 1.0, -0.5, -0.5, 1.0, -0.5, -0.5, 1.0, -0.5,
                                            },
        {
                                                -0.5, 1.0, -0.5, -0.5, 1.0, -0.5, -0.5, 1.0, -0.5,
                                            },
        {
                                                -0.5, 1.0, -0.5, -0.5, 1.0, -0.5, -0.5, 1.0, -0.5,
                                            },
        {
                                                -0.5, 1.0, -0.5, -0.5, 1.0, -0.5, -0.5, 1.0, -0.5,
                                            },
        {
                                                -0.5, 1.0, -0.5, -0.5, 1.0, -0.5, -0.5, 1.0, -0.5,
                                            },
        };

        private static double[,] grid4 =
        {
        {
            -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375,
        },
        {
                                                -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375,
                                            },
        {
                                                -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375,
                                            },
        {
                                                -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375,
                                            },
        {
                                                -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375,
                                            },
        {
                                                -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375,
                                            },
        {
                                                -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375, -0.375, -0.375, 1.0, -0.375,
                                            },
        };

        private static double[,] grid2Wave =
        {
        {
            -0.5, -0.5, -0.5,
        },
        {
                                                1.0,  1.0,  1.0,
                                            },
        {
                                                -1.5, -1.5, -1.5,
                                            },
        {
                                                2.0,  2.0,  2.0,
                                            },
        {
                                                -1.5, -1.5, -1.5,
                                            },
        {
                                                1.0,  1.0,  1.0,
                                            },
        {
                                                -0.5, -0.5, -0.5,
                                            },
        };

        private static double[,] grid3Wave =
        {
        {
            -0.5, -0.5, -0.5,
        },
        {
                                                1.0,  1.0,  1.0,
                                            },
        {
                                                -0.5, -0.5, -0.5,
                                            },
        {
                                                -1.0, -1.0, -1.0,
                                            },
        {
                                                2.0,  2.0,  2.0,
                                            },
        {
                                                -1.0, -1.0, -1.0,
                                            },
        {
                                                -0.5, -0.5, -0.5,
                                            },
        {
                                                1.0,  1.0,  1.0,
                                            },
        {
                                                -0.5, -0.5, -0.5,
                                            },
        };

        public static double[,] SobelX =
        {
        {
            -1.0,  0.0,  1.0,
        },
        {
                                                -2.0,  0.0,  -2.0,
                                            },
        {
                                                -1.0,  0.0,  1.0,
                                            },
        };

        public static double[,] SobelY =
        {
        {
            1.0,  2.0,  1.0,
        },
        {
                                                0.0,  0.0,  0.0,
                                            },
        {
                                                -1.0, -2.0, -1.0,
                                            },
        };

        public static Bitmap ReadImage2Bitmap(string fileName)
        {
            if (!File.Exists(fileName))
            {
                return null;
            }

            return (Bitmap)Image.FromFile(fileName);
        }

        public static void WriteBitmap2File(Bitmap binaryBmp, string opPath)
        {
            binaryBmp.Save(opPath);
        }

        public static Bitmap ApplyInvert(Bitmap bitmapImage)
        {
            byte A, R, G, B;
            Color pixelColor;
            Bitmap returnImage = new Bitmap(bitmapImage);

            for (int y = 0; y < bitmapImage.Height; y++)
            {
                for (int x = 0; x < bitmapImage.Width; x++)
                {
                    pixelColor = bitmapImage.GetPixel(x, y);

                    //A = (byte)(255 - pixelColor.A);
                    A = pixelColor.A;
                    R = (byte)(255 - pixelColor.R);
                    G = (byte)(255 - pixelColor.G);
                    B = (byte)(255 - pixelColor.B);
                    returnImage.SetPixel(x, y, Color.FromArgb(A, R, G, B));
                }
            }

            return returnImage;
        }

        /// <summary>
        /// reads the intensity of a grey scale image into a matrix of double.
        /// Assumes gray scale is 0-255 and that color.R = color.G = color.B.
        /// </summary>
        public static double[,] GreyScaleImage2Matrix(Bitmap bitmap)
        {
            int height = bitmap.Height; //height
            int width = bitmap.Width;   //width

            var matrix = new double[height, width];
            for (int r = 0; r < height; r++)
            {
                for (int c = 0; c < width; c++)
                {
                    Color color = bitmap.GetPixel(c, r);

                    //double value = (255 - color.R) / (double)255;
                    //if (value > 0.0) LoggedConsole.WriteLine(value);
                    matrix[r, c] = (255 - color.R) / (double)255;
                }
            }

            return matrix;
        }

        public static Color GetPixel(Point position)
        {
            using (var bitmap = new Bitmap(1, 1))
            {
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(position, new Point(0, 0), new Size(1, 1));
                }

                return bitmap.GetPixel(0, 0);
            }
        } //GetPixel(Point position)

        public static double[,] Convolve(double[,] matrix, Kernal name)
        {
            double[,] kernal;

            //SWITCH KERNALS
            switch (name)
            {
                case Kernal.LowPass: kernal = lowPassKernal;
                    break;
                case Kernal.HighPass1: kernal = highPassKernal1;
                    break;
                case Kernal.HighPass2: kernal = highPassKernal2;
                    LoggedConsole.WriteLine("Applied highPassKernal2 Kernal");
                    break;
                case Kernal.HorizontalLine3: kernal = horiLineKernal3;
                    break;
                case Kernal.HorizontalLine5: kernal = horiLineKernal5;
                    LoggedConsole.WriteLine("Applied Horizontal Line5 Kernal");
                    break;
                case Kernal.VerticalLine: kernal = vertLineKernal;
                    break;
                case Kernal.DiagLine1: kernal = diagLineKernal1;
                    LoggedConsole.WriteLine("Applied diagLine1 Kernal");
                    break;
                case Kernal.DiagLine2: kernal = diagLineKernal2;
                    LoggedConsole.WriteLine("Applied diagLine2 Kernal");
                    break;
                case Kernal.Laplace1: kernal = Laplace1Kernal;
                    LoggedConsole.WriteLine("Applied Laplace1 Kernal");
                    break;
                case Kernal.Laplace2: kernal = Laplace2Kernal;
                    LoggedConsole.WriteLine("Applied Laplace2 Kernal");
                    break;
                case Kernal.Laplace3: kernal = Laplace3Kernal;
                    LoggedConsole.WriteLine("Applied Laplace3 Kernal");
                    break;
                case Kernal.Laplace4: kernal = Laplace4Kernal;
                    LoggedConsole.WriteLine("Applied Laplace4 Kernal");
                    break;

                default:
                    throw new Exception("\nWARNING: INVALID MODE!");
            }//end of switch statement

            int mRows = matrix.GetLength(0);
            int mCols = matrix.GetLength(1);
            int kRows = kernal.GetLength(0);
            int kCols = kernal.GetLength(1);
            int rNH = kRows / 2;
            int cNH = kCols / 2;

            if (rNH <= 0 && cNH <= 0)
            {
                return matrix; //no operation required
            }

            //int area = ((2 * cNH) + 1) * ((2 * rNH) + 1);//area of rectangular neighbourhood

            //double[,] newMatrix = (double[,])matrix.Clone();
            double[,] newMatrix = new double[mRows, mCols]; //init new matrix to return

            // fix up the edges first
            for (int r = 0; r < mRows; r++)
            {
                for (int c = 0; c < cNH; c++)
                {
                    newMatrix[r, c] = matrix[r, c];
                }

                for (int c = mCols - cNH; c < mCols; c++)
                {
                    newMatrix[r, c] = matrix[r, c];
                }
            }

            // fix up other edges
            for (int c = 0; c < mCols; c++)
            {
                for (int r = 0; r < rNH; r++)
                {
                    newMatrix[r, c] = matrix[r, c];
                }

                for (int r = mRows - rNH; r < mRows; r++)
                {
                    newMatrix[r, c] = matrix[r, c];
                }
            }

            //now do bulk of image
            for (int r = rNH; r < mRows - rNH; r++)
            {
                for (int c = cNH; c < mCols - cNH; c++)
                {
                    double sum = 0.0;
                    for (int y = -rNH; y < rNH; y++)
                    {
                        for (int x = -cNH; x < cNH; x++)
                        {
                            sum += matrix[r + y, c + x] * kernal[rNH - y, cNH - x];
                        }
                    }

                    newMatrix[r, c] = sum; // / (double)area;
                }
            }

            return newMatrix;
        }//end method Convolve()

        public static double[,] GridFilter(double[,] m, Kernal name)
        {
            double[,] kernal;
            int noiseSampleCount = 500000;

            //double thresholdZScore = 3.1;  //zscore threshold for p=0.001
            //double thresholdZScore = 2.58; //zscore threshold for p=0.005
            //double thresholdZScore = 2.33; //zscore threshold for p=0.01
            double thresholdZScore = 1.98;   //zscore threshold for p=0.05

            //SWITCH KERNALS
            switch (name)
            {
                case Kernal.Grid2: kernal = grid2;
                    LoggedConsole.WriteLine("Applied Grid Kernal 2");
                    break;
                case Kernal.Grid3: kernal = grid3;
                    LoggedConsole.WriteLine("Applied Grid Kernal 2");
                    break;
                case Kernal.Grid4: kernal = grid4;
                    LoggedConsole.WriteLine("Applied Grid Kernal 2");
                    break;
                case Kernal.Grid2Wave: kernal = grid2Wave;
                    LoggedConsole.WriteLine("Applied Grid Wave Kernal 2");
                    break;
                case Kernal.Grid3Wave: kernal = grid3Wave;
                    LoggedConsole.WriteLine("Applied Grid Wave Kernal 3");
                    break;

                default:
                    throw new Exception("\nWARNING: INVALID MODE!");
            }//end of switch statement

            int mRows = m.GetLength(0);
            int mCols = m.GetLength(1);
            int kRows = kernal.GetLength(0);
            int kCols = kernal.GetLength(1);
            int rNH = kRows / 2;
            int cNH = kCols / 2;
            if (rNH <= 0 && cNH <= 0)
            {
                return m; //no operation required
            }

            //int area = ((2 * cNH) + 1) * ((2 * rNH) + 1);//area of rectangular neighbourhood

            double[,] normM = DataTools.normalise(m);

            double[] noiseScores = new double[noiseSampleCount];
            for (int n = 0; n < noiseSampleCount; n++)
            {
                double[,] noise = GetNoise(normM, kRows, kCols);
                double sum = 0.0;
                for (int i = 0; i < kRows; i++)
                {
                    for (int j = 0; j < kCols; j++)
                    {
                        sum += noise[i, j] * kernal[i, j];
                    }
                }

                noiseScores[n] = sum / kRows;
            }

            double noiseAv;
            double noiseSd;
            NormalDist.AverageAndSD(noiseScores, out noiseAv, out noiseSd);
            LoggedConsole.WriteLine("noiseAv=" + noiseAv + "   noiseSd=" + noiseSd);

            double[,] newMatrix = new double[mRows, mCols]; //init new matrix to return

            //now do bulk of image
            for (int r = rNH; r < mRows - rNH; r++)
            {
                for (int c = cNH; c < mCols - cNH; c++)
                {
                    double sum = 0.0;
                    for (int y = -rNH; y < rNH; y++)
                    {
                        for (int x = -cNH; x < cNH; x++)
                        {
                            sum += normM[r + y, c + x] * kernal[rNH + y, cNH + x];
                        }
                    }

                    sum /= kRows;
                    double zScore = (sum - noiseAv) / noiseSd;

                    if (zScore >= thresholdZScore)
                    {
                        newMatrix[r, c] = 1.0;
                        for (int n = -rNH; n < rNH; n++)
                        {
                            newMatrix[r + n, c] = 1.0;
                        }

                        //newMatrix[r, c] = zScore;
                        //newMatrix[r + 1, c] = zScore;
                    }

                    //else newMatrix[r, c] = 0.0;
                }//end of loops
            }

            return newMatrix;
        }//end method GridFilter()

        /// <summary>
        /// Returns a small matrix of pixels chosen randomly from the passed matrix, m.
        /// The row and column is chosen randomly and then the reuired number of consecutive pixels is transferred.
        /// These noise matrices are used to obtain statistics for cross-correlation coefficients.
        /// </summary>
        /// <param name="m"></param>
        /// <param name="kRows"></param>
        /// <param name="kCols"></param>
        /// <returns></returns>
        public static double[,] GetNoise(double[,] m, int kRows, int kCols)
        {
            int mHeight = m.GetLength(0);
            int mWidth = m.GetLength(1);

            double[,] noise = new double[kRows, kCols];
            RandomNumber rn = new RandomNumber();
            for (int r = 0; r < kRows; r++)
            {
                int randomRow = rn.GetInt(mHeight - kRows);
                int randomCol = rn.GetInt(mWidth - kCols);
                for (int c = 0; c < kCols; c++)
                {
                    noise[r, c] = m[randomRow, randomCol + c];
                }
            }

            return noise;
        } //end getNoise()

        public static double[,] WienerFilter(double[,] matrix)
        {
            int NH = 3;
            return WienerFilter(matrix, NH);
        }

        public static double[,] WienerFilter(double[,] matrix, int NH)
        {
            int M = NH;
            int N = NH;
            int rNH = M / 2;
            int cNH = N / 2;

            //double totMean = 0.0;
            //double totSD = 0.0;
            //NormalDist.AverageAndSD(matrix, out totMean, out totSD);
            //double colVar = totSD * totSD;

            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            double[,] outM = new double[rows, cols];
            for (int c = 0; c < cols; c++)
            {
                double[] column = DataTools.GetColumn(matrix, c);
                double colMean = 0.0;
                double colSD = 0.0;
                NormalDist.AverageAndSD(column, out colMean, out colSD);
                double colVar = colSD * colSD;

                for (int r = 0; r < rows; r++)
                {
                    double X = 0.0;
                    double Xe2 = 0.0;
                    int count = 0;
                    for (int i = r - rNH; i <= r + rNH; i++)
                    {
                        if (i < 0)
                        {
                            continue;
                        }

                        if (i >= rows)
                        {
                            continue;
                        }

                        for (int j = c - cNH; j <= c + cNH; j++)
                        {
                            if (j < 0)
                            {
                                continue;
                            }

                            if (j >= cols)
                            {
                                continue;
                            }

                            X += matrix[i, j];
                            Xe2 += matrix[i, j] * matrix[i, j];
                            count++;

                            //LoggedConsole.WriteLine(i+"  "+j+"   count="+count);
                            //Console.ReadLine();
                        }
                    }

                    //LoggedConsole.WriteLine("End NH count="+count);
                    //calculate variance of the neighbourhood
                    double mean = X / count;
                    double variance = (Xe2 / count) - (mean * mean);
                    double numerator = variance - colVar;
                    if (numerator < 0.0)
                    {
                        numerator = 0.0;
                    }

                    double denominator = variance;
                    if (colVar > denominator)
                    {
                        denominator = colVar;
                    }

                    double ratio = numerator / denominator;
                    outM[r, c] = mean + (ratio * (matrix[r, c] - mean));

                    // LoggedConsole.WriteLine((outM[r, c]).ToString("F1") + "   " + (matrix[r, c]).ToString("F1"));
                    // Console.ReadLine();
                }
            }

            return outM;
        }

        /// <summary>
        /// this method assumes that all the values in the passed matrix are between zero & one.
        /// Will truncate all values > 1 to 1.0.
        /// Spurious results will occur if have negative values or values > 1.
        /// Should NormaliseMatrixValues matrix first if these conditions do not apply.
        /// </summary>
        /// <param name="M"></param>
        /// <param name="fractionalStretching"></param>
        /// <returns></returns>
        public static double[,] ContrastStretching(double[,] M, double fractionalStretching)
        {
            int rowCount = M.GetLength(0);
            int colCount = M.GetLength(1);
            double[,] norm = MatrixTools.NormaliseMatrixValues(M);

            int binCount = 100;
            double binWidth = 0.01;
            double min = 0.0;
            double max = 1.0;
            int[] histo = Histogram.Histo(M, binCount, min, max, binWidth);

            int cellCount = rowCount * colCount;
            int thresholdCount = (int)(cellCount * fractionalStretching);

            // get low side stretching bound
            int bottomSideCount = 0;
            for (int i = 0; i < binCount; i++)
            {
                bottomSideCount += histo[i];
                if (bottomSideCount > thresholdCount)
                {
                    min = i * binWidth;
                    break;
                }
            }

            // get high side stretching bound
            int topSideCount = 0;
            for (int i = binCount - 1; i >= 0; i--)
            {
                topSideCount += histo[i];
                if (topSideCount > thresholdCount)
                {
                    max = 1 - ((binCount - i) * binWidth);
                    break;
                }
            }

            // truncate min and max and thereby contrast stretch.
            norm = MatrixTools.NormaliseInZeroOne(norm, min, max);
            return norm;
        }

        /// <summary>
        /// This method is a TEST method for Canny edge detection - see below.
        /// </summary>
        public static void TestCannyEdgeDetection()
        {
            //string path = @"C:\SensorNetworks\Output\Human\DM420036_min465Speech_0min.png";
            //string path = @"C:\SensorNetworks\Output\Sonograms\TestForHoughTransform.png";
            //string path = @"C:\SensorNetworks\Output\LewinsRail\BAC1_20071008-081607_0min.png";
            string path = @"C:\SensorNetworks\Output\LewinsRail\BAC2_20071008-085040_0min.png";
            FileInfo file = new FileInfo(path);
            Bitmap sourceImage = ReadImage2Bitmap(file.FullName);
            ApplyInvert(sourceImage);
            byte lowThreshold = 0;
            byte highThreshold = 30;
            Bitmap bmp2 = CannyEdgeDetection(sourceImage, lowThreshold, highThreshold);
            string path1 = @"C:\SensorNetworks\Output\LewinsRail\Canny.png";
            bmp2.Save(path1, ImageFormat.Png);
        }

        /// <summary>
        /// The below method is derived from the following site
        /// http://premsivakumar.wordpress.com/2010/12/13/edge-detection-using-c-and-aforge-net/
        /// The author references the following Afroge source code
        /// http://www.aforgenet.com/framework/features/edge_detectors_filters.html
        /// See the below link for how to set the thresholds etc
        /// http://homepages.inf.ed.ac.uk/rbf/HIPR2/canny.htm
        ///
        /// </summary>
        /// <param name="bmp"></param>
        /// <returns></returns>
        public static Bitmap CannyEdgeDetection(Bitmap bmp, byte lowThreshold, byte highThreshold)
        {
            //Bitmap image = (Bitmap)bmp.Clone();
            //convert image to gray scale
            //Bitmap gsImage = Grayscale.CommonAlgorithms.BT709.Apply(image);
            Bitmap gsImage = (Bitmap)bmp.Clone();

            //this filter converts standard pixel format to indexed as used by the hough transform
            var filter1 = Grayscale.CommonAlgorithms.BT709;
            gsImage = filter1.Apply(gsImage);

            var filter2 = new GaussianBlur();
            filter2.Size = 3;
            filter2.Sigma = 2;

            //filter2.Threshold = 1;
            filter2.Apply(gsImage);

            CannyEdgeDetector cannyFilter = new CannyEdgeDetector();
            cannyFilter.LowThreshold = lowThreshold;
            cannyFilter.HighThreshold = highThreshold;
            Bitmap edge = cannyFilter.Apply(gsImage);
            return edge;
        }

        public static double[,] SobelEdgeDetection(double[,] m)
        {
            double relThreshold = 0.2;
            return SobelEdgeDetection(m, relThreshold);
        }

        /// <summary>
        /// This version of Sobel's edge detection taken from  Graig A. Lindley, Practical Image Processing
        /// which includes C code.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static double[,] SobelEdgeDetection(double[,] m, double relThreshold)
        {
            //define indices into grid using Lindley notation
            const int a = 0;
            const int b = 1;
            const int c = 2;
            const int d = 3;
            const int e = 4;
            const int f = 5;
            const int g = 6;
            const int h = 7;
            const int i = 8;
            int mRows = m.GetLength(0);
            int mCols = m.GetLength(1);
            double[,] normM = DataTools.normalise(m);
            double[,] newMatrix = new double[mRows, mCols]; //init new matrix to return
            double[] grid = new double[9]; //to represent 3x3 grid
            double min = double.MaxValue;
            double max = -double.MaxValue;

            for (int y = 1; y < mRows - 1; y++)
            {
                for (int x = 1; x < mCols - 1; x++)
                {
                    grid[a] = normM[y - 1, x - 1];
                    grid[b] = normM[y, x - 1];
                    grid[c] = normM[y + 1, x - 1];
                    grid[d] = normM[y - 1, x];
                    grid[e] = normM[y, x];
                    grid[f] = normM[y + 1, x];
                    grid[g] = normM[y - 1, x + 1];
                    grid[h] = normM[y, x + 1];
                    grid[i] = normM[y + 1, x + 1];
                    double[] differences = new double[4];
                    double DivideAEI_avBelow = (grid[d] + grid[g] + grid[h]) / 3;
                    double DivideAEI_avAbove = (grid[b] + grid[c] + grid[f]) / 3;
                    differences[0] = Math.Abs(DivideAEI_avAbove - DivideAEI_avBelow);

                    double DivideBEH_avBelow = (grid[a] + grid[d] + grid[g]) / 3;
                    double DivideBEH_avAbove = (grid[c] + grid[f] + grid[i]) / 3;
                    differences[1] = Math.Abs(DivideBEH_avAbove - DivideBEH_avBelow);

                    double DivideCEG_avBelow = (grid[f] + grid[h] + grid[i]) / 3;
                    double DivideCEG_avAbove = (grid[a] + grid[b] + grid[d]) / 3;
                    differences[2] = Math.Abs(DivideCEG_avAbove - DivideCEG_avBelow);

                    double DivideDEF_avBelow = (grid[g] + grid[h] + grid[i]) / 3;
                    double DivideDEF_avAbove = (grid[a] + grid[b] + grid[c]) / 3;
                    differences[3] = Math.Abs(DivideDEF_avAbove - DivideDEF_avBelow);
                    double gridMin;
                    double gridMax;
                    DataTools.MinMax(differences, out gridMin, out gridMax);

                    newMatrix[y, x] = gridMax;
                    if (min > gridMin)
                    {
                        min = gridMin;
                    }

                    if (max < gridMax)
                    {
                        max = gridMax;
                    }
                }
            }

            //double relThreshold = 0.2;
            double threshold = min + ((max - min) * relThreshold);

            for (int y = 1; y < mRows - 1; y++)
            {
                for (int x = 1; x < mCols - 1; x++)
                {
                    if (newMatrix[y, x] > threshold)
                    {
                        newMatrix[y, x] = 1.0;
                    }
                    else
                    {
                        newMatrix[y, x] = 0.0;
                    }
                }
            }

            return newMatrix;
        }

        /// <summary>
        /// This version of Sobel's edge detection taken from  Graig A. Lindley, Practical Image Processing
        /// which includes C code.
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static double[,] SobelRidgeDetection(double[,] m)
        {
            //define indices into grid using Lindley notation
            const int a = 0;
            const int b = 1;
            const int c = 2;
            const int d = 3;
            const int e = 4;
            const int f = 5;
            const int g = 6;
            const int h = 7;
            const int i = 8;
            int mRows = m.GetLength(0);
            int mCols = m.GetLength(1);
            double[,] normM = DataTools.normalise(m);
            double[,] newMatrix = new double[mRows, mCols]; //init new matrix to return
            double[] grid = new double[9]; //to represent 3x3 grid
            double min = double.MaxValue;
            double max = -double.MaxValue;

            for (int y = 1; y < mRows - 1; y++)
            {
                for (int x = 1; x < mCols - 1; x++)
                {
                    grid[a] = normM[y - 1, x - 1];
                    grid[b] = normM[y, x - 1];
                    grid[c] = normM[y + 1, x - 1];
                    grid[d] = normM[y - 1, x];
                    grid[e] = normM[y, x];
                    grid[f] = normM[y + 1, x];
                    grid[g] = normM[y - 1, x + 1];
                    grid[h] = normM[y, x + 1];
                    grid[i] = normM[y + 1, x + 1];
                    double[] differences = new double[4];
                    double DivideAEI_avBelow = (grid[d] + grid[g] + grid[h]) / 3;
                    double DivideAEI_avAbove = (grid[b] + grid[c] + grid[f]) / 3;

                    //differences[0] = Math.Abs(DivideAEI_avAbove - DivideAEI_avBelow);
                    differences[0] = grid[e] - DivideAEI_avAbove + (grid[e] - DivideAEI_avBelow);
                    if (differences[0] < 0.0)
                    {
                        differences[0] = 0.0;
                    }

                    double DivideBEH_avBelow = (grid[a] + grid[d] + grid[g]) / 3;
                    double DivideBEH_avAbove = (grid[c] + grid[f] + grid[i]) / 3;

                    //differences[1] = Math.Abs(DivideBEH_avAbove - DivideBEH_avBelow);
                    differences[1] = grid[e] - DivideBEH_avBelow + (grid[e] - DivideBEH_avAbove);
                    if (differences[1] < 0.0)
                    {
                        differences[1] = 0.0;
                    }

                    double DivideCEG_avBelow = (grid[f] + grid[h] + grid[i]) / 3;
                    double DivideCEG_avAbove = (grid[a] + grid[b] + grid[d]) / 3;

                    //differences[2] = Math.Abs(DivideCEG_avAbove - DivideCEG_avBelow);
                    differences[2] = grid[e] - DivideCEG_avBelow + (grid[e] - DivideCEG_avAbove);
                    if (differences[2] < 0.0)
                    {
                        differences[2] = 0.0;
                    }

                    double DivideDEF_avBelow = (grid[g] + grid[h] + grid[i]) / 3;
                    double DivideDEF_avAbove = (grid[a] + grid[b] + grid[c]) / 3;

                    //differences[3] = Math.Abs(DivideDEF_avAbove - DivideDEF_avBelow);
                    differences[3] = grid[e] - DivideDEF_avBelow + (grid[e] - DivideDEF_avAbove);
                    if (differences[3] < 0.0)
                    {
                        differences[3] = 0.0;
                    }

                    double diffMin;
                    double diffMax;
                    DataTools.MinMax(differences, out diffMin, out diffMax);

                    newMatrix[y, x] = diffMax;
                    if (min > diffMin)
                    {
                        min = diffMin; //store minimum difference value of entire matrix
                    }

                    if (max < diffMax)
                    {
                        max = diffMax; //store maximum difference value of entire matrix
                    }
                }
            }

            double threshold = min + ((max - min) / 4); //threshold is 1/5th of range above min

            for (int y = 1; y < mRows - 1; y++)
            {
                for (int x = 1; x < mCols - 1; x++)
                {
                    if (newMatrix[y, x] > threshold)
                    {
                        newMatrix[y, x] = 1.0;
                    }
                    else
                    {
                        newMatrix[y, x] = 0.0;
                    }
                }
            }

            return newMatrix;
        }

        /// <summary>
        /// This version of Sobel's edge detection taken from  Graig A. Lindley, Practical Image Processing
        /// which includes C code.
        /// HOWEVER MODIFED TO PROCESS 5x5 matrix
        /// MATRIX must be square with odd number dimensions
        /// </summary>
        /// <param name="m"></param>
        /// <returns></returns>
        public static void SobelRidgeDetection(double[,] m, out bool isRidge, out double magnitude, out double direction)
        {
            //for clarity, give matrix elements LETTERS using Lindley notation
            //ABCDE  00 01 02 03 04
            //FGHIJ  10 11 12 13 14
            //KLMNO  20 21 22 23 24
            //PQRST  30 31 32 33 34
            //UVWXY  40 41 42 43 44
            // We have eight possible ridges with slopes 0, Pi/8, Pi/4, 3Pi/8, pi/2, 5Pi/8, 3Pi/4, 7Pi/8
            // Slope categories are 0 to 7.
            // We calculate the ridge magnitude for each possible ridge direction.

            int rows = m.GetLength(0);
            int cols = m.GetLength(1);
            if (rows != cols) // must be square matrix
            {
                isRidge = false;
                magnitude = 0.0;
                direction = 0.0;
                return;
            }

            int centreID = rows / 2;
            int cm1 = centreID - 1;
            int cp1 = centreID + 1;
            int cm2 = centreID - 2;
            int cp2 = centreID + 2;
            double[,] ridgeMagnitudes = new double[8, 3];

            //ridge magnitude having slope=0;
            double[] rowSums = MatrixTools.SumRows(m);
            ridgeMagnitudes[0, 1] = rowSums[centreID];
            for (int r = 0; r < centreID; r++)
            {
                ridgeMagnitudes[0, 0] += rowSums[r]; //positve  side magnitude
            }

            for (int r = centreID + 1; r < rows; r++)
            {
                ridgeMagnitudes[0, 2] += rowSums[r]; //negative side magnitude
            }

            ridgeMagnitudes[0, 0] /= centreID * cols;
            ridgeMagnitudes[0, 1] /= cols;
            ridgeMagnitudes[0, 2] /= centreID * cols;

            //ridge magnitude having slope=Pi/8;
            ridgeMagnitudes[1, 1] = (m[cm1, cp2] + m[centreID, cp1] + m[centreID, centreID] + m[centreID, cm1] + m[cp1, cm2]) / 5;

            //positve side magnitude
            ridgeMagnitudes[1, 0] = (m[cm2, cm2] + m[cm2, cm1] + m[cm2, centreID] + m[cm2, cp1] + m[cm2, cp2] + m[cm1, cm2] + m[cm1, cm1] + m[cm1, centreID] + m[cm1, cp1] + m[centreID, cm2]) / 10;

            //negative side magnitude
            ridgeMagnitudes[1, 2] = (m[cp2, cm2] + m[cp2, cm1] + m[cp2, centreID] + m[cp2, cp1] + m[cp2, cp2] + m[cp1, cm1] + m[cp1, centreID] + m[cp1, cp1] + m[cp1, cp2] + m[centreID, cp2]) / 10;

            //ridge magnitude having slope=2Pi/8;
            ridgeMagnitudes[2, 1] = MatrixTools.SumPositiveDiagonal(m) / cols;
            double upperAv, lowerAv;
            MatrixTools.AverageValuesInTriangleAboveAndBelowPositiveDiagonal(m, out upperAv, out lowerAv);
            ridgeMagnitudes[2, 0] = upperAv;
            ridgeMagnitudes[2, 2] = lowerAv;

            //ridge magnitude having slope=3Pi/8;
            ridgeMagnitudes[3, 1] = (m[cm2, cp1] + m[cm1, cp1] + m[centreID, centreID] + m[cp1, cm1] + m[cp2, cm1]) / 5;

            //positve side magnitude
            ridgeMagnitudes[3, 0] = (m[cm2, cm2] + m[cm2, cm1] + m[cm2, centreID] + m[cm1, cm2] + m[cm1, cm1] + m[cm1, centreID] + m[centreID, cm2] + m[centreID, cm1] + m[cp1, cm2] + m[cp2, cm2]) / 10;

            //negative side magnitude
            ridgeMagnitudes[3, 2] = (m[cp2, centreID] + m[cp2, cp1] + m[cp2, cp2] + m[cp1, centreID] + m[cp1, cp1] + m[cp1, cp2] + m[centreID, cp1] + m[centreID, cp2] + m[cm1, cp2] + m[cm2, cp2]) / 10;

            //ridge magnitude having slope=4Pi/8;
            double[] colSums = MatrixTools.SumColumns(m);
            ridgeMagnitudes[4, 1] = colSums[centreID];
            for (int c = 0; c < centreID; c++)
            {
                ridgeMagnitudes[4, 0] += colSums[c]; //positve  side magnitude
            }

            for (int c = centreID + 1; c < rows; c++)
            {
                ridgeMagnitudes[4, 2] += colSums[c]; //negative side magnitude
            }

            ridgeMagnitudes[4, 0] /= centreID * cols;
            ridgeMagnitudes[4, 1] /= cols;
            ridgeMagnitudes[4, 2] /= centreID * cols;

            //ridge magnitude having slope=5Pi/8;
            ridgeMagnitudes[5, 1] = (m[cm2, cm1] + m[cm1, centreID] + m[centreID, centreID] + m[cp1, centreID] + m[cp2, cp1]) / 5;

            //positve side magnitude
            ridgeMagnitudes[5, 0] = (m[cm2, cm2] + m[cm1, cm2] + m[cm1, cm1] + m[centreID, cm2] + m[centreID, cm1] + m[cp1, cm2] + m[cp1, cm1] + m[cp2, cm2] + m[cp2, cm1] + m[cp2, centreID]) / 10; //ABCDE FGHIJ

            //negative side magnitude
            ridgeMagnitudes[5, 2] = (m[cm2, centreID] + m[cm2, cp1] + m[cm2, cp2] + m[cm1, cp1] + m[cm1, cp2] + m[centreID, cp1] + m[centreID, cp2] + m[cp1, cp1] + m[cp1, cp2] + m[cp2, cp2]) / 10; //PQRST UVWXY

            //ridge magnitude having slope=6Pi/8;
            ridgeMagnitudes[6, 1] = MatrixTools.SumNegativeDiagonal(m) / cols;

            //double upperAv, lowerAv;
            MatrixTools.AverageValuesInTriangleAboveAndBelowNegativeDiagonal(m, out upperAv, out lowerAv);
            ridgeMagnitudes[6, 0] = upperAv;
            ridgeMagnitudes[6, 2] = lowerAv;

            //ridge magnitude having slope=7Pi/8;
            ridgeMagnitudes[7, 1] = (m[cm1, cm2] + m[cm1, cm1] + m[centreID, centreID] + m[cp1, cp1] + m[cp1, cp2]) / 5;

            //positve side magnitude
            ridgeMagnitudes[7, 0] = (m[centreID, cm2] + m[centreID, cm1] + m[cp1, cm2] + m[cp1, cm1] + m[cp1, centreID] + m[cp2, cm2] + m[cp2, cm1] + m[cp2, centreID] + m[cp2, cp1] + m[cp2, cp2]) / 10;

            //negative side magnitude
            ridgeMagnitudes[7, 2] = (m[cm2, cm2] + m[cm2, cm1] + m[cm2, centreID] + m[cm2, cp1] + m[cm2, cp2] + m[cm1, centreID] + m[cm1, cp1] + m[cm1, cp2] + m[centreID, cp1] + m[centreID, cp2]) / 10;

            double[] differences = new double[7]; // difference for each direction
            for (int i = 0; i < 7; i++)
            {
                differences[i] = ridgeMagnitudes[i, 1] - ridgeMagnitudes[i, 0] + (ridgeMagnitudes[i, 1] - ridgeMagnitudes[i, 2]);
                differences[i] /= 2; // want average of both differences because easier to select an appropiate decibel threshold for ridge magnitude.
            }

            int indexMin, indexMax;
            double diffMin, diffMax;
            DataTools.MinMax(differences, out indexMin, out indexMax, out diffMin, out diffMax);

            //double threshold = min + (max - min) / 4; //threshold is 1/5th of range above min
            double threshold = 0; // dB
            isRidge = ridgeMagnitudes[indexMax, 1] > ridgeMagnitudes[indexMax, 0] + threshold
                      && ridgeMagnitudes[indexMax, 1] > ridgeMagnitudes[indexMax, 2] + threshold;
            magnitude = diffMax;
            direction = indexMax * Math.PI / 8;
        }

        /// <summary>
        /// This version of Sobel's edge detection taken from  Graig A. Lindley, Practical Image Processing which includes C code.
        /// HOWEVER MODIFED TO PROCESS 5x5 matrix
        /// MATRIX must be square with odd number dimensions
        /// </summary>
        public static void Sobel5X5RidgeDetection(double[,] m, out bool isRidge, out double magnitude, out int direction)
        {
            // We have four possible ridge directions with slopes 0, Pi/4, pi/2, 3Pi/4
            // Slope categories are 0 to 3.
            // We calculate the ridge magnitude for each possible ridge direction using masks.
            // 0 = ridge direction = horizontal or slope = 0;
            // 1 = ridge is positive slope or pi/4
            // 2 = ridge is vertical or pi/2
            // 3 = ridge is negative slope or 3pi/4.

            double[] ridgeMagnitudes = Sobel5X5RidgeDetection(m);

            if (ridgeMagnitudes == null)
            {
                // something has gone wrong
                isRidge = false;
                magnitude = 0.0;
                direction = 0;
                return;
            }

            int indexMin, indexMax;
            double diffMin, diffMax;
            DataTools.MinMax(ridgeMagnitudes, out indexMin, out indexMax, out diffMin, out diffMax);

            double threshold = 0; // dB
            isRidge = ridgeMagnitudes[indexMax] > threshold;
            magnitude = diffMax / 2;
            direction = indexMax;
        }

        public static double[] Sobel5X5RidgeDetection(double[,] m)
        {
            // We have four possible ridges with slopes 0, Pi/4, pi/2, 3Pi/4
            // Slope categories are 0 to 3.
            // We calculate the ridge magnitude for each possible ridge direction using masks.
            // 0 = ridge direction = horizontal or slope = 0;
            // 1 = ridge is positive slope or pi/4
            // 2 = ridge is vertical or pi/2
            // 3 = ridge is negative slope or 3pi/4.

            int rows = m.GetLength(0);
            int cols = m.GetLength(1);

            // must be a square 5X5 matrix
            if (rows != cols || rows != 5)
            {
                return null;
            }

            double[,] ridgeDir0Mask =
            {
                {
                    -0.1, -0.1, -0.1, -0.1, -0.1,
                },
                {
                    -0.1, -0.1, -0.1, -0.1, -0.1,
                },
                {
                    0.4, 0.4, 0.4, 0.4, 0.4,
                },
                {
                    -0.1, -0.1, -0.1, -0.1, -0.1,
                },
                {
                    -0.1, -0.1, -0.1, -0.1, -0.1,
                },
            };
            double[,] ridgeDir1Mask =
            {
                {
                    -0.1, -0.1, -0.1, -0.1, 0.4,
                },
                {
                    -0.1, -0.1, -0.1, 0.4, -0.1,
                },
                {
                    -0.1, -0.1, 0.4, -0.1, -0.1,
                },
                {
                    -0.1, 0.4, -0.1, -0.1, -0.1,
                },
                {
                    0.4, -0.1, -0.1, -0.1, -0.1,
                },
            };
            double[,] ridgeDir2Mask =
            {
                {
                    -0.1, -0.1, 0.4, -0.1, -0.1,
                },
                {
                    -0.1, -0.1, 0.4, -0.1, -0.1,
                },
                {
                    -0.1, -0.1, 0.4, -0.1, -0.1,
                },
                {
                    -0.1, -0.1, 0.4, -0.1, -0.1,
                },
                {
                    -0.1, -0.1, 0.4, -0.1, -0.1,
                },
            };
            double[,] ridgeDir3Mask =
            {
                {
                    0.4, -0.1, -0.1, -0.1, -0.1,
                },
                {
                    -0.1, 0.4, -0.1, -0.1, -0.1,
                },
                {
                    -0.1, -0.1, 0.4, -0.1, -0.1,
                },
                {
                    -0.1, -0.1, -0.1, 0.4, -0.1,
                },
                {
                    -0.1, -0.1, -0.1, -0.1, 0.4,
                },
            };

            double[] ridgeMagnitudes = new double[4];
            ridgeMagnitudes[0] = MatrixTools.DotProduct(ridgeDir0Mask, m);
            ridgeMagnitudes[1] = MatrixTools.DotProduct(ridgeDir1Mask, m);
            ridgeMagnitudes[2] = MatrixTools.DotProduct(ridgeDir2Mask, m);
            ridgeMagnitudes[3] = MatrixTools.DotProduct(ridgeDir3Mask, m);
            return ridgeMagnitudes;
        }

        /// <summary>
        /// This modifies Sobel's ridge detection by using mexican hat filter.
        /// The mexican hat is the difference of two gaussians on different scales.
        /// DoG is used in image processing to find ridges.
        /// MATRIX must be square with odd number dimensions
        /// </summary>
        public static void MexicanHat5X5RidgeDetection(double[,] m, out bool isRidge, out double magnitude, out int direction)
        {
            // We have four possible ridges with slopes 0, Pi/4, pi/2, 3Pi/4
            // Slope categories are 0 to 3.
            // We calculate the ridge magnitude for each possible ridge direction using masks.

            int rows = m.GetLength(0);
            int cols = m.GetLength(1);

            // must be square 5X5 matrix
            if (rows != cols || rows != 5)
            {
                isRidge = false;
                magnitude = 0.0;
                direction = 0;
                return;
            }

            double[,] ridgeDir0Mask =
            {
            {
                -0.2, -0.2, -0.2, -0.2, -0.2,
            },
            {
                                            -0.3, -0.3, -0.3, -0.3, -0.3,
                                        },
            {
                                            1.0, 1.0, 1.0, 1.0, 1.0,
                                        },
            {
                                            -0.3, -0.3, -0.3, -0.3, -0.3,
                                        },
            {
                                            -0.2, -0.2, -0.2, -0.2, -0.2,
                                        },
                                      };
            double[,] ridgeDir1Mask =
            {
            {
                -0.1, -0.2, -0.2, -0.3, 0.8,
            },
            {
                                            -0.2, -0.2, -0.3, 1.0, -0.3,
                                        },
            {
                                            -0.2, -0.3, 1.0, -0.3, -0.2,
                                        },
            {
                                            -0.3, 1.0, -0.3, -0.2, -0.2,
                                        },
            {
                                            0.8, -0.3, -0.2, -0.2, -0.1,
                                        },
                                      };
            double[,] ridgeDir2Mask =
            {
            {
                -0.2, -0.3, 1.0, -0.3, -0.2,
            },
            {
                                            -0.2, -0.3, 1.0, -0.3, -0.2,
                                        },
            {
                                            -0.2, -0.3, 1.0, -0.3, -0.2,
                                        },
            {
                                            -0.2, -0.3, 1.0, -0.3, -0.2,
                                        },
            {
                                            -0.2, -0.3, 1.0, -0.3, -0.2,
                                        },
                                      };
            double[,] ridgeDir3Mask =
            {
            {
                0.8, -0.3, -0.2, -0.2, -0.1,
            },
            {
                                            -0.3, 1.0, -0.3, -0.2, -0.2,
                                        },
            {
                                            -0.2, -0.3, 1.0, -0.3, -0.2,
                                        },
            {
                                            -0.2, -0.2, -0.3, 1.0, -0.3,
                                        },
            {
                                            -0.1, -0.2, -0.2, -0.3, 0.8,
                                        },
                                      };

            double[] ridgeMagnitudes = new double[4];
            ridgeMagnitudes[0] = MatrixTools.DotProduct(ridgeDir0Mask, m);
            ridgeMagnitudes[1] = MatrixTools.DotProduct(ridgeDir1Mask, m);
            ridgeMagnitudes[2] = MatrixTools.DotProduct(ridgeDir2Mask, m);
            ridgeMagnitudes[3] = MatrixTools.DotProduct(ridgeDir3Mask, m);

            int indexMin, indexMax;
            double diffMin, diffMax;
            DataTools.MinMax(ridgeMagnitudes, out indexMin, out indexMax, out diffMin, out diffMax);

            // dB
            double threshold = 0;
            isRidge = ridgeMagnitudes[indexMax] > threshold;
            magnitude = diffMax / 2;
            direction = indexMax;
        }

        public static void Sobel5X5CornerDetection(double[,] m, out bool isCorner, out double magnitude, out double direction)
        {
            // We have eight possible corners in directions 0, Pi/4, pi/2, 3Pi/4
            // Corner categories are 0 to 7.
            // We calculate the ridge magnitude for each possible ridge direction using masks.

            int rows = m.GetLength(0);
            int cols = m.GetLength(1);

            // must be square 5X5 matrix
            if (rows != cols || rows != 5)
            {
                isCorner = false;
                magnitude = 0.0;
                direction = 0.0;
                return;
            }

            double[,] ridgeDir0Mask =
            {
            {
                -0.1, -0.1, 0.4, -0.1, -0.1,
            },
            {
                                            -0.1, -0.1, 0.4, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, 0.4, 0.4, 0.4,
                                        },
            {
                                            -0.1, -0.1, -0.1, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, -0.1, -0.1, -0.1,
                                        },
                                      };
            double[,] ridgeDir1Mask =
            {
            {
                0.4, -0.1, -0.1, -0.1, 0.4,
            },
            {
                                            -0.1, 0.4, -0.1, 0.4, -0.1,
                                        },
            {
                                            -0.1, -0.1, 0.4, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, -0.1, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, -0.1, -0.1, -0.1,
                                        },
                                      };
            double[,] ridgeDir2Mask =
            {
            {
                -0.1, -0.1, 0.4, -0.1, -0.1,
            },
            {
                                            -0.1, -0.1, 0.4, -0.1, -0.1,
                                        },
            {
                                            0.4, 0.4, 0.4, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, -0.1, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, -0.1, -0.1, -0.1,
                                        },
                                      };
            double[,] ridgeDir3Mask =
            {
            {
                0.4, -0.1, -0.1, -0.1, -0.1,
            },
            {
                                            -0.1, 0.4, -0.1, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, 0.4, -0.1, -0.1,
                                        },
            {
                                            -0.1, 0.4, -0.1, -0.1, -0.1,
                                        },
            {
                                            0.4, -0.1, -0.1, -0.1, -0.1,
                                        },
                                      };
            double[,] ridgeDir4Mask =
            {
            {
                -0.1, -0.1, -0.1, -0.1, -0.1,
            },
            {
                                            -0.1, -0.1, -0.1, -0.1, -0.1,
                                        },
            {
                                            0.4, 0.4, 0.4, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, 0.4, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, 0.4, -0.1, -0.1,
                                        },
                                      };
            double[,] ridgeDir5Mask =
            {
            {
                -0.1, -0.1, -0.1, -0.1, -0.1,
            },
            {
                                            -0.1, -0.1, -0.1, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, 0.4, -0.1, -0.1,
                                        },
            {
                                            -0.1, 0.4, -0.1, 0.4, -0.1,
                                        },
            {
                                            0.4, -0.1, -0.1, -0.1, 0.4,
                                        },
                                      };
            double[,] ridgeDir6Mask =
            {
            {
                -0.1, -0.1, -0.1, -0.1, -0.1,
            },
            {
                                            -0.1, -0.1, -0.1, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, 0.4, 0.4, 0.4,
                                        },
            {
                                            -0.1, -0.1, 0.4, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, 0.4, -0.1, -0.1,
                                        },
                                      };
            double[,] ridgeDir7Mask =
            {
            {
                -0.1, -0.1, -0.1, -0.1, 0.4,
            },
            {
                                            -0.1, -0.1, -0.1, 0.4, -0.1,
                                        },
            {
                                            -0.1, -0.1, 0.4, -0.1, -0.1,
                                        },
            {
                                            -0.1, -0.1, -0.1, 0.4, -0.1,
                                        },
            {
                                            -0.1, -0.1, -0.1, -0.1, 0.4,
                                        },
                                      };

            double[] cornerMagnitudes = new double[8];
            cornerMagnitudes[0] = MatrixTools.DotProduct(ridgeDir0Mask, m);
            cornerMagnitudes[1] = MatrixTools.DotProduct(ridgeDir1Mask, m);
            cornerMagnitudes[2] = MatrixTools.DotProduct(ridgeDir2Mask, m);
            cornerMagnitudes[3] = MatrixTools.DotProduct(ridgeDir3Mask, m);
            cornerMagnitudes[4] = MatrixTools.DotProduct(ridgeDir4Mask, m);
            cornerMagnitudes[5] = MatrixTools.DotProduct(ridgeDir5Mask, m);
            cornerMagnitudes[6] = MatrixTools.DotProduct(ridgeDir6Mask, m);
            cornerMagnitudes[7] = MatrixTools.DotProduct(ridgeDir7Mask, m);

            int indexMin, indexMax;
            double diffMin, diffMax;
            DataTools.MinMax(cornerMagnitudes, out indexMin, out indexMax, out diffMin, out diffMax);

            // dB
            double threshold = 0;
            isCorner = cornerMagnitudes[indexMax] > threshold;
            magnitude = diffMax / 2;
            direction = indexMax * Math.PI / 8;
        }

        /// <summary>
        /// Reverses a 256 grey scale image
        /// </summary>
        public static double[,] Reverse256GreyScale(double[,] m)
        {
            const int scaleMax = 256 - 1;
            int mRows = m.GetLength(0);
            int mCols = m.GetLength(1);
            double[,] newMatrix = DataTools.normalise(m);
            for (int i = 0; i < mRows; i++)
            {
                for (int j = 0; j < mCols; j++)
                {
                    newMatrix[i, j] = scaleMax - newMatrix[i, j];
                }
            }

            return newMatrix;
        }

        /// <summary>
        /// blurs an image using a square neighbourhood
        /// </summary>
        /// <param name="matrix">the image ot be blurred</param>
        /// <param name="nh">Note that neighbourhood is distance either side of central pixel.</param>
        public static double[,] Blur(double[,] matrix, int nh)
        {
            if (nh <= 0)
            {
                return matrix; //no blurring required
            }

            int M = matrix.GetLength(0);
            int N = matrix.GetLength(1);

            int cellCount = ((2 * nh) + 1) * ((2 * nh) + 1);

            //double[,] newMatrix = new double[M, N];
            double[,] newMatrix = (double[,])matrix.Clone();

            for (int i = nh; i < M - nh; i++)
            {
                for (int j = nh; j < N - nh; j++)
                {
                    double sum = 0.0;
                    for (int x = i - nh; x < i + nh; x++)
                    {
                        for (int y = j - nh; y < j + nh; y++)
                        {
                            sum += matrix[x, y];
                        }
                    }

                    double v = sum / cellCount;
                    newMatrix[i, j] = v;
                }
            }

            return newMatrix;
        }

        /// <summary>
        /// blurs and image using a rectangular neighbourhood.
        /// Note that in this method neighbourhood dimensions are full side or window.
        /// </summary>
        /// <param name="matrix">image to be blurred</param>
        /// <param name="cWindow">column Window i.e. x-dimension</param>
        /// <param name="rWindow">row Window i.e. y-dimension</param>
        public static double[,] Blur(double[,] matrix, int cWindow, int rWindow)
        {
            if (cWindow <= 1 && rWindow <= 1)
            {
                return matrix; //no blurring required
            }

            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            int cNh = cWindow / 2;
            int rNh = rWindow / 2;

            //LoggedConsole.WriteLine("cNH=" + cNH + ", rNH" + rNH);
            int area = ((2 * cNh) + 1) * ((2 * rNh) + 1); //area of rectangular neighbourhood
            double[,] newMatrix = new double[rows, cols]; //init new matrix to return

            // fix up the edges first
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cNh; c++)
                {
                    newMatrix[r, c] = matrix[r, c];
                }

                for (int c = cols - cNh; c < cols; c++)
                {
                    newMatrix[r, c] = matrix[r, c];
                }
            }

            // fix up other edges
            for (int c = 0; c < cols; c++)
            {
                for (int r = 0; r < rNh; r++)
                {
                    newMatrix[r, c] = matrix[r, c];
                }

                for (int r = rows - rNh; r < rows; r++)
                {
                    newMatrix[r, c] = matrix[r, c];
                }
            }

            for (int r = rNh; r < rows - rNh; r++)
            {
                for (int c = cNh; c < cols - cNh; c++)
                {
                    double sum = 0.0;
                    for (int y = r - rNh; y <= r + rNh; y++)
                    {
                        //System.LoggedConsole.WriteLine(r+", "+c+ "  y="+y);
                        for (int x = c - cNh; x <= c + cNh; x++)
                        {
                            sum += matrix[y, x];
                        }
                    }

                    newMatrix[r, c] = sum / area;
                }
            }

            return newMatrix;
        } //end method Blur()

        // ###################################################################################################################################

        /// <summary>
        /// returns the upper and lower thresholds for the pass upper and lower percentile cuts of matrix M
        /// Used for some of the noise reduciton algorithms
        /// </summary>
        public static void PercentileThresholds(double[,] M, double lowerCut, double upperCut, out double lowerThreshold, out double upperThreshold)
        {
            int binCount = 50;
            int count = M.GetLength(0) * M.GetLength(1);
            double binWidth;
            double min;
            double max;
            int[] powerHisto = Histogram.Histo(M, binCount, out binWidth, out min, out max);
            powerHisto[binCount - 1] = 0; //just in case it is the max ????????????????????????????????????? !!!!!!!!!!!!!!!
            double[] smooth = DataTools.filterMovingAverage(powerHisto, 3);
            int maxindex;
            DataTools.getMaxIndex(smooth, out maxindex);

            //calculate threshold for upper percentile
            int clipCount = (int)(upperCut * count);
            int i = binCount - 1;
            int sum = 0;
            while (sum < clipCount && i > 0)
            {
                sum += powerHisto[i--];
            }

            upperThreshold = min + (i * binWidth);

            //calculate threshold for lower percentile
            clipCount = (int)(lowerCut * count);
            int j = 0;
            sum = 0;
            while (sum < clipCount && j < binCount)
            {
                sum += powerHisto[j++];
            }

            lowerThreshold = min + (j * binWidth);
        }

        public static double[,] TrimPercentiles(double[,] matrix)
        {
            //set up parameters for a set of overlapping bands. All numbers should be powers of 2
            int ncbbc = 8;  //number of columns between band centres
            int bandWidth = 64;
            double lowerPercentile = 0.7;
            double upperPercentile = 0.001;

            int height = matrix.GetLength(0);
            int width = matrix.GetLength(1);
            int halfWidth = bandWidth / 2;
            int bandCount = width / ncbbc;

            double[,] tmpM = new double[height, ncbbc];
            double[,] outM = new double[height, width];
            double[,] thresholdSubatrix = DataTools.Submatrix(matrix, 0, 0, height - 1, bandWidth);
            double lowerThreshold;
            double upperThreshold;
            PercentileThresholds(thresholdSubatrix, lowerPercentile, upperPercentile, out lowerThreshold, out upperThreshold);

            for (int col = 0; col < width; col++)
            {
                var tmpCol = col % ncbbc;
                if (tmpCol == 0 && !(col == 0))
                {
                    //NormaliseMatrixValues existing submatrix and transfer to the output matrix, outM
                    tmpM = DataTools.normalise(tmpM);
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < ncbbc; x++)
                        {
                            int startCol = col - ncbbc;
                            outM[y, startCol + x] = tmpM[y, x];
                        }
                    }

                    //set up a new submatrix for processing
                    tmpM = new double[height, ncbbc];

                    //construct new threshold submatrix to recalculate the current threshold
                    int start = col - halfWidth;   //extend range of submatrix below col for smoother changes
                    if (start < 0)
                    {
                        start = 0;
                    }

                    int stop = col + halfWidth;
                    if (stop >= width)
                    {
                        stop = width - 1;
                    }

                    thresholdSubatrix = DataTools.Submatrix(matrix, 0, start, height - 1, stop);
                    PercentileThresholds(thresholdSubatrix, lowerPercentile, upperPercentile, out lowerThreshold, out upperThreshold);
                }

                for (int y = 0; y < height; y++)
                {
                    tmpM[y, tmpCol] = matrix[y, col];
                    if (tmpM[y, tmpCol] > upperThreshold)
                    {
                        tmpM[y, tmpCol] = upperThreshold;
                    }

                    if (tmpM[y, tmpCol] < lowerThreshold)
                    {
                        tmpM[y, tmpCol] = lowerThreshold;
                    }

                    //outM[y, col] = matrix[y, col] - upperThreshold;
                    //if (outM[y, col] < upperThreshold) outM[y, col] = upperThreshold;

                    //if (matrix[y, col] < upperThreshold) M[y, col] = 0.0;
                    //else M[y, col] = 1.0;
                }
            }//for all cols

            return outM;
        }// end of TrimPercentiles()

        // ###################################################################################################################################

        /// <summary>
        /// Calculates the local signal to noise ratio in the neighbourhood of side=window
        /// SNR is defined as local mean / local std dev.
        /// Must check that the local std dev is not too small.
        /// </summary>
        public static double[,] Signal2NoiseRatio_Local(double[,] matrix, int window)
        {
            int nh = window / 2;
            int M = matrix.GetLength(0);
            int N = matrix.GetLength(1);

            int cellCount = ((2 * nh) + 1) * ((2 * nh) + 1);
            double[,] newMatrix = new double[M, N];

            for (int i = nh; i < M - nh; i++)
            {
                for (int j = nh; j < N - nh; j++)
                {
                    int id = 0;
                    double[] values = new double[cellCount];
                    for (int x = i - nh + 1; x < i + nh; x++)
                    {
                        for (int y = j - nh + 1; y < j + nh; y++)
                        {
                            values[id++] = matrix[x, y];
                        }
                    }

                    double av;
                    double sd;
                    NormalDist.AverageAndSD(values, out av, out sd);
                    if (sd < 0.0001)
                    {
                        sd = 0.0001;
                    }

                    newMatrix[i, j] = (matrix[i, j] - av) / sd;
                }
            }

            return newMatrix;
        }

        public static double[,] Signal2NoiseRatio_BandWise(double[,] matrix)
        {
            int bandWidth = 64;
            int halfWidth = bandWidth / 2;
            int height = matrix.GetLength(0);
            int width = matrix.GetLength(1);

            double[,] M = new double[height, width];
            double[,] subMatrix = DataTools.Submatrix(matrix, 0, 0, height - 1, bandWidth);

            for (int col = 0; col < width; col++)
            {
                int start = col - halfWidth; //extend range of submatrix below col for smoother changes
                if (start < 0)
                {
                    start = 0;
                }

                int stop = col + halfWidth;
                if (stop >= width)
                {
                    stop = width - 1;
                }

                if (col % 8 == 0 && !(col == 0))
                {
                    subMatrix = DataTools.Submatrix(matrix, 0, start, height - 1, stop);
                }

                double av;
                double sd;
                NormalDist.AverageAndSD(subMatrix, out av, out sd);
                if (sd < 0.0001)
                {
                    sd = 0.0001;  //to prevent division by zero
                }

                for (int y = 0; y < height; y++)
                {
                    M[y, col] = (matrix[y, col] - av) / sd;
                }
            }//for all cols

            return M;
        }// end of SubtractAverage()

        public static double[,] SubtractAverage_BandWise(double[,] matrix)
        {
            int bandWidth = 64;
            int halfWidth = bandWidth / 2;
            int height = matrix.GetLength(0);
            int width = matrix.GetLength(1);

            double[,] M = new double[height, width];
            double[,] subMatrix = DataTools.Submatrix(matrix, 0, 0, height - 1, bandWidth);

            for (int col = 0; col < width; col++)
            {
                int start = col - halfWidth;   //extend range of submatrix below col for smoother changes
                if (start < 0)
                {
                    start = 0;
                }

                int stop = col + halfWidth;
                if (stop >= width)
                {
                    stop = width - 1;
                }

                if (col % 8 == 0 && !(col == 0))
                {
                    subMatrix = DataTools.Submatrix(matrix, 0, start, height - 1, stop);
                }

                double av;
                double sd;
                NormalDist.AverageAndSD(subMatrix, out av, out sd);

                //LoggedConsole.WriteLine(0 + "," + start + "," + (height - 1) + "," + stop + "   Threshold " + b + "=" + threshold);

                for (int y = 0; y < height; y++)
                {
                    M[y, col] = matrix[y, col] - av;
                }//for all rows
            }//for all cols

            return M;
        }// end of SubtractAverage()

        /// <summary>
        /// Returns matrix after convolving with Gaussian blur.
        /// The blurring is in 2D, first blurred in x-direction, then in y-direction.
        /// Blurring function is {0.006,0.061, 0.242,0.383,0.242,0.061,0.006}
        /// </summary>
        public static double[,] GaussianBlur_5cell(double[,] matrix)
        {
            double[] bf = { 0.006, 0.061, 0.242, 0.382, 0.242, 0.061, 0.006 }; //blurring function
            int edge = 4;
            int backtrack = edge - 1;

            int height = matrix.GetLength(0);
            int width = matrix.GetLength(1);

            // first convolve in x-dimension, i.e. along a row
            double[,] M1 = (double[,])matrix.Clone();
            for (int r = edge; r < height - edge; r++)
            {
                for (int c = edge; c < width - edge; c++)
                {
                    double sum = 0.0;
                    for (int i = 0; i < bf.Length; i++)
                    {
                        sum += bf[i] * matrix[r, c - backtrack + i];
                    }

                    M1[r, c] = sum;
                }//for all cols
            }//for all rows

            // then convolve in y-dimension, i.e. along a col
            double[,] M2 = (double[,])M1.Clone();
            for (int r = edge; r < height - edge; r++) //for all rows
            {
                for (int c = edge; c < width - edge; c++) //for all cols
                {
                    double sum = 0.0;
                    for (int i = 0; i < bf.Length; i++)
                    {
                        sum += bf[i] * M1[r - backtrack + i, c];
                    }

                    M2[r, c] = sum;
                }//for all cols
            }//for all rows

            return M2;
        }// end of Shapes_lines()

        /// <summary>
        /// Detect high intensity / high energy regions in an image using blurring
        /// followed by rules involving positive and negative gradients.
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static double[,] DetectHighEnergyRegions1(double[,] matrix)
        {
            double gradThreshold = 1.2;
            int fWindow = 9;
            int tWindow = 9;
            int bandCount = 16;  // 16 bands, width=512pixels, 32pixels/band
            double lowerShoulder = 0.5;   //used to increase or decrease the threshold from modal value
            double upperShoulder = 0.05;

            double[,] blurM = Blur(matrix, fWindow, tWindow);

            int height = blurM.GetLength(0);
            int width = blurM.GetLength(1);
            double bandWidth = width / (double)bandCount;

            double[,] M = new double[height, width];

            for (int x = 0; x < width; x++)
            {
                M[0, x] = 0.0; //patch in first  time step with zero gradient
            }

            for (int x = 0; x < width; x++)
            {
                M[1, x] = 0.0; //patch in second time step with zero gradient
            }

            for (int b = 0; b < bandCount; b++) //for all bands
            {
                int start = (int)((b - 1) * bandWidth);   //extend range of submatrix below b for smoother changes
                if (start < 0)
                {
                    start = 0;
                }

                int stop = (int)((b + 2) * bandWidth);
                if (stop >= width)
                {
                    stop = width - 1;
                }

                double[,] subMatrix = DataTools.Submatrix(blurM, 0, start, height - 1, stop);
                double lowerThreshold;
                double upperThreshold;
                PercentileThresholds(subMatrix, lowerShoulder, upperShoulder, out lowerThreshold, out upperThreshold);

                //LoggedConsole.WriteLine(0 + "," + start + "," + (height - 1) + "," + stop + "   Threshold " + b + "=" + threshold);

                for (int x = start; x < stop; x++)
                {
                    int state = 0;
                    for (int y = 2; y < height - 1; y++)
                    {
                        double grad1 = blurM[y, x] - blurM[y - 1, x]; //calculate one step gradient
                        double grad2 = blurM[y + 1, x] - blurM[y - 1, x]; //calculate two step gradient

                        if (blurM[y, x] < upperThreshold)
                        {
                            state = 0;
                        }
                        else
                            if (grad1 < -gradThreshold)
                        {
                            state = 0;    // local decrease
                        }
                        else
                                if (grad1 > gradThreshold)
                        {
                            state = 1;     // local increase
                        }
                        else
                                    if (grad2 < -gradThreshold)
                        {
                            state = 0;    // local decrease
                        }
                        else
                                        if (grad2 > gradThreshold)
                        {
                            state = 1;     // local increase
                        }

                        M[y, x] = state;
                    }
                }//for all x in a band
            }//for all bands

            int minRowWidth = 2;
            int minColWidth = 5;
            M = Shapes_RemoveSmall(M, minRowWidth, minColWidth);
            return M;
        }// end of DetectHighEnergyRegions1()

        /// <summary>
        /// Detect high intensity / high energy regions in an image using blurring
        /// followed by bandwise thresholding.
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static double[,] DetectHighEnergyRegions3(double[,] matrix)
        {
            double lowerShoulder = 0.3;   //used to increase/decrease the intensity threshold from modal value
            double upperShoulder = 0.4;
            int bandWidth = 64;
            int halfWidth = bandWidth / 2;

            int fWindow = 7;
            int tWindow = 7;
            double[,] blurM = Blur(matrix, fWindow, tWindow);

            int height = matrix.GetLength(0);
            int width = matrix.GetLength(1);
            double[,] M = new double[height, width];

            double[,] subMatrix = DataTools.Submatrix(blurM, 0, 0, height - 1, bandWidth);
            double lowerThreshold;
            double upperThreshold;
            PercentileThresholds(subMatrix, lowerShoulder, upperShoulder, out lowerThreshold, out upperThreshold);

            for (int col = 0; col < width; col++) //for all cols
            {
                int start = col - halfWidth;   //extend range of submatrix below col for smoother changes
                if (start < 0)
                {
                    start = 0;
                }

                int stop = col + halfWidth;
                if (stop >= width)
                {
                    stop = width - 1;
                }

                if (col % 8 == 0 && !(col == 0))
                {
                    subMatrix = DataTools.Submatrix(blurM, 0, start, height - 1, stop);
                    PercentileThresholds(subMatrix, lowerShoulder, upperShoulder, out lowerThreshold, out upperThreshold);
                }

                for (int y = 0; y < height; y++)
                {
                    if (blurM[y, col] < upperThreshold)
                    {
                        M[y, col] = 0.0;
                    }
                    else
                    {
                        M[y, col] = 1.0;
                    }
                }
            }//for all cols

            return M;
        }// end of Shapes2()

        public static double[,] Shapes3(double[,] m)
        {
            double[,] m1 = DetectHighEnergyRegions3(m); //detect blobs of high acoustic energy
            double[,] m2 = Shapes_lines(m);

            int height = m.GetLength(0);
            int width = m.GetLength(1);
            double[,] tmpM = new double[height, width];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (m2[y, x] == 0.0)
                    {
                        continue; //nothing here
                    }

                    if (tmpM[y, x] == 1.0)
                    {
                        continue; //already have something here
                    }

                    int colWidth; //colWidth of object
                    Oblong.ColumnWidth(m2, x, y, out colWidth);
                    int x2 = x + colWidth;
                    for (int j = x; j < x2; j++)
                    {
                        tmpM[y, j] = 1.0;
                    }

                    //find distance to nearest object in hi frequency direction
                    // and join the two if within threshold distance
                    int thresholdDistance = 15;
                    int dist = 1;
                    while (x2 + dist < width && m2[y, x2 + dist] == 0)
                    {
                        dist++;
                    }

                    if (x2 + dist < width && dist < thresholdDistance)
                    {
                        for (int d = 0; d < dist; d++)
                        {
                            tmpM[y, x2 + d] = 1.0;
                        }
                    }
                }
            }

            //transfer line objects to output matrix IF they overlap a high energy blob in m1
            double[,] outM = new double[height, width];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (tmpM[y, x] == 0.0)
                    {
                        continue; //nothing here
                    }

                    if (outM[y, x] == 1.0)
                    {
                        continue; //already have something here
                    }

                    //int rowWidth; //rowWidth of object
                    //Shape.Row_Width(m2, x, y, out rowWidth);
                    int colWidth; //colWidth of object
                    Oblong.ColumnWidth(tmpM, x, y, out colWidth);
                    int x2 = x + colWidth;

                    //check to see if object is in blob
                    bool overlapsHighEnergy = false;
                    for (int j = x; j < x2; j++)
                    {
                        if (m1[y, j] == 1.0)
                        {
                            overlapsHighEnergy = true;
                            break;
                        }
                    }//end of ascertaining if line overlapsHighEnergy

                    if (overlapsHighEnergy)
                    {
                        for (int j = x; j < x2; j++)
                        {
                            outM[y, j] = 1.0;
                        }
                    }
                }
            }

            outM = FillGaps(outM);
            int minRowWidth = 2;
            int minColWidth = 4;
            outM = Shapes_RemoveSmallUnattached(outM, minRowWidth, minColWidth);
            return outM;
        }

        public static ArrayList Shapes4(double[,] m)
        {
            double[,] m1 = DetectHighEnergyRegions3(m); //binary matrix showing areas of high acoustic energy
            double[,] m2 = Shapes_lines(m); //binary matrix showing high energy lines

            int height = m.GetLength(0);
            int width = m.GetLength(1);
            double[,] tmpM = new double[height, width];
            ArrayList shapes = new ArrayList();

            //transfer m2 lines spectrogram to temporary matrix and merge adjacent high energy objects
            for (int y = 0; y < height; y++) //row at a time
            {
                for (int x = 0; x < width; x++) //transfer values to tmpM
                {
                    if (m2[y, x] == 0.0)
                    {
                        continue; //nothing here
                    }

                    if (tmpM[y, x] == 1.0)
                    {
                        continue; //already have something here
                    }

                    int colWidth; //colWidth of object
                    Oblong.ColumnWidth(m2, x, y, out colWidth);
                    int x2 = x + colWidth - 1;
                    for (int j = x; j < x2; j++)
                    {
                        tmpM[y, j] = 1.0;
                    }

                    //find distance to nearest object in hi frequency direction
                    // and join the two if within threshold distance
                    int thresholdDistance = 10;
                    int dist = 1;
                    while (x2 + dist < width && m2[y, x2 + dist] == 0)
                    {
                        dist++;
                    }

                    if (x2 + dist < width && dist < thresholdDistance)
                    {
                        for (int d = 0; d < dist; d++)
                        {
                            tmpM[y, x2 + d] = 1.0;
                        }
                    }
                }

                y++; //only even rows
            }

            //transfer line objects to output matrix IF they overlap a high energy region in m1
            int objectCount = 0;
            double[,] outM = new double[height, width];
            for (int y = 0; y < height - 2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (tmpM[y, x] == 0.0)
                    {
                        continue; //nothing here
                    }

                    if (outM[y, x] == 1.0)
                    {
                        continue; //already have something here
                    }

                    int colWidth; //colWidth of object
                    Oblong.ColumnWidth(tmpM, x, y, out colWidth);

                    int x2 = x + colWidth;

                    //check to see if object is in high energy region
                    bool overlapsHighEnergy = false;
                    for (int j = x; j < x2; j++)
                    {
                        if (m1[y + 1, j] == 1.0 || m1[y, j] == 1.0)
                        {
                            overlapsHighEnergy = true;
                            break;
                        }
                    }//end of ascertaining if line overlapsHighEnergy

                    if (overlapsHighEnergy)
                    {
                        shapes.Add(new Oblong(y, x, y + 1, x2));
                        objectCount++;
                        for (int j = x; j < x2; j++)
                        {
                            outM[y, j] = 1.0;
                        }

                        for (int j = x; j < x2; j++)
                        {
                            tmpM[y, j] = 0.0;
                        }

                        for (int j = x; j < x2; j++)
                        {
                            outM[y + 1, j] = 1.0;
                        }

                        for (int j = x; j < x2; j++)
                        {
                            tmpM[y + 1, j] = 0.0;
                        }
                    }
                }//end cols
            }//end rows

            //NOW DO SHAPE MERGING TO REDUCE NUMBERS
            LoggedConsole.WriteLine("Object Count 1 =" + objectCount);
            int dxThreshold = 25; //upper limit on centroid displacement - set higher for fewer bigger shapes
            double widthRatio = 5.0; //upper limit on difference in shape width - set higher for fewer bigger shapes
            shapes = Oblong.MergeShapesWithAdjacentRows(shapes, dxThreshold, widthRatio);
            LoggedConsole.WriteLine("Object Count 2 =" + shapes.Count);

            //shapes = Shape.RemoveEnclosedShapes(shapes);
            shapes = Oblong.RemoveOverlappingShapes(shapes);
            int minArea = 14;
            shapes = Oblong.RemoveSmall(shapes, minArea);
            LoggedConsole.WriteLine("Object Count 3 =" + shapes.Count);
            return shapes;
        }

        /// <summary>
        /// Returns an ArrayList of rectabgular shapes that represent acoustic events / syllables in the sonogram.
        /// </summary>
        public static ArrayList Shapes5(double[,] m)
        {
            //get binary matrix showing high energy regions
            int fWindow = 5;
            int tWindow = 3;
            double[,] tmp = Blur(m, fWindow, tWindow);
            double threshold = 0.2;
            double[,] m1 = DataTools.Threshold(tmp, threshold);

            //get binary matrix showing high energy lines
            double[,] m2 = Convolve(tmp, Kernal.HorizontalLine5);
            threshold = 0.2;
            m2 = DataTools.Threshold(m2, threshold);

            //prepare to extract acoustic events or shapes
            int height = m.GetLength(0);
            int width = m.GetLength(1);
            double[,] tmpM = new double[height, width];
            ArrayList shapes = new ArrayList();

            //transfer m2 lines spectrogram to temporary matrix and join adjacent high energy objects
            for (int y = 0; y < height; y++) //row at a time
            {
                for (int x = 0; x < width; x++) //transfer values to tmpM
                {
                    if (m2[y, x] == 0.0)
                    {
                        continue; //nothing here
                    }

                    if (tmpM[y, x] == 1.0)
                    {
                        continue; //already have something here
                    }

                    int colWidth; //colWidth of object
                    Oblong.ColumnWidth(m2, x, y, out colWidth);
                    int x2 = x + colWidth - 1;
                    for (int j = x; j < x2; j++)
                    {
                        tmpM[y, j] = 1.0;
                    }

                    //find distance to nearest object in hi frequency direction
                    // and join the two if within threshold distance
                    int thresholdDistance = 10;
                    int dist = 1;
                    while (x2 + dist < width && m2[y, x2 + dist] == 0)
                    {
                        dist++;
                    }

                    if (x2 + dist < width && dist < thresholdDistance)
                    {
                        for (int d = 0; d < dist; d++)
                        {
                            tmpM[y, x2 + d] = 1.0;
                        }
                    }
                }

                y++; //only even rows
            }

            //transfer line objects to output matrix IF they overlap a high energy region in m1
            int objectCount = 0;
            double[,] outM = new double[height, width];
            for (int y = 0; y < height - 2; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (tmpM[y, x] == 0.0)
                    {
                        continue; //nothing here
                    }

                    if (outM[y, x] == 1.0)
                    {
                        continue; //already have something here
                    }

                    int colWidth; //colWidth of object
                    Oblong.ColumnWidth(tmpM, x, y, out colWidth);

                    int x2 = x + colWidth;

                    //check to see if object is in high energy region
                    bool overlapsHighEnergy = false;
                    for (int j = x; j < x2; j++)
                    {
                        if (m1[y + 1, j] == 1.0 || m1[y, j] == 1.0)
                        {
                            overlapsHighEnergy = true;
                            break;
                        }
                    }//end of ascertaining if line overlapsHighEnergy

                    if (overlapsHighEnergy)
                    {
                        shapes.Add(new Oblong(y, x, y + 1, x2));
                        objectCount++;
                        for (int j = x; j < x2; j++)
                        {
                            outM[y, j] = 1.0;
                        }

                        for (int j = x; j < x2; j++)
                        {
                            tmpM[y, j] = 0.0;
                        }

                        for (int j = x; j < x2; j++)
                        {
                            outM[y + 1, j] = 1.0;
                        }

                        for (int j = x; j < x2; j++)
                        {
                            tmpM[y + 1, j] = 0.0;
                        }
                    }
                }//end cols
            }//end rows

            //NOW DO SHAPE MERGING TO REDUCE NUMBERS
            LoggedConsole.WriteLine("Object Count 1 =" + objectCount);
            int dxThreshold = 25; //upper limit on centroid displacement - set higher for fewer bigger shapes
            double widthRatio = 4.0; //upper limit on difference in shape width - set higher for fewer bigger shapes
            shapes = Oblong.MergeShapesWithAdjacentRows(shapes, dxThreshold, widthRatio);
            LoggedConsole.WriteLine("Object Count 2 =" + shapes.Count);
            shapes = Oblong.RemoveEnclosedShapes(shapes);

            //shapes = Shape.RemoveOverlappingShapes(shapes);
            int minArea = 30;
            shapes = Oblong.RemoveSmall(shapes, minArea);
            LoggedConsole.WriteLine("Object Count 3 =" + shapes.Count);
            return shapes;
        }

        /// <summary>
        /// Returns a binary matrix containing high energy lines in the oriignal spectrogram
        /// </summary>
        public static double[,] Shapes_lines(double[,] matrix)
        {
            double threshold = 0.3;

            int fWindow = 5;
            int tWindow = 3;
            double[,] tmpM = Blur(matrix, fWindow, tWindow);

            //double[,] tmpM = ImageTools.Convolve(matrix, Kernal.HorizontalLine5);
            tmpM = Convolve(tmpM, Kernal.HorizontalLine5);

            //tmpM = ImageTools.Convolve(tmpM, Kernal.HorizontalLine5);

            //int height = matrix.GetLength(0);
            //int width = matrix.GetLength(1);
            //double[,] M = new double[height, width];
            double[,] M = DataTools.Threshold(tmpM, threshold);
            return M;
        }// end of Shapes_lines()

        /// <summary>
        /// Returns a binary matrix containing high energy lines in the original spectrogram
        /// calculates the threshold bandwise
        /// </summary>
        /// <param name="matrix"></param>
        /// <returns></returns>
        public static double[,] Shapes_lines_bandwise(double[,] matrix)
        {
            double lowerShoulder = 0.7;   //used to increase or decrease the threshold from modal value
            double upperShoulder = 0.1;
            int bandWidth = 64;
            int halfWidth = bandWidth / 2;

            int fWindow = 3;
            int tWindow = 3;
            double[,] tmpM = Blur(matrix, fWindow, tWindow);
            tmpM = Convolve(tmpM, Kernal.HorizontalLine5);
            tmpM = Convolve(tmpM, Kernal.HorizontalLine5);

            int height = matrix.GetLength(0);
            int width = matrix.GetLength(1);
            double[,] M = new double[height, width];

            double[,] subMatrix = DataTools.Submatrix(tmpM, 0, 0, height - 1, bandWidth);
            double lowerThreshold;
            double upperThreshold;
            PercentileThresholds(subMatrix, lowerShoulder, upperShoulder, out lowerThreshold, out upperThreshold);

            for (int col = 2; col < width; col++) // for all cols
            {
                int start = col - halfWidth; //extend range of submatrix below col for smoother changes
                if (start < 0)
                {
                    start = 0;
                }

                int stop = col + halfWidth;
                if (stop >= width)
                {
                    stop = width - 1;
                }

                if (col % 8 == 0 && !(col == 0))
                {
                    subMatrix = DataTools.Submatrix(tmpM, 0, start, height - 1, stop);
                    PercentileThresholds(subMatrix, lowerShoulder, upperShoulder, out lowerThreshold, out upperThreshold);
                }

                for (int y = 1; y < height - 1; y++)
                {
                    bool evenRow = y % 2 == 0;
                    if (tmpM[y, col] > upperThreshold)
                    {
                        M[y, col] = 1;
                        if (evenRow)
                        {
                            M[y + 1, col] = 1;
                        }
                        else
                        {
                            M[y - 1, col] = 1;
                        }

                        //fill in gaps
                        if (M[y, col - 2] == 1.0 && M[y, col - 1] == 0.0)
                        {
                            M[y, col - 1] = 1;
                        }
                    }
                }
            }//for all cols

            int minRowWidth = 2;
            int minColWidth = 5;
            M = Shapes_RemoveSmall(M, minRowWidth, minColWidth);
            return M;
        }// end of Shapes_lines()

        public static double[,] Shapes_RemoveSmall(double[,] m, int minRowWidth, int minColWidth)
        {
            int height = m.GetLength(0);
            int width = m.GetLength(1);
            double[,] M = new double[height, width];

            for (int x = 0; x < width; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    if (m[y, x] == 0.0)
                    {
                        continue; //nothing here
                    }

                    if (M[y, x] == 1.0)
                    {
                        continue; //already have something here
                    }

                    int rowWidth; //rowWidth of object
                    Oblong.Row_Width(m, x, y, out rowWidth);
                    int colWidth; //colWidth of object
                    Oblong.ColumnWidth(m, x, y, out colWidth);
                    bool sizeOK = rowWidth >= minRowWidth && colWidth >= minColWidth;

                    if (sizeOK)
                    {
                        for (int c = 0; c < colWidth; c++)
                        {
                            for (int r = 0; r < minRowWidth; r++)
                            {
                                M[y + r, x + c] = 1.0;
                            }
                        }
                    }

                    y += rowWidth - 1;
                }//end y loop
            }//end x loop

            //M = m;

            return M;
        }

        public static double[,] Shapes_RemoveSmallUnattached(double[,] m, int minRowWidth, int minColWidth)
        {
            int height = m.GetLength(0);
            int width = m.GetLength(1);
            double[,] M = new double[height, width];

            for (int x = 0; x < width; x++)
            {
                for (int y = 1; y < height - 3; y++)
                {
                    if (m[y, x] == 0.0)
                    {
                        continue; //nothing here
                    }

                    if (M[y, x] == 1.0)
                    {
                        continue; //already have something here
                    }

                    int rowWidth; //rowWidth of object
                    Oblong.Row_Width(m, x, y, out rowWidth);
                    int colWidth; //colWidth of object
                    Oblong.ColumnWidth(m, x, y, out colWidth);
                    bool sizeOK = false;
                    if (rowWidth >= minRowWidth && colWidth >= minColWidth)
                    {
                        sizeOK = true;
                    }

                    //now check if object is unattached to other object
                    bool attachedOK = false;
                    for (int j = x; j < x + colWidth; j++)
                    {
                        if (m[y - 1, j] == 1.0 || /*(m[y + 1, j] == 1.0) ||*/ m[y + 2, j] == 1.0 || m[y + 3, j] == 1.0)
                        {
                            attachedOK = true;
                            break;
                        }
                    }//end of ascertaining if line overlapsHighEnergy

                    //attachedOK = true;
                    if (sizeOK && attachedOK)
                    {
                        for (int c = 0; c < colWidth; c++)
                        {
                            //Shape.Row_Width(m, x+c, y, out rowWidth);
                            for (int r = 0; r < minRowWidth; r++)
                            {
                                M[y + r, x + c] = 1.0;
                            }
                        }
                    }
                }//end y loop
            }//end x loop

            //M = m;

            return M;
        }

        public static double[,] FillGaps(double[,] m)
        {
            double coverThreshold = 0.7;
            int cNH = 4; //neighbourhood
            int rNH = 11;

            int height = m.GetLength(0);
            int width = m.GetLength(1);

            //double[,] M = new double[height, width];
            int area = ((2 * cNH) + 1) * ((2 * rNH) + 1);

            //LoggedConsole.WriteLine("area=" + area);

            for (int x = cNH; x < width - cNH; x++)
            {
                for (int y = rNH; y < height - rNH; y++)
                {
                    double sum = 0.0;
                    for (int r = -rNH; r < rNH; r++)
                    {
                        for (int c = -cNH; c < cNH; c++)
                        {
                            sum += m[y + r, x + c];
                        }
                    }

                    double cover = sum / area;

                    if (cover >= coverThreshold)
                    {
                        m[y, x] = 1.0;
                        m[y - 1, x] = 1.0;
                        m[y + 1, x] = 1.0;

                        //m[y - 2, x] = 1.0;
                        //m[y + 2, x] = 1.0;
                    }
                }//end y loop
            }//end x loop

            return m;
        }

        /// <summary>
        /// returns a palette of a variety of coluor.
        /// Used for displaying clusters identified by colour.
        /// </summary>
        public static List<Pen> GetRedGradientPalette()
        {
            var pens = new List<Pen>();
            for (int c = 0; c < 256; c++)
            {
                pens.Add(new Pen(Color.FromArgb(255, c, c)));
            }

            return pens;
        }

        /// <summary>
        /// returns a palette of a variety of coluor.
        /// Used for displaying clusters identified by colour.
        /// </summary>
        /// <param name="paletteSize"></param>
        /// <returns></returns>
        public static List<Pen> GetColorPalette(int paletteSize)
        {
            var pens = new List<Pen>();
            pens.Add(new Pen(Color.Pink));
            pens.Add(new Pen(Color.Red));
            pens.Add(new Pen(Color.Orange));
            pens.Add(new Pen(Color.Yellow));
            pens.Add(new Pen(Color.Green));
            pens.Add(new Pen(Color.Blue));
            pens.Add(new Pen(Color.Crimson));
            pens.Add(new Pen(Color.LimeGreen));
            pens.Add(new Pen(Color.Tomato));

            //pens.Add(new Pen(Color.Indigo));
            pens.Add(new Pen(Color.Violet));

            //now add random coloured pens
            int max = 255;
            var rn = new RandomNumber(1234567);
            for (int c = 10; c <= paletteSize; c++)
            {
                int rd = rn.GetInt(max);
                int gr = rn.GetInt(max);
                int bl = rn.GetInt(max);
                pens.Add(new Pen(Color.FromArgb(rd, gr, bl)));
            }

            return pens;
        }

        /// <summary>
        /// Returns an image of an array of the passed colour patches.
        /// </summary>
        public static Image DrawColourChart(int width, int ht, Color[] colorArray)
        {
            if (colorArray == null || colorArray.Length == 0)
            {
                return null;
            }

            int colourCount = colorArray.Length;
            int patchWidth = width / colourCount;
            int maxPathWidth = (int)(ht / 1.5);
            if (patchWidth > maxPathWidth)
            {
                patchWidth = maxPathWidth;
            }
            else if (width < 3)
            {
                width = 3;
            }

            Bitmap colorScale = new Bitmap(colourCount * patchWidth, ht);
            Graphics gr = Graphics.FromImage(colorScale);
            int offset = width + 1;
            if (width < 5)
            {
                offset = width;
            }

            Bitmap colorBmp = new Bitmap(width - 1, ht);
            Graphics gr2 = Graphics.FromImage(colorBmp);
            Color c;
            int x = 0;

            for (int i = 0; i < colourCount; i++)
            {
                //c = Color.FromArgb(250, 15, 250);
                gr2.Clear(colorArray[i]);

                //int x = 0;
                gr.DrawImage(colorBmp, x, 0); //dra

                //c = Color.FromArgb(250, 15, 15);
                //gr2.Clear(c);
                x += patchWidth;
                gr.DrawImage(colorBmp, x, 0); //dra
            }

            return colorScale;
        }

        /// <summary>
        /// returns a colour array of 256 gray scale values
        /// </summary>
        public static Color[] GrayScale()
        {
            int max = 256;
            Color[] grayScale = new Color[256];
            for (int c = 0; c < max; c++)
            {
                grayScale[c] = Color.FromArgb(c, c, c);
            }

            return grayScale;
        }

        public static Color[] GreenScale()
        {
            int max = 256;
            Color[] greenScale = new Color[256];
            for (int c = 0; c < max; c++)
            {
                greenScale[c] = Color.FromArgb(0, c, 0);
            }

            return greenScale;
        }

        /// <summary>
        /// Normalises the matrix between zero and one.
        /// Then draws the reversed matrix and saves image to passed path.
        /// </summary>
        /// <param name="matrix">the data</param>
        public static void DrawReversedMDNMatrix(Matrix<double> matrix, string pathName)
        {
            double[,] matrix1 = matrix.ToArray();
            Image bmp = DrawReversedMatrix(matrix1);
            bmp.Save(pathName);
        }

        /// <summary>
        /// Normalises the matrix between zero and one.
        /// Then draws the reversed matrix and saves image to passed path.
        /// </summary>
        /// <param name="matrix">the data</param>
        public static void DrawReversedMatrix(double[,] matrix, string pathName)
        {
            Image bmp = DrawReversedMatrix(matrix);
            bmp.Save(pathName);
        }

        public static double[,] ByteMatrix2DoublesMatrix(byte[,] mb)
        {
            int rows = mb.GetLength(0); //number of rows
            int cols = mb.GetLength(1); //number

            double[,] matrix = new double[rows, cols];
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    matrix[r, c] = mb[r, c];
                }
            }

            return matrix;
        }

        public static void DrawMatrix(byte[,] mBytes, string pathName)
        {
            double[,] matrix = ByteMatrix2DoublesMatrix(mBytes);
            Image bmp = DrawNormalisedMatrix(matrix);
            bmp.Save(pathName);
        }

        /// <summary>
        /// Draws matrix and save image
        /// </summary>
        /// <param name="vector">the data</param>
        public static void DrawMatrix(double[] vector, string pathName)
        {
            double[,] matrix = new double[1, vector.Length];
            for (int i = 0; i < vector.Length; i++)
            {
                matrix[0, i] = vector[i];
            }

            Image bmp = DrawNormalisedMatrix(matrix);
            bmp.Save(pathName);
        }

        /// <summary>
        /// Draws matrix and save image
        /// </summary>
        /// <param name="matrix">the data</param>
        /// <param name="pathName"></param>
        public static void DrawMatrix(double[,] matrix, string pathName)
        {
            Image bmp = DrawNormalisedMatrix(matrix);
            bmp.Save(pathName);
        }

        public static void DrawMatrix(double[,] matrix, double lowerBound, double upperBound, string pathName)
        {
            Image bmp = DrawNormalisedMatrix(matrix, lowerBound, upperBound);
            bmp.Save(pathName);
        }

        /// <summary>
        /// Draws matrix after first normalising the data
        /// </summary>
        /// <param name="matrix">the data</param>
        /// <param name="pathName"></param>
        public static Image DrawNormalisedMatrix(double[,] matrix)
        {
            double[,] norm = DataTools.normalise(matrix);
            return DrawMatrixWithoutNormalisation(norm);
        }

        public static Image DrawNormalisedMatrix(double[,] matrix, double lowerBound, double upperBound)
        {
            double[,] norm = DataTools.NormaliseInZeroOne(matrix, lowerBound, upperBound);
            return DrawMatrixWithoutNormalisation(norm);
        }

        /// <summary>
        /// Draws matrix after first normalising the data
        /// </summary>
        /// <param name="matrix">the data</param>
        public static Image DrawReversedMatrix(double[,] matrix)
        {
            double[,] norm = DataTools.normalise(matrix);
            return DrawReversedMatrixWithoutNormalisation(norm);
        }

        /// <summary>
        /// Draws matrix without normkalising the values in the matrix.
        /// Assume some form of normalisation already done.
        /// </summary>
        /// <param name="matrix">the data</param>
        /// <param name="pathName"></param>
        public static Image DrawReversedMatrixWithoutNormalisation(double[,] matrix)
        {
            int rows = matrix.GetLength(0); //number of rows
            int cols = matrix.GetLength(1); //number

            Color[] grayScale = GrayScale();

            Bitmap bmp = new Bitmap(cols, rows, PixelFormat.Format24bppRgb);
            int greyId = 0;

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    if (double.IsNaN(matrix[r, c]))
                    {
                        greyId = 128; //want NaN values in gray,
                    }
                    else
                    {
                        greyId = (int)Math.Floor(matrix[r, c] * 255);
                        if (greyId < 0)
                        {
                            greyId = 0;
                        }
                        else
                        {
                            if (greyId > 255)
                            {
                                greyId = 255;
                            }
                        }

                        greyId = 255 - greyId; // reverse image - want high values in black, low values in white
                    }

                    bmp.SetPixel(c, r, grayScale[greyId]);
                }//end all columns
            }

            return bmp;
        }

        /// <summary>
        /// Draws matrix without normkalising the values in the matrix.
        /// Assume some form of normalisation already done.
        /// </summary>
        /// <param name="matrix">the data</param>
        public static Image DrawMatrixWithoutNormalisation(double[,] matrix)
        {
            int rows = matrix.GetLength(0); //number of rows
            int cols = matrix.GetLength(1); //number

            Color[] grayScale = GrayScale();

            Bitmap bmp = new Bitmap(cols, rows, PixelFormat.Format24bppRgb);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int greyId = (int)Math.Floor(matrix[r, c] * 255);

                    if (greyId < 0)
                    {
                        greyId = 0;
                    }
                    else
                    {
                        if (greyId > 255)
                        {
                            greyId = 255;
                        }
                    }

                    bmp.SetPixel(c, r, grayScale[greyId]);
                }//end all columns
            }

            return bmp;
        }

        public static Image DrawMatrixWithoutNormalisationGreenScale(double[,] matrix)
        {
            int rows = matrix.GetLength(0); //number of rows
            int cols = matrix.GetLength(1); //number

            Color[] grayScale = GreenScale();

            Bitmap bmp = new Bitmap(cols, rows, PixelFormat.Format24bppRgb);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int greyId = (int)Math.Floor(matrix[r, c] * 255);

                    if (greyId < 0)
                    {
                        greyId = 0;
                    }
                    else
                    {
                        if (greyId > 255)
                        {
                            greyId = 255;
                        }
                    }

                    bmp.SetPixel(c, r, grayScale[greyId]);
                } //end all columns
            } //end all rows

            return bmp;
        }

        public static Image DrawRGBMatrix(double[,] matrixR, double[,] matrixG, double[,] matrixB)
        {
            int rows = matrixR.GetLength(0); //number of rows
            int cols = matrixR.GetLength(1); //number

            Bitmap bmp = new Bitmap(cols, rows, PixelFormat.Format24bppRgb);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int R = (int)Math.Floor(matrixR[r, c] * 255);
                    if (R < 0)
                    {
                        R = 0;
                    }
                    else
                    {
                        if (R > 255)
                        {
                            R = 255;
                        }
                    }

                    int G = (int)Math.Floor(matrixG[r, c] * 255);
                    if (G < 0)
                    {
                        G = 0;
                    }
                    else
                    {
                        if (G > 255)
                        {
                            G = 255;
                        }
                    }

                    int B = (int)Math.Floor(matrixB[r, c] * 255);
                    if (B < 0)
                    {
                        B = 0;
                    }
                    else
                    {
                        if (B > 255)
                        {
                            B = 255;
                        }
                    }

                    bmp.SetPixel(c, r, Color.FromArgb(R, G, B));
                }//end all columns
            }//end all rows

            return bmp;
        }

        /// <summary>
        /// Draws horizontal gridlines on Image
        /// </summary>
        public static Image DrawYaxisScale(Image image, int scaleWidth, double yInterval, double yTicInterval, int yOffset)
        {
            int ticCount = (int)(image.Height / yTicInterval);
            var pen = new Pen(Color.White);
            var stringFont = new Font("Arial", 10);

            var g = Graphics.FromImage(image);
            for (int i = 1; i <= ticCount; i++)
            {
                int y1 = image.Height - (int)(i * yTicInterval) + yOffset;
                g.DrawLine(pen, 0, y1, image.Width - 1, y1);
                string value = Math.Round(yInterval * i).ToString();
                g.DrawString(value, stringFont, Brushes.White, new PointF(2, y1 + 1));
            }

            Image yAxisImage = new Bitmap(scaleWidth, image.Height);
            g = Graphics.FromImage(yAxisImage);
            pen = new Pen(Color.Black);
            g.Clear(Color.LightGray);
            for (int i = 1; i <= ticCount; i++)
            {
                int y1 = yAxisImage.Height - (int)(i * yTicInterval) + yOffset;
                g.DrawLine(pen, 0, y1, scaleWidth - 1, y1);
                g.DrawLine(pen, 0, y1 - 1, scaleWidth - 1, y1 - 1);
            }

            g.DrawRectangle(pen, 0, 0, scaleWidth - 1, image.Height - 1);
            Image[] array = new Image[2];
            array[0] = yAxisImage;
            array[1] = image;
            return CombineImagesInLine(array);
        }

        /// <summary>
        /// assumes the y-axis has already been drawn already.
        /// Therefore require an offset at bottom left to accommodate the width of the y-axis.
        /// </summary>
        public static Image DrawXaxisScale(Image image, int scaleHeight, double xInterval, double xTicInterval, int yScalePadding, int xOffset)
        {
            int ticCount = (int)((image.Width - scaleHeight) / xTicInterval);
            var pen = new Pen(Color.White);
            var stringFont = new Font("Arial", 10);
            var g = Graphics.FromImage(image);

            // draw on the grid lines
            for (int i = 1; i <= ticCount; i++)
            {
                int x1 = yScalePadding + (int)(i * xTicInterval) + xOffset;
                g.DrawLine(pen, x1, 0, x1, image.Height - 1);
            }

            // create the x-axis scale
            Image scaleImage = new Bitmap(image.Width, scaleHeight);
            g = Graphics.FromImage(scaleImage);
            pen = new Pen(Color.Black);
            g.Clear(Color.LightGray);
            for (int i = 0; i <= ticCount; i++)
            {
                int x1 = yScalePadding + (int)(i * xTicInterval) + xOffset;
                g.DrawLine(pen, x1, 0, x1, scaleHeight - 1);
                //g.DrawLine(pen, x1 + 1, 0, x1 + 1, scaleHeight - 1);

                string value = Math.Round(xInterval * i).ToString();
                g.DrawString(value, stringFont, Brushes.Black, new PointF(x1, 0));
            }

            g.DrawRectangle(pen, 0, 0, image.Width - 1, scaleHeight - 1);
            var array = new Image[2];
            array[0] = image;
            array[1] = scaleImage;
            return CombineImagesVertically(array);
        }

        /// <summary>
        /// Draws matrix but automatically determines the scale to fit 1000x1000 pixel image.
        /// </summary>
        /// <param name="matrix">the data</param>
        public static void DrawMatrix(double[,] matrix, string pathName, bool doScale)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);

            int maxYpixels = rows;
            int maxXpixels = cols;
            int YpixelsPerCell = 1;
            int XpixelsPerCell = 1;
            if (doScale)
            {
                maxYpixels = 1000;
                maxXpixels = 2500;
                YpixelsPerCell = maxYpixels / rows;
                XpixelsPerCell = maxXpixels / cols;
                if (YpixelsPerCell == 0)
                {
                    YpixelsPerCell = 1;
                }

                if (XpixelsPerCell == 0)
                {
                    XpixelsPerCell = 1;
                }
            }

            int Ypixels = YpixelsPerCell * rows;
            int Xpixels = XpixelsPerCell * cols;

            Color[] grayScale = GrayScale();

            var bmp = new Bitmap(Xpixels, Ypixels, PixelFormat.Format24bppRgb);

            double[,] norm = DataTools.normalise(matrix);
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int greyId = (int)Math.Floor(norm[r, c] * 255);
                    if (greyId < 0)
                    {
                        greyId = 0;
                    }

                    int xOffset = XpixelsPerCell * c;
                    int yOffset = YpixelsPerCell * r;
                    for (int x = 0; x < XpixelsPerCell; x++)
                    {
                        for (int y = 0; y < YpixelsPerCell; y++)
                        {
                            bmp.SetPixel(xOffset + x, yOffset + y, grayScale[greyId]);
                        }
                    }
                }
            }

            bmp.Save(pathName);
        }

        public static void DrawMatrixInColour(double[,] matrix, string pathName, bool doScale)
        {
            Image image = DrawMatrixInColour(matrix, doScale);
            image.Save(pathName);
        }

        /// <summary>
        /// Draws colour matrix but automatically determines the scale to fit 1000x1000 pixel image.
        /// </summary>
        /// <param name="matrix">the data</param>
        public static Image DrawMatrixInColour(double[,] matrix, bool doScale)
        {
            int xscale = 10;
            int yscale = 5;
            return DrawMatrixInColour(matrix, xscale, yscale);
        }

        public static Image DrawVectorInColour(double[] vector, int cellWidth)
        {
            double[] norm = DataTools.normalise(vector);

            int bottomColour = 1;     // to avoid using the reds
            int topColour = 250;      // to avoid using the magentas
            int hueRange = topColour - bottomColour;

            int rows = vector.Length;
            int cols = 1;
            int yPixelCount = cellWidth * rows;
            int xPixelCount = cellWidth;
            Bitmap bmp = new Bitmap(xPixelCount, yPixelCount, PixelFormat.Format24bppRgb);

            // Can comment next two lines if want black
            Graphics g = Graphics.FromImage(bmp);
            g.Clear(Color.Gray);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int xOffset = cellWidth * c;
                    int yOffset = cellWidth * r;

                    // use HSV colour space
                    int hue = topColour - (int)Math.Floor(hueRange * norm[r]);

                    // double saturation = 0.75 + (norm[r, c] * 0.25);
                    // double saturation = norm[r, c] * 0.5;
                    // double saturation = (1 - norm[r, c]) * 0.5;
                    double saturation = 1.0;
                    var myHsv = new Hsv { H = hue, S = saturation, V = 1.0 };
                    var myRgb = myHsv.To<Rgb>();

                    // use black as background zero colour rather than blue
                    if (myRgb.B < 255.0)
                    {
                        var colour = Color.FromArgb((int)myRgb.R, (int)myRgb.G, (int)myRgb.B / 3);
                        for (int x = 0; x < cellWidth; x++)
                        {
                            for (int y = 0; y < cellWidth; y++)
                            {
                                bmp.SetPixel(xOffset + x, yOffset + y, colour);
                            }
                        }
                    }
                }
            }

            return bmp;
        }

        public static Image DrawVectorInGrayScale(double[] vector, int cellWidth, int cellHeight)
        {
            var norm = DataTools.normalise(vector);
            var bmp = DrawVectorInGrayScaleWithoutNormalisation(norm, cellWidth, cellHeight, true);
            return bmp;
        }

        /// <summary>
        /// This method assumes that the vector has already been normalised by some means such that all values lie between 0.0 and 1.0
        /// </summary>
        /// <param name="vector">the vector of normalised values</param>
        /// <param name="cellWidth">the width of the image</param>
        /// <param name="cellHeight">the height of each image row</param>
        public static Image DrawVectorInGrayScaleWithoutNormalisation(double[] vector, int cellWidth, int cellHeight, bool reverse)
        {
            int rows = vector.Length;
            int cols = 1;
            int yPixelCount = cellHeight * rows;
            int xPixelCount = cellWidth;
            var bmp = new Bitmap(xPixelCount, yPixelCount, PixelFormat.Format24bppRgb);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int xOffset = cellWidth * c;
                    int yOffset = cellHeight * r;

                    // use reverse gray scale i.e. white = low value, black = high value
                    int gray;
                    if (reverse)
                    {
                        gray = (int)Math.Floor(255 * vector[r]);
                    }
                    else
                    {
                        gray = 255 - (int)Math.Floor(255 * vector[r]);
                    }

                    var colour = Color.FromArgb(gray, (int)gray, (int)gray);
                    for (int x = 0; x < xPixelCount; x++)
                    {
                        for (int y = 0; y < cellHeight; y++)
                        {
                            bmp.SetPixel(xOffset + x, yOffset + y, colour);
                        }
                    }
                }
            }

            return bmp;
        }

        public static Image DrawMatrixInColour(double[,] matrix, int xPixelsPerCell, int yPixelsPerCell)
        {
            double[,] norm = DataTools.normalise(matrix);

            int bottomColour = 1;     // to avoid using the reds
            int topColour = 250;      // to avoid using the magentas
            int hueRange = topColour - bottomColour;

            int rows = matrix.GetLength(0); //number of rows
            int cols = matrix.GetLength(1);
            int yPixelCount = yPixelsPerCell * rows;
            int xPixelCount = xPixelsPerCell * cols;
            Bitmap bmp = new Bitmap(xPixelCount, yPixelCount, PixelFormat.Format24bppRgb);

            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    int xOffset = xPixelsPerCell * c;
                    int yOffset = yPixelsPerCell * r;

                    // use HSV colour space
                    // int hue = bottomColour + (int)Math.Floor(hueRange * norm[r, c]);
                    int hue = topColour - (int)Math.Floor(hueRange * norm[r, c]);

                    // double saturation = 0.75 + (norm[r, c] * 0.25);
                    // double saturation = norm[r, c] * 0.5;
                    // double saturation = (1 - norm[r, c]) * 0.5;
                    double saturation = 1.0;
                    var myHsv = new Hsv { H = hue, S = saturation, V = 1.0 };
                    var myRgb = myHsv.To<Rgb>();

                    // use black as background zero colour rather than blue
                    if (myRgb.B < 255.0)
                    {
                        var colour = Color.FromArgb((int)myRgb.R, (int)myRgb.G, (int)myRgb.B / 3);
                        for (int x = 0; x < xPixelsPerCell; x++)
                        {
                            for (int y = 0; y < yPixelsPerCell; y++)
                            {
                                bmp.SetPixel(xOffset + x, yOffset + y, colour);
                            }
                        }
                    }
                }
            }

            return bmp;
        }

        public static void DrawMatrix(double[,] matrix, int cellXpixels, int cellYpixels, string pathName)
        {
            var bmp = ImageTools.DrawMatrixInGrayScale(matrix, cellXpixels, cellYpixels, true);
            bmp.Save(pathName);
        }

        /// <summary>
        /// Draws matrix according to user defined scale
        /// </summary>
        /// <param name="matrix">the data</param>
        /// <param name="xPixelsPerCell">X axis scale - pixels per cell</param>
        /// <param name="yPixelsPerCell">Y axis scale - pixels per cell</param>
        /// <param name="reverse">determines black on white or white on black</param>
        public static Bitmap DrawMatrixInGrayScale(double[,] matrix, int xPixelsPerCell, int yPixelsPerCell, bool reverse)
        {
            int rows = matrix.GetLength(0);
            int cols = matrix.GetLength(1);
            int Ypixels = yPixelsPerCell * rows;
            int Xpixels = xPixelsPerCell * cols;
            Color[] grayScale = GrayScale();
            var bmp = new Bitmap(Xpixels, Ypixels, PixelFormat.Format24bppRgb);

            double[,] norm = DataTools.normalise(matrix);
            for (int r = 0; r < rows; r++)
            {
                int colorId;
                for (int c = 0; c < cols; c++)
                {
                    if (reverse)
                    {
                        colorId = (int)Math.Floor(norm[r, c] * 255);
                    }
                    else
                    {
                        colorId = 255 - (int)Math.Floor(norm[r, c] * 255);
                    }

                    int xOffset = xPixelsPerCell * c;
                    int yOffset = yPixelsPerCell * r;

                    for (int x = 0; x < xPixelsPerCell; x++)
                    {
                        for (int y = 0; y < yPixelsPerCell; y++)
                        {
                            bmp.SetPixel(xOffset + x, yOffset + y, grayScale[colorId]);
                        }
                    }
                }
            }

            return bmp;
        }

        /// <summary>
        /// Stacks the passed images one on top of the other.
        /// Assumes that all images have the same width.
        /// </summary>
        public static Image CombineImagesVertically(List<Image> list)
        {
            return CombineImagesVertically(list.ToArray());
        }

        public static Image CombineImagesVertically(List<Image> list, int maxWidth)
        {
            return CombineImagesVertically(list.ToArray(), maxWidth);
        }

        /// <summary>
        /// Stacks the passed images one on top of the other.
        /// Assumes that all images have the same width.
        /// </summary>
        /// <param name="array"></param>
        /// <param name="maximumWidth">The maximum width of the output images</param>
        /// <returns></returns>
        public static Image CombineImagesVertically(Image[] array, int? maximumWidth = null)
        {
            int width = maximumWidth ?? array[0].Width;   // assume all images have the same width

            int compositeHeight = 0;
            for (int i = 0; i < array.Length; i++)
            {
                if (null == array[i])
                {
                    continue;
                }

                compositeHeight += array[i].Height;
            }

            Bitmap compositeBmp = new Bitmap(width, compositeHeight, PixelFormat.Format24bppRgb);
            int yOffset = 0;
            Graphics gr = Graphics.FromImage(compositeBmp);

            //gr.Clear(Color.Black);
            gr.Clear(Color.DarkGray);

            for (int i = 0; i < array.Length; i++)
            {
                if (null == array[i])
                {
                    continue;
                }

                gr.DrawImage(array[i], 0, yOffset); //draw in the top image
                yOffset += array[i].Height;
            }

            return compositeBmp;
        }

        /// <summary>
        /// Stacks the passed images one on top of the other.
        /// Assumes that all images have the same width.
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static Image CombineImagesInLine(List<Image> list)
        {
            return CombineImagesInLine(list.ToArray());
        }

        /// <summary>
        /// Stacks the passed images one on top of the other.
        /// Assumes that all images have the same width.
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static Image CombineImagesInLine(Image[] array)
        {
            int height = array[0].Height; // assume all images have the same height

            int compositeWidth = 0;
            for (int i = 0; i < array.Length; i++)
            {
                compositeWidth += array[i].Width;
                if (height < array[i].Height)
                {
                    height = array[i].Height;
                }
            }

            //Bitmap compositeBmp = new Bitmap(compositeWidth, height, PixelFormat.Format24bppRgb);
            var compositeBmp = new Bitmap(compositeWidth, height);
            int xOffset = 0;
            Graphics gr = Graphics.FromImage(compositeBmp);
            gr.Clear(Color.Black);

            for (int i = 0; i < array.Length; i++)
            {
                gr.DrawImage(array[i], xOffset, 0); //draw in the top spectrogram
                xOffset += array[i].Width;

                //string name = String.Format("TESTIMAGE" + i + ".png");
                //array[i].Save(Path.Combine(@"C:\SensorNetworks\Output\Frommolt\ConcatImageOutput", name));
            }

            // this was done in Berlin beacuse could not get images to save properly.

            //string fileName2 = String.Format("TESTIMAGE3.png");
            //compositeBmp.Save(Path.Combine(@"C:\SensorNetworks\Output\Frommolt\ConcatImageOutput", fileName2));

            return compositeBmp;
        }

        //public static void METHOD(string[] titleArray, int imageWidth, int imageHeight)
        //{
        //    // now make images
        //    var images = new List<Image>();
        //    int scalingFactor = 20;
        //    foreach (string key in titleArray)
        //    {
        //        string label = String.Format("{0} {1} ({2})", speciesLabel, key, speciesNumbers[i]);
        //        Image image = ImageTools.DrawGraph(label, dictionary[key], imageWidth, imageHeight, scalingFactor);
        //        images.Add(image);
        //    }
        //    Image combinedImage = ImageTools.CombineImagesVertically(images);
        //}

        public static Tuple<int, double> DetectLine(double[,] m, int row, int col, int lineLength, double centreThreshold, int resolutionAngle)
        {
            double endThreshold = centreThreshold / 2;

            if (m[row, col] < centreThreshold)
            {
                return null; //to not proceed if current pixel is low intensity
            }

            int rows = m.GetLength(0);
            int cols = m.GetLength(1);

            int maxAngle = -1;
            double intensitySum = 0.0;

            // double sumThreshold = lineLength * sensitivity;
            int degrees = 0;

            while (degrees < 180) //loop over 180 degrees in jumps of 10 degrees.
            {
                double cosAngle = Math.Cos(Math.PI * degrees / 180);
                double sinAngle = Math.Sin(Math.PI * degrees / 180);

                //check if extreme end of line goes outside bound
                if (row + (int)(cosAngle * lineLength) >= rows || row + (int)(cosAngle * lineLength) < 0)
                {
                    degrees += resolutionAngle;
                    continue;
                }

                if (col + (int)(sinAngle * lineLength) >= cols || col + (int)(sinAngle * lineLength) < 0)
                {
                    degrees += resolutionAngle;
                    continue;
                }

                //check if extreme end of line is low intensity pixel
                if (m[row + (int)(cosAngle * lineLength), col + (int)(sinAngle * lineLength)] < endThreshold)
                {
                    degrees += resolutionAngle;
                    continue;
                }

                double sum = 0.0;
                for (int j = 0; j < lineLength; j++)
                {
                    sum += m[row + (int)(cosAngle * j), col + (int)(sinAngle * j)];
                }

                if (sum > intensitySum)
                {
                    maxAngle = degrees;
                    intensitySum = sum;
                }

                degrees += resolutionAngle;
            } // while loop

            return Tuple.Create(maxAngle, intensitySum);
        }// DetectLine()

        /// <summary>
        /// Returns an image of the data matrix.
        /// Normalises the values from min->max to 0->1.
        /// Thus the grey-scale image pixels will range from 0 to 255.
        /// This method was originally written to draw sonograms,
        ///       hence the avoidance of outliers and references to freq bins.
        /// Perhaps this method should be put back in BaseSonogram.cs.
        /// </summary>
        public static Image GetMatrixImage(double[,] data)
        {
            int width = data.GetLength(0); // Number of spectra in sonogram
            int binCount = data.GetLength(1);
            int binHeight = 1;

            int imageHeight = binCount * binHeight; // image ht = sonogram ht

            //set up min, max for normalising of sonogram values
            int minRank = 50;
            int maxRank = 1000;

            // find min and max of matrix avoiding outliers.
            double[] array = MatrixTools.Matrix2Array(data);
            double minValue = DataTools.GetNthSmallestValue(array, minRank);
            double maxValue = DataTools.GetNthLargestValue(array, maxRank);
            double[] minmax = { minValue, maxValue };
            double min = minmax[0];
            double max = minmax[1];
            double range = max - min;

            Color[] grayScale = GrayScale();
            var bmp = new Bitmap(width, imageHeight, PixelFormat.Format24bppRgb);
            int yOffset = imageHeight;

            // over all freq bins
            for (int y = 0; y < binCount; y++)
            {
                for (int r = 0; r < binHeight; r++)
                {
                    //repeat this bin if pixel rows per bin>1
                    for (int x = 0; x < width; x++)
                    {
                        //for pixels in the line - NormaliseMatrixValues and bound the value - use min bound, max and 255 image intensity range
                        double value = (data[x, y] - min) / range;
                        int c = 255 - (int)Math.Floor(255.0 * value); //original version
                        if (c < 0)
                        {
                            c = 0;
                        }
                        else
                        if (c >= 256)
                        {
                            c = 255;
                        }

                        bmp.SetPixel(x, yOffset - 1, grayScale[c]);
                    } //for all pixels in line

                    yOffset--;
                } //end repeats over one track
            } //end over all freq bins

            return bmp;
        }

        public static Dictionary<Color, double> GetColorHistogramNormalized(Bitmap image, Rectangle? region = null)
        {
            Rectangle definiteRegion = region ?? new Rectangle(0, 0, image.Width, image.Height);
            var histogram = new Dictionary<Color, int>(100);

            int sum = definiteRegion.Area();

            for (var i = definiteRegion.Left; i < definiteRegion.Right; i++)
            {
                for (var j = definiteRegion.Top; j < definiteRegion.Bottom; j++)
                {
                    var color = image.GetPixel(i, j);
                    if (histogram.ContainsKey(color))
                    {
                        histogram[color] = histogram[color] + 1;
                    }
                    else
                    {
                        histogram.Add(color, 1);
                    }
                }
            }

            return histogram.ToDictionary(kvp => kvp.Key, kvp => kvp.Value / (double)sum);
            ;
        }
    }
}
