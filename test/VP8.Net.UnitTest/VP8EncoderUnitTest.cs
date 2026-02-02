//-----------------------------------------------------------------------------
// Filename: VP8EncoderUnitTest.cs
//
// Description: Unit tests for VP8 encoder architecture and interfaces.
//
// Author(s):
// GitHub Copilot
//
// History:
// 02 Feb 2026  GitHub Copilot  Created.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using Xunit;
using Vpx.Net;
using System;

namespace VP8.Net.UnitTest
{
    /// <summary>
    /// Basic tests for VP8 encoder infrastructure.
    /// Note: Full encoding functionality is not yet implemented.
    /// </summary>
    public class VP8EncoderUnitTest
    {
        [Fact]
        public void EncoderInterfaceCanBeCreated()
        {
            // Test that the encoder interface can be instantiated
            var encoderIface = vp8_cx_simple.vpx_codec_vp8_cx();
            
            Assert.NotNull(encoderIface);
            Assert.Equal("WebM Project VP8 Encoder (Simple/Experimental)", encoderIface.name);
            Assert.NotNull(encoderIface.init);
            Assert.NotNull(encoderIface.destroy);
            Assert.NotNull(encoderIface.encode);
        }
        
        [Fact]
        public void EncoderHasCorrectCapabilities()
        {
            var encoderIface = vp8_cx_simple.vpx_codec_vp8_cx();
            
            // Check that encoder capability flag is set
            Assert.True((encoderIface.caps & vpx_codec.VPX_CODEC_CAP_ENCODER) != 0);
        }
        
        [Fact]
        public void EncoderInitializationSucceeds()
        {
            var encoderIface = vp8_cx_simple.vpx_codec_vp8_cx();
            var ctx = new vpx_codec_ctx_t();
            
            // Test that initialization completes without error
            var result = encoderIface.init(ctx, null);
            
            Assert.Equal(vpx_codec_err_t.VPX_CODEC_OK, result);
        }
        
        [Fact]
        public void EncoderDestroySucceeds()
        {
            var encoderIface = vp8_cx_simple.vpx_codec_vp8_cx();
            var priv = new vpx_codec_alg_priv_t();
            
            // Test that cleanup completes without error
            var result = encoderIface.destroy(priv);
            
            Assert.Equal(vpx_codec_err_t.VPX_CODEC_OK, result);
        }
        
        [Fact]
        public void EncoderReturnsIncapableForEncoding()
        {
            // Since encoding is not fully implemented, verify it returns the expected error
            var encoderIface = vp8_cx_simple.vpx_codec_vp8_cx();
            var ctx = new vpx_codec_ctx_t();
            var img = new vpx_image_t();
            
            var result = encoderIface.encode(ctx, img, 0, 1, 0);
            
            // Should return INCAPABLE since encoding is not yet implemented
            Assert.Equal(vpx_codec_err_t.VPX_CODEC_INCAPABLE, result);
        }
        
        [Fact]
        public void EntropyModuleHasTokenConstants()
        {
            // Verify that token constants are defined
            Assert.Equal(0, entropy.ZERO_TOKEN);
            Assert.Equal(1, entropy.ONE_TOKEN);
            Assert.Equal(2, entropy.TWO_TOKEN);
            Assert.Equal(11, entropy.DCT_EOB_TOKEN);
            Assert.Equal(12, entropy.MAX_ENTROPY_TOKENS);
        }
        
        [Fact]
        public void EntropyModuleHasCoefficientBands()
        {
            // Verify coefficient band mapping exists
            Assert.NotNull(entropy.vp8_coef_bands);
            Assert.Equal(16, entropy.vp8_coef_bands.Length);
        }
        
        [Fact]
        public void VP8CodecThrowsNotImplementedForEncoding()
        {
            // Verify that VP8Codec correctly indicates encoding is not yet complete
            var codec = new VP8Codec();
            var sample = new byte[640 * 480 * 3 / 2];  // I420 format
            
            var ex = Assert.Throws<NotImplementedException>(() =>
            {
                codec.EncodeVideo(640, 480, sample, 
                    SIPSorceryMedia.Abstractions.VideoPixelFormatsEnum.I420, 
                    SIPSorceryMedia.Abstractions.VideoCodecsEnum.VP8);
            });
            
            Assert.Contains("under development", ex.Message);
        }
    }
}
