//-----------------------------------------------------------------------------
// Filename: vp8_cx_iface.cs
//
// Description: VP8 encoder interface
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

/*
 *  Copyright (c) 2010 The WebM project authors. All Rights Reserved.
 *
 *  Use of this source code is governed by a BSD-style license
 *  that can be found in the LICENSE file in the root of the source
 *  tree. An additional intellectual property rights grant can be found
 *  in the file PATENTS.  All contributing project authors may
 *  be found in the AUTHORS file in the root of the source tree.
 */

using System;

namespace Vpx.Net
{
    /// <summary>
    /// VP8 Encoder context
    /// </summary>
    public unsafe class VP8E_COMP
    {
        public VP8_COMMON common;           // Common encoder/decoder structures
        public vpx_codec_enc_cfg_t config;  // Encoder configuration
        public bool force_next_keyframe;    // Force next frame to be keyframe
        public uint frame_count;            // Number of frames encoded
        public byte[] compressed_buffer;    // Output buffer
        public int buffer_level;            // Rate control - buffer level
        
        public VP8E_COMP()
        {
            common = new VP8_COMMON();
            config = new vpx_codec_enc_cfg_t();
            compressed_buffer = new byte[1024 * 1024];  // 1MB buffer
        }
    }

    /// <summary>
    /// VP8 encoder interface functions
    /// </summary>
    public unsafe static class vp8_cx_iface
    {
        /// <summary>
        /// Initialize encoder with default configuration
        /// </summary>
        public static vpx_codec_err_t vp8e_init(VP8E_COMP ctx, uint width, uint height)
        {
            ctx.common.Width = (int)width;
            ctx.common.Height = (int)height;
            ctx.common.mb_rows = ((int)height + 15) / 16;
            ctx.common.mb_cols = ((int)width + 15) / 16;
            ctx.frame_count = 0;
            ctx.force_next_keyframe = true;  // First frame is keyframe
            
            // Initialize quantization tables
            vp8_init_quant_tables(ctx);
            
            return vpx_codec_err_t.VPX_CODEC_OK;
        }

        /// <summary>
        /// Initialize quantization tables
        /// </summary>
        private static void vp8_init_quant_tables(VP8E_COMP ctx)
        {
            // Use default quantization index (mid-range quality)
            int qindex = 63;  // Range is 0-127, 63 is middle
            
            // Store quantization index in common context
            ctx.common.base_qindex = qindex;
            
            // Initialize quantizer deltas
            ctx.common.y1dc_delta_q = 0;
            ctx.common.y2dc_delta_q = 0;
            ctx.common.y2ac_delta_q = 0;
            ctx.common.uvdc_delta_q = 0;
            ctx.common.uvac_delta_q = 0;
        }

        /// <summary>
        /// Encode a single frame
        /// </summary>
        public static vpx_codec_err_t vp8e_encode_frame(VP8E_COMP ctx, vpx_image_t img, 
            out byte[] compressed, out uint compressed_size)
        {
            compressed = null;
            compressed_size = 0;

            try
            {
                bool is_keyframe = ctx.force_next_keyframe || (ctx.frame_count == 0);
                
                // For now, always encode as keyframe (inter-frame support not implemented yet)
                is_keyframe = true;
                
                if (is_keyframe)
                {
                    // Encode as keyframe
                    return vp8e_encode_keyframe(ctx, img, out compressed, out compressed_size);
                }
                else
                {
                    // For now, only keyframe encoding is supported
                    return vpx_codec_err_t.VPX_CODEC_INCAPABLE;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encoding error: {ex.Message}");
                return vpx_codec_err_t.VPX_CODEC_ERROR;
            }
        }

        /// <summary>
        /// Encode a keyframe (I-frame with intra prediction only)
        /// </summary>
        private static vpx_codec_err_t vp8e_encode_keyframe(VP8E_COMP ctx, vpx_image_t img,
            out byte[] compressed, out uint compressed_size)
        {
            compressed = null;
            compressed_size = 0;

            // Allocate output buffer
            byte[] output = new byte[ctx.compressed_buffer.Length];

            fixed (byte* output_ptr = output)
            {
                // Write uncompressed frame header first (10 bytes for keyframe)
                int header_pos = 0;
                
                // Frame tag: 3 bytes (includes first partition size, will be filled at end)
                uint frame_tag = 0;
                frame_tag |= 0;  // P=0 for keyframe
                frame_tag |= (0 << 1);  // version = 0
                frame_tag |= (1 << 4);  // show_frame = 1
                // Bits 5-23 will be first partition size (set later)
                output_ptr[header_pos++] = (byte)(frame_tag & 0xFF);
                output_ptr[header_pos++] = (byte)((frame_tag >> 8) & 0xFF);
                output_ptr[header_pos++] = (byte)((frame_tag >> 16) & 0xFF);

                // Start code: 0x9D 0x01 0x2A
                output_ptr[header_pos++] = 0x9D;
                output_ptr[header_pos++] = 0x01;
                output_ptr[header_pos++] = 0x2A;

                // Width and height (14 bits each, with 2 bits scale)
                uint width = (uint)ctx.common.Width;
                uint height = (uint)ctx.common.Height;
                output_ptr[header_pos++] = (byte)(width & 0xFF);
                output_ptr[header_pos++] = (byte)((width >> 8) & 0x3F);  // Upper 6 bits of width, lower 2 bits are scale
                output_ptr[header_pos++] = (byte)(height & 0xFF);
                output_ptr[header_pos++] = (byte)((height >> 8) & 0x3F);  // Upper 6 bits of height, lower 2 bits are scale

                // Initialize boolean encoder for compressed data
                BOOL_CODER bc = new BOOL_CODER();
                boolhuff.vp8_start_encode(ref bc, output_ptr + header_pos, output_ptr + output.Length);

                // Write compressed frame header
                // Colorspace (1 bit) - 0 for normal colorspace
                boolhuff.vp8_encode_bool(ref bc, 0, 128);
                
                // Clamping type (1 bit) - 0 for no clamping
                boolhuff.vp8_encode_bool(ref bc, 0, 128);

                // Segmentation enabled (1 bit) - 0 for disabled
                boolhuff.vp8_encode_bool(ref bc, 0, 128);

                // Filter type (1 bit) - 0 for normal filter
                boolhuff.vp8_encode_bool(ref bc, 0, 128);

                // Loop filter level (6 bits) - 0 for no loop filter
                boolhuff.vp8_encode_value(ref bc, 0, 6);

                // Sharpness level (3 bits) - 0
                boolhuff.vp8_encode_value(ref bc, 0, 3);

                // MB loop filter adjustments enabled (1 bit) - 0 for disabled
                boolhuff.vp8_encode_bool(ref bc, 0, 128);

                // log2_nbr_of_dct_partitions (2 bits) - 0 for 1 partition
                boolhuff.vp8_encode_value(ref bc, 0, 2);

                // Base Q index (7 bits)
                int qindex = ctx.common.base_qindex;
                boolhuff.vp8_encode_value(ref bc, qindex, 7);

                // Y1 DC delta Q (1 bit update flag + 4 bits + 1 sign bit if updated)
                boolhuff.vp8_encode_bool(ref bc, 0, 128);  // No delta

                // Y2 DC delta Q
                boolhuff.vp8_encode_bool(ref bc, 0, 128);  // No delta

                // Y2 AC delta Q
                boolhuff.vp8_encode_bool(ref bc, 0, 128);  // No delta

                // UV DC delta Q
                boolhuff.vp8_encode_bool(ref bc, 0, 128);  // No delta

                // UV AC delta Q
                boolhuff.vp8_encode_bool(ref bc, 0, 128);  // No delta

                // Refresh entropy probs (1 bit) - 0 for keyframe
                boolhuff.vp8_encode_bool(ref bc, 0, 128);

                // refresh_last_frame (always 1 for keyframes, read anyway for non-keyframes)
                // For keyframes this is implicit, but let's write it anyway
                boolhuff.vp8_encode_bool(ref bc, 1, 128);

                // Coefficient probability updates
                // For each coefficient position, write whether it's being updated
                // Use the update probabilities from coefupdateprobs
                for (int i = 0; i < 4; i++)      // Block types
                {
                    for (int j = 0; j < 8; j++)  // Bands
                    {
                        for (int k = 0; k < 3; k++)  // Contexts
                        {
                            for (int l = 0; l < 11; l++)  // Tokens
                            {
                                // Write 0 using the update probability to indicate no update
                                byte update_prob = coefupdateprobs.vp8_coef_update_probs[i, j, k, l];
                                boolhuff.vp8_encode_bool(ref bc, 0, update_prob);
                            }
                        }
                    }
                }

                // MB skip coeff flag context (keyframe doesn't use this, but decoder reads it)
                boolhuff.vp8_encode_bool(ref bc, 0, 128);  // No update

                // Encode macroblocks
                int mb_rows = ctx.common.mb_rows;
                int mb_cols = ctx.common.mb_cols;

                for (int mb_row = 0; mb_row < mb_rows; mb_row++)
                {
                    for (int mb_col = 0; mb_col < mb_cols; mb_col++)
                    {
                        // Encode single macroblock (16x16)
                        vp8e_encode_macroblock_keyframe(ctx, ref bc, img, mb_row, mb_col);
                    }
                }

                // Finish encoding
                boolhuff.vp8_stop_encode(ref bc);

                // Calculate first partition size
                uint first_partition_size = bc.pos;

                // Write first partition size into frame tag (bits 5-23, 19 bits)
                uint size_in_frame_tag = first_partition_size << 5;
                output_ptr[0] |= (byte)((size_in_frame_tag) & 0xFF);
                output_ptr[1] = (byte)((size_in_frame_tag >> 8) & 0xFF);
                output_ptr[2] = (byte)((size_in_frame_tag >> 16) & 0xFF);

                // Calculate total compressed size
                compressed_size = (uint)(header_pos + bc.pos);
                compressed = new byte[compressed_size];
                Array.Copy(output, compressed, compressed_size);

                ctx.frame_count++;
                if (ctx.force_next_keyframe)
                {
                    ctx.force_next_keyframe = false;
                }

                return vpx_codec_err_t.VPX_CODEC_OK;
            }
        }

        /// <summary>
        /// Encode a single 16x16 macroblock for keyframe
        /// </summary>
        private static void vp8e_encode_macroblock_keyframe(VP8E_COMP ctx, ref BOOL_CODER bc,
            vpx_image_t img, int mb_row, int mb_col)
        {
            // Get macroblock position in image
            int mb_y = mb_row * 16;
            int mb_x = mb_col * 16;

            // Skip macroblocks outside image bounds
            if (mb_y >= img.d_h || mb_x >= img.d_w)
            {
                return;
            }

            // Allocate buffers for prediction and residual
            byte* pred_buffer = stackalloc byte[256 + 64 + 64];  // 16x16 Y + 8x8 U + 8x8 V
            short* residual = stackalloc short[256 + 64 + 64];
            short* dct_coeffs = stackalloc short[16];

            // Use DC prediction (simplest mode for keyframes)
            // Encode intra mode - DC_PRED
            int intra_mode = (int)MB_PREDICTION_MODE.DC_PRED;
            boolhuff.vp8_encode_value(ref bc, intra_mode, 4);

            // Generate DC prediction for 16x16 Y macroblock
            byte* y_pred = pred_buffer;
            byte* u_pred = pred_buffer + 256;
            byte* v_pred = pred_buffer + 256 + 64;

            // Simple DC prediction: use 128 for all pixels (mid-gray)
            // In full implementation, we'd use average of above/left pixels
            for (int i = 0; i < 256; i++) y_pred[i] = 128;
            for (int i = 0; i < 64; i++) u_pred[i] = 128;
            for (int i = 0; i < 64; i++) v_pred[i] = 128;

            // Get source pixels from image
            byte* y_src = img.planes[0] + (mb_y * img.stride[0]) + mb_x;
            byte* u_src = img.planes[1] + ((mb_y / 2) * img.stride[1]) + (mb_x / 2);
            byte* v_src = img.planes[2] + ((mb_y / 2) * img.stride[2]) + (mb_x / 2);

            // Process 16 4x4 Y blocks
            for (int block_y = 0; block_y < 4; block_y++)
            {
                for (int block_x = 0; block_x < 4; block_x++)
                {
                    int block_idx = block_y * 4 + block_x;
                    int pixel_y = block_y * 4;
                    int pixel_x = block_x * 4;

                    // Compute residuals for 4x4 block
                    short* block_residual = residual + (block_idx * 16);
                    for (int y = 0; y < 4; y++)
                    {
                        for (int x = 0; x < 4; x++)
                        {
                            int src_offset = (pixel_y + y) * img.stride[0] + (pixel_x + x);
                            int pred_offset = (pixel_y + y) * 16 + (pixel_x + x);
                            block_residual[y * 4 + x] = (short)(y_src[src_offset] - y_pred[pred_offset]);
                        }
                    }

                    // Apply forward DCT
                    fdctllm.vp8_short_fdct4x4_c(block_residual, dct_coeffs, 4);

                    // Simple quantization (divide by quantizer)
                    int qindex = ctx.common.base_qindex;
                    int quantizer = quant_common.vp8_ac_yquant(qindex);
                    for (int i = 0; i < 16; i++)
                    {
                        dct_coeffs[i] = (short)((dct_coeffs[i] * 4) / quantizer);
                    }

                    // Encode coefficients (simplified - just check if all zero)
                    bool all_zero = true;
                    for (int i = 0; i < 16; i++)
                    {
                        if (dct_coeffs[i] != 0)
                        {
                            all_zero = false;
                            break;
                        }
                    }

                    if (all_zero)
                    {
                        // Write EOB immediately
                        boolhuff.vp8_encode_bool(ref bc, 1, 128);
                    }
                    else
                    {
                        // Write some coefficients (simplified encoding)
                        // Just write a few non-zero tokens
                        for (int i = 0; i < 16; i++)
                        {
                            if (dct_coeffs[i] != 0)
                            {
                                // Encode non-zero coefficient
                                boolhuff.vp8_encode_bool(ref bc, 0, 128);  // Not EOB
                                boolhuff.vp8_encode_value(ref bc, System.Math.Abs(dct_coeffs[i]), 8);
                            }
                        }
                        // Write EOB
                        boolhuff.vp8_encode_bool(ref bc, 1, 128);
                    }
                }
            }

            // Process 4 U blocks (8x8 split into 4x4)
            for (int block = 0; block < 4; block++)
            {
                // For UV, just write EOB (simplified)
                boolhuff.vp8_encode_bool(ref bc, 1, 128);
            }

            // Process 4 V blocks (8x8 split into 4x4)
            for (int block = 0; block < 4; block++)
            {
                // For UV, just write EOB (simplified)
                boolhuff.vp8_encode_bool(ref bc, 1, 128);
            }
        }
    }
}
