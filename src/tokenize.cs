//-----------------------------------------------------------------------------
// Filename: tokenize.cs
//
// Description: Tokenization for VP8 encoding (reverse of detokenize.cs).
//              Converts quantized coefficients into tokens for entropy coding.
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
    /// Token structure for coefficient encoding.
    /// </summary>
    public struct TOKEN
    {
        public int token;      // Token value
        public int extra;      // Extra bits value
        public int context;    // Context for probability
        public int band;       // Frequency band
    }

    /// <summary>
    /// Tokenization for encoding quantized coefficients.
    /// </summary>
    public static class tokenize
    {
        /// <summary>
        /// Tokenize a block of quantized coefficients.
        /// </summary>
        public static List<TOKEN> vp8_tokenize_block(short[] qcoeff, int eob, int type)
        {
            List<TOKEN> tokens = new List<TOKEN>();
            int c = 0;
            int pt = 0;  // Previous token type
            
            // EOB token at the end
            if (eob == 0)
            {
                tokens.Add(new TOKEN { token = entropy.DCT_EOB_TOKEN, band = 0, context = 0 });
                return tokens;
            }
            
            // Process each coefficient
            for (c = 0; c < eob; c++)
            {
                int v = qcoeff[c];
                int band = entropy.vp8_coef_bands[c];
                
                if (v == 0)
                {
                    // Zero token
                    tokens.Add(new TOKEN 
                    { 
                        token = entropy.ZERO_TOKEN, 
                        band = band,
                        context = pt 
                    });
                    pt = 0;  // Zero context
                }
                else
                {
                    int abs_v = Math.Abs(v);
                    int token_value;
                    int extra_bits = 0;
                    
                    // Determine token based on coefficient magnitude
                    if (abs_v == 1)
                    {
                        token_value = entropy.ONE_TOKEN;
                    }
                    else if (abs_v == 2)
                    {
                        token_value = entropy.TWO_TOKEN;
                    }
                    else if (abs_v == 3)
                    {
                        token_value = entropy.THREE_TOKEN;
                    }
                    else if (abs_v == 4)
                    {
                        token_value = entropy.FOUR_TOKEN;
                    }
                    else if (abs_v <= 6)
                    {
                        token_value = entropy.DCT_VAL_CATEGORY1;
                        extra_bits = abs_v - 5;
                    }
                    else if (abs_v <= 10)
                    {
                        token_value = entropy.DCT_VAL_CATEGORY2;
                        extra_bits = abs_v - 7;
                    }
                    else if (abs_v <= 18)
                    {
                        token_value = entropy.DCT_VAL_CATEGORY3;
                        extra_bits = abs_v - 11;
                    }
                    else if (abs_v <= 34)
                    {
                        token_value = entropy.DCT_VAL_CATEGORY4;
                        extra_bits = abs_v - 19;
                    }
                    else if (abs_v <= 66)
                    {
                        token_value = entropy.DCT_VAL_CATEGORY5;
                        extra_bits = abs_v - 35;
                    }
                    else
                    {
                        token_value = entropy.DCT_VAL_CATEGORY6;
                        extra_bits = abs_v - 67;
                    }
                    
                    // Add sign bit to extra
                    extra_bits = (extra_bits << 1) | (v < 0 ? 1 : 0);
                    
                    tokens.Add(new TOKEN 
                    { 
                        token = token_value, 
                        extra = extra_bits,
                        band = band,
                        context = pt 
                    });
                    
                    pt = (token_value == entropy.ONE_TOKEN) ? 1 : 2;
                }
            }
            
            // Add EOB token
            tokens.Add(new TOKEN 
            { 
                token = entropy.DCT_EOB_TOKEN, 
                band = entropy.vp8_coef_bands[c > 0 ? c - 1 : 0],
                context = pt 
            });
            
            return tokens;
        }
    }
}
