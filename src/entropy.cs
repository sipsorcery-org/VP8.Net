//-----------------------------------------------------------------------------
// Filename: entropy.cs
//
// Description: Port of:
//  - entropy.h
//  - entropy.c
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
//
// History:
// 26 Oct 2020	Aaron Clauson	Created, Dublin, Ireland.
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
    public static class entropy
    {
        // Token constants for encoding/decoding coefficients
        public const int ZERO_TOKEN = 0;          // 0         Extra Bits 0+0
        public const int ONE_TOKEN = 1;           // 1         Extra Bits 0+1
        public const int TWO_TOKEN = 2;           // 2         Extra Bits 0+1
        public const int THREE_TOKEN = 3;         // 3         Extra Bits 0+1
        public const int FOUR_TOKEN = 4;          // 4         Extra Bits 0+1
        public const int DCT_VAL_CATEGORY1 = 5;   // 5-6       Extra Bits 1+1
        public const int DCT_VAL_CATEGORY2 = 6;   // 7-10      Extra Bits 2+1
        public const int DCT_VAL_CATEGORY3 = 7;   // 11-18     Extra Bits 3+1
        public const int DCT_VAL_CATEGORY4 = 8;   // 19-34     Extra Bits 4+1
        public const int DCT_VAL_CATEGORY5 = 9;   // 35-66     Extra Bits 5+1
        public const int DCT_VAL_CATEGORY6 = 10;  // 67+       Extra Bits 11+1
        public const int DCT_EOB_TOKEN = 11;      // EOB       Extra Bits 0+0
        
        public const int MAX_ENTROPY_TOKENS = 12;
        public const int ENTROPY_NODES = 11;
        
        // Coefficient band mapping (position to band)
        public static readonly byte[] vp8_coef_bands = {
            0, 1, 2, 3, 6, 4, 5, 6, 6, 6, 6, 6, 6, 6, 6, 7
        };

        public static readonly byte[] vp8_norm = {
            0, 7, 6, 6, 5, 5, 5, 5, 4, 4, 4, 4, 4, 4, 4, 4, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3,
            3, 3, 3, 3, 3, 3, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
            2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
            1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
            0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        };

        /* Coefficients are predicted via a 3-dimensional probability table. */
        /* Outside dimension.  0 = Y no DC, 1 = Y2, 2 = UV, 3 = Y with DC */
        public const int BLOCK_TYPES = 4;

        /* Middle dimension is a coarsening of the coefficient's
        position within the 4x4 DCT. */
        public const int COEF_BANDS = 8;

        /*# define DC_TOKEN_CONTEXTS        3*/ /* 00, 0!0, !0!0 */
        public const int PREV_COEF_CONTEXTS = 3;

        //const int vp8_mb_feature_data_bits[MB_LVL_MAX] = { 7, 6 };
        public static readonly int[] vp8_mb_feature_data_bits = { 7, 6 };

        public static void vp8_default_coef_probs(VP8_COMMON pc)
        {
            //memcpy(pc->fc.coef_probs, default_coef_probs, sizeof(default_coef_probs));
            Array.Copy(default_coef_probs_c.default_coef_probs, pc.fc.coef_probs, default_coef_probs_c.default_coef_probs.Length);
        }
    }
}
