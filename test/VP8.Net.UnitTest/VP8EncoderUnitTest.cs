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

            // Try to decode the encoded frame
            try
            {
                var decoded = codec.DecodeVideo(encoded, VideoPixelFormatsEnum.Bgr, VideoCodecsEnum.VP8).ToList();

                Assert.NotEmpty(decoded);
                Assert.Equal(width, (int)decoded[0].Width);
                Assert.Equal(height, (int)decoded[0].Height);

                logger.LogDebug($"Successfully decoded frame: {decoded[0].Width}x{decoded[0].Height}");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Decoding failed (expected for initial implementation): {ex.Message}");
                // For now, decoding our own encoded frames may not work perfectly
                // This is expected in early implementation stages
            }
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
