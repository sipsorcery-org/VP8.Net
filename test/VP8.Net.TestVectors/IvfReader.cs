//-----------------------------------------------------------------------------
// Filename: IvfReader.cs
//
// Description: Reader for IVF (Indeo Video Format) files containing VP8 test vectors.
//
// Author(s):
// Copilot
//
// History:
// 23 Jun 2025	Copilot	Created, Dublin, Ireland.
//
// License: 
// BSD 3-Clause "New" or "Revised" License, see included LICENSE.md file.
//-----------------------------------------------------------------------------

using System;
using System.IO;

namespace VP8.Net.TestVectors
{
    /// <summary>
    /// Reader for IVF (Indeo Video Format) files containing VP8 test vectors.
    /// </summary>
    public class IvfReader
    {
        private const uint IVF_SIGNATURE = 0x46494C45; // "ELFI" in little endian
        private const ushort IVF_VERSION = 0;
        
        /// <summary>
        /// Reads an IVF file and extracts VP8 frames.
        /// </summary>
        /// <param name="filePath">Path to the IVF file.</param>
        /// <returns>Array of VP8 frame data.</returns>
        public static byte[][] ReadFrames(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"IVF file not found: {filePath}");
            }

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            using var br = new BinaryReader(fs);

            // Read IVF header
            var signature = br.ReadUInt32();
            if (signature != IVF_SIGNATURE)
            {
                throw new InvalidDataException("Invalid IVF file signature");
            }

            var version = br.ReadUInt16();
            var headerLength = br.ReadUInt16();
            var fourcc = br.ReadUInt32();
            var width = br.ReadUInt16();
            var height = br.ReadUInt16();
            var timebaseNumerator = br.ReadUInt32();
            var timebaseDenominator = br.ReadUInt32();
            var frameCount = br.ReadUInt32();
            var reserved = br.ReadUInt32();

            // Skip to end of header
            fs.Seek(headerLength, SeekOrigin.Begin);

            var frames = new byte[frameCount][];
            
            for (int i = 0; i < frameCount; i++)
            {
                var frameSize = br.ReadUInt32();
                var timestamp = br.ReadUInt64();
                
                frames[i] = br.ReadBytes((int)frameSize);
            }

            return frames;
        }
    }
}