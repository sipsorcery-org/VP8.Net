//-----------------------------------------------------------------------------
// Filename: treewriter.cs
//
// Description: Port of:
//  - treewriter.h
//  - treewriter.c
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

using System;

using vp8_prob = System.Byte;
using vp8_writer = Vpx.Net.BOOL_CODER;
using vp8_tree_index = System.SByte;
using vp8_tree = System.SByte;

namespace Vpx.Net
{
    /// <summary>
    /// Trees map alphabets into huffman-like codes suitable for an arithmetic
    /// bit coder. Timothy S Murphy  11 October 2004
    /// </summary>
    /// <remarks>
    /// This is the encoder equivalent of treereader.cs
    /// </remarks>
    public struct vp8_token
    {
        public int value;
        public int Len;

        public vp8_token(int value, int len)
        {
            this.value = value;
            this.Len = len;
        }
    }

    public static class treewriter
    {
        public const byte vp8_prob_half = 128;

        public static byte vp8_complement(byte x) => (byte)(255 - x);

        // Approximate length of an encoded bool in 256ths of a bit at given prob
        public static uint vp8_cost_zero(byte prob) => boolhuff.vp8_prob_cost[prob];
        public static uint vp8_cost_one(byte prob) => vp8_cost_zero(vp8_complement(prob));
        public static uint vp8_cost_bit(byte prob, int bit) => vp8_cost_zero(bit != 0 ? vp8_complement(prob) : prob);

        /// <summary>
        /// Calculate the cost of a branch given counts.
        /// </summary>
        public static uint vp8_cost_branch(uint[] ct, vp8_prob p)
        {
            return (uint)(((((ulong)ct[0]) * vp8_cost_zero(p)) +
                           (((ulong)ct[1]) * vp8_cost_one(p))) >> 8);
        }

        /// <summary>
        /// Write a value to the bitstream using a tree.
        /// </summary>
        public static void vp8_treed_write(ref vp8_writer w, vp8_tree[] t,
                                          vp8_prob[] p, int v, int n)
        {
            vp8_tree_index i = 0;

            do
            {
                int b = (v >> --n) & 1;
                boolhuff.vp8_encode_bool(ref w, b, p[i >> 1]);
                i = t[i + b];
            } while (n > 0);
        }

        public unsafe static void vp8_treed_write(ref vp8_writer w, vp8_tree[] t,
                                                  vp8_prob* p, int v, int n)
        {
            vp8_tree_index i = 0;

            do
            {
                int b = (v >> --n) & 1;
                boolhuff.vp8_encode_bool(ref w, b, p[i >> 1]);
                i = t[i + b];
            } while (n > 0);
        }

        /// <summary>
        /// Write a token using tree.
        /// </summary>
        public static void vp8_write_token(ref vp8_writer w, vp8_tree[] t,
                                          vp8_prob[] p, vp8_token x)
        {
            vp8_treed_write(ref w, t, p, x.value, x.Len);
        }

        public unsafe static void vp8_write_token(ref vp8_writer w, vp8_tree[] t,
                                                  vp8_prob* p, vp8_token x)
        {
            vp8_treed_write(ref w, t, p, x.value, x.Len);
        }

        /// <summary>
        /// Calculate the cost of encoding a value using a tree.
        /// </summary>
        public static int vp8_treed_cost(vp8_tree[] t, vp8_prob[] p, int v, int n)
        {
            int c = 0;
            vp8_tree_index i = 0;

            do
            {
                int b = (v >> --n) & 1;
                c += (int)vp8_cost_bit(p[i >> 1], b);
                i = t[i + b];
            } while (n > 0);

            return c;
        }

        public unsafe static int vp8_treed_cost(vp8_tree[] t, vp8_prob* p, int v, int n)
        {
            int c = 0;
            vp8_tree_index i = 0;

            do
            {
                int b = (v >> --n) & 1;
                c += (int)vp8_cost_bit(p[i >> 1], b);
                i = t[i + b];
            } while (n > 0);

            return c;
        }

        /// <summary>
        /// Calculate the cost of encoding a token.
        /// </summary>
        public static int vp8_cost_token(vp8_tree[] t, vp8_prob[] p, vp8_token x)
        {
            return vp8_treed_cost(t, p, x.value, x.Len);
        }

        public unsafe static int vp8_cost_token(vp8_tree[] t, vp8_prob* p, vp8_token x)
        {
            return vp8_treed_cost(t, p, x.value, x.Len);
        }

        /// <summary>
        /// Recursively calculate costs for all possible token values.
        /// </summary>
        private unsafe static void cost(int* C, vp8_tree[] T, vp8_prob* P, int i, int c)
        {
            vp8_prob p = P[i >> 1];

            do
            {
                vp8_tree_index j = T[i];
                int d = c + (int)vp8_cost_bit(p, i & 1);

                if (j <= 0)
                {
                    C[-j] = d;
                }
                else
                {
                    cost(C, T, P, j, d);
                }
            } while ((++i & 1) != 0);
        }

        /// <summary>
        /// Fill array of costs for all possible token values.
        /// </summary>
        public unsafe static void vp8_cost_tokens(int[] c, vp8_prob[] p, vp8_tree[] t)
        {
            fixed (int* pC = c)
            fixed (vp8_prob* pP = p)
            {
                cost(pC, t, pP, 0, 0);
            }
        }

        public unsafe static void vp8_cost_tokens(int* c, vp8_prob* p, vp8_tree[] t)
        {
            cost(c, t, p, 0, 0);
        }

        /// <summary>
        /// Fill array of costs for all possible token values starting at a specific node.
        /// </summary>
        public unsafe static void vp8_cost_tokens2(int[] c, vp8_prob[] p, vp8_tree[] t, int start)
        {
            fixed (int* pC = c)
            fixed (vp8_prob* pP = p)
            {
                cost(pC, t, pP, start, 0);
            }
        }

        public unsafe static void vp8_cost_tokens2(int* c, vp8_prob* p, vp8_tree[] t, int start)
        {
            cost(c, t, p, start, 0);
        }

        // Convenience macros/methods for writing
        public static void vp8_write(ref vp8_writer w, int bit, byte probability)
            => boolhuff.vp8_encode_bool(ref w, bit, probability);

        public static void vp8_write_literal(ref vp8_writer w, int data, int bits)
            => boolhuff.vp8_encode_value(ref w, data, bits);

        public static void vp8_write_bit(ref vp8_writer w, int bit)
            => vp8_write(ref w, bit, vp8_prob_half);

        private unsafe static void branch_counts(int n, vp8_token[] tok, vp8_tree[] tree,
                                                 uint[,] branch_ct, uint[] num_events)
        {
            int t = 0;
            do
            {
                int L = tok[t].Len;
                int x = tok[t].value;
                uint ct = num_events[t];

                vp8_tree_index i = 0;

                do
                {
                    int b = (x >> --L) & 1;
                    branch_ct[i >> 1, b] += ct;
                    i = tree[i + b];
                } while (i > 0);

            } while (++t < n);
        }

        public unsafe static void vp8_tree_probs_from_distribution(int n, vp8_token[] tok,
                                                                    vp8_tree[] tree, vp8_prob[] probs,
                                                                    uint[,] branch_ct, uint[] num_events,
                                                                    uint Pfactor, int Round)
        {
            int tree_len = n - 1;
            int t = 0;

            branch_counts(n, tok, tree, branch_ct, num_events);

            do
            {
                uint c0 = branch_ct[t, 0];
                uint c1 = branch_ct[t, 1];
                ulong tot = c0 + c1;

                if (tot != 0)
                {
                    ulong p = ((ulong)c0 * Pfactor) + (Round != 0 ? tot >> 1 : 0);
                    p = p / tot;
                    probs[t] = (vp8_prob)(p < 256 ? (p != 0 ? p : 1) : 255);
                }
                else
                {
                    probs[t] = vp8_prob_half;
                }
            } while (++t < tree_len);
        }
    }
}
