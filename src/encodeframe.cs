//-----------------------------------------------------------------------------
// Filename: encodeframe.cs
//
// Description: Main frame encoding logic for VP8 encoder.
//              This is the encoding equivalent of decodeframe.cs
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
using System.Collections.Generic;

namespace Vpx.Net
{
    /// <summary>
    /// Frame encoding functions.
    /// </summary>
    public static class encodeframe
    {
        /// <summary>
        /// Encode a single macroblock.
        /// </summary>
        public static unsafe void encode_macroblock(VP8_COMP cpi, MACROBLOCK x, int mb_row, int mb_col)
        {
            VP8_COMMON cm = cpi.common;
            MACROBLOCKD xd = x.e_mbd;
            
            // Get source macroblock data
            int mb_index = mb_row * cm.mb_cols + mb_col;
            
            // For simplicity, use DC_PRED mode for all macroblocks in keyframes
            xd.mode_info_context.get().mbmi.mode = MB_PREDICTION_MODE.DC_PRED;
            xd.mode_info_context.get().mbmi.uv_mode = MB_PREDICTION_MODE.DC_PRED;
            
            // Build intra prediction
            reconintra.vp8_build_intra_predictors_mby_s(xd, 
                xd.dst.y_buffer, xd.dst.y_stride);
            reconintra.vp8_build_intra_predictors_mbuv_s(xd);
            
            // Calculate residual (source - prediction)
            calculate_residual(x, xd);
            
            // Transform and quantize
            transform_and_quantize(x, cpi.base_qindex);
            
            // Reconstruct for reference
            reconstruct_macroblock(x, xd);
        }
        
        /// <summary>
        /// Calculate residual between source and prediction.
        /// </summary>
        private static unsafe void calculate_residual(MACROBLOCK x, MACROBLOCKD xd)
        {
            // Y plane residual (16x16)
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    int src_val = xd.dst.y_buffer[i * xd.dst.y_stride + j];
                    int pred_val = xd.predictor[i * 16 + j];
                    x.src_diff[i * 16 + j] = (short)(src_val - pred_val);
                }
            }
            
            // U plane residual (8x8)
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int src_val = xd.dst.u_buffer[i * xd.dst.uv_stride + j];
                    int pred_val = 128;  // Simple prediction
                    x.src_diff[256 + i * 8 + j] = (short)(src_val - pred_val);
                }
            }
            
            // V plane residual (8x8)
            for (int i = 0; i < 8; i++)
            {
                for (int j = 0; j < 8; j++)
                {
                    int src_val = xd.dst.v_buffer[i * xd.dst.uv_stride + j];
                    int pred_val = 128;  // Simple prediction
                    x.src_diff[320 + i * 8 + j] = (short)(src_val - pred_val);
                }
            }
        }
        
        /// <summary>
        /// Transform and quantize macroblock residual.
        /// </summary>
        private static void transform_and_quantize(MACROBLOCK x, int q_index)
        {
            x.q_index = q_index;
            
            // Transform and quantize Y blocks (16 4x4 blocks)
            for (int i = 0; i < 16; i++)
            {
                int block_offset = i * 16;
                short[] input = new short[16];
                short[] output = new short[16];
                
                // Extract 4x4 block
                int row = (i / 4) * 4;
                int col = (i % 4) * 4;
                for (int r = 0; r < 4; r++)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        input[r * 4 + c] = x.src_diff[(row + r) * 16 + col + c];
                    }
                }
                
                // Forward DCT
                fdctllm.vp8_short_fdct4x4(input, output, 4);
                
                // Quantize
                short[] qcoeff = new short[16];
                short[] dequant = new short[16];
                quantize.vp8_quantize_block(output, qcoeff, dequant, q_index);
                
                // Store quantized coefficients
                Array.Copy(qcoeff, 0, x.coeff, block_offset, 16);
            }
            
            // Transform and quantize U blocks (4 4x4 blocks)
            for (int i = 0; i < 4; i++)
            {
                int block_offset = 256 + i * 16;
                short[] input = new short[16];
                short[] output = new short[16];
                
                int row = (i / 2) * 4;
                int col = (i % 2) * 4;
                for (int r = 0; r < 4; r++)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        input[r * 4 + c] = x.src_diff[256 + (row + r) * 8 + col + c];
                    }
                }
                
                fdctllm.vp8_short_fdct4x4(input, output, 4);
                
                short[] qcoeff = new short[16];
                short[] dequant = new short[16];
                quantize.vp8_quantize_block(output, qcoeff, dequant, q_index);
                
                Array.Copy(qcoeff, 0, x.coeff, block_offset, 16);
            }
            
            // Transform and quantize V blocks (4 4x4 blocks)
            for (int i = 0; i < 4; i++)
            {
                int block_offset = 320 + i * 16;
                short[] input = new short[16];
                short[] output = new short[16];
                
                int row = (i / 2) * 4;
                int col = (i % 2) * 4;
                for (int r = 0; r < 4; r++)
                {
                    for (int c = 0; c < 4; c++)
                    {
                        input[r * 4 + c] = x.src_diff[320 + (row + r) * 8 + col + c];
                    }
                }
                
                fdctllm.vp8_short_fdct4x4(input, output, 4);
                
                short[] qcoeff = new short[16];
                short[] dequant = new short[16];
                quantize.vp8_quantize_block(output, qcoeff, dequant, q_index);
                
                Array.Copy(qcoeff, 0, x.coeff, block_offset, 16);
            }
        }
        
        /// <summary>
        /// Reconstruct macroblock for use as reference.
        /// </summary>
        private static unsafe void reconstruct_macroblock(MACROBLOCK x, MACROBLOCKD xd)
        {
            // Inverse quantize and IDCT
            for (int i = 0; i < 16; i++)
            {
                short[] qcoeff = new short[16];
                short[] dqcoeff = new short[16];
                short[] residual = new short[16];
                
                Array.Copy(x.coeff, i * 16, qcoeff, 0, 16);
                
                // Dequantize
                dequantize.vp8_dequantize_b(qcoeff, dqcoeff, x.q_index);
                
                // IDCT
                idctllm.vp8_short_idct4x4llm(dqcoeff, residual, 4, xd.dst.y_buffer + i * 4, xd.dst.y_stride);
            }
            
            // Similar for U and V planes (simplified)
        }
        
        /// <summary>
        /// Encode an entire frame.
        /// </summary>
        public static void vp8_encode_frame(VP8_COMP cpi)
        {
            VP8_COMMON cm = cpi.common;
            
            // Encode all macroblocks
            for (int mb_row = 0; mb_row < cm.mb_rows; mb_row++)
            {
                for (int mb_col = 0; mb_col < cm.mb_cols; mb_col++)
                {
                    encode_macroblock(cpi, cpi.mb, mb_row, mb_col);
                }
            }
        }
    }
}
