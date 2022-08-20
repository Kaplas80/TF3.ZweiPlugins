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
    using TF3.YarhlPlugin.ZweiArges.Formats;
    using TF3.YarhlPlugin.ZweiArges.Types;
    using Yarhl.FileFormat;
    using Yarhl.IO;

    /// <summary>
    /// Deserializes PE files.
    /// </summary>
    public class Reader : IConverter<BinaryFormat, PEFileFormat>, IInitializer<DllStringInfo>
    {
        private DllStringInfo _stringInfo = null;

        /// <summary>
        /// Converter initializer.
        /// </summary>
        /// <remarks>
        /// Initialization is mandatory.
        /// </remarks>
        /// <param name="parameters">String info.</param>
        public void Initialize(DllStringInfo parameters) => _stringInfo = parameters;

        /// <summary>
        /// Converts a BinaryFormat into a PEFile.
        /// </summary>
        /// <param name="source">Input format.</param>
        /// <returns>The PEFile format.</returns>
        /// <exception cref="ArgumentNullException">Thrown if source is null.</exception>
        public PEFileFormat Convert(BinaryFormat source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (_stringInfo == null)
            {
                throw new InvalidOperationException("Uninitialized");
            }

            source.Stream.Position = 0;
            var reader = new DataReader(source.Stream);
            byte[] data = reader.ReadBytes((int)source.Stream.Length);
            var result = new PEFileFormat()
            {
                Internal = AsmResolver.PE.File.PEFile.FromBytes(data),
                StringInfo = _stringInfo,
            };

            return result;
        }
    }
}
