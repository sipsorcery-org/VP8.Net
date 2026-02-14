//-----------------------------------------------------------------------------
// Filename: VP8EncoderUnitTest.cs
//
// Description: Unit tests for VP8 encoder.
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
//
// History:
// 14 Feb 2026  Aaron Clauson	Created, Dublin, Ireland.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using SIPSorceryMedia.Abstractions;
using Xunit;

namespace Vpx.Net.UnitTest
{
    public class VP8EncoderUnitTest
    {
        private Microsoft.Extensions.Logging.ILogger logger = null;

        public VP8EncoderUnitTest(Xunit.Abstractions.ITestOutputHelper output)
        {
            logger = TestLogger.GetLogger(output).CreateLogger(this.GetType().Name);
        }

        /// <summary>
        /// Test encoding a simple solid color frame
        /// </summary>
        [Fact]
        public void EncodeSimpleSolidColorFrame()
        {
            logger.LogDebug("---EncodeSimpleSolidColorFrame---");

            int width = 32;
            int height = 32;

            // Create a simple solid color frame (black in I420 format)
            // I420 format: Y plane (width*height) + U plane (width*height/4) + V plane (width*height/4)
            int ySize = width * height;
            int uvSize = ySize / 4;
            byte[] i420Frame = new byte[ySize + uvSize + uvSize];

            // Fill with mid-gray (Y=128, U=128, V=128)
            Array.Fill<byte>(i420Frame, 128, 0, ySize);  // Y plane
            Array.Fill<byte>(i420Frame, 128, ySize, uvSize);  // U plane
            Array.Fill<byte>(i420Frame, 128, ySize + uvSize, uvSize);  // V plane

            VP8Codec codec = new VP8Codec();

            // Force keyframe
            codec.ForceKeyFrame();

            // Encode the frame
            var encoded = codec.EncodeVideo(width, height, i420Frame, VideoPixelFormatsEnum.I420, VideoCodecsEnum.VP8);

            Assert.NotNull(encoded);
            Assert.True(encoded.Length > 0);

            logger.LogDebug($"Encoded {width}x{height} frame to {encoded.Length} bytes");
            logger.LogDebug($"Encoded frame (hex): {StrHelper.HexStr(encoded, Math.Min(100, encoded.Length))}...");
        }

        /// <summary>
        /// Test encoding and then decoding a frame to verify round-trip
        /// </summary>
        [Fact]
        public void EncodeAndDecodeFrame()
        {
            logger.LogDebug("---EncodeAndDecodeFrame---");

            int width = 64;
            int height = 64;

            // Create a simple test pattern (gradient)
            int ySize = width * height;
            int uvSize = ySize / 4;
            byte[] i420Frame = new byte[ySize + uvSize + uvSize];

            // Create a gradient pattern in Y plane
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    i420Frame[y * width + x] = (byte)((x * 255) / width);
                }
            }

            // Fill U and V with mid-gray
            Array.Fill<byte>(i420Frame, 128, ySize, uvSize);
            Array.Fill<byte>(i420Frame, 128, ySize + uvSize, uvSize);

            VP8Codec codec = new VP8Codec();
            codec.ForceKeyFrame();

            // Encode the frame
            var encoded = codec.EncodeVideo(width, height, i420Frame, VideoPixelFormatsEnum.I420, VideoCodecsEnum.VP8);

            Assert.NotNull(encoded);
            Assert.True(encoded.Length > 0);

            logger.LogDebug($"Encoded {width}x{height} frame to {encoded.Length} bytes");

            // Decode the encoded frame
            var decoded = codec.DecodeVideo(encoded, VideoPixelFormatsEnum.Bgr, VideoCodecsEnum.VP8).ToList();

            Assert.NotEmpty(decoded);
            Assert.Equal(width, (int)decoded[0].Width);
            Assert.Equal(height, (int)decoded[0].Height);
            Assert.NotNull(decoded[0].Sample);
            Assert.True(decoded[0].Sample.Length > 0);

            logger.LogDebug($"Successfully decoded frame: {decoded[0].Width}x{decoded[0].Height}, {decoded[0].Sample.Length} bytes");
        }

        /// <summary>
        /// Test encoding with actual image verification
        /// </summary>
        [Fact]
        public void EncodeAndVerifyImageQuality()
        {
            logger.LogDebug("---EncodeAndVerifyImageQuality---");

            int width = 32;
            int height = 32;

            // Create a test pattern with blocks of different colors
            int ySize = width * height;
            int uvSize = ySize / 4;
            byte[] i420Frame = new byte[ySize + uvSize + uvSize];

            // Create a checkerboard-like pattern in Y plane
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    bool isDark = ((x / 8) + (y / 8)) % 2 == 0;
                    i420Frame[y * width + x] = isDark ? (byte)64 : (byte)192;
                }
            }

            // Fill U and V with mid-gray
            Array.Fill<byte>(i420Frame, 128, ySize, uvSize);
            Array.Fill<byte>(i420Frame, 128, ySize + uvSize, uvSize);

            VP8Codec codec = new VP8Codec();
            codec.ForceKeyFrame();

            // Encode the frame
            var encoded = codec.EncodeVideo(width, height, i420Frame, VideoPixelFormatsEnum.I420, VideoCodecsEnum.VP8);

            Assert.NotNull(encoded);
            Assert.True(encoded.Length > 50, "Encoded size should be reasonable");

            logger.LogDebug($"Encoded checkerboard {width}x{height} frame to {encoded.Length} bytes");

            // Decode and verify (decoder outputs BGR format)
            var decoded = codec.DecodeVideo(encoded, VideoPixelFormatsEnum.Bgr, VideoCodecsEnum.VP8).ToList();

            Assert.NotEmpty(decoded);
            Assert.Equal(width, (int)decoded[0].Width);
            Assert.Equal(height, (int)decoded[0].Height);

            // Check that decoded BGR data has the right size (3 bytes per pixel)
            int expectedSize = width * height * 3;
            Assert.Equal(expectedSize, decoded[0].Sample.Length);

            // Verify that the pattern is somewhat preserved (allowing for lossy compression)
            byte[] decodedBgr = decoded[0].Sample;
            int matchingPixels = 0;
            int totalPixels = width * height;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    // Get original Y value
                    byte originalY = i420Frame[y * width + x];
                    
                    // Get decoded RGB values (BGR format, so R=2, G=1, B=0)
                    int pixelOffset = (y * width + x) * 3;
                    byte b = decodedBgr[pixelOffset];
                    byte g = decodedBgr[pixelOffset + 1];
                    byte r = decodedBgr[pixelOffset + 2];
                    
                    // Convert RGB to approximate Y (luma)
                    byte decodedY = (byte)((r * 0.299 + g * 0.587 + b * 0.114));
                    
                    // Allow some difference due to lossy compression and color space conversion
                    if (Math.Abs(originalY - decodedY) < 40)
                    {
                        matchingPixels++;
                    }
                }
            }

            double matchPercentage = (matchingPixels * 100.0) / totalPixels;
            logger.LogDebug($"Pixel match rate: {matchPercentage:F1}% ({matchingPixels}/{totalPixels})");

            // Require at least 60% of pixels to be reasonably close to original
            // (lossy compression + color space conversion reduces accuracy)
            Assert.True(matchPercentage > 60, $"Expected >60% pixel match, got {matchPercentage:F1}%");
        }

        /// <summary>
        /// Test encoding multiple frames
        /// </summary>
        [Fact]
        public void EncodeMultipleFrames()
        {
            logger.LogDebug("---EncodeMultipleFrames---");

            int width = 32;
            int height = 32;
            int numFrames = 5;

            VP8Codec codec = new VP8Codec();

            for (int i = 0; i < numFrames; i++)
            {
                // Create a frame with varying brightness
                int ySize = width * height;
                int uvSize = ySize / 4;
                byte[] i420Frame = new byte[ySize + uvSize + uvSize];

                byte brightness = (byte)(50 + i * 40);  // Varying brightness
                Array.Fill<byte>(i420Frame, brightness, 0, ySize);
                Array.Fill<byte>(i420Frame, 128, ySize, uvSize);
                Array.Fill<byte>(i420Frame, 128, ySize + uvSize, uvSize);

                // Force keyframe for first frame
                if (i == 0)
                {
                    codec.ForceKeyFrame();
                }

                var encoded = codec.EncodeVideo(width, height, i420Frame, VideoPixelFormatsEnum.I420, VideoCodecsEnum.VP8);

                Assert.NotNull(encoded);
                Assert.True(encoded.Length > 0);

                logger.LogDebug($"Frame {i}: Encoded to {encoded.Length} bytes");
            }
        }
    }
}
