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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using AsmResolver;
    using AsmResolver.PE.File;
    using AsmResolver.PE.File.Headers;
    using TF3.YarhlPlugin.ZweiArges.Formats;
    using TF3.YarhlPlugin.ZweiArges.Helpers;
    using Yarhl.FileFormat;
    using Yarhl.IO;
    using Yarhl.Media.Text;

    /// <summary>
    /// Inserts strings from Po file to the game dll.
    /// </summary>
    public class Translate : IConverter<PEFileFormat, PEFileFormat>, IInitializer<Po>
    {
        private readonly List<Tuple<string, string>> _replacements = new List<Tuple<string, string>>();

        private Po _translation = null;

        /// <summary>
        /// Converter initializer.
        /// </summary>
        /// <remarks>
        /// Initialization is mandatory.
        /// </remarks>
        /// <param name="parameters">Po with translation.</param>
        public void Initialize(Po parameters) => _translation = parameters;

        /// <summary>
        /// Inserts the translated strings from Po file in a Armp table.
        /// </summary>
        /// <param name="source">Original Dll.</param>
        /// <returns>Translated Dll.</returns>
        public PEFileFormat Convert(PEFileFormat source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (_translation == null)
            {
                throw new InvalidOperationException("Uninitialized");
            }

            if (File.Exists("./plugins/TF3.StringReplacements.ZweiArges.txt"))
            {
                LoadReplacements("./plugins/TF3.StringReplacements.ZweiArges.txt");
            }

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            var shiftJis = Encoding.GetEncoding(932);

            var result = source.DeepClone() as PEFileFormat;

            uint pointerTableOffset = result.StringInfo.PointerTableOffset;
            uint stringCount = result.StringInfo.StringCount;

            byte[] translationData = new byte[0x100000];
            uint[] translationOffsets = new uint[stringCount];
            using (DataStream stream = DataStreamFactory.FromArray(translationData, 0, translationData.Length))
            {
                var writer = new DataWriter(stream)
                {
                    DefaultEncoding = shiftJis,
                };

                var toHalfWidth = new MatchEvaluator((match) => MapStringLib.ToHalfWidth(match.Groups[1].ToString()));

                const string pattern = "＜([^＞]+)＞";
                var regex = new Regex(pattern);
                for (int i = _translation.Entries.Count - 1; i >= 0; i--)
                {
                    PoEntry entry = _translation.Entries[i];

                    string[] references = entry.Reference.Split(',');

                    for (int j = 0; j < references.Length; j++)
                    {
                        int stringIndex = int.Parse(references[j]);
                        translationOffsets[stringIndex] = (uint)writer.Stream.Position;
                    }

                    if (entry.Original == "<!empty>")
                    {
                        writer.Write((byte)0x00);
                    }
                    else
                    {
                        string text = entry.Translated;
                        if (string.IsNullOrEmpty(text))
                        {
                            text = entry.Original;
                        }

                        foreach (Tuple<string, string> replacement in _replacements)
                        {
                            text = text.Replace(replacement.Item1, replacement.Item2);
                        }

                        text = text.Replace('\u002D', '\u2010');
                        string strFullWidth = MapStringLib.ToFullWidth(text);
                        if (regex.IsMatch(strFullWidth))
                        {
                            strFullWidth = regex.Replace(strFullWidth, toHalfWidth);
                        }

                        strFullWidth = strFullWidth.Replace('\uFF07', '\u2019');
                        strFullWidth = strFullWidth.Replace('\u3000', '\u002D');
                        writer.Write(strFullWidth);
                    }
                }
            }

            var section = new PESection(".tf3", SectionFlags.MemoryRead | SectionFlags.ContentInitializedData)
            {
                Contents = new DataSegment(translationData),
            };

            result.Internal.Sections.Add(section);
            result.Internal.UpdateHeaders();

            PESection pointerTableSection = result.Internal.GetSectionContainingOffset(pointerTableOffset);
            byte[] pointerTableSectionData = pointerTableSection.ToArray();

            using (DataStream stream = DataStreamFactory.FromArray(pointerTableSectionData, 0, pointerTableSectionData.Length))
            {
                stream.Position = pointerTableOffset - (long)pointerTableSection.Offset;
                var writer = new DataWriter(stream);

                for (int i = 0; i < translationOffsets.Length; i++)
                {
                    writer.Write((uint)(translationOffsets[i] + section.Rva + result.Internal.OptionalHeader.ImageBase));
                }
            }

            pointerTableSection.Contents = new DataSegment(pointerTableSectionData);
            return result;
        }

        private void LoadReplacements(string file)
        {
            foreach (string line in File.ReadAllLines(file))
            {
                string[] split = line.Split('=');
                if (split.Length == 2)
                {
                    _replacements.Add(new Tuple<string, string>(split[0], split[1]));
                }
            }
        }
    }
}
