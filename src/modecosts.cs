//-----------------------------------------------------------------------------
// Filename: modecosts.cs
//
// Description: Port of:
//  - modecosts.h
//  - modecosts.c
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

using vp8_prob = System.Byte;

namespace Vpx.Net
{
    /// <summary>
    /// Mode cost calculations for encoder rate-distortion optimization.
    /// Computes the bit cost of using different prediction modes.
    /// </summary>
    public static class modecosts
    {
        /// <summary>
        /// Initialize all mode costs for rate-distortion optimization.
        /// Calculates the bit costs for various prediction modes based on probability trees.
        /// </summary>
        /// <param name="c">VP8 encoder context containing the cost structures</param>
        public unsafe static void vp8_init_mode_costs(VP8_COMP c)
        {
            VP8_COMMON x = c.common;
            RD_COSTS rd_costs = c.rd_costs;

            // Calculate costs for block prediction modes (B_PRED mode)
            // For each possible context (previous block modes)
            for (int i = 0; i < blockd.VP8_BINTRAMODES; i++)
            {
                for (int j = 0; j < blockd.VP8_BINTRAMODES; j++)
                {
                    fixed (vp8_prob* p = vp8_entropymodedata.vp8_kf_bmode_prob)
                    {
                        // Get pointer to the probability array for this context
                        vp8_prob* prob_ptr = p + (i * blockd.VP8_BINTRAMODES * (blockd.VP8_BINTRAMODES - 1)) +
                                                  (j * (blockd.VP8_BINTRAMODES - 1));
                        
                        // Calculate costs for all possible block modes in this context
                        fixed (int* costs = &rd_costs.bmode_costs[i, j, 0])
                        {
                            treewriter.vp8_cost_tokens(costs, prob_ptr, entropymode.vp8_bmode_tree);
                        }
                    }
                }
            }

            // Calculate costs for inter-frame block modes
            fixed (vp8_prob* p = x.fc.bmode_prob)
            fixed (int* costs = rd_costs.inter_bmode_costs)
            {
                treewriter.vp8_cost_tokens(costs, p, entropymode.vp8_bmode_tree);
            }

            // Calculate costs for sub-MV reference modes
            fixed (vp8_prob* p = x.fc.sub_mv_ref_prob)
            fixed (int* costs = rd_costs.inter_bmode_costs)
            {
                treewriter.vp8_cost_tokens(costs, p, entropymode.vp8_sub_mv_ref_tree);
            }

            // Calculate costs for macroblock Y mode (inter-frame)
            fixed (vp8_prob* p = x.fc.ymode_prob)
            fixed (int* costs = &rd_costs.mbmode_cost[1, 0])
            {
                treewriter.vp8_cost_tokens(costs, p, entropymode.vp8_ymode_tree);
            }

            // Calculate costs for macroblock Y mode (keyframe)
            fixed (vp8_prob* p = vp8_entropymodedata.vp8_kf_ymode_prob)
            fixed (int* costs = &rd_costs.mbmode_cost[0, 0])
            {
                treewriter.vp8_cost_tokens(costs, p, entropymode.vp8_kf_ymode_tree);
            }

            // Calculate costs for UV (chroma) mode (inter-frame)
            fixed (vp8_prob* p = x.fc.uv_mode_prob)
            fixed (int* costs = &rd_costs.intra_uv_mode_cost[1, 0])
            {
                treewriter.vp8_cost_tokens(costs, p, entropymode.vp8_uv_mode_tree);
            }

            // Calculate costs for UV (chroma) mode (keyframe)
            fixed (vp8_prob* p = vp8_entropymodedata.vp8_kf_uv_mode_prob)
            fixed (int* costs = &rd_costs.intra_uv_mode_cost[0, 0])
            {
                treewriter.vp8_cost_tokens(costs, p, entropymode.vp8_uv_mode_tree);
            }
        }
    }
}
