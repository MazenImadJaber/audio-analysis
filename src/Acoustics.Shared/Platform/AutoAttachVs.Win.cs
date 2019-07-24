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
    using EnvDTE;
    using DTEProcess = EnvDTE.Process;
    using Process = System.Diagnostics.Process;

    /// <summary>
    /// Example taken from <a href="https://gist.github.com/3813175">this gist</a>.
    /// </summary>
    public static partial class VisualStudioAttacher
    {
        [DllImport("ole32.dll")]
        public static extern int CreateBindCtx(int reserved, out IBindCtx ppbc);

        [DllImport("ole32.dll")]
        public static extern int GetRunningObjectTable(int reserved, out IRunningObjectTable prot);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetFocus(IntPtr hWnd);

        public static string GetSolutionForVisualStudio(Process visualStudioProcess)
        {
            if (TryGetVsInstance(visualStudioProcess.Id, out var visualStudioInstance))
            {
                try
                {
                    return visualStudioInstance.Solution.FullName;
                }
                catch (Exception)
                {
                }
            }

            return null;
        }

        public static Process GetAttachedVisualStudio(Process applicationProcess)
        {
            var visualStudios = GetVisualStudioProcesses();

            foreach (var visualStudio in visualStudios)
            {
                if (TryGetVsInstance(visualStudio.Id, out var visualStudioInstance))
                {
                    try
                    {
                        foreach (Process debuggedProcess in visualStudioInstance.Debugger.DebuggedProcesses)
                        {
                            if (debuggedProcess.Id == applicationProcess.Id)
                            {
                                return debuggedProcess;
                            }
                        }
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            return null;
        }

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
        public static void AttachVisualStudioToProcess(Process visualStudioProcess, Process applicationProcess)
        {
            if (TryGetVsInstance(visualStudioProcess.Id, out var visualStudioInstance))
            {
                // Find the process you want the VS instance to attach to...
                var processToAttachTo =
                    visualStudioInstance.Debugger.LocalProcesses.Cast<DTEProcess>()
                        .FirstOrDefault(process => process.ProcessID == applicationProcess.Id);

                // Attach to the process.
                if (processToAttachTo != null)
                {
                    processToAttachTo.Attach();

                    ShowWindow((int)visualStudioProcess.MainWindowHandle, 3);
                    SetForegroundWindow(visualStudioProcess.MainWindowHandle);
                }
                else
                {
                    throw new InvalidOperationException(
                        "Visual Studio process cannot find specified application '" + applicationProcess.Id + "'");
                }
            }
        }

        /// <summary>
        /// The get visual studio process that is running and has the specified solution loaded.
        /// </summary>
        /// <param name="solutionNames">
        /// The solution names.
        /// </param>
        /// <returns>
        /// The visual studio <see cref="Process"/> with the specified solution name.
        /// </returns>
        public static Process GetVisualStudioForSolutions(List<string> solutionNames)
        {
            var visualStudios = GetVisualStudioProcesses();

            foreach (var visualStudio in visualStudios)
            {
                if (TryGetVsInstance(visualStudio.Id, out var visualStudioInstance))
                {
                    var actualSolutionName = Path.GetFileName(visualStudioInstance.Solution.FullName);

                    foreach (var solutionName in solutionNames)
                    {
                        if (string.Compare(
                                actualSolutionName,
                                solutionName,
                                StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            return visualStudio;
                        }
                    }
                }
            }

            return null;
        }

        [DllImport("User32")]
        private static extern int ShowWindow(int hwnd, int nCmdShow);

        private static IEnumerable<Process> GetVisualStudioProcesses()
        {
            var processes = Process.GetProcesses();
            return processes.Where(o => o.ProcessName.Contains("devenv"));
        }

        private static bool TryGetVsInstance(int processId, out _DTE instance)
        {
            var numFetched = IntPtr.Zero;
            var monikers = new IMoniker[1];

            GetRunningObjectTable(0, out var runningObjectTable);
            runningObjectTable.EnumRunning(out var monikerEnumerator);
            monikerEnumerator.Reset();

            while (monikerEnumerator.Next(1, monikers, numFetched) == 0)
            {
                CreateBindCtx(0, out var ctx);

                monikers[0].GetDisplayName(ctx, null, out var runningObjectName);

                runningObjectTable.GetObject(monikers[0], out var runningObjectVal);

                if (runningObjectVal is _DTE dte && runningObjectName.StartsWith("!VisualStudio"))
                {
                    var currentProcessId = int.Parse(runningObjectName.Split(':')[1]);

                    if (currentProcessId == processId)
                    {
                        instance = dte;
                        return true;
                    }
                }
            }

            instance = null;
            return false;
        }
    }
}

#endif