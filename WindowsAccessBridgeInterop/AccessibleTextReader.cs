// Copyright 2016 Google Inc. All Rights Reserved.
// 
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// 
//     http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.IO;

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// Expose a <see cref="TextReader"/> interface over an accessible
  /// text component.
  /// </summary>
  public class AccessibleTextReader : TextReader {
    private const int TextChunkSize = 4096;
    private readonly AccessibleContextNode _node;
    private readonly int _charCount;
    private readonly char[] _chunk;
    private int _chunkLength;
    private int _chunkOffset;
    private int _streamOffset;

    public AccessibleTextReader(AccessibleContextNode node, int charCount) {
      _node = node;
      _charCount = charCount;
      _chunk = new char[TextChunkSize];
    }

    private bool EndOfFile {
      get { return _streamOffset >= _charCount; }
    }

    public override int Peek() {
      TryEnsureChunk();
      if (EndOfFile)
        return -1;
      return _chunk[_streamOffset - _chunkOffset];
    }

    public override int Read() {
      var result = Peek();
      if (result >= 0)
        _streamOffset++;
      return result;
    }

    private void TryEnsureChunk() {
      if (EndOfFile)
        return;

      if (_streamOffset < _chunkOffset + _chunkLength)
        return;

      // len = output buffer size: we use - 1 to be on the safe size in case
      // JAB api decides to include append a NUL terminator, which it
      // currently does not seem to do.
      // start = offset of first character of range (inclusive)
      // end = offset of last character of range (exclusive), except for the
      //   special case of the end of the range. The JAB API will only accept
      //   end offset == characterCount - 1.
      var len = (short)(_chunk.Length - 1);
      var start = _chunkOffset + _chunkLength;
      var end = Math.Min(_charCount, start + len);
      var fixedupEnd = Math.Min(_charCount - 1, end);
      if (Failed(_node.AccessBridge.Functions.GetAccessibleTextRange(_node.JvmId, _node.AccessibleContextHandle, start, fixedupEnd, _chunk, len))) {
        _streamOffset = _charCount;
        return;
      }

      _chunkLength = end - start;
      _chunkOffset = start;
    }

    private static bool Failed(bool result) {
      return !result;
    }
  }
}