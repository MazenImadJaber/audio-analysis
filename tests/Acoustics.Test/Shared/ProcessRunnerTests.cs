// <copyright file="ProcessRunnerTests.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace Acoustics.Test.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Acoustics.Shared;
    using Fasterflect;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using TestHelpers;

    [TestClass]
    public class ProcessRunnerTests : OutputDirectoryTest
    {
        public const string TestFile = "very_large_file_20170522-180007Z.flac";

        [TestMethod]
        public void ProcessRunnerDoesNotDeadlock()
        {
            var result = Enumerable.Range(0, 100).AsParallel().Select(this.RunFfprobe).ToArray();

            Assert.IsTrue(result.All());
        }

        [TestMethod]
        public void ProcessRunnerCanDetectNotStartedProcess()
        {
            // if we create a process runner, but dispose of it before
            // the process has started it throws an exception because
            // the dotnet process object has no reliable method for
            // checking if it started the process (without throwing
            // an exception!). We now manually track whether we start
            // the process and this test tests the tracking.
            // This state is pretty hard to get to with our public API.
            // We think we encountered this weirdness due to a race condition
            // caused by some difference between Mono on OSX and Windows.
            var runner = new ProcessRunner(AppConfigHelper.FfprobeExe);

            // use magic to set internal state to an invalid state - which is
            // only possible in a race condition, or exception unwinding.
            runner.SetFieldValue("process", new Process());

            // do not start the process, just wait a lil bit
            Thread.Sleep(0);

            // dispose would throw before bug was fixed
            runner.Dispose();
        }

        [TestMethod]
        public void ProcessRunnerSimple()
        {
            this.RunFfprobe(0);
        }

        private bool RunFfprobe(int _)
        {
            var path = PathHelper.ResolveAssetPath(TestFile);

            bool result;
            using (ProcessRunner runner = new ProcessRunner(AppConfigHelper.FfprobeExe))
            {
                runner.WaitForExitMilliseconds = 5_000;
                runner.WaitForExit = true;

                result = false;
                try
                {
                    runner.Run(
                        $@"-sexagesimal -print_format default -show_error -show_streams -show_format ""{path}""",
                        this.outputDirectory.FullName);
                    result = true;
                }
                catch
                {
                    result = false;
                }
                finally
                {
                    Assert.IsTrue(
                        runner.StandardOutput.Length > 1200,
                        $"Expected stdout to at least include ffmpeg header but it was only {runner.StandardOutput.Length} chars. StdOut:\n{runner.StandardOutput}");
                    Assert.IsTrue(
                        runner.ErrorOutput.Length > 1500,
                        $"Expected stderr to at least include ffmpeg header but it was only {runner.ErrorOutput.Length} chars. StdErr:\n{runner.ErrorOutput}");
                    Assert.AreEqual(0, runner.ExitCode);
                }
            }

            return result;
        }

        [TestMethod]
        public void ProcessRunnerTimeOutDoesNotDeadlock()
        {
            var result = Enumerable.Range(0, 100).AsParallel().Select(this.RunFfprobeIndefinite).ToArray();

            Assert.IsTrue(result.All());
        }

        [TestMethod]
        public void ProcessRunnerTimeOutSimple()
        {
            this.RunFfprobeIndefinite(0);
        }

        private bool RunFfprobeIndefinite(int _)
        {
            var path = PathHelper.ResolveAssetPath(TestFile);
            var dest = PathHelper.GetTempFile(this.outputDirectory, ".mp3");
            using (ProcessRunner runner = new ProcessRunner(AppConfigHelper.FfmpegExe))
            {
                runner.WaitForExitMilliseconds = 1000;
                runner.WaitForExit = true;
                runner.MaxRetries = 1;

                Assert.ThrowsException<ProcessRunner.ProcessMaximumRetriesException>(() =>
                {
                    runner.Run($@"-i ""{path}"" -ar 8000 ""{dest}""", this.outputDirectory.FullName);
                });

                Assert.AreEqual(0, runner.StandardOutput.Length);
                Assert.IsTrue(
                    runner.ErrorOutput.Length > 1500,
                    $"Expected stderr to at least include ffmpeg header but it was only {runner.ErrorOutput.Length} chars. StdErr:\n{runner.ErrorOutput}");

                // we're killing the program this the exit code should be invalid
                Assert.AreEqual(-1, runner.ExitCode);
            }

            return true;
        }

        [TestMethod]
        public void ProcessRunnerSetsExitCode()
        {
            string command;
            string argument;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                command = @"C:\Windows\system32\cmd.exe";
                argument = @" /C ""exit 3""";

            }
            else
            {
                command = "bash";
                argument = @" -c ""exit 3""";
            }

            using (ProcessRunner runner = new ProcessRunner(command))
            {
                runner.WaitForExitMilliseconds = 5_000;
                runner.WaitForExit = true;

                runner.Run(argument, Environment.CurrentDirectory);

                Assert.AreEqual(3, runner.ExitCode);
            }
        }
    }
}
