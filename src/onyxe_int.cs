//-----------------------------------------------------------------------------
// Filename: onyxe_int.cs
//
// Description: Port of:
//  - vp8/encoder/onyx_int.h (simplified)
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
    /// Simplified encoder macroblock structure.
    /// </summary>
    public unsafe class MACROBLOCK
    {
        public short[] src_diff = new short[400];  // 25 blocks Y,U,V,Y2
        public short[] coeff = new short[400];     // 25 blocks Y,U,V,Y2
        public byte[] thismb = new byte[256];      // Current macroblock pixels
        
        public MACROBLOCKD e_mbd = new MACROBLOCKD();  // Decoder MB info
        
        public int q_index;  // Quantization index
        
        // Function pointers for transform
        public Action<short[], short[], int> short_fdct4x4;
        public Action<short[], short[], int> short_fdct8x4;
        public Action<short[], short[], int> short_walsh4x4;
    }

    /// <summary>
    /// Simplified encoder context.
    /// </summary>
    public unsafe class VP8_COMP
    {
        public VP8_COMMON common = new VP8_COMMON();
        public vpx_codec_enc_cfg_t oxcf = new vpx_codec_enc_cfg_t();
        
        public MACROBLOCK mb = new MACROBLOCK();
        
        // Encoding state
        public int frame_count;
        public int key_frame_frequency;
        public bool force_next_frame_intra;
        
        // Rate control
        public int base_qindex;  // Base quantization index
        
        // Output buffer
        public byte[] output_buffer;
        public int output_buffer_size;
        
        // Boolean encoder for header and data
        public BOOL_CODER bc;
        public BOOL_CODER bc2;
    }
}
