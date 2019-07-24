// <copyright file="Process.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace Acoustics.Shared.Platform
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Runtime.InteropServices;

    public static class ProcessHelper
    {
        /// <summary>
        /// A utility class to determine a process parent.
        /// http://stackoverflow.com/a/3346055
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct ParentProcessUtilities
        {
            // These members must match PROCESS_BASIC_INFORMATION
            internal IntPtr Reserved1;

            internal IntPtr PebBaseAddress;

            internal IntPtr Reserved2_0;

            internal IntPtr Reserved2_1;

            internal IntPtr UniqueProcessId;

            internal IntPtr InheritedFromUniqueProcessId;

            [DllImport("ntdll.dll")]
            private static extern int NtQueryInformationProcess(
                IntPtr processHandle,
                int processInformationClass,
                ref ParentProcessUtilities processInformation,
                int processInformationLength,
                out int returnLength);

            /// <summary>
            /// Gets the parent process of the current process.
            /// </summary>
            /// <returns>An instance of the Process class.</returns>
            public static Process GetParentProcess()
            {
                return GetParentProcess(Process.GetCurrentProcess().Handle);
            }

            /// <summary>
            /// Gets the parent process of specified process.
            /// </summary>
            /// <param name="id">The process id.</param>
            /// <returns>An instance of the Process class.</returns>
            public static Process GetParentProcess(int id)
            {
                Process process = Process.GetProcessById(id);
                return GetParentProcess(process.Handle);
            }

            /// <summary>
            /// Gets the parent process of a specified process.
            /// </summary>
            /// <remarks>
            /// TODO CORE: There will be cross-platform version of this .NET Core 5.
            /// </remarks>
            /// <param name="handle">The process handle.</param>
            /// <returns>An instance of the Process class.</returns>
            public static Process GetParentProcess(IntPtr handle)
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return null;
                }

                var pbi = default(ParentProcessUtilities);
                int status =
                    NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out var returnLength);
                if (status != 0)
                {
                    throw new Win32Exception(status);
                }

                try
                {
                    return Process.GetProcessById(pbi.InheritedFromUniqueProcessId.ToInt32());
                }
                catch (ArgumentException)
                {
                    // not found
                    return null;
                }
            }
        }
    }
}
