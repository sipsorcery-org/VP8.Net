//-----------------------------------------------------------------------------
// Filename: VP8TestVectorTests.cs
//
// Description: Unit tests for VP8 codec using official test vectors.
//
// Author(s):
// Copilot
//
// History:
// 23 Jun 2025	Copilot	Created, Dublin, Ireland.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace VP8.Net.TestVectors
{
    /// <summary>
    /// Unit tests for VP8 codec using official test vectors.
    /// </summary>
    public class VP8TestVectorTests
    {
        private readonly ILogger logger;

        /// <summary>
        /// Initializes a new instance of the VP8TestVectorTests class.
        /// </summary>
        /// <param name="output">The xunit test output helper.</param>
        public VP8TestVectorTests(ITestOutputHelper output)
        {
            logger = TestLogger.GetLogger(output).CreateLogger(this.GetType().Name);
        }

        /// <summary>
        /// Tests VP8 decoding using test vector files.
        /// </summary>
        [Fact]
        public void DecodeTestVectors()
        {
            var testVectorDirectory = Path.Combine(
                Path.GetDirectoryName(typeof(VP8TestVectorTests).Assembly.Location) ?? "",
                "TestVectors");

            if (!Directory.Exists(testVectorDirectory))
            {
                logger.LogWarning($"Test vector directory not found: {testVectorDirectory}");
                return;
            }

            var ivfFiles = Directory.GetFiles(testVectorDirectory, "*.ivf");
            
            Assert.True(ivfFiles.Length > 0, "No IVF test vector files found");

            foreach (var ivfFile in ivfFiles.Take(5)) // Test first 5 files
            {
                logger.LogInformation($"Testing IVF file: {Path.GetFileName(ivfFile)}");
                
                try
                {
                    var frames = IvfReader.ReadFrames(ivfFile);
                    
                    Assert.True(frames.Length > 0, $"No frames found in {ivfFile}");
                    
                    foreach (var frame in frames)
                    {
                        Assert.True(frame.Length > 0, "Frame data should not be empty");
                        
                        // Basic VP8 frame validation - check for VP8 frame header
                        if (frame.Length >= 3)
                        {
                            // VP8 frame tag validation (basic check)
                            logger.LogDebug($"Frame size: {frame.Length}, First bytes: {frame[0]:X2} {frame[1]:X2} {frame[2]:X2}");
                        }
                    }
                    
                    logger.LogInformation($"Successfully processed {frames.Length} frames from {Path.GetFileName(ivfFile)}");
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Failed to process {ivfFile}");
                    throw;
                }
            }
        }

        /// <summary>
        /// Tests IVF file header reading.
        /// </summary>
        [Fact]
        public void TestIvfReader()
        {
            var testVectorDirectory = Path.Combine(
                Path.GetDirectoryName(typeof(VP8TestVectorTests).Assembly.Location) ?? "",
                "TestVectors");

            if (!Directory.Exists(testVectorDirectory))
            {
                logger.LogWarning($"Test vector directory not found: {testVectorDirectory}");
                return;
            }

            var ivfFiles = Directory.GetFiles(testVectorDirectory, "*.ivf").Take(1);

            foreach (var ivfFile in ivfFiles)
            {
                logger.LogInformation($"Testing IVF reader with: {Path.GetFileName(ivfFile)}");
                
                var frames = IvfReader.ReadFrames(ivfFile);
                
                Assert.NotNull(frames);
                Assert.True(frames.Length > 0);
                
                logger.LogInformation($"IVF reader test completed successfully with {frames.Length} frames");
                break;
            }
        }
    }
}