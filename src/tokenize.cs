//-----------------------------------------------------------------------------
// Filename: tokenize.cs
//
// Description: Token generation for VP8 encoder (inverse of detokenize.cs)
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

using System.Collections.Generic;

namespace Vpx.Net
{
    /// <summary>
    /// Token for entropy coding
    /// </summary>
    public struct TOKEN
    {
        public int value;           // Token value
        public int context;         // Context for probability
        public int extra;           // Extra bits if needed
        public int skip;            // Skip end-of-block token
    }

    /// <summary>
    /// Tokenization functions - convert quantized coefficients to tokens
    /// </summary>
    public unsafe static class tokenize
    {
        // Token values for different coefficient ranges
        public const int DCT_EOB_TOKEN = 11;  // End of block
        public const int ZERO_TOKEN = 0;
        public const int DCT_VAL_CATEGORY1 = 1;  // 1
        public const int DCT_VAL_CATEGORY2 = 2;  // 2
        public const int DCT_VAL_CATEGORY3 = 3;  // 3,4
        public const int DCT_VAL_CATEGORY4 = 4;  // 5-6
        public const int DCT_VAL_CATEGORY5 = 5;  // 7-10
        public const int DCT_VAL_CATEGORY6 = 6;  // 11-26

        /// <summary>
        /// Convert quantized coefficients to tokens
        /// </summary>
        public static List<TOKEN> vp8_tokenize_block(short* qcoeff, int block_type)
        {
            List<TOKEN> tokens = new List<TOKEN>();
            int c = 0;
            int pt = 0;  // Previous token

            // Find last non-zero coefficient
            int eob = 15;
            while (eob > 0 && qcoeff[eob] == 0)
            {
                eob--;
            }

            if (qcoeff[0] == 0 && eob == 0)
            {
                // Empty block - no tokens
                return tokens;
            }

            // Process coefficients in zig-zag order
            for (c = 0; c <= eob; ++c)
            {
                int v = qcoeff[c];
                int abs_v = v < 0 ? -v : v;

                TOKEN token = new TOKEN();
                token.context = pt;

                if (v == 0)
                {
                    token.value = ZERO_TOKEN;
                    pt = 0;
                }
                else if (abs_v == 1)
                {
                    token.value = DCT_VAL_CATEGORY1;
                    token.extra = v < 0 ? 1 : 0;  // Sign bit
                    pt = 1;
                }
                else if (abs_v == 2)
                {
                    token.value = DCT_VAL_CATEGORY2;
                    token.extra = v < 0 ? 1 : 0;
                    pt = 2;
                }
                else if (abs_v <= 4)
                {
                    token.value = DCT_VAL_CATEGORY3;
                    token.extra = ((abs_v - 3) << 1) | (v < 0 ? 1 : 0);
                    pt = 2;
                }
                else if (abs_v <= 6)
                {
                    token.value = DCT_VAL_CATEGORY4;
                    token.extra = ((abs_v - 5) << 1) | (v < 0 ? 1 : 0);
                    pt = 2;
                }
                else if (abs_v <= 10)
                {
                    token.value = DCT_VAL_CATEGORY5;
                    token.extra = ((abs_v - 7) << 1) | (v < 0 ? 1 : 0);
                    pt = 2;
                }
                else
                {
                    token.value = DCT_VAL_CATEGORY6;
                    token.extra = ((abs_v - 11) << 1) | (v < 0 ? 1 : 0);
                    pt = 2;
                }

                tokens.Add(token);
            }

            // Add end-of-block token
            TOKEN eob_token = new TOKEN();
            eob_token.value = DCT_EOB_TOKEN;
            eob_token.context = pt;
            tokens.Add(eob_token);

            return tokens;
        }

        /// <summary>
        /// Encode tokens using boolean encoder
        /// </summary>
        public static void vp8_encode_tokens(ref BOOL_CODER bc, List<TOKEN> tokens, byte* coef_probs)
        {
            foreach (var token in tokens)
            {
                // In a full implementation, we would use the coefficient probability tables
                // to entropy encode each token. For now, simplified encoding.
                
                // Encode token value
                boolhuff.vp8_encode_value(ref bc, token.value, 4);
                
                // Encode extra bits if present
                if (token.extra != 0)
                {
                    boolhuff.vp8_encode_value(ref bc, token.extra, 2);
                }
            }
        }
    }
}
