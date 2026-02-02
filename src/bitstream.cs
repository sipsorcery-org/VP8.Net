//-----------------------------------------------------------------------------
// Filename: bitstream.cs
//
// Description: Bitstream writing for VP8 encoding.
//              Writes frame headers and encoded data.
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
using vp8_prob = System.Byte;

namespace Vpx.Net
{
    /// <summary>
    /// Bitstream writing for VP8 encoder.
    /// </summary>
    public static class bitstream
    {
        /// <summary>
        /// Write uncompressed frame header.
        /// </summary>
        public static unsafe void write_frame_header(ref BOOL_CODER bc, VP8_COMMON cm, bool key_frame)
        {
            // Frame tag (3 bytes for keyframe, 3 bytes for inter frame)
            if (key_frame)
            {
                // Keyframe: frame_tag[0] = 0 (keyframe) | version | show_frame
                boolhuff.vp8_encode_value(ref bc, 0, 1);  // frame type: 0 = keyframe
                boolhuff.vp8_encode_value(ref bc, 0, 2);  // version
                boolhuff.vp8_encode_value(ref bc, 1, 1);  // show_frame
                
                // Frame size
                boolhuff.vp8_encode_value(ref bc, (int)cm.Width, 16);
                boolhuff.vp8_encode_value(ref bc, (int)cm.Height, 16);
            }
            else
            {
                // Inter frame
                boolhuff.vp8_encode_value(ref bc, 1, 1);  // frame type: 1 = inter frame
            }
            
            // Write quantization parameters
            write_quantization_params(ref bc, cm);
            
            // Write loop filter parameters
            write_loopfilter_params(ref bc, cm);
            
            // Write partition sizes (simplified - single partition)
            // In a full implementation, this would write multiple partition sizes
        }
        
        /// <summary>
        /// Write quantization parameters.
        /// </summary>
        private static void write_quantization_params(ref BOOL_CODER bc, VP8_COMMON cm)
        {
            // Base Q index (7 bits)
            boolhuff.vp8_encode_value(ref bc, cm.base_qindex, 7);
            
            // Delta Q values (simplified - no deltas)
            boolhuff.vp8_encode_bool(ref bc, 0, 128);  // y1_dc_delta_q present
            boolhuff.vp8_encode_bool(ref bc, 0, 128);  // y2_dc_delta_q present
            boolhuff.vp8_encode_bool(ref bc, 0, 128);  // y2_ac_delta_q present
            boolhuff.vp8_encode_bool(ref bc, 0, 128);  // uv_dc_delta_q present
            boolhuff.vp8_encode_bool(ref bc, 0, 128);  // uv_ac_delta_q present
        }
        
        /// <summary>
        /// Write loop filter parameters.
        /// </summary>
        private static void write_loopfilter_params(ref BOOL_CODER bc, VP8_COMMON cm)
        {
            // Filter type (1 bit): 0 = normal, 1 = simple
            boolhuff.vp8_encode_bool(ref bc, 0, 128);
            
            // Filter level (6 bits)
            boolhuff.vp8_encode_value(ref bc, cm.filter_level, 6);
            
            // Sharpness level (3 bits)
            boolhuff.vp8_encode_value(ref bc, 0, 3);
            
            // MB level adjustments (simplified - none)
            boolhuff.vp8_encode_bool(ref bc, 0, 128);  // mode_ref_lf_delta_update
        }
        
        /// <summary>
        /// Write macroblock mode information.
        /// </summary>
        public static void write_mb_modes(ref BOOL_CODER bc, MACROBLOCKD xd, MODE_INFO mi)
        {
            MB_PREDICTION_MODE mode = mi.mbmi.mode;
            
            // For keyframes, write intra mode
            // Simplified: always use DC_PRED
            if (mode == MB_PREDICTION_MODE.DC_PRED)
            {
                boolhuff.vp8_encode_bool(ref bc, 1, 145);  // DC_PRED probability
            }
            else
            {
                boolhuff.vp8_encode_bool(ref bc, 0, 145);
                // Would write other mode information here
            }
        }
        
        /// <summary>
        /// Write macroblock coefficients using tokenization.
        /// </summary>
        public static void write_mb_coefficients(ref BOOL_CODER bc, MACROBLOCK mb, vp8_prob[] coef_probs)
        {
            // Write Y block coefficients
            for (int i = 0; i < 16; i++)
            {
                short[] qcoeff = new short[16];
                Array.Copy(mb.coeff, i * 16, qcoeff, 0, 16);
                
                // Find EOB
                int eob = 0;
                for (int j = 15; j >= 0; j--)
                {
                    if (qcoeff[j] != 0)
                    {
                        eob = j + 1;
                        break;
                    }
                }
                
                // Tokenize and write
                var tokens = tokenize.vp8_tokenize_block(qcoeff, eob, 0);
                write_tokens(ref bc, tokens, coef_probs);
            }
            
            // Write U and V block coefficients (simplified)
            for (int i = 0; i < 4; i++)
            {
                short[] qcoeff = new short[16];
                Array.Copy(mb.coeff, 256 + i * 16, qcoeff, 0, 16);
                
                int eob = 0;
                for (int j = 15; j >= 0; j--)
                {
                    if (qcoeff[j] != 0)
                    {
                        eob = j + 1;
                        break;
                    }
                }
                
                var tokens = tokenize.vp8_tokenize_block(qcoeff, eob, 2);
                write_tokens(ref bc, tokens, coef_probs);
            }
            
            for (int i = 0; i < 4; i++)
            {
                short[] qcoeff = new short[16];
                Array.Copy(mb.coeff, 320 + i * 16, qcoeff, 0, 16);
                
                int eob = 0;
                for (int j = 15; j >= 0; j--)
                {
                    if (qcoeff[j] != 0)
                    {
                        eob = j + 1;
                        break;
                    }
                }
                
                var tokens = tokenize.vp8_tokenize_block(qcoeff, eob, 2);
                write_tokens(ref bc, tokens, coef_probs);
            }
        }
        
        /// <summary>
        /// Write tokens to bitstream.
        /// </summary>
        private static void write_tokens(ref BOOL_CODER bc, System.Collections.Generic.List<TOKEN> tokens, vp8_prob[] coef_probs)
        {
            foreach (var token in tokens)
            {
                // Write token using context-based probability
                int prob = 128;  // Simplified - should use coef_probs based on context
                
                if (token.token == entropy.DCT_EOB_TOKEN)
                {
                    boolhuff.vp8_encode_bool(ref bc, 1, 128);  // EOB
                }
                else if (token.token == entropy.ZERO_TOKEN)
                {
                    boolhuff.vp8_encode_bool(ref bc, 0, prob);  // Zero
                }
                else
                {
                    // Non-zero token
                    boolhuff.vp8_encode_bool(ref bc, 1, prob);
                    boolhuff.vp8_encode_value(ref bc, token.token, 4);
                    
                    // Write extra bits if needed
                    if (token.extra > 0)
                    {
                        boolhuff.vp8_encode_value(ref bc, token.extra, 8);
                    }
                }
            }
        }
    }
}
