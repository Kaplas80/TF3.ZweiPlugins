// Copyright (c) 2022 Kaplas
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

namespace TF3.YarhlPlugin.ZweiArges.Converters.Dll
{
    using System;
    using System.Text;
    using AsmResolver.IO;
    using AsmResolver.PE.File;
    using TF3.YarhlPlugin.ZweiArges.Formats;
    using TF3.YarhlPlugin.ZweiArges.Helpers;
    using Yarhl.FileFormat;
    using Yarhl.Media.Text;

    /// <summary>
    /// Extracts Zwei.dll translatable strings to a Po file.
    /// </summary>
    public class ExtractStrings : IConverter<PEFileFormat, Po>, IInitializer<PoHeader>
    {
        private PoHeader _poHeader = new PoHeader("NoName", "dummy@dummy.com", "en");

        /// <summary>
        /// Converter initializer.
        /// </summary>
        /// <param name="parameters">Header to use in created Po elements.</param>
        public void Initialize(PoHeader parameters) => _poHeader = parameters;

        /// <summary>
        /// Extracts strings to a Po file.
        /// </summary>
        /// <param name="source">Input format.</param>
        /// <returns>The po file.</returns>
        /// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
        public Po Convert(PEFileFormat source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var shiftJis = Encoding.GetEncoding(932);

            var po = new Po(_poHeader);

            uint pointerTableOffset = source.StringInfo.PointerTableOffset;
            uint stringCount = source.StringInfo.StringCount;

            PESection pointerTableSection = source.Internal.GetSectionContainingOffset(pointerTableOffset);
            BinaryStreamReader pointerReader = pointerTableSection.CreateReader(pointerTableOffset, stringCount * 4);

            for (int i = 0; i < stringCount; i++)
            {
                uint stringVirtualAddress = pointerReader.ReadUInt32();
                uint stringRelativeVirtualAddress = stringVirtualAddress - (uint)source.Internal.OptionalHeader.ImageBase;

                PESection stringSection = source.Internal.GetSectionContainingRva(stringRelativeVirtualAddress);
                BinaryStreamReader stringReader = stringSection.CreateReader(stringSection.Offset, stringSection.GetPhysicalSize());

                stringReader.Offset = stringSection.RvaToFileOffset(stringRelativeVirtualAddress);
                byte[] stringBytes = stringReader.ReadBytesUntil(0x00);
                string strFullWidth = ParseString(shiftJis.GetString(stringBytes).TrimEnd('\0'));
                string strHalfWidth = MapStringLib.ToHalfWidth(strFullWidth);

                if (string.IsNullOrEmpty(strHalfWidth))
                {
                    strHalfWidth = "<!empty>";
                }

                var entry = new PoEntry()
                {
                    Original = strHalfWidth,
                    Translated = strHalfWidth,
                    Context = $"{stringSection.GetPhysicalSize() - stringVirtualAddress}",
                    Reference = $"{i}",
                };
                po.Add(entry);
            }

            return po;
        }

        private string ParseString(string input)
        {
            var sb = new StringBuilder();

            for (int i = 0; i < input.Length;)
            {
                if (input[i] < ' ' || input[i] > 0x7F)
                {
                    sb.Append(input[i]);
                    i++;
                }
                else if (input[i] == '-')
                {
                    sb.Append(' ');
                    i++;
                }
                else if (input[i] == '\u2010' /* Unicode Character 'HYPHEN' */)
                {
                    sb.Append('-');
                    i++;
                }
                else
                {
                    sb.Append('<');
                    while (i < input.Length && input[i] != '-' && input[i] > ' ' && input[i] <= 0x7F)
                    {
                        sb.Append(input[i]);
                        i++;
                    }

                    sb.Append('>');
                }
            }

            return sb.ToString();
        }
    }
}
