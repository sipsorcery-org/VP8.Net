//-----------------------------------------------------------------------------
// Filename: block.cs
//
// Description: Port of:
//  - vp8/encoder/block.h
//
// Author(s):
// Aaron Clauson (aaron@sipsorcery.com)
//
// History:
// 09 Jan 2025	Aaron Clauson	Created, Dublin, Ireland.
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
    public static class vp8_block
    {
        public const int MAX_MODES = 20;
        public const int MAX_ERROR_BINS = 1024;
    }

    public unsafe struct BLOCK
    {
        public short* src_diff;
        public short* coeff;

        public short* quant;
        public short* quant_fast;
        public short* quant_shift;
        public short* zbin;
        public short* zrun_zbin_boost;
        public short* round;

        public short zbin_extra;

        public byte** base_src;
        public int src;
        public int src_stride;
    }

    public struct PARTITION_INFO
    {
        public int count;
        public PARTITION_BMI[] bmi;

        public PARTITION_INFO()
        {
            bmi = new PARTITION_BMI[16];
        }
    }

    public struct PARTITION_BMI
    {
        public B_PREDICTION_MODE mode;
        public int_mv mv;
    }

    public unsafe class MACROBLOCK
    {
        public short[] src_diff = new short[400];
        public short[] coeff = new short[400];
        public byte[] thismb = new byte[256];

        public byte* thismb_ptr;

        public BLOCK[] block = new BLOCK[25];

        public YV12_BUFFER_CONFIG src;

        public MACROBLOCKD e_mbd = new MACROBLOCKD();
        public PARTITION_INFO* partition_info;
        public PARTITION_INFO* pi;
        public PARTITION_INFO* pip;

        public int[] ref_frame_cost = new int[(int)MV_REFERENCE_FRAME.MAX_REF_FRAMES];

        public search_site* ss;
        public int ss_count;
        public int searches_per_step;

        public int errorperbit;
        public int sadperbit16;
        public int sadperbit4;
        public int rddiv;
        public int rdmult;
        public uint* mb_activity_ptr;
        public int* mb_norm_activity_ptr;
        public int act_zbin_adj;
        public int last_act_zbin_adj;

        public int*[] mvcost = new int*[2];
        public int*[] mvsadcost = new int*[2];
        public int* mbmode_cost;
        public int* intra_uv_mode_cost;
        public int* bmode_costs;
        public int* inter_bmode_costs;
        public int* token_costs;

        public int mv_col_min;
        public int mv_col_max;
        public int mv_row_min;
        public int mv_row_max;

        public int skip;

        public uint encode_breakout;

        public sbyte* gf_active_ptr;

        public byte* active_ptr;
        public MV_CONTEXT* mvc;

        public int optimize;
        public int q_index;
        public int is_skin;
        public int denoise_zeromv;

        public int increase_denoising;
        public MB_PREDICTION_MODE best_sse_inter_mode;
        public int_mv best_sse_mv;
        public MV_REFERENCE_FRAME best_reference_frame;
        public MV_REFERENCE_FRAME best_zeromv_reference_frame;
        public byte need_to_clamp_best_mvs;

        public int skip_true_count;
        public uint[,,,] coef_counts;
        public uint[,] MVcount;
        public int[] ymode_count;
        public int[] uv_mode_count;
        public long prediction_error;
        public long intra_error;
        public int[] count_mb_ref_frame_usage;

        public int[] rd_thresh_mult;
        public int[] rd_threshes;
        public uint mbs_tested_so_far;
        public uint[] mode_test_hit_counts;
        public int zbin_mode_boost_enabled;
        public int zbin_mode_boost;
        public int last_zbin_mode_boost;

        public int last_zbin_over_quant;
        public int zbin_over_quant;
        public int[] error_bins;

        public void* short_fdct4x4;
        public void* short_fdct8x4;
        public void* short_walsh4x4;
        public void* quantize_b;

        public uint mbs_zero_last_dot_suppress;
        public int zero_last_dot_suppress;

        public MACROBLOCK()
        {
            coef_counts = new uint[entropy.BLOCK_TYPES, entropy.COEF_BANDS, 
                entropy.PREV_COEF_CONTEXTS, entropy.MAX_ENTROPY_TOKENS];
            MVcount = new uint[2, (int)MV_ENUM.MVvals];
            ymode_count = new int[blockd.VP8_YMODES];
            uv_mode_count = new int[blockd.VP8_UV_MODES];
            count_mb_ref_frame_usage = new int[(int)MV_REFERENCE_FRAME.MAX_REF_FRAMES];
            rd_thresh_mult = new int[vp8_block.MAX_MODES];
            rd_threshes = new int[vp8_block.MAX_MODES];
            mode_test_hit_counts = new uint[vp8_block.MAX_MODES];
            error_bins = new int[vp8_block.MAX_ERROR_BINS];
        }
    }
}
