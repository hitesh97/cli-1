﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.InteropServices;
using Microsoft.DotNet.Tools.Build;
using FluentAssertions;
using Microsoft.DotNet.Tools.Tests.Utilities;
using Xunit;

namespace Microsoft.DotNet.Cli.MSBuild.Tests
{
    public class GivenDotnetBuildInvocation
    {
        const string ExpectedPrefix = "exec <msbuildpath> -maxcpucount -verbosity:m";

        private static readonly string WorkingDirectory = 
            TestPathUtilities.FormatAbsolutePath(nameof(GivenDotnetBuildInvocation));

        [Theory]
        [InlineData(new string[] { }, "-target:Build")]
        [InlineData(new string[] { "-o", "foo" }, "-target:Build -property:OutputPath=<cwd>foo")]
        [InlineData(new string[] { "-property:Verbosity=diag" }, "-target:Build -property:Verbosity=diag")]
        [InlineData(new string[] { "--output", "foo" }, "-target:Build -property:OutputPath=<cwd>foo")]
        [InlineData(new string[] { "-o", "foo1 foo2" }, "-target:Build \"-property:OutputPath=<cwd>foo1 foo2\"")]
        [InlineData(new string[] { "--no-incremental" }, "-target:Rebuild")]
        [InlineData(new string[] { "-r", "rid" }, "-target:Build -property:RuntimeIdentifier=rid")]
        [InlineData(new string[] { "--runtime", "rid" }, "-target:Build -property:RuntimeIdentifier=rid")]
        [InlineData(new string[] { "-c", "config" }, "-target:Build -property:Configuration=config")]
        [InlineData(new string[] { "--configuration", "config" }, "-target:Build -property:Configuration=config")]
        [InlineData(new string[] { "--version-suffix", "mysuffix" }, "-target:Build -property:VersionSuffix=mysuffix")]
        [InlineData(new string[] { "--no-dependencies" }, "-target:Build -property:BuildProjectReferences=false")]
        [InlineData(new string[] { "-v", "diag" }, "-target:Build -verbosity:diag")]
        [InlineData(new string[] { "--verbosity", "diag" }, "-target:Build -verbosity:diag")]
        [InlineData(new string[] { "--no-incremental", "-o", "myoutput", "-r", "myruntime", "-v", "diag", "/ArbitrarySwitchForMSBuild" },
                                  "-target:Rebuild -property:OutputPath=<cwd>myoutput -property:RuntimeIdentifier=myruntime -verbosity:diag /ArbitrarySwitchForMSBuild")]
        public void MsbuildInvocationIsCorrect(string[] args, string expectedAdditionalArgs)
        {
            CommandDirectoryContext.PerformActionWithBasePath(WorkingDirectory, () =>
            {
                expectedAdditionalArgs =
                    (string.IsNullOrEmpty(expectedAdditionalArgs) ? "" : $" {expectedAdditionalArgs}")
                    .Replace("<cwd>", WorkingDirectory);

                var msbuildPath = "<msbuildpath>";
                var command = BuildCommand.FromArgs(args, msbuildPath);

                command.SeparateRestoreCommand.Should().BeNull();

                command.GetProcessStartInfo()
                    .Arguments.Should()
                    .Be($"{ExpectedPrefix} -restore -consoleloggerparameters:Summary{expectedAdditionalArgs}");
            });
        }

        [Theory]
        [InlineData(new string[] { "-f", "tfm" }, "-target:Restore", "-target:Build -property:TargetFramework=tfm")]
        [InlineData(new string[] { "-o", "myoutput", "-f", "tfm", "-v", "diag", "/ArbitrarySwitchForMSBuild" },
                                  "-target:Restore -property:OutputPath=<cwd>myoutput -verbosity:diag /ArbitrarySwitchForMSBuild",
                                  "-target:Build -property:OutputPath=<cwd>myoutput -property:TargetFramework=tfm -verbosity:diag /ArbitrarySwitchForMSBuild")]
        public void MsbuildInvocationIsCorrectForSeparateRestore(
            string[] args, 
            string expectedAdditionalArgsForRestore, 
            string expectedAdditionalArgs)
        {
            CommandDirectoryContext.PerformActionWithBasePath(WorkingDirectory, () =>
            {
                expectedAdditionalArgsForRestore = expectedAdditionalArgsForRestore.Replace("<cwd>", WorkingDirectory);

                expectedAdditionalArgs = (string.IsNullOrEmpty(expectedAdditionalArgs) ? "" : $" {expectedAdditionalArgs}");
                expectedAdditionalArgs = expectedAdditionalArgs.Replace("<cwd>", WorkingDirectory);

                var msbuildPath = "<msbuildpath>";
                var command = BuildCommand.FromArgs(args, msbuildPath);

                command.SeparateRestoreCommand.GetProcessStartInfo()
                    .Arguments.Should()
                    .Be($"{ExpectedPrefix} {expectedAdditionalArgsForRestore}");

                command.GetProcessStartInfo()
                    .Arguments.Should()
                    .Be($"{ExpectedPrefix} -nologo -consoleloggerparameters:Summary{expectedAdditionalArgs}");
            });

        }
    }
}
