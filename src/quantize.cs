//-----------------------------------------------------------------------------
// Filename: quantize.cs
//
// Description: Quantization functions for VP8 encoding.
//              This is the encoding equivalent of dequantize.cs
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

using System;

namespace Vpx.Net
{
    /// <summary>
    /// Quantization for VP8 encoding.
    /// </summary>
    public static class quantize
    {
        /// <summary>
        /// Quantize a block of DCT coefficients.
        /// </summary>
        /// <param name="coeff">Input DCT coefficients</param>
        /// <param name="qcoeff">Output quantized coefficients</param>
        /// <param name="dequant">Dequantization values</param>
        /// <param name="q_index">Quantization index (0-127)</param>
        public static void vp8_quantize_block(short[] coeff, short[] qcoeff, short[] dequant, int q_index)
        {
            // Simple quantization: divide by quantizer
            int quantizer = quant_common.vp8_dc_quant(q_index, 0);
            
            for (int i = 0; i < 16; i++)
            {
                int val = coeff[i];
                int abs_val = Math.Abs(val);
                int sign = val < 0 ? -1 : 1;
                
                // Quantize
                int q = (abs_val * quant_common.vp8_dc_quant(q_index, 0)) >> 7;
                
                // Threshold small values to zero
                if (q < quantizer / 8)
                {
                    q = 0;
                }
                else
                {
                    q = (q + quantizer / 2) / quantizer;
                }
                
                qcoeff[i] = (short)(sign * q);
                
                // Dequantize for reconstruction
                dequant[i] = (short)(qcoeff[i] * quantizer);
            }
        }
        
        /// <summary>
        /// Quantize a 4x4 block with proper quantization tables.
        /// </summary>
        public static int vp8_regular_quantize_b_4x4(MACROBLOCK mb, int b_idx, short[] coeff, short[] qcoeff)
        {
            int q_index = mb.q_index;
            int eob = 0;  // End of block marker
            
            // Get quantizer values
            int quantizer = quant_common.vp8_ac_yquant(q_index);
            if (b_idx < 16)  // Y blocks
            {
                quantizer = quant_common.vp8_ac_yquant(q_index);
            }
            else if (b_idx < 20)  // U blocks
            {
                quantizer = quant_common.vp8_ac_uv_quant(q_index, 0);
            }
            else  // V blocks
            {
                quantizer = quant_common.vp8_ac_uv_quant(q_index, 0);
            }
            
            // Quantize coefficients
            for (int i = 0; i < 16; i++)
            {
                int val = coeff[i];
                int abs_val = Math.Abs(val);
                int sign = val < 0 ? -1 : 1;
                
                // Quantize
                int q = (abs_val + quantizer / 2) / quantizer;
                
                if (q > 0)
                {
                    qcoeff[i] = (short)(sign * q);
                    eob = i + 1;  // Track last non-zero coefficient
                }
                else
                {
                    qcoeff[i] = 0;
                }
            }
            
            return eob;
        }
    }
}
