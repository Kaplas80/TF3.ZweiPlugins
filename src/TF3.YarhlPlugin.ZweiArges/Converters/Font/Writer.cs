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
    using System.Drawing;
    using System.Drawing.Imaging;
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

            foreach (Node bmpNode in source.Root.Children)
            {
                int charIndex = int.Parse(bmpNode.Name.Replace(".bmp", string.Empty)) - 0x8141;
                writer.Stream.Position = charIndex * 0x80;

                Bitmap bmp = Image.FromStream(bmpNode.Stream) as Bitmap;
                BitmapData bmpData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, bmp.PixelFormat);
                IntPtr ptr = bmpData.Scan0;

                int size = Math.Abs(bmpData.Stride) * bmp.Height;
                byte[] bmpValues = new byte[size];

                System.Runtime.InteropServices.Marshal.Copy(ptr, bmpValues, 0, size);

                ushort[] rowValues = new ushort[4];
                for (int y = 0; y < 16; y++)
                {
                    rowValues[0] = 0x0000;
                    rowValues[1] = 0x0000;
                    rowValues[2] = 0x0000;
                    rowValues[3] = y == 0 ? (ushort)(bmp.Width << 9) : (ushort)0x0000;

                    for (int x = 0; x < 17; x++)
                    {
                        int ushortIndex = x / 5;
                        int offset = (x % 5) * 3;

                        byte value = x >= bmp.Width ? (byte)7 : bmpValues[(y * bmpData.Stride) + x];

                        rowValues[ushortIndex] |= (ushort)(value << offset);
                    }

                    writer.Write(rowValues[0]);
                    writer.Write(rowValues[1]);
                    writer.Write(rowValues[2]);
                    writer.Write(rowValues[3]);
                }

                bmp.UnlockBits(bmpData);
            }

            return new BinaryFormat(stream);
        }
    }
}
