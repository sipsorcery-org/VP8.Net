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

                // Get coefficient probabilities (used for coefficient encoding phase)
                byte* coef_probs = stackalloc byte[4 * 8 * 3 * 11];
                fixed (byte* pDefaultProbs = default_coef_probs_c.default_coef_probs)
                {
                    for (int i = 0; i < 4 * 8 * 3 * 11; i++)
                    {
                        coef_probs[i] = pDefaultProbs[i];
                    }
                }

                int mb_rows = ctx.common.mb_rows;
                int mb_cols = ctx.common.mb_cols;

                // PHASE 1: Encode ALL macroblock modes first
                // The decoder reads all modes via vp8_decode_mode_mvs() before reading coefficients
                for (int mb_row = 0; mb_row < mb_rows; mb_row++)
                {
                    for (int mb_col = 0; mb_col < mb_cols; mb_col++)
                    {
                        // Encode Y mode using tree structure
                        int intra_mode = (int)MB_PREDICTION_MODE.DC_PRED;
                        fixed (byte* ymode_probs = vp8_entropymodedata.vp8_kf_ymode_prob)
                        {
                            vp8_treed_write(ref bc, entropymode.vp8_kf_ymode_tree, ymode_probs, intra_mode);
                        }
                        
                        // Encode UV mode
                        int uv_mode = (int)MB_PREDICTION_MODE.DC_PRED;
                        fixed (byte* uvmode_probs = vp8_entropymodedata.vp8_kf_uv_mode_prob)
                        {
                            vp8_treed_write(ref bc, entropymode.vp8_uv_mode_tree, uvmode_probs, uv_mode);
                        }
                    }
                }

                // PHASE 2: Encode ALL macroblock coefficients
                // The decoder reads coefficients via decode_mb_rows() after reading all modes
                for (int mb_row = 0; mb_row < mb_rows; mb_row++)
                {
                    for (int mb_col = 0; mb_col < mb_cols; mb_col++)
                    {
                        // Encode coefficient data for this macroblock
                        vp8e_encode_macroblock_coeffs(ctx, ref bc, img, mb_row, mb_col, coef_probs);
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
        /// Encode a value using a tree structure (inverse of vp8_treed_read)
        /// </summary>
        private static void vp8_treed_write(ref BOOL_CODER bc, sbyte[] tree, byte* probs, int value)
        {
            // The tree structure uses negative values as leaf nodes
            // We need to traverse the tree and write the bits that lead to our value
            
            // Build the path to the value
            System.Collections.Generic.List<int> path = new System.Collections.Generic.List<int>();
            int i = 0;
            bool found = false;
            
            // Simple approach: try both paths at each node and see which leads to our value
            void FindPath(int node, System.Collections.Generic.List<int> currentPath)
            {
                if (found) return;
                
                if (tree[node] <= 0)
                {
                    // Leaf node
                    if (-tree[node] == value)
                    {
                        path = new System.Collections.Generic.List<int>(currentPath);
                        found = true;
                    }
                    return;
                }
                
                // Try left (0)
                currentPath.Add(0);
                FindPath(tree[node], currentPath);
                currentPath.RemoveAt(currentPath.Count - 1);
                
                if (found) return;
                
                // Try right (1)
                currentPath.Add(1);
                FindPath(tree[node] + 1, currentPath);
                currentPath.RemoveAt(currentPath.Count - 1);
            }
            
            FindPath(0, new System.Collections.Generic.List<int>());
            
            // Write the path
            for (int j = 0; j < path.Count; j++)
            {
                boolhuff.vp8_encode_bool(ref bc, path[j], probs[j]);
            }
        }

        /// <summary>
        /// Encode coefficients using VP8 token tree (inverse of GetCoeffs in detokenize.cs)
        /// </summary>
        private static void WriteCoeffs(ref BOOL_CODER bc, byte* prob, short* coeffs, int n)
        {
            // Zigzag order for DCT coefficients
            byte[] zigzag = { 0, 1, 4, 8, 5, 2, 3, 6, 9, 12, 13, 10, 7, 11, 14, 15 };
            
            // Bands for coefficient positions
            byte[] bands = { 0, 1, 2, 3, 6, 4, 5, 6, 6, 6, 6, 6, 6, 6, 6, 7, 0 };
            
            int NUM_PROBAS = 11;
            int NUM_CTX = 3;
            int bigSlice = NUM_CTX * NUM_PROBAS;
            int smallSlice = NUM_PROBAS;
            
            // Check if all coefficients are zero
            bool has_coeffs = false;
            int last_nz = 0;
            for (int i = 0; i < 16; i++)
            {
                if (coeffs[i] != 0)
                {
                    has_coeffs = true;
                    last_nz = i;
                }
            }
            
            // Get probability for this band and context
            byte* p = prob + n * bigSlice; // Start with context 0
            
            // Write first bit: has coefficients or EOB
            if (!has_coeffs)
            {
                boolhuff.vp8_encode_bool(ref bc, 0, p[0]);
                return;
            }
            
            boolhuff.vp8_encode_bool(ref bc, 1, p[0]);
            
            // Encode each non-zero coefficient
            int ctx = 0; // Previous coefficient context (0, 1, or 2)
            
            for (int i = 0; i < 16; i++)
            {
                int coeff_idx = zigzag[i];
                int v = System.Math.Abs(coeffs[coeff_idx]);
                
                n++;
                p = prob + bands[n] * bigSlice + ctx * smallSlice;
                
                if (v == 0)
                {
                    // Write EOB if this is after last non-zero
                    if (i > last_nz)
                    {
                        boolhuff.vp8_encode_bool(ref bc, 0, p[1]); // EOB
                        return;
                    }
                    // Skip this zero coefficient, continue
                    continue;
                }
                
                // Write "not EOB" bit
                boolhuff.vp8_encode_bool(ref bc, 1, p[1]);
                
                // Encode coefficient value
                if (v == 1)
                {
                    boolhuff.vp8_encode_bool(ref bc, 0, p[2]); // v == 1
                    ctx = 1;
                }
                else if (v == 2)
                {
                    boolhuff.vp8_encode_bool(ref bc, 1, p[2]); // v > 1
                    boolhuff.vp8_encode_bool(ref bc, 0, p[3]); // v == 2
                    boolhuff.vp8_encode_bool(ref bc, 0, p[4]); // select 2
                    ctx = 2;
                }
                else if (v == 3 || v == 4)
                {
                    boolhuff.vp8_encode_bool(ref bc, 1, p[2]); // v > 1
                    boolhuff.vp8_encode_bool(ref bc, 0, p[3]); // v < 5
                    boolhuff.vp8_encode_bool(ref bc, 1, p[4]); // v == 3 or 4
                    boolhuff.vp8_encode_bool(ref bc, v == 4 ? 1 : 0, p[5]); // which one
                    ctx = 2;
                }
                else if (v >= 5 && v <= 6)
                {
                    boolhuff.vp8_encode_bool(ref bc, 1, p[2]); // v > 1
                    boolhuff.vp8_encode_bool(ref bc, 1, p[3]); // v >= 5
                    boolhuff.vp8_encode_bool(ref bc, 0, p[6]); // v < 7
                    boolhuff.vp8_encode_bool(ref bc, 0, p[7]); // CAT1
                    boolhuff.vp8_encode_bool(ref bc, v == 6 ? 1 : 0, 159); // extra bit
                    ctx = 2;
                }
                else if (v >= 7 && v <= 10)
                {
                    boolhuff.vp8_encode_bool(ref bc, 1, p[2]); // v > 1
                    boolhuff.vp8_encode_bool(ref bc, 1, p[3]); // v >= 5
                    boolhuff.vp8_encode_bool(ref bc, 0, p[6]); // v < 11
                    boolhuff.vp8_encode_bool(ref bc, 1, p[7]); // CAT2
                    int offset = v - 7;
                    boolhuff.vp8_encode_bool(ref bc, (offset >> 1) & 1, 165); // bit 1
                    boolhuff.vp8_encode_bool(ref bc, offset & 1, 145); // bit 0
                    ctx = 2;
                }
                else
                {
                    // Larger values (CAT3-CAT6) - simplified, just clamp to 10
                    boolhuff.vp8_encode_bool(ref bc, 1, p[2]); // v > 1
                    boolhuff.vp8_encode_bool(ref bc, 1, p[3]); // v >= 5
                    boolhuff.vp8_encode_bool(ref bc, 0, p[6]); // CAT2
                    boolhuff.vp8_encode_bool(ref bc, 1, p[7]); 
                    boolhuff.vp8_encode_bool(ref bc, 1, 165); 
                    boolhuff.vp8_encode_bool(ref bc, 1, 145);
                    ctx = 2;
                }
                
                // Write sign bit
                if (coeffs[coeff_idx] < 0)
                {
                    boolhuff.vp8_encode_bool(ref bc, 1, 128);
                }
                else
                {
                    boolhuff.vp8_encode_bool(ref bc, 0, 128);
                }
                
                // Check if this was the last non-zero coefficient
                if (i >= last_nz)
                {
                    return;
                }
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
            
            // Get coefficient probabilities (use default for keyframes)
            byte* coef_probs = stackalloc byte[4 * 8 * 3 * 11];
            
            // Copy default probabilities
            fixed (byte* pDefaultProbs = default_coef_probs_c.default_coef_probs)
            {
                for (int i = 0; i < 4 * 8 * 3 * 11; i++)
                {
                    coef_probs[i] = pDefaultProbs[i];
                }
            }

            // Use DC prediction (simplest mode for keyframes)
            // Encode intra mode using tree structure
            int intra_mode = (int)MB_PREDICTION_MODE.DC_PRED;
            fixed (byte* ymode_probs = vp8_entropymodedata.vp8_kf_ymode_prob)
            {
                vp8_treed_write(ref bc, entropymode.vp8_kf_ymode_tree, ymode_probs, intra_mode);
            }
            
            // Encode UV mode
            int uv_mode = (int)MB_PREDICTION_MODE.DC_PRED;
            fixed (byte* uvmode_probs = vp8_entropymodedata.vp8_kf_uv_mode_prob)
            {
                vp8_treed_write(ref bc, entropymode.vp8_uv_mode_tree, uvmode_probs, uv_mode);
            }

            // Generate DC prediction for 16x16 Y macroblock
            byte* y_pred = pred_buffer;
            byte* u_pred = pred_buffer + 256;
            byte* v_pred = pred_buffer + 256 + 64;

            // Simple DC prediction: use 128 for all pixels (mid-gray)
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
                    // For now, encode all blocks as empty (EOB) to test basic structure
                    // Just write EOB immediately using p[0] = 0
                    byte* block_probs = coef_probs; // Y1 block type
                    int NUM_PROBAS = 11;
                    int NUM_CTX = 3;
                    int bigSlice = NUM_CTX * NUM_PROBAS;
                    byte* p = block_probs; // band 0, context 0
                    
                    // Write "no coefficients" (EOB at first position)
                    boolhuff.vp8_encode_bool(ref bc, 0, p[0]);
                }
            }

            // Process 4 U blocks (8x8 split into 4x4)  
            for (int block = 0; block < 4; block++)
            {
                byte* uv_probs = coef_probs + (2 * 8 * 3 * 11); // UV block type (type 2)
                byte* p = uv_probs;
                boolhuff.vp8_encode_bool(ref bc, 0, p[0]);
            }

            // Process 4 V blocks (8x8 split into 4x4)
            for (int block = 0; block < 4; block++)
            {
                byte* uv_probs = coef_probs + (2 * 8 * 3 * 11); // UV block type (type 2)
                byte* p = uv_probs;
                boolhuff.vp8_encode_bool(ref bc, 0, p[0]);
            }
        }

        /// <summary>
        /// Encode coefficient data only for a single macroblock (modes already encoded separately)
        /// </summary>
        private static void vp8e_encode_macroblock_coeffs(VP8E_COMP ctx, ref BOOL_CODER bc,
            vpx_image_t img, int mb_row, int mb_col, byte* coef_probs)
        {
            // Get macroblock position in image
            int mb_y = mb_row * 16;
            int mb_x = mb_col * 16;

            // Skip macroblocks outside image bounds
            if (mb_y >= img.d_h || mb_x >= img.d_w)
            {
                return;
            }

            int NUM_PROBAS = 11;
            int NUM_CTX = 3;
            int bigSlice = NUM_CTX * NUM_PROBAS;

            // Process 16 4x4 Y blocks
            for (int block_y = 0; block_y < 4; block_y++)
            {
                for (int block_x = 0; block_x < 4; block_x++)
                {
                    // For now, encode all blocks as empty (EOB) to test basic structure
                    byte* block_probs = coef_probs; // Y1 block type (type 0)
                    byte* p = block_probs; // band 0, context 0
                    
                    // Write "no coefficients" (EOB at first position)
                    boolhuff.vp8_encode_bool(ref bc, 0, p[0]);
                }
            }

            // Process 4 U blocks (8x8 split into 4x4)  
            for (int block = 0; block < 4; block++)
            {
                byte* uv_probs = coef_probs + (2 * 8 * 3 * 11); // UV block type (type 2)
                byte* p = uv_probs;
                boolhuff.vp8_encode_bool(ref bc, 0, p[0]);
            }

            // Process 4 V blocks (8x8 split into 4x4)
            for (int block = 0; block < 4; block++)
            {
                byte* uv_probs = coef_probs + (2 * 8 * 3 * 11); // UV block type (type 2)
                byte* p = uv_probs;
                boolhuff.vp8_encode_bool(ref bc, 0, p[0]);
            }
        }
    }
}
