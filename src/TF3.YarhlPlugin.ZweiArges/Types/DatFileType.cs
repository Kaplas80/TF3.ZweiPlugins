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

namespace TF3.YarhlPlugin.ZweiArges.Types
{
    using Yarhl.IO.Serialization.Attributes;

    /// <summary>
    /// File Type structure.
    /// </summary>
    [Serializable]
    public class DatFileType
    {
        /// <summary>
        /// Gets or sets the file type extension.
        /// </summary>
        [BinaryString(MaxSize = 4)]
        public string Extension { get; set; }

        /// <summary>
        /// Gets or sets the data start offset.
        /// </summary>
        public uint Offset { get; set; }

        /// <summary>
        /// Gets or sets the file count.
        /// </summary>
        public int FileCount { get; set; }
    }
}
