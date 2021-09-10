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
    using Yarhl.FileFormat;
    using Yarhl.FileSystem;
    using Yarhl.IO;

    /// <summary>
    /// Foont replacer.
    /// </summary>
    public class Replace : IConverter<NodeContainerFormat, BinaryFormat>, IInitializer<NodeContainerFormat>
    {
        private NodeContainerFormat _newFormat = null;

        /// <summary>
        /// Set the new font.
        /// </summary>
        /// <param name="parameters">The new font.</param>
        public void Initialize(NodeContainerFormat parameters) => _newFormat = parameters;

        /// <summary>
        /// Fully replace a font.
        /// </summary>
        /// <param name="source">The original font.</param>
        /// <returns>The new font.</returns>
        public BinaryFormat Convert(NodeContainerFormat source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (_newFormat == null)
            {
                throw new InvalidOperationException("Uninitialized.");
            }

            return (BinaryFormat)ConvertFormat.With<Writer>(_newFormat);
        }
    }
}
