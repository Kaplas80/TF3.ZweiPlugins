// Copyright (c) 2021 Kaplas
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

namespace TF3.YarhlPlugin.ZweiArges.Converters.Font
{
    using System;
    using SixLabors.ImageSharp;
    using SixLabors.ImageSharp.Formats.Bmp;
    using SixLabors.ImageSharp.PixelFormats;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    /// <summary>
    /// Deserializes Font archives.
    /// </summary>
    public class Reader : IConverter<BinaryFormat, NodeContainerFormat>
    {
        /// <summary>
        /// Converts a BinaryFormat into a NodeContainerFormat.
        /// </summary>
        /// <param name="source">Input format.</param>
        /// <returns>The node container format.</returns>
        /// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
        public virtual NodeContainerFormat Convert(BinaryFormat source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            source.Stream.Seek(0);

            var reader = new DataReader(source.Stream);

            BmpEncoder encoder = new ();
            encoder.BitsPerPixel = BmpBitsPerPixel.Pixel8;

            Node root = NodeFactory.CreateContainer("root");
            for (int charIndex = 0x8141; reader.Stream.Position < reader.Stream.Length; charIndex++)
            {
                byte[] imageData = reader.ReadBytes(8 * 16);
                int width = ((imageData[6] | (imageData[7] << 8)) >> 9) & 0x7F;

                using Image<L8> image = new (width, 16);

                for (int y = 0; y < 16; y++)
                {
                    Span<L8> pixelRowSpan = image.GetPixelRowSpan(y);
                    for (int x = 0; x < width; x++)
                    {
                        int byteIndex = (y * 8) + ((x / 5) * 2);
                        int offset = (x % 5) * 3;
                        int value = ((imageData[byteIndex] | (imageData[byteIndex + 1] << 8)) >> offset) & 7;

                        if (value != 0)
                        {
                            // The image is indexed but the pallete is unknown, so I use a custom grayscale palette.
                            byte v = (byte)((value * 32) + 31);
                            pixelRowSpan[x] = new L8(v);
                        }
                    }
                }

                Node node = NodeFactory.FromMemory($"{charIndex}.bmp");

                image.SaveAsBmp(node.Stream, encoder);

                root.Add(node);
            }

            return root.GetFormatAs<NodeContainerFormat>();
        }
    }
}
