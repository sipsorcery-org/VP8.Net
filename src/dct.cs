//-----------------------------------------------------------------------------
// Filename: dct.cs
//
// Description: Port of:
//  - vp8/encoder/dct.c
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
//
// History:
// 17 Feb 2025	Aaron Clauson	Created, Dublin, Ireland.
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
    public unsafe static class dct
    {
        public static void vp8_short_fdct4x4_c(short* input, short* output, int pitch)
        {
            int i;
            int a1, b1, c1, d1;
            short* ip = input;
            short* op = output;

            for (i = 0; i < 4; ++i)
            {
                a1 = ((ip[0] + ip[3]) * 8);
                b1 = ((ip[1] + ip[2]) * 8);
                c1 = ((ip[1] - ip[2]) * 8);
                d1 = ((ip[0] - ip[3]) * 8);

                op[0] = (short)(a1 + b1);
                op[2] = (short)(a1 - b1);

                op[1] = (short)((c1 * 2217 + d1 * 5352 + 14500) >> 12);
                op[3] = (short)((d1 * 2217 - c1 * 5352 + 7500) >> 12);

                ip += pitch / 2;
                op += 4;
            }

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

                op[4] = (short)(((c1 * 2217 + d1 * 5352 + 12000) >> 16) + (d1 != 0 ? 1 : 0));
                op[12] = (short)((d1 * 2217 - c1 * 5352 + 51000) >> 16);

                ip++;
                op++;
            }
        }

        public static void vp8_short_fdct8x4_c(short* input, short* output, int pitch)
        {
            vp8_short_fdct4x4_c(input, output, pitch);
            vp8_short_fdct4x4_c(input + 4, output + 16, pitch);
        }

        public static void vp8_short_walsh4x4_c(short* input, short* output, int pitch)
        {
            int i;
            int a1, b1, c1, d1;
            int a2, b2, c2, d2;
            short* ip = input;
            short* op = output;

            for (i = 0; i < 4; ++i)
            {
                a1 = ((ip[0] + ip[2]) * 4);
                d1 = ((ip[1] + ip[3]) * 4);
                c1 = ((ip[1] - ip[3]) * 4);
                b1 = ((ip[0] - ip[2]) * 4);

                op[0] = (short)(a1 + d1 + (a1 != 0 ? 1 : 0));
                op[1] = (short)(b1 + c1);
                op[2] = (short)(b1 - c1);
                op[3] = (short)(a1 - d1);
                ip += pitch / 2;
                op += 4;
            }

            ip = output;
            op = output;

            for (i = 0; i < 4; ++i)
            {
                a1 = ip[0] + ip[8];
                d1 = ip[4] + ip[12];
                c1 = ip[4] - ip[12];
                b1 = ip[0] - ip[8];

                a2 = a1 + d1;
                b2 = b1 + c1;
                c2 = b1 - c1;
                d2 = a1 - d1;

                a2 += a2 < 0 ? 1 : 0;
                b2 += b2 < 0 ? 1 : 0;
                c2 += c2 < 0 ? 1 : 0;
                d2 += d2 < 0 ? 1 : 0;

                op[0] = (short)((a2 + 3) >> 3);
                op[4] = (short)((b2 + 3) >> 3);
                op[8] = (short)((c2 + 3) >> 3);
                op[12] = (short)((d2 + 3) >> 3);

                ip++;
                op++;
            }
        }
    }
}
