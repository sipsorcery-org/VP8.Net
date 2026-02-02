//-----------------------------------------------------------------------------
// Filename: onyxe_if.cs
//
// Description: VP8 encoder interface functions.
//              This is the encoding equivalent of onyxd_if.cs
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

namespace Vpx.Net
{
    /// <summary>
    /// VP8 encoder interface.
    /// </summary>
    public static class onyxe_if
    {
        /// <summary>
        /// Create a new VP8 encoder instance.
        /// </summary>
        public static VP8_COMP vp8_create_compressor(vpx_codec_enc_cfg_t oxcf)
        {
            VP8_COMP cpi = new VP8_COMP();
            VP8_COMMON cm = cpi.common;
            
            // Copy configuration
            cpi.oxcf = oxcf;
            
            // Set dimensions
            cm.Width = oxcf.g_w;
            cm.Height = oxcf.g_h;
            cm.mb_rows = (int)((cm.Height + 15) / 16);
            cm.mb_cols = (int)((cm.Width + 15) / 16);
            cm.MBs = cm.mb_rows * cm.mb_cols;
            
            // Initialize quantization
            cpi.base_qindex = 60;  // Default Q index
            cm.base_qindex = 60;
            
            // Initialize filter level
            cm.filter_level = 10;
            
            // Initialize frame count
            cpi.frame_count = 0;
            cpi.key_frame_frequency = (int)oxcf.kf_max_dist;
            if (cpi.key_frame_frequency == 0)
            {
                cpi.key_frame_frequency = 30;  // Default: keyframe every 30 frames
            }
            
            // Allocate output buffer
            cpi.output_buffer_size = (int)(cm.Width * cm.Height * 2);  // Generous size
            cpi.output_buffer = new byte[cpi.output_buffer_size];
            
            // Allocate frame buffers
            alloccommon.vp8_alloc_frame_buffers(cm, (int)cm.Width, (int)cm.Height);
            
            // Initialize macroblock structure
            cpi.mb.e_mbd.mode_info_context = new ArrPtr<MODE_INFO>(new MODE_INFO[cm.MBs], 0);
            
            return cpi;
        }
        
        /// <summary>
        /// Encode a frame.
        /// </summary>
        public static unsafe int vp8_encode(VP8_COMP cpi, byte[] source, int source_length, 
            byte[] dest, ref int dest_length)
        {
            VP8_COMMON cm = cpi.common;
            
            // Determine if this should be a keyframe
            bool key_frame = cpi.force_next_frame_intra || 
                           (cpi.frame_count % cpi.key_frame_frequency == 0);
            
            if (key_frame)
            {
                cm.frame_type = FRAME_TYPE.KEY_FRAME;
                cpi.force_next_frame_intra = false;
            }
            else
            {
                cm.frame_type = FRAME_TYPE.INTER_FRAME;
            }
            
            // Copy source data to frame buffer
            copy_source_to_buffer(cm, source, (int)cm.Width, (int)cm.Height);
            
            // Encode frame
            fixed (byte* pDest = dest)
            {
                // Initialize boolean encoder for frame data
                boolhuff.vp8_start_encode(ref cpi.bc, dest, dest.Length);
                
                // Write frame header
                bitstream.write_frame_header(ref cpi.bc, cm, key_frame);
                
                // Encode frame data
                encodeframe.vp8_encode_frame(cpi);
                
                // Flush boolean encoder
                boolhuff.vp8_stop_encode(ref cpi.bc);
                
                // Get encoded size
                dest_length = (int)cpi.bc.pos;
            }
            
            cpi.frame_count++;
            
            return 0;  // Success
        }
        
        /// <summary>
        /// Copy source image data to encoder buffer.
        /// </summary>
        private static unsafe void copy_source_to_buffer(VP8_COMMON cm, byte[] source, int width, int height)
        {
            // Assume source is in I420 format: Y, U, V planes
            int y_size = width * height;
            int uv_size = (width / 2) * (height / 2);
            
            // Copy Y plane
            if (cm.yv12_fb[cm.new_fb_idx].y_buffer != null)
            {
                fixed (byte* pSrc = source)
                {
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            cm.yv12_fb[cm.new_fb_idx].y_buffer[i * cm.yv12_fb[cm.new_fb_idx].y_stride + j] = 
                                pSrc[i * width + j];
                        }
                    }
                }
            }
            
            // Copy U plane
            if (cm.yv12_fb[cm.new_fb_idx].u_buffer != null)
            {
                fixed (byte* pSrc = source)
                {
                    for (int i = 0; i < height / 2; i++)
                    {
                        for (int j = 0; j < width / 2; j++)
                        {
                            cm.yv12_fb[cm.new_fb_idx].u_buffer[i * cm.yv12_fb[cm.new_fb_idx].uv_stride + j] = 
                                pSrc[y_size + i * (width / 2) + j];
                        }
                    }
                }
            }
            
            // Copy V plane
            if (cm.yv12_fb[cm.new_fb_idx].v_buffer != null)
            {
                fixed (byte* pSrc = source)
                {
                    for (int i = 0; i < height / 2; i++)
                    {
                        for (int j = 0; j < width / 2; j++)
                        {
                            cm.yv12_fb[cm.new_fb_idx].v_buffer[i * cm.yv12_fb[cm.new_fb_idx].uv_stride + j] = 
                                pSrc[y_size + uv_size + i * (width / 2) + j];
                        }
                    }
                }
            }
        }
        
        /// <summary>
        /// Destroy encoder instance.
        /// </summary>
        public static void vp8_remove_compressor(VP8_COMP cpi)
        {
            if (cpi != null)
            {
                // Free resources
                cpi.output_buffer = null;
            }
        }
    }
}
