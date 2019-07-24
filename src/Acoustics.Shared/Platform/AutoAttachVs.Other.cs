// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AutoAttachVs.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//   Example taken from this gist.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

#if DEBUG

namespace Acoustics.Shared.Platform
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Runtime.InteropServices.ComTypes;
    using Process = System.Diagnostics.Process;

    /// <summary>
    /// Example taken from <a href="https://gist.github.com/3813175">this gist</a>.
    /// </summary>
    public static partial class VisualStudioAttacher
    {

        public static string GetSolutionForVisualStudio(Process visualStudioProcess) => null;

        public static Process GetAttachedVisualStudio(Process applicationProcess) => null;

        /// <summary>
        /// The method to use to attach visual studio to a specified process.
        /// </summary>
        /// <param name="visualStudioProcess">
        /// The visual studio process to attach to.
        /// </param>
        /// <param name="applicationProcess">
        /// The application process that needs to be debugged.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when the application process is null.
        /// </exception>
        public static void AttachVisualStudioToProcess(Process visualStudioProcess, Process applicationProcess) => throw new PlatformNotSupportedException();

        /// <summary>
        /// The get visual studio process that is running and has the specified solution loaded.
        /// </summary>
        /// <param name="solutionNames">
        /// The solution names.
        /// </param>
        /// <returns>
        /// The visual studio <see cref="Process"/> with the specified solution name.
        /// </returns>
        public static Process GetVisualStudioForSolutions(List<string> solutionNames) => null;
    }
}

#endif