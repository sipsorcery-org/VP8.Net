//-----------------------------------------------------------------------------
// Filename: fdctllm.cs
//
// Description: Forward DCT implementation for VP8 encoder
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

namespace Vpx.Net
{
    /// <summary>
    /// Forward DCT - converts spatial domain to frequency domain
    /// This is the inverse operation of the IDCT in idctllm.cs
    /// </summary>
    public static unsafe class fdctllm
    {
        // Same constants as IDCT but used in forward direction
        private const int cospi8sqrt2minus1 = 20091;
        private const int sinpi8sqrt2 = 35468;

        /// <summary>
        /// Forward 4x4 DCT
        /// </summary>
        /// <param name="input">4x4 block of residual values (difference between original and prediction)</param>
        /// <param name="output">4x4 block of DCT coefficients</param>
        public static void vp8_short_fdct4x4_c(short* input, short* output, int stride)
        {
            int i;
            int a1, b1, c1, d1;
            short* ip = input;
            short* op = output;

            // First pass - process rows
            for (i = 0; i < 4; ++i)
            {
                a1 = (ip[0] + ip[3]) << 3;
                b1 = (ip[1] + ip[2]) << 3;
                c1 = (ip[1] - ip[2]) << 3;
                d1 = (ip[0] - ip[3]) << 3;

                op[0] = (short)(a1 + b1);
                op[2] = (short)(a1 - b1);

                op[1] = (short)((c1 * 2217 + d1 * 5352 + 14500) >> 12);
                op[3] = (short)((d1 * 2217 - c1 * 5352 + 7500) >> 12);

                ip += stride;
                op += 4;
            }

            // Second pass - process columns
            ip = output;
            op = output;
            for (i = 0; i < 4; ++i)
            {
                a1 = ip[0] + ip[12];
                b1 = ip[4] + ip[8];
                c1 = ip[4] - ip[8];
                d1 = ip[0] - ip[12];

                op[0] = (short)((a1 + b1 + 7) >> 4);
                op[8] = (short)((a1 - b1 + 7) >> 4);

                op[4] = (short)(((c1 * 2217 + d1 * 5352 + 12000) >> 16) + ((d1 != 0) ? 1 : 0));
                op[12] = (short)((d1 * 2217 - c1 * 5352 + 51000) >> 16);

                ++ip;
                ++op;
            }
        }

        /// <summary>
        /// Forward Walsh-Hadamard Transform for DC coefficients
        /// </summary>
        public static void vp8_short_walsh4x4_c(short* input, short* output, int stride)
        {
            int i;
            int a1, b1, c1, d1;
            short* ip = input;
            short* op = output;
            
            // First pass
            for (i = 0; i < 4; ++i)
            {
                a1 = ip[0] + ip[12];
                b1 = ip[4] + ip[8];
                c1 = ip[4] - ip[8];
                d1 = ip[0] - ip[12];

                op[0] = (short)(a1 + b1);
                op[4] = (short)(c1 + d1);
                op[8] = (short)(a1 - b1);
                op[12] = (short)(d1 - c1);
                
                ++ip;
                ++op;
            }

            // Second pass
            ip = output;
            op = output;
            for (i = 0; i < 4; ++i)
            {
                a1 = ip[0] + ip[3];
                b1 = ip[1] + ip[2];
                c1 = ip[1] - ip[2];
                d1 = ip[0] - ip[3];

                op[0] = (short)((a1 + b1 + 1) >> 1);
                op[1] = (short)((c1 + d1) >> 1);
                op[2] = (short)((a1 - b1) >> 1);
                op[3] = (short)((d1 - c1) >> 1);

                ip += 4;
                op += 4;
            }
        }
    }
}
