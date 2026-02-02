//-----------------------------------------------------------------------------
// Filename: vp8_cx_iface.cs
//
// Description: VP8 encoder codec interface.
//              This is the encoding equivalent of vp8_dx_iface.cs
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
//
// History:
// 02 Feb 2026  Aaron Clauson  Created, Dublin, Ireland.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using System.Runtime.InteropServices;

namespace Vpx.Net
{
    /// <summary>
    /// VP8 encoder codec context.
    /// </summary>
    public unsafe class vpx_codec_cx_pkt_t
    {
        public int kind;           // Packet type
        public byte* data;         // Encoded data
        public uint sz;            // Data size
        public long pts;           // Presentation timestamp
        public uint flags;         // Flags (e.g., keyframe)
    }
    
    /// <summary>
    /// VP8 encoder interface.
    /// </summary>
    public static class vp8_cx
    {
        /// <summary>
        /// Get VP8 encoder algorithm interface.
        /// </summary>
        public static vpx_codec_iface_t vpx_codec_vp8_cx()
        {
            return new vpx_codec_iface_t
            {
                name = "WebM Project VP8 Encoder (Experimental)",
                fourcc = 0x30385056,  // "VP80"
                caps = vpx_codec_caps_t.VPX_CODEC_CAP_ENCODER,
                
                init = encoder_init,
                destroy = encoder_destroy,
                encode = encoder_encode
            };
        }
        
        /// <summary>
        /// Initialize encoder.
        /// </summary>
        private static vpx_codec_err_t encoder_init(vpx_codec_ctx_t ctx, vpx_codec_enc_cfg_t cfg)
        {
            try
            {
                // Create encoder instance
                VP8_COMP cpi = onyxe_if.vp8_create_compressor(cfg);
                
                // Store in context
                ctx.encoder_state = cpi;
                
                return vpx_codec_err_t.VPX_CODEC_OK;
            }
            catch (Exception)
            {
                return vpx_codec_err_t.VPX_CODEC_MEM_ERROR;
            }
        }
        
        /// <summary>
        /// Destroy encoder.
        /// </summary>
        private static vpx_codec_err_t encoder_destroy(vpx_codec_ctx_t ctx)
        {
            if (ctx.encoder_state is VP8_COMP cpi)
            {
                onyxe_if.vp8_remove_compressor(cpi);
                ctx.encoder_state = null;
            }
            
            return vpx_codec_err_t.VPX_CODEC_OK;
        }
        
        /// <summary>
        /// Encode a frame.
        /// </summary>
        private static unsafe vpx_codec_err_t encoder_encode(vpx_codec_ctx_t ctx, 
            vpx_image_t img, long pts, uint duration, uint flags)
        {
            if (!(ctx.encoder_state is VP8_COMP cpi))
            {
                return vpx_codec_err_t.VPX_CODEC_ERROR;
            }
            
            if (img == null)
            {
                // Flush encoder
                return vpx_codec_err_t.VPX_CODEC_OK;
            }
            
            try
            {
                // Convert image to I420 buffer
                int width = (int)img.d_w;
                int height = (int)img.d_h;
                int y_size = width * height;
                int uv_size = (width / 2) * (height / 2);
                
                byte[] source = new byte[y_size + 2 * uv_size];
                
                // Copy Y plane
                Marshal.Copy((IntPtr)img.planes[0], source, 0, y_size);
                
                // Copy U plane
                Marshal.Copy((IntPtr)img.planes[1], source, y_size, uv_size);
                
                // Copy V plane
                Marshal.Copy((IntPtr)img.planes[2], source, y_size + uv_size, uv_size);
                
                // Check if keyframe requested
                if ((flags & vpx_enc_frame_flags_t.VPX_EFLAG_FORCE_KF) != 0)
                {
                    cpi.force_next_frame_intra = true;
                }
                
                // Encode frame
                byte[] dest = cpi.output_buffer;
                int dest_length = 0;
                
                int result = onyxe_if.vp8_encode(cpi, source, source.Length, dest, ref dest_length);
                
                if (result == 0 && dest_length > 0)
                {
                    // Store encoded data in context for retrieval
                    ctx.encoded_frame_data = new byte[dest_length];
                    Array.Copy(dest, ctx.encoded_frame_data, dest_length);
                    ctx.encoded_frame_size = dest_length;
                    ctx.encoded_frame_pts = pts;
                    ctx.encoded_frame_flags = (cpi.common.frame_type == FRAME_TYPE.KEY_FRAME) ? 
                        vpx_codec_frame_flags_t.VPX_FRAME_IS_KEY : 0;
                    
                    return vpx_codec_err_t.VPX_CODEC_OK;
                }
                
                return vpx_codec_err_t.VPX_CODEC_ERROR;
            }
            catch (Exception)
            {
                return vpx_codec_err_t.VPX_CODEC_ERROR;
            }
        }
    }
    
    /// <summary>
    /// Encoder frame flags.
    /// </summary>
    public static class vpx_enc_frame_flags_t
    {
        public const uint VPX_EFLAG_FORCE_KF = 0x1;  // Force keyframe
    }
}
