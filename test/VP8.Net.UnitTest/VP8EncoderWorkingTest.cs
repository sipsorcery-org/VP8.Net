using Xunit;
using Vpx.Net;
using System;

namespace VP8.Net.UnitTest
{
    /// <summary>
    /// Tests for the working VP8 encoder.
    /// </summary>
    public class VP8EncoderWorkingTest
    {
        [Fact]
        public void EncoderCanEncodeSimpleFrame()
        {
            // Create a simple test frame (64x64, solid gray)
            int width = 64;
            int height = 64;
            
            // I420 format: Y plane + U plane + V plane
            int ySize = width * height;
            int uvSize = (width / 2) * (height / 2);
            byte[] frame = new byte[ySize + 2 * uvSize];
            
            // Fill with gray (128)
            for (int i = 0; i < ySize; i++)
                frame[i] = 128;
            for (int i = ySize; i < frame.Length; i++)
                frame[i] = 128;
            
            // Create encoder
            var encoder = new VP8Encoder(width, height);
            encoder.SetQuantizer(10);
            
            // Encode frame
            byte[] encoded = encoder.EncodeFrame(frame, true);
            
            // Verify we got output
            Assert.NotNull(encoded);
            Assert.True(encoded.Length > 0);
            Assert.True(encoded.Length < frame.Length);  // Should be compressed
            
            // Verify frame header (keyframe)
            Assert.Equal(0, encoded[0] & 0x01);  // Bit 0 should be 0 for keyframe
            
            // Verify start code for keyframe (0x9d012a at bytes 3-5)
            Assert.Equal(0x9d, encoded[3]);
            Assert.Equal(0x01, encoded[4]);
            Assert.Equal(0x2a, encoded[5]);
        }
        
        [Fact]
        public void EncoderCanEncodeAndDecodeFrame()
        {
            // Create a test pattern (64x64)
            int width = 64;
            int height = 64;
            
            int ySize = width * height;
            int uvSize = (width / 2) * (height / 2);
            byte[] frame = new byte[ySize + 2 * uvSize];
            
            // Create a simple checkerboard pattern in Y plane
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    frame[y * width + x] = (byte)(((x / 8 + y / 8) % 2) * 255);
                }
            }
            
            // Fill UV planes with neutral gray
            for (int i = ySize; i < frame.Length; i++)
                frame[i] = 128;
            
            // Encode
            var encoder = new VP8Encoder(width, height);
            encoder.SetQuantizer(10);
            byte[] encoded = encoder.EncodeFrame(frame, true);
            
            Assert.NotNull(encoded);
            Assert.True(encoded.Length > 10);
            
            // Try to decode with existing decoder
            var decoder = new vpx_codec_ctx_t();
            vpx_codec_iface_t algo = vp8_dx.vpx_codec_vp8_dx();
            vpx_codec_dec_cfg_t cfg = new vpx_codec_dec_cfg_t { threads = 1 };
            vpx_codec_err_t res = vpx_decoder.vpx_codec_dec_init(decoder, algo, cfg, 0);
            
            Assert.Equal(vpx_codec_err_t.VPX_CODEC_OK, res);
            
            unsafe
            {
                fixed (byte* pFrame = encoded)
                {
                    var result = vpx_decoder.vpx_codec_decode(decoder, pFrame, (uint)encoded.Length, IntPtr.Zero, 0);
                    Assert.Equal(vpx_codec_err_t.VPX_CODEC_OK, result);
                }
            }
            
            // Get decoded frame
            IntPtr iter = IntPtr.Zero;
            var img = vpx_decoder.vpx_codec_get_frame(decoder, iter);
            
            Assert.NotNull(img);
            Assert.Equal((uint)width, img.d_w);
            Assert.Equal((uint)height, img.d_h);
        }
        
        [Fact]
        public void EncoderWithDifferentQuantizers()
        {
            // Test that different quantizers produce different sizes
            int width = 64;
            int height = 64;
            
            int ySize = width * height;
            int uvSize = (width / 2) * (height / 2);
            byte[] frame = new byte[ySize + 2 * uvSize];
            
            // Create a complex pattern
            Random rnd = new Random(42);
            rnd.NextBytes(frame);
            
            // Encode with low quantizer (high quality)
            var encoder1 = new VP8Encoder(width, height);
            encoder1.SetQuantizer(5);
            byte[] encoded1 = encoder1.EncodeFrame(frame, true);
            
            // Encode with high quantizer (low quality)
            var encoder2 = new VP8Encoder(width, height);
            encoder2.SetQuantizer(50);
            byte[] encoded2 = encoder2.EncodeFrame(frame, true);
            
            // High quantizer should produce smaller file
            Assert.True(encoded2.Length < encoded1.Length);
        }
        
        [Fact]
        public void EncoderHandlesDifferentResolutions()
        {
            // Test various resolutions
            int[][] resolutions = new int[][]
            {
                new int[] { 32, 32 },
                new int[] { 64, 64 },
                new int[] { 128, 128 },
                new int[] { 176, 144 },  // QCIF
                new int[] { 320, 240 },  // QVGA
            };
            
            foreach (var res in resolutions)
            {
                int width = res[0];
                int height = res[1];
                
                int ySize = width * height;
                int uvSize = (width / 2) * (height / 2);
                byte[] frame = new byte[ySize + 2 * uvSize];
                
                // Fill with solid color
                for (int i = 0; i < frame.Length; i++)
                    frame[i] = 128;
                
                var encoder = new VP8Encoder(width, height);
                byte[] encoded = encoder.EncodeFrame(frame, true);
                
                Assert.NotNull(encoded);
                Assert.True(encoded.Length > 0);
                
                // Verify dimensions in header
                int w = encoded[6] | ((encoded[7] & 0x3F) << 8);
                int h = encoded[8] | ((encoded[9] & 0x3F) << 8);
                Assert.Equal(width - 1, w);
                Assert.Equal(height - 1, h);
            }
        }
    }
}
