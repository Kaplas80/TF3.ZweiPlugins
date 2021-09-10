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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using TF3.YarhlPlugin.ZweiArges.Types;
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    /// <summary>
    /// Serializes DAT archives.
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

            IEnumerable<IGrouping<string, Node>> groupByExtension = source.Root.Children.GroupBy(x => Path.GetExtension(x.Name.ToLowerInvariant()));
            var extensions = groupByExtension.Select(g => new { Extension = g.Key.ToLowerInvariant(), Count = g.Count() }).ToArray();

            DataStream stream = DataStreamFactory.FromMemory();

            stream.Position = 0;

            var writer = new DataWriter(stream)
            {
                DefaultEncoding = Encoding.ASCII,
            };

            var header = new DatFileHeader
            {
                Magic = 12345678,
                TypesCount = extensions.Length,
            };

            writer.WriteOfType(header);

            int numFilesAcum = 0;
            for (int i = 0; i < extensions.Length; i++)
            {
                DatFileType fileType = new DatFileType
                {
                    Extension = extensions[i].Extension.TrimStart('.'),
                    Offset = (uint)(0x08 + (extensions.Length * 0x0C) + (numFilesAcum * 0x10)),
                    FileCount = extensions[i].Count,
                };

                writer.WriteOfType(fileType);
                numFilesAcum += fileType.FileCount;
            }

            uint fileOffset = (uint)(0x08 + (extensions.Length * 0x0C) + (numFilesAcum * 0x10));
            for (int i = 0; i < source.Root.Children.Count; i++)
            {
                Node node = source.Root.Children[i];
                writer.Write(Path.GetFileNameWithoutExtension(node.Name), 8, false);
                writer.Write((uint)node.Stream.Length);
                writer.Write(fileOffset);

                fileOffset += (uint)node.Stream.Length;
            }

            for (int i = 0; i < source.Root.Children.Count; i++)
            {
                Node node = source.Root.Children[i];
                node.Stream.WriteTo(writer.Stream);
            }

            return new BinaryFormat(stream);
        }
    }
}
