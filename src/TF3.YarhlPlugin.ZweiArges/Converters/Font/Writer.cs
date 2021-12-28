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
    using SixLabors.ImageSharp.PixelFormats;
    using SixLabors.ImageSharp.Processing;
    using SixLabors.ImageSharp.Processing.Processors.Quantization;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    /// <summary>
    /// Serializes Font archives.
    /// </summary>
    public class Writer : IConverter<NodeContainerFormat, BinaryFormat>
    {
        /// <summary>
        /// Converts a NodeContainerFormat into a BinaryFormat.
        /// </summary>
        /// <param name="source">Input format.</param>
        /// <returns>The binary format.</returns>
        /// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
        public virtual BinaryFormat Convert(NodeContainerFormat source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            byte[] data = new byte[source.Root.Children.Count * 0x80];
            DataStream stream = DataStreamFactory.FromArray(data, 0, data.Length);
            var writer = new DataWriter(stream);

            ReadOnlyMemory<Color> palette = BuildPalette();
            IQuantizer quantizer = new PaletteQuantizer(palette);

            foreach (Node bmpNode in source.Root.Children)
            {
                int charIndex = int.Parse(bmpNode.Name.Replace(".bmp", string.Empty)) - 0x8141;
                writer.Stream.Position = charIndex * 0x80;

                Image<Rgb24> image = Image.Load<Rgb24>(bmpNode.Stream);
                image.Mutate(x => x.Quantize(quantizer));

                ushort[] rowValues = new ushort[4];
                for (int y = 0; y < 16; y++)
                {
                    rowValues[0] = 0x0000;
                    rowValues[1] = 0x0000;
                    rowValues[2] = 0x0000;
                    rowValues[3] = y == 0 ? (ushort)(image.Width << 9) : (ushort)0x0000;

                    Span<Rgb24> pixelRowSpan = image.GetPixelRowSpan(y);

                    for (int x = 0; x < 17; x++)
                    {
                        int ushortIndex = x / 5;
                        int offset = (x % 5) * 3;

                        byte value = x >= image.Width ? (byte)7 : GetColorIndex(pixelRowSpan[x]);

                        rowValues[ushortIndex] |= (ushort)(value << offset);
                    }

                    writer.Write(rowValues[0]);
                    writer.Write(rowValues[1]);
                    writer.Write(rowValues[2]);
                    writer.Write(rowValues[3]);
                }
            }

            return new BinaryFormat(stream);
        }

        private static ReadOnlyMemory<Color> BuildPalette()
        {
            Color[] palette = new Color[8];
            palette[0] = new Color(new Rgb24(0, 0, 0));
            for (int i = 1; i < 8; i++)
            {
                byte value = (byte)((i * 32) + 31);
                palette[i] = new Color(new Rgb24(value, value, value));
            }

            return palette;
        }

        private static byte GetColorIndex(Rgb24 color)
        {
            // After quantization R == G == B, so I can use any of them
            return (byte)((color.R - 31) / 32);
        }
    }
}
