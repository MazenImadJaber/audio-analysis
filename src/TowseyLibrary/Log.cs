// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Log.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//   Defines the Log type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace TowseyLibrary
{
    using log4net;

    public static class Log
    {
        ////private const string MesgFormat = "{0:yyyy-MM-dd HH:mm:ss.fff}  {1}";

        private static readonly ILog Log4Net = LogManager.GetLogger(typeof(Log));

        static Log()
        {
            Verbosity = 0;
        }

        public static int Verbosity { get; set; }

        public static void WriteLine(string format, params object[] args)
        {
            Log4Net.InfoFormat(format, args);
        }

        public static void WriteIfVerbose(string format, params object[] args)
        {
            if (Verbosity > 0)
            {
                Log4Net.DebugFormat(format, args);
            }
        }
    }
}