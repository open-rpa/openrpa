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

using System.Collections.Generic;
using System.IO;
using System.Text;

namespace WindowsAccessBridgeInterop {
  /// <summary>
  /// Extension methods over a <see cref="TextReader"/> implementation.
  /// </summary>
  public static class TextReaderExtensions {
    public class LineData {
      /// <summary>
      /// The line offset relative to the beginning of the stream.
      /// </summary>
      public int Offset { get; set; }

      /// <summary>
      /// The line number, 0-based.
      /// </summary>
      public int Number { get; set; }

      /// <summary>
      /// The text contents of the line, including the <code>new line</code>
      /// terminator, except if the line is longer than the specified maximum
      /// length.
      /// </summary>
      public string Text { get; set; }

      /// <summary>
      /// Specifies if this line is a continuation information from a previous
      /// line that was too long too fit into a single instance.
      /// </summary>
      public bool IsContinuation { get; set; }

      /// <summary>
      /// Specifies if this line is too long to fit in a single instance.
      /// The next entry will have the <see cref="IsContinuation"/> property set
      /// to <code>true</code>.
      /// </summary>
      public bool IsComplete { get; set; }
    }

    public class LineContents {
      private readonly string _text;
      private readonly bool _endOfLine;
      private readonly bool _endOfFile;

      public LineContents(string text, bool endOfLine, bool endOfFile) {
        _text = text;
        _endOfLine = endOfLine;
        _endOfFile = endOfFile;
      }

      public string Text {
        get { return _text; }
      }

      public bool EndOfLine {
        get { return _endOfLine; }
      }

      public bool EndOfFile {
        get { return _endOfFile; }
      }
    }

    /// <summary>
    /// Enumerate each line of a <see cref="TextReader"/> into <see
    /// cref="LineData"/> results.
    /// </summary>
    public static IEnumerable<LineData> ReadFullLines(this TextReader reader) {
      return ReadFullLines(reader, int.MaxValue);
    }

    /// <summary>
    /// Enumerate each line of a <see cref="TextReader"/> into <see
    /// cref="LineData"/> results. Lines longer than <paramref
    /// name="maxLineLength"/> are split into multiple <see cref="LineData"/>
    /// results.
    /// </summary>
    public static IEnumerable<LineData> ReadFullLines(this TextReader reader, int maxLineLength) {
      var index = 0;
      var lineNumber = 0;
      var isContinuation = false;
      while (true) {
        var lineOffset = index;
        var lineContents = reader.ReadFullLine(maxLineLength);
        if (lineContents == null)
          yield break;

        var isComplete = lineContents.EndOfFile || lineContents.EndOfLine;
        yield return new LineData {
          Offset = lineOffset,
          Number = lineNumber,
          Text = lineContents.Text,
          IsContinuation = isContinuation,
          IsComplete = isComplete
        };
        index += lineContents.Text.Length;
        if (isComplete) {
          isContinuation = false;
          lineNumber++;
        } else {
          isContinuation = true;
        }
      }
    }

    /// <summary>
    /// Read a single line of <see cref="TextReader"/> into a <see
    /// cref="string"/>. The returning string contains the terminating new line
    /// character. Returns <code>null</code> if the end of the stream has been
    /// reached.
    /// </summary>
    public static LineContents ReadFullLine(this TextReader reader) {
      return ReadFullLine(reader, int.MaxValue);
    }

    /// <summary>
    /// Read a single line from a <see cref="TextReader"/> into a <see
    /// cref="string"/>. The returning string contains the terminating new line
    /// character, except if the line is longer than <paramref
    /// name="maxLength"/> characters, in which case the returned string only
    /// contains the first <paramref name="maxLength"/> characters. To obtain
    /// the reset of the string, call to <see
    /// cref="ReadFullLine(System.IO.TextReader, int)"/> .
    /// Returns <code>null</code> if the end of the stream has been reached.
    /// </summary>. 
    public static LineContents ReadFullLine(this TextReader reader, int maxLength) {
      if (maxLength == 0) {
        return new LineContents("", false, false);
      }

      bool endOfFile = false;
      bool endOfLine = false;
      var sb = new StringBuilder();
      while (sb.Length < maxLength) {
        var ch = reader.Read();
        if (ch < 0) {
          endOfFile = true;
          break;
        }
        sb.Append((char)ch);
        if (ch == '\n') {
          endOfLine = true;
          break;
        }
      }
      if (sb.Length > 0) {
        return new LineContents(sb.ToString(), endOfLine, endOfFile);
      }
      return null;
    }
  }
}