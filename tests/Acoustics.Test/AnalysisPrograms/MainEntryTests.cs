// <copyright file="MainEntryTests.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace Acoustics.Test.AnalysisPrograms
{
    using System;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Accord.Math.Optimization;
    using Acoustics.Shared;
    using Acoustics.Test.TestHelpers;
    using global::AnalysisPrograms;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [DoNotParallelize]
    public class MainEntryTests
    {
        [DoNotParallelize]
        [TestMethod]
        public async Task DefaultCliWorks()
        {
            using (var console = new ConsoleRedirector())
            {
                var code = await MainEntry.Main(Array.Empty<string>());

                Assert.AreEqual(2, code);

                this.AssertContainsCopyright(console.Lines);
                this.AssertContainsGitHashAndVersion(console.Lines);
            }
        }

        [DoNotParallelize]
        [TestMethod]
        public async Task DefaultHelpWorks()
        {
            using (var console = new ConsoleRedirector())
            {
                var code = await MainEntry.Main(new[] { "--help" });

                Assert.AreEqual(0, code);

                this.AssertContainsCopyright(console.Lines);
                this.AssertContainsGitHashAndVersion(console.Lines);
                StringAssert.StartsWith(console.Lines[6], Meta.Description);
            }
        }

        [DoNotParallelize]
        [TestMethod]
        public async Task DefaultVersionWorks()
        {
            using (var console = new ConsoleRedirector())
            {
                var code = await MainEntry.Main(new[] { "--version" });

                Assert.AreEqual(0, code);

                this.AssertContainsCopyright(console.Lines);
                this.AssertContainsGitHashAndVersion(console.Lines);
                StringAssert.StartsWith(console.Lines[3], BuildMetadata.VersionString);
            }
        }

        [DoNotParallelize]
        [TestMethod]
        public async Task CheckEnvironmentWorks()
        {
            using (var console = new ConsoleRedirector())
            {
                var code = await MainEntry.Main(new[] { "CheckEnvironment" });

                Assert.AreEqual(0, code);

                this.AssertContainsCopyright(console.Lines);
                this.AssertContainsGitHashAndVersion(console.Lines);
                StringAssert.Contains(console.Lines[4], "SUCCESS - Valid environment");
            }
        }

        [TestMethod]
        public void OptionClusteringIsDisabled()
        {
            var args = new[]
            {
                "ConcatenateIndexFiles",
                PathHelper.ResolveAssetPath("Concatenation"),
                "-o:null",

                // with option clustering enabled the -fcs actually means -f -c -s
                "-fcs",
                "foo.yml",

                // which conflicts with the -f argument here
                "-f",
                "blah",
            };

            var app = MainEntry.CreateCommandLineApplication();

            // before the code for this test was fixed an exception was thrown on the following line
            // `McMaster.Extensions.CommandLineUtils.CommandParsingException: Unexpected value 'blah' for option 'f'`
            var parseResult = app.Parse(args);

            Assert.AreEqual("blah", parseResult.SelectedCommand.Options.Single(x => x.LongName == "file-stem-name").Value());
            Assert.AreEqual("foo.yml", parseResult.SelectedCommand.Options.Single(x => x.LongName == "false-colour-spectrogram-config").Value());
            Assert.IsFalse(parseResult.SelectedCommand.ClusterOptions);
        }

        [TestMethod]
        public void HelpPagingIsDisabled()
        {
            var app = MainEntry.CreateCommandLineApplication();

            Assert.IsFalse(app.UsePagerForHelpText);
        }

        private void AssertContainsCopyright(ReadOnlyCollection<string> lines)
        {
            // copyright always on third line
            var expected = $"Copyright {DateTime.Now.Year} QUT";
            Assert.That.StringEqualWithDiff(expected, lines[2]);
        }

        private void AssertContainsGitHashAndVersion(ReadOnlyCollection<string> lines)
        {
            StringAssert.StartsWith(lines[0], Meta.Description);
            StringAssert.Contains(lines[0], BuildMetadata.VersionString);
            StringAssert.Contains(lines[0], BuildMetadata.BuildDate);
            StringAssert.Contains(lines[1], BuildMetadata.GitBranch);
            StringAssert.Contains(lines[1], BuildMetadata.GitCommit);
        }
    }
}
