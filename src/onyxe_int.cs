//-----------------------------------------------------------------------------
// Filename: onyxe_int.cs
//
// Description: Port of:
//  - vp8/encoder/onyx_int.h
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
using vp8_prob = System.Byte;
using vpx_rational = Vpx.Net.vpx_rational_t;
using vp8_writer = Vpx.Net.BOOL_CODER;

namespace Vpx.Net
{
    public static class onyxe_int
    {
        public const int MIN_GF_INTERVAL = 4;
        public const int DEFAULT_GF_INTERVAL = 7;
        public const int KEY_FRAME_CONTEXT = 5;
        public const int MAX_LAG_BUFFERS = 25;
        public const int AF_THRESH = 25;
        public const int AF_THRESH2 = 100;
        public const int ARF_DECAY_THRESH = 12;
        public const int MIN_THRESHMULT = 32;
        public const int MAX_THRESHMULT = 512;
        public const int GF_ZEROMV_ZBIN_BOOST = 12;
        public const int LF_ZEROMV_ZBIN_BOOST = 6;
        public const int MV_ZBIN_BOOST = 4;
        public const int ZBIN_OQ_MAX = 192;
        public const int TICKS_PER_SEC = 10000000;
    }

    public class CODING_CONTEXT
    {
        public int kf_indicated;
        public uint frames_since_key;
        public uint frames_since_golden;
        public int filter_level;
        public int frames_till_gf_update_due;
        public int[] recent_ref_frame_usage = new int[(int)MV_REFERENCE_FRAME.MAX_REF_FRAMES];

        public MV_CONTEXT[] mvc = new MV_CONTEXT[2];
        public int[,] mvcosts = new int[2, (int)MV_ENUM.MVvals + 1];

        public vp8_prob[] ymode_prob = new vp8_prob[4];
        public vp8_prob[] uv_mode_prob = new vp8_prob[3];
        public vp8_prob[] kf_ymode_prob = new vp8_prob[4];
        public vp8_prob[] kf_uv_mode_prob = new vp8_prob[3];

        public int[] ymode_count = new int[5];
        public int[] uv_mode_count = new int[4];

        public int[] count_mb_ref_frame_usage = new int[(int)MV_REFERENCE_FRAME.MAX_REF_FRAMES];

        public int this_frame_percent_intra;
        public int last_frame_percent_intra;

        public CODING_CONTEXT()
        {
            mvc[0] = new MV_CONTEXT();
            mvc[1] = new MV_CONTEXT();
        }
    }

    public struct FIRSTPASS_STATS
    {
        public double frame;
        public double intra_error;
        public double coded_error;
        public double ssim_weighted_pred_err;
        public double pcnt_inter;
        public double pcnt_motion;
        public double pcnt_second_ref;
        public double pcnt_neutral;
        public double MVr;
        public double mvr_abs;
        public double MVc;
        public double mvc_abs;
        public double MVrv;
        public double MVcv;
        public double mv_in_out_count;
        public double new_mv_count;
        public double duration;
        public double count;
    }

    public struct ONEPASS_FRAMESTATS
    {
        public int frames_so_far;
        public double frame_intra_error;
        public double frame_coded_error;
        public double frame_pcnt_inter;
        public double frame_pcnt_motion;
        public double frame_mvr;
        public double frame_mvr_abs;
        public double frame_mvc;
        public double frame_mvc_abs;
    }

    public enum THR_MODES
    {
        THR_ZERO1 = 0,
        THR_DC = 1,
        THR_NEAREST1 = 2,
        THR_NEAR1 = 3,
        THR_ZERO2 = 4,
        THR_NEAREST2 = 5,
        THR_ZERO3 = 6,
        THR_NEAREST3 = 7,
        THR_NEAR2 = 8,
        THR_NEAR3 = 9,
        THR_V_PRED = 10,
        THR_H_PRED = 11,
        THR_TM = 12,
        THR_NEW1 = 13,
        THR_NEW2 = 14,
        THR_NEW3 = 15,
        THR_SPLIT1 = 16,
        THR_SPLIT2 = 17,
        THR_SPLIT3 = 18,
        THR_B_PRED = 19
    }

    public enum SEARCH_METHODS
    {
        DIAMOND = 0,
        NSTEP = 1,
        HEX = 2
    }

    public enum BLOCK_SIZE
    {
        BLOCK_16X8,
        BLOCK_8X16,
        BLOCK_8X8,
        BLOCK_4X4,
        BLOCK_16X16,
        BLOCK_MAX_SEGMENTS
    }

    public class SPEED_FEATURES
    {
        public int RD;
        public SEARCH_METHODS search_method;
        public int improved_quant;
        public int improved_dct;
        public int auto_filter;
        public int recode_loop;
        public int iterative_sub_pixel;
        public int half_pixel_search;
        public int quarter_pixel_search;
        public int[] thresh_mult = new int[vp8_block.MAX_MODES];
        public int max_step_search_steps;
        public int first_step;
        public int optimize_coefficients;
        public int use_fastquant_for_pick;
        public int no_skip_block4x4_search;
        public int improved_mv_pred;
    }

    public class LAYER_CONTEXT
    {
        public double framerate;
        public int target_bandwidth;

        public long starting_buffer_level;
        public long optimal_buffer_level;
        public long maximum_buffer_size;
        public long starting_buffer_level_in_ms;
        public long optimal_buffer_level_in_ms;
        public long maximum_buffer_size_in_ms;

        public int avg_frame_size_for_layer;

        public long buffer_level;
        public long bits_off_target;

        public long total_actual_bits;
        public int total_target_vs_actual;

        public int worst_quality;
        public int active_worst_quality;
        public int best_quality;
        public int active_best_quality;

        public int ni_av_qi;
        public int ni_tot_qi;
        public int ni_frames;
        public int avg_frame_qindex;

        public double rate_correction_factor;
        public double key_frame_rate_correction_factor;
        public double gf_rate_correction_factor;

        public int zbin_over_quant;

        public int inter_frame_target;
        public long total_byte_count;

        public int filter_level;

        public int frames_since_last_drop_overshoot;

        public int force_maxqp;

        public int last_frame_percent_intra;

        public int[] count_mb_ref_frame_usage = new int[(int)MV_REFERENCE_FRAME.MAX_REF_FRAMES];

        public int[] last_q = new int[2];
    }

    public class TWOPASS_RC
    {
        public uint section_intra_rating;
        public double section_max_qfactor;
        public uint next_iiratio;
        public uint this_iiratio;
        public FIRSTPASS_STATS total_stats;
        public FIRSTPASS_STATS this_frame_stats;
        public unsafe FIRSTPASS_STATS* stats_in;
        public unsafe FIRSTPASS_STATS* stats_in_end;
        public unsafe FIRSTPASS_STATS* stats_in_start;
        public FIRSTPASS_STATS total_left_stats;
        public int first_pass_done;
        public long bits_left;
        public long clip_bits_total;
        public double avg_iiratio;
        public double modified_error_total;
        public double modified_error_used;
        public double modified_error_left;
        public double kf_intra_err_min;
        public double gf_intra_err_min;
        public int frames_to_key;
        public int maxq_max_limit;
        public int maxq_min_limit;
        public int gf_decay_rate;
        public int static_scene_max_gf_interval;
        public int kf_bits;
        public int gf_group_error_left;
        public long kf_group_bits;
        public long kf_group_error_left64;
        public long gf_group_bits;
        public int gf_bits;
        public int alt_extra_bits;
        public double est_max_qcorrection_factor;
    }

    public unsafe class VP8_COMP
    {
        public short[,] Y1quant = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] Y1quant_shift = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] Y1zbin = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] Y1round = new short[VP8_COMMON.QINDEX_RANGE, 16];

        public short[,] Y2quant = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] Y2quant_shift = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] Y2zbin = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] Y2round = new short[VP8_COMMON.QINDEX_RANGE, 16];

        public short[,] UVquant = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] UVquant_shift = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] UVzbin = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] UVround = new short[VP8_COMMON.QINDEX_RANGE, 16];

        public short[,] zrun_zbin_boost_y1 = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] zrun_zbin_boost_y2 = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] zrun_zbin_boost_uv = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] Y1quant_fast = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] Y2quant_fast = new short[VP8_COMMON.QINDEX_RANGE, 16];
        public short[,] UVquant_fast = new short[VP8_COMMON.QINDEX_RANGE, 16];

        public MACROBLOCK mb = new MACROBLOCK();
        public VP8_COMMON common = new VP8_COMMON();
        public vp8_writer[] bc = new vp8_writer[9];

        public VP8_CONFIG oxcf = new VP8_CONFIG();

        public void* lookahead;
        public void* source;
        public void* alt_ref_source;
        public void* last_source;

        public YV12_BUFFER_CONFIG* Source;
        public YV12_BUFFER_CONFIG* un_scaled_source;
        public YV12_BUFFER_CONFIG scaled_source;
        public YV12_BUFFER_CONFIG* last_frame_unscaled_source;

        public uint frames_till_alt_ref_frame;
        public int source_alt_ref_pending;
        public int source_alt_ref_active;
        public int is_src_frame_alt_ref;

        public int gold_is_last;
        public int alt_is_last;
        public int gold_is_alt;

        public YV12_BUFFER_CONFIG pick_lf_lvl_frame;

        public TOKENEXTRA* tok;
        public uint tok_count;

        public uint frames_since_key;
        public uint key_frame_frequency;
        public uint this_key_frame_forced;
        public uint next_key_frame_forced;

        public int ambient_err;

        public uint[] mode_check_freq = new uint[vp8_block.MAX_MODES];
        public int[] rd_baseline_thresh = new int[vp8_block.MAX_MODES];

        public int RDMULT;
        public int RDDIV;

        public CODING_CONTEXT coding_context = new CODING_CONTEXT();

        public long last_prediction_error;
        public long last_intra_error;

        public int this_frame_target;
        public int projected_frame_size;
        public int[] last_q = new int[2];

        public double rate_correction_factor;
        public double key_frame_rate_correction_factor;
        public double gf_rate_correction_factor;

        public int frames_since_golden;
        public int frames_till_gf_update_due;
        public int current_gf_interval;

        public int gf_overspend_bits;
        public int non_gf_bitrate_adjustment;
        public int kf_overspend_bits;
        public int kf_bitrate_adjustment;
        public int max_gf_interval;
        public int baseline_gf_interval;
        public int active_arnr_frames;

        public long key_frame_count;
        public int[] prior_key_frame_distance = new int[onyxe_int.KEY_FRAME_CONTEXT];

        public int per_frame_bandwidth;
        public int av_per_frame_bandwidth;
        public int min_frame_bandwidth;
        public int inter_frame_target;
        public double output_framerate;
        public long last_time_stamp_seen;
        public long last_end_time_stamp_seen;
        public long first_time_stamp_ever;

        public int ni_av_qi;
        public int ni_tot_qi;
        public int ni_frames;
        public int avg_frame_qindex;

        public long total_byte_count;

        public int buffered_mode;

        public double framerate;
        public double ref_framerate;
        public long buffer_level;
        public long bits_off_target;

        public int rolling_target_bits;
        public int rolling_actual_bits;

        public int long_rolling_target_bits;
        public int long_rolling_actual_bits;

        public long total_actual_bits;
        public int total_target_vs_actual;

        public int worst_quality;
        public int active_worst_quality;
        public int best_quality;
        public int active_best_quality;

        public int cq_target_quality;

        public int drop_frames_allowed;
        public int drop_frame;

        public vp8_prob[,,,] frame_coef_probs = new vp8_prob[entropy.BLOCK_TYPES, entropy.COEF_BANDS, 
            entropy.PREV_COEF_CONTEXTS, entropy.ENTROPY_NODES];
        public sbyte[,,,] update_probs = new sbyte[entropy.BLOCK_TYPES, entropy.COEF_BANDS, 
            entropy.PREV_COEF_CONTEXTS, entropy.ENTROPY_NODES];

        public uint[,,,,] frame_branch_ct = new uint[entropy.BLOCK_TYPES, entropy.COEF_BANDS, 
            entropy.PREV_COEF_CONTEXTS, entropy.ENTROPY_NODES, 2];

        public int gfu_boost;
        public int kf_boost;
        public int last_boost;

        public int target_bandwidth;
        public void* output_pkt_list;

        public int decimation_factor;
        public int decimation_count;

        public int avg_encode_time;
        public int avg_pick_mode_time;
        public int Speed;
        public int compressor_speed;

        public int auto_gold;
        public int auto_adjust_gold_quantizer;
        public int auto_worst_q;
        public int cpu_used;
        public int pass;

        public int prob_intra_coded;
        public int prob_last_coded;
        public int prob_gf_coded;
        public int prob_skip_false;
        public int[] last_skip_false_probs = new int[3];
        public int[] last_skip_probs_q = new int[3];
        public int[] recent_ref_frame_usage = new int[(int)MV_REFERENCE_FRAME.MAX_REF_FRAMES];

        public int this_frame_percent_intra;
        public int last_frame_percent_intra;

        public int ref_frame_flags;

        public SPEED_FEATURES sf = new SPEED_FEATURES();

        public int zeromv_count;
        public int lf_zeromv_pct;

        public byte* skin_map;

        public byte* segmentation_map;
        public sbyte[,] segment_feature_data = new sbyte[(int)MB_LVL_FEATURES.MB_LVL_MAX, 
            blockd.MAX_MB_SEGMENTS];
        public int[] segment_encode_breakout = new int[blockd.MAX_MB_SEGMENTS];

        public byte* active_map;
        public uint active_map_enabled;

        public int cyclic_refresh_mode_enabled;
        public int cyclic_refresh_mode_max_mbs_perframe;
        public int cyclic_refresh_mode_index;
        public int cyclic_refresh_q;
        public sbyte* cyclic_refresh_map;
        public byte* consec_zero_last;
        public byte* consec_zero_last_mvbias;

        public uint temporal_pattern_counter;
        public int temporal_layer_id;

        public int mse_source_denoised;

        public int force_maxqp;
        public int frames_since_last_drop_overshoot;
        public int last_pred_err_mb;

        public int gf_update_onepass_cbr;
        public int gf_interval_onepass_cbr;
        public int gf_noboost_onepass_cbr;

        public TOKENLIST* tplist;
        public uint[] partition_sz = new uint[onyx.MAX_PARTITIONS];
        public byte*[] partition_d = new byte*[onyx.MAX_PARTITIONS];
        public byte*[] partition_d_end = new byte*[onyx.MAX_PARTITIONS];

        public void* find_fractional_mv_step;
        public void* full_search_sad;
        public void* refining_search_sad;
        public void* diamond_search_sad;
        public void*[] fn_ptr = new void*[(int)BLOCK_SIZE.BLOCK_MAX_SEGMENTS];
        public ulong time_receive_data;
        public ulong time_compress_data;
        public ulong time_pick_lpf;
        public ulong time_encode_mb_row;

        public int[] base_skip_false_prob = new int[128];

        public FRAME_CONTEXT lfc_n = new FRAME_CONTEXT();
        public FRAME_CONTEXT lfc_a = new FRAME_CONTEXT();
        public FRAME_CONTEXT lfc_g = new FRAME_CONTEXT();

        public TWOPASS_RC twopass = new TWOPASS_RC();

        public YV12_BUFFER_CONFIG alt_ref_buffer;
        public YV12_BUFFER_CONFIG*[] frames = new YV12_BUFFER_CONFIG*[onyxe_int.MAX_LAG_BUFFERS];
        public int[] fixed_divide = new int[512];

        public int b_calculate_psnr;

        public uint activity_avg;
        public uint* mb_activity_map;

        public byte* gf_active_flags;
        public int gf_active_count;

        public int output_partition;

        public int_mv* lfmv;
        public int* lf_ref_frame_sign_bias;
        public int* lf_ref_frame;

        public int force_next_frame_intra;

        public int droppable;

        public int initial_width;
        public int initial_height;

        public uint current_layer;
        public LAYER_CONTEXT[] layer_context = new LAYER_CONTEXT[vpx_encoder.VPX_TS_MAX_LAYERS];

        public long[] frames_in_layer = new long[vpx_encoder.VPX_TS_MAX_LAYERS];
        public long[] bytes_in_layer = new long[vpx_encoder.VPX_TS_MAX_LAYERS];
        public double[] sum_psnr = new double[vpx_encoder.VPX_TS_MAX_LAYERS];
        public double[] sum_psnr_p = new double[vpx_encoder.VPX_TS_MAX_LAYERS];
        public double[] total_error2 = new double[vpx_encoder.VPX_TS_MAX_LAYERS];
        public double[] total_error2_p = new double[vpx_encoder.VPX_TS_MAX_LAYERS];
        public double[] sum_ssim = new double[vpx_encoder.VPX_TS_MAX_LAYERS];
        public double[] sum_weights = new double[vpx_encoder.VPX_TS_MAX_LAYERS];

        public double[] total_ssimg_y_in_layer = new double[vpx_encoder.VPX_TS_MAX_LAYERS];
        public double[] total_ssimg_u_in_layer = new double[vpx_encoder.VPX_TS_MAX_LAYERS];
        public double[] total_ssimg_v_in_layer = new double[vpx_encoder.VPX_TS_MAX_LAYERS];
        public double[] total_ssimg_all_in_layer = new double[vpx_encoder.VPX_TS_MAX_LAYERS];

        public uint[] current_ref_frames = new uint[(int)MV_REFERENCE_FRAME.MAX_REF_FRAMES];
        public MV_REFERENCE_FRAME closest_reference_frame;

        public RD_COSTS rd_costs = new RD_COSTS();

        public int use_roi_static_threshold;

        public int ext_refresh_frame_flags_pending;

        public VP8_COMP()
        {
            for (int i = 0; i < bc.Length; i++)
            {
                bc[i] = new vp8_writer();
            }

            for (int i = 0; i < layer_context.Length; i++)
            {
                layer_context[i] = new LAYER_CONTEXT();
            }
        }
    }

    public struct RD_COSTS
    {
        public int[,] mvcosts;
        public int[,] mvsadcosts;
        public int[,] mbmode_cost;
        public int[,] intra_uv_mode_cost;
        public int[,,] bmode_costs;
        public int[] inter_bmode_costs;
        public int[,,,] token_costs;
    }

    public struct TOKENEXTRA
    {
        public byte token;
        public byte extra;
        public byte context_tree;
        public byte skip_eob_node;
    }

    public struct TOKENLIST
    {
        public unsafe TOKENEXTRA* start;
        public unsafe TOKENEXTRA* stop;
    }

    public enum VPX_SCALING
    {
        NORMAL = 0,
        FOURFIVE = 1,
        THREEFIVE = 2,
        ONETWO = 3
    }

    public enum END_USAGE
    {
        USAGE_LOCAL_FILE_PLAYBACK = 0x0,
        USAGE_STREAM_FROM_SERVER = 0x1,
        USAGE_CONSTRAINED_QUALITY = 0x2,
        USAGE_CONSTANT_QUALITY = 0x3
    }

    public enum MODE
    {
        MODE_REALTIME = 0x0,
        MODE_GOODQUALITY = 0x1,
        MODE_BESTQUALITY = 0x2,
        MODE_FIRSTPASS = 0x3,
        MODE_SECONDPASS = 0x4,
        MODE_SECONDPASS_BEST = 0x5
    }

    public enum FRAMETYPE_FLAGS
    {
        FRAMEFLAGS_KEY = 1,
        FRAMEFLAGS_GOLDEN = 2,
        FRAMEFLAGS_ALTREF = 4
    }

    public class VP8_CONFIG
    {
        public int Version;
        public int Width;
        public int Height;
        public vpx_rational timebase;
        public uint target_bandwidth;

        public int noise_sensitivity;

        public int Sharpness;
        public int cpu_used;
        public uint rc_max_intra_bitrate_pct;
        public uint gf_cbr_boost_pct;
        public uint screen_content_mode;

        public int Mode;

        public int auto_key;
        public int key_freq;

        public int allow_lag;
        public int lag_in_frames;

        public int end_usage;

        public int under_shoot_pct;
        public int over_shoot_pct;

        public long starting_buffer_level;
        public long optimal_buffer_level;
        public long maximum_buffer_size;

        public long starting_buffer_level_in_ms;
        public long optimal_buffer_level_in_ms;
        public long maximum_buffer_size_in_ms;

        public int fixed_q;
        public int worst_allowed_q;
        public int best_allowed_q;
        public int cq_level;

        public int allow_spatial_resampling;
        public int resample_down_water_mark;
        public int resample_up_water_mark;

        public int allow_df;
        public int drop_frames_water_mark;

        public int two_pass_vbrbias;
        public int two_pass_vbrmin_section;
        public int two_pass_vbrmax_section;

        public int play_alternate;
        public int alt_freq;
        public int alt_q;
        public int key_q;
        public int gold_q;

        public int multi_threaded;
        public int token_partitions;

        public int encode_breakout;

        public uint error_resilient_mode;

        public int arnr_max_frames;
        public int arnr_strength;
        public int arnr_type;

        public vpx_fixed_buf_t two_pass_stats_in;
        public unsafe void* output_pkt_list;

        public vp8e_tuning tuning;

        public uint number_of_layers;
        public uint[] target_bitrate = new uint[vpx_encoder.VPX_TS_MAX_PERIODICITY];
        public uint[] rate_decimator = new uint[vpx_encoder.VPX_TS_MAX_PERIODICITY];
        public uint periodicity;
        public uint[] layer_id = new uint[vpx_encoder.VPX_TS_MAX_PERIODICITY];
    }

    public struct search_site
    {
        public MV mv;
        public int offset;
    }

    public struct MB_ROW_COMP
    {
        public MACROBLOCK mb;
        public int[] segment_counts;
        public int totalrate;
    }
}
