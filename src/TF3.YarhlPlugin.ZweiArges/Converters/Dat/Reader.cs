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

namespace TF3.YarhlPlugin.ZweiArges.Converters.Dat
{
    using System;
    using System.Text;
    using TF3.YarhlPlugin.ZweiArges.Types;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    /// <summary>
    /// Deserializes DAT archives.
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

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var reader = new DataReader(source.Stream)
            {
                DefaultEncoding = Encoding.ASCII,
            };

            // Read the file header
            var header = reader.Read<DatFileHeader>() as DatFileHeader;
            CheckHeader(header);

            Node root = NodeFactory.CreateContainer("root");
            DatFileType[] fileTypeInfo = new DatFileType[header.TypesCount];

            for (int i = 0; i < header.TypesCount; i++)
            {
                fileTypeInfo[i] = reader.Read<DatFileType>();
            }

            for (int i = 0; i < header.TypesCount; i++)
            {
                reader.Stream.Seek(fileTypeInfo[i].Offset, System.IO.SeekOrigin.Begin);

                for (int j = 0; j < fileTypeInfo[i].FileCount; j++)
                {
                    string name = reader.ReadString(8).TrimEnd('\0');
                    uint size = reader.ReadUInt32();
                    uint offset = reader.ReadUInt32();

                    Node file = NodeFactory.FromSubstream($"{name}.{fileTypeInfo[i].Extension}", source.Stream, offset, size);

                    root.Add(file);
                }
            }

            return root.GetFormatAs<NodeContainerFormat>();
        }

        /// <summary>
        /// Checks the validity of the DAT header.
        /// <remarks>Throws an exception if any of the values is invalid.</remarks>
        /// </summary>
        /// <param name="header">The header to check.</param>
        /// <exception cref="ArgumentNullException">Thrown if header is null.</exception>
        /// <exception cref="FormatException">Thrown when some value is invalid.</exception>
        private static void CheckHeader(DatFileHeader header)
        {
            if (header.Magic != 12345678)
            {
                throw new FormatException($"DAT: Bad magic Id ({header.Magic} != 12345678)");
            }

            if (header.TypesCount <= 0)
            {
                throw new FormatException($"DAT: Bad TypesCount ({header.TypesCount} <= 0)");
            }
        }
    }
}
