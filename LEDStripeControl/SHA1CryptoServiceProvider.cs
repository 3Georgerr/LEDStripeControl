using System;
using Microsoft.SPOT;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Static class providing Secure Hashing Algorithm (SHA-1, SHA-224, SHA-256)
    /// </summary>
    public static class SHA1CryptoServiceProvider
    {
        // Rotate bits left
        static uint rotateleft(uint x, int n)
        {
            return ((x << n) | (x >> (32 - n)));
        }

        // Convert 4 bytes to big endian uint32
        static uint big_endian_from_bytes(byte[] input, uint start)
        {
            uint r = 0;
            r |= (((uint)input[start]) << 24);
            r |= (((uint)input[start + 1]) << 16);
            r |= (((uint)input[start + 2]) << 8);
            r |= (((uint)input[start + 3]));
            return r;
        }

        // Convert big endian uint32 to bytes
        static void bytes_from_big_endian(uint input, ref byte[] output, int start)
        {
            output[start] = (byte)((input & 0xFF000000) >> 24);
            output[start + 1] = (byte)((input & 0x00FF0000) >> 16);
            output[start + 2] = (byte)((input & 0x0000FF00) >> 8);
            output[start + 3] = (byte)((input & 0x000000FF));
        }

        /// <summary>
        /// Compute SHA1 digest
        /// </summary>
        /// <param name="input">Input byte array</param>
        /// <returns>20 byte SHA1 of input</returns>
        public static byte[] ComputeHash(byte[] input)
        {
            // Initialize working parameters
            uint a, b, c, d, e, i, temp;
            uint h0 = 0x67452301;
            uint h1 = 0xEFCDAB89;
            uint h2 = 0x98BADCFE;
            uint h3 = 0x10325476;
            uint h4 = 0xC3D2E1F0;
            uint blockstart = 0;

            // Calculate how long the padded message should be
            int newinputlength = input.Length + 1;
            while ((newinputlength % 64) != 56) // length mod 512bits = 448bits
            {
                newinputlength++;
            }

            // Create array for padded data
            byte[] processed = new byte[newinputlength + 8];
            Array.Copy(input, processed, input.Length);

            // Pad data with an 1
            processed[input.Length] = 0x80;

            // Pad data with big endian 64bit length of message
            // We do only 32 bits becouse input.length is 32 bit
            bytes_from_big_endian((uint)input.Length * 8, ref processed, processed.Length - 4);

            // Block of 32 bits values used in calculations
            uint[] wordblock = new uint[80];

            // Now process each 512 bit block
            while (blockstart < processed.Length)
            {
                // break chunk into sixteen 32-bit big-endian words 
                for (i = 0; i < 16; i++)
                    wordblock[i] = big_endian_from_bytes(processed, blockstart + (i * 4));

                // Extend the sixteen 32-bit words into eighty 32-bit words:
                for (i = 16; i < 80; i++)
                    wordblock[i] = rotateleft(wordblock[i - 3] ^ wordblock[i - 8] ^ wordblock[i - 14] ^ wordblock[i - 16], 1);


                // Initialize hash value for this chunk
                a = h0;
                b = h1;
                c = h2;
                d = h3;
                e = h4;

                // Main loop
                for (i = 0; i < 80; i++)
                {
                    // Perform function dependend of word number
                    if (i <= 19)
                        temp = (rotateleft(a, 5) + ((b & c) | (~b & d)) + e + wordblock[i] + 0x5A827999);
                    else if (i <= 39)
                        temp = (rotateleft(a, 5) + (b ^ c ^ d) + e + wordblock[i] + 0x6ED9EBA1);
                    else if (i <= 59)
                        temp = (rotateleft(a, 5) + ((b & c) | (b & d) | (c & d)) + e + wordblock[i] + 0x8F1BBCDC);
                    else
                        temp = (rotateleft(a, 5) + (b ^ c ^ d) + e + wordblock[i] + 0xCA62C1D6);

                    // Perform standard function
                    e = d;
                    d = c;
                    c = rotateleft(b, 30);
                    b = a;
                    a = temp;
                }

                // Add this chunk's hash to result so far
                h0 += a;
                h1 += b;
                h2 += c;
                h3 += d;
                h4 += e;

                // Next 512 bit block
                blockstart += 64;
            }

            // Prepare output
            byte[] output = new byte[20];
            bytes_from_big_endian(h0, ref output, 0);
            bytes_from_big_endian(h1, ref output, 4);
            bytes_from_big_endian(h2, ref output, 8);
            bytes_from_big_endian(h3, ref output, 12);
            bytes_from_big_endian(h4, ref output, 16);

            return output;
        }
    }
}
