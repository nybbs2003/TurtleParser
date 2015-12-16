/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://upokecenter.dreamhosters.com/articles/donate-now-2/
*/
namespace PeterO.Rdf {
using System;
using System.Text;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using PeterO;
  using PeterO.Text;

public sealed class NTriplesParser : IRDFParser {
  public static bool isAsciiChar(int c, string asciiChars) {
    return (c >= 0 && c <= 0x7f && asciiChars.IndexOf((char)c) >= 0);
  }

  IDictionary<string, RDFTerm> bnodeLabels;

  StackableCharacterInput input;

  public NTriplesParser(PeterO.IByteReader stream) {
    if ((stream) == null) {
 throw new ArgumentNullException("stream");
}
    this.input = new StackableCharacterInput(
        Encodings.GetDecoderInput(Encodings.GetEncoding("us-ascii", true),
        stream));
    bnodeLabels = new Dictionary<string, RDFTerm>();
  }

  public NTriplesParser(string str) {
    if ((str) == null) {
 throw new ArgumentNullException("stream");
}
    this.input = new StackableCharacterInput(
        Encodings.StringToInput(str));
    bnodeLabels = new Dictionary<string, RDFTerm>();
  }

  private void endOfLine(int ch) {
    if (ch == 0x0a) {
 return;
  } else if (ch == 0x0d) {
      ch = input.ReadChar();
      if (ch != 0x0a && ch >= 0) {
        input.moveBack(1);
      }
    } else {
 throw new ParserException();
}
  }

  private RDFTerm finishStringLiteral(string str) {
    int mark = input.setHardMark();
    int ch = input.ReadChar();
    if (ch=='@') {
 return RDFTerm.fromLangString(str, readLanguageTag());
  } else if (ch=='^' && input.ReadChar()=='^') {
      ch = input.ReadChar();
      if (ch=='<') {
 return RDFTerm.fromTypedString(str, readIriReference());
} else {
 throw new ParserException();
}
    } else {
      input.setMarkPosition(mark);
      return RDFTerm.fromTypedString(str);
    }
  }

  public ISet<RDFTriple> parse() {
    ISet<RDFTriple> rdf = new HashSet<RDFTriple>();
    while (true) {
      skipWhitespace();
      input.setHardMark();
      int ch = input.ReadChar();
      if (ch< 0) {
 return rdf;
}
      if (ch=='#') {
        while (true) {
          ch = input.ReadChar();
          if (ch == 0x0a || ch == 0x0d) {
            endOfLine(ch);
            break;
          } else if (ch<0x20 || ch>0x7e) {
 throw new ParserException();
}
        }
      } else if (ch == 0x0a || ch == 0x0d) {
        endOfLine(ch);
      } else {
        input.moveBack(1);
        rdf.Add(readTriples());
      }
    }
  }

  private string readBlankNodeLabel() {
    var ilist = new StringBuilder();
    int startChar = input.ReadChar();
    if (!((startChar>= 'A' && startChar<= 'Z') ||
        (startChar>= 'a' && startChar<= 'z'))) {
 throw new ParserException();
}
    if (startChar <= 0xffff) {
  { ilist.Append((char)(startChar));
}
  } else if (startChar <= 0x10ffff) {
ilist.Append((char)((((startChar-0x10000) >> 10) & 0x3ff)+0xd800));
ilist.Append((char)(((startChar-0x10000) & 0x3ff)+0xdc00));
}
    input.setSoftMark();
    while (true) {
      int ch = input.ReadChar();
      if ((ch>= 'A' && ch<= 'Z') || (ch>= 'a' && ch<= 'z') ||
          (ch>= '0' && ch<= '9')) {
        if (ch <= 0xffff) {
  { ilist.Append((char)(ch));
}
  } else if (ch <= 0x10ffff) {
ilist.Append((char)((((ch-0x10000) >> 10) & 0x3ff)+0xd800));
ilist.Append((char)(((ch-0x10000) & 0x3ff)+0xdc00));
}
      } else {
        if (ch >= 0) {
          input.moveBack(1);
        }
        return ilist.ToString();
      }
    }
  }

  private string readIriReference() {
    var ilist = new StringBuilder();
    var haveString = false;
    var colon = false;
    while (true) {
      int c2 = input.ReadChar();
if ((c2<= 0x20 || c2>0x7e) || ((c2&0x7F)==c2 && "<\"{}|^`"
        .IndexOf((char)c2) >= 0)) throw new ParserException();
      else if (c2=='\\') {
        c2 = readUnicodeEscape(true);
        if (c2 <= 0x20 || (c2 >= 0x7f && c2 <= 0x9f) || ((c2 & 0x7f) == c2 &&
          "<\"{}|\\^`" .IndexOf((char)c2)>= 0)) throw new ParserException();
        if (c2==':') {
          colon = true;
        }
        if (c2 <= 0xffff) {
  { ilist.Append((char)(c2));
}
  } else if (c2 <= 0x10ffff) {
ilist.Append((char)((((c2-0x10000) >> 10) & 0x3ff)+0xd800));
ilist.Append((char)(((c2-0x10000) & 0x3ff)+0xdc00));
}
        haveString = true;
      } else if (c2=='>') {
        if (!haveString || !colon) {
 throw new ParserException();
}
        return ilist.ToString();
      } else if (c2=='\"')
        // Should have been escaped
        throw new ParserException();
      else {
        if (c2==':') {
          colon = true;
        }
        if (c2 <= 0xffff) {
  { ilist.Append((char)(c2));
}
  } else if (c2 <= 0x10ffff) {
ilist.Append((char)((((c2-0x10000) >> 10) & 0x3ff)+0xd800));
ilist.Append((char)(((c2-0x10000) & 0x3ff)+0xdc00));
}
        haveString = true;
      }
    }
  }

  private string readLanguageTag() {
    var ilist = new StringBuilder();
    var hyphen = false;
    var haveHyphen = false;
    var haveString = false;
    input.setSoftMark();
    while (true) {
      int c2 = input.ReadChar();
      if (c2>= 'a' && c2<= 'z') {
        if (c2 <= 0xffff) {
  { ilist.Append((char)(c2));
}
  } else if (c2 <= 0x10ffff) {
ilist.Append((char)((((c2-0x10000) >> 10) & 0x3ff)+0xd800));
ilist.Append((char)(((c2-0x10000) & 0x3ff)+0xdc00));
}
        haveString = true;
        hyphen = false;
      } else if (haveHyphen && (c2>= '0' && c2<= '9')) {
        if (c2 <= 0xffff) {
  { ilist.Append((char)(c2));
}
  } else if (c2 <= 0x10ffff) {
ilist.Append((char)((((c2-0x10000) >> 10) & 0x3ff)+0xd800));
ilist.Append((char)(((c2-0x10000) & 0x3ff)+0xdc00));
}
        haveString = true;
        hyphen = false;
      } else if (c2=='-') {
        if (hyphen||!haveString) {
 throw new ParserException();
}
        if (c2 <= 0xffff) {
  { ilist.Append((char)(c2));
}
  } else if (c2 <= 0x10ffff) {
ilist.Append((char)((((c2-0x10000) >> 10) & 0x3ff)+0xd800));
ilist.Append((char)(((c2-0x10000) & 0x3ff)+0xdc00));
}
        hyphen = true;
        haveHyphen = true;
        haveString = true;
      } else {
        if (c2 >= 0) {
          input.moveBack(1);
        }
        if (hyphen||!haveString) {
 throw new ParserException();
}
        return ilist.ToString();
      }
    }
  }

  private RDFTerm readObject(bool acceptLiteral) {
    int ch = input.ReadChar();
    if (ch< 0) {
 throw new ParserException();
  } else if (ch=='<') {
 return (RDFTerm.fromIRI(readIriReference()));
  } else if (acceptLiteral && (ch=='\"')) { // start of quote literal
      string str = readStringLiteral(ch);
      return (finishStringLiteral(str));
    } else if (ch=='_') { // Blank Node Label
      if (input.ReadChar()!=':') {
 throw new ParserException();
}
      string label = readBlankNodeLabel();
      RDFTerm term = bnodeLabels[label];
      if (term == null) {
        term = RDFTerm.fromBlankNode(label);
        bnodeLabels.Add(label, term);
      }
      return (term);
    } else {
 throw new ParserException();
}
  }
  private string readStringLiteral(int ch) {
    var ilist = new StringBuilder();
    while (true) {
      int c2 = input.ReadChar();
      if (c2<0x20 || c2>0x7e) {
 throw new ParserException();
  } else if (c2=='\\') {
        c2 = readUnicodeEscape(true);
        if (c2 <= 0xffff) {
  { ilist.Append((char)(c2));
}
  } else if (c2 <= 0x10ffff) {
ilist.Append((char)((((c2-0x10000) >> 10) & 0x3ff)+0xd800));
ilist.Append((char)(((c2-0x10000) & 0x3ff)+0xdc00));
}
      } else if (c2 == ch) {
 return ilist.ToString();
} else {
        if (c2 <= 0xffff) {
  { ilist.Append((char)(c2));
}
  } else if (c2 <= 0x10ffff) {
ilist.Append((char)((((c2-0x10000) >> 10) & 0x3ff)+0xd800));
ilist.Append((char)(((c2-0x10000) & 0x3ff)+0xdc00));
}
      }
    }
  }

  private RDFTriple readTriples() {
    int mark = input.setHardMark();
    int ch = input.ReadChar();
    #if DEBUG
if (!(ch >= 0)) {
  { throw new InvalidOperationException("ch>= 0");
}
 }
#endif
    input.setMarkPosition(mark);
    RDFTerm subject = readObject(false);
    if (!skipWhitespace()) {
 throw new ParserException();
}
    if (input.ReadChar()!='<') {
 throw new ParserException();
}
    RDFTerm predicate = RDFTerm.fromIRI(readIriReference());
    if (!skipWhitespace()) {
 throw new ParserException();
}
    RDFTerm obj = readObject(true);
    skipWhitespace();
    if (input.ReadChar()!='.') {
 throw new ParserException();
}
    skipWhitespace();
    var ret = new RDFTriple(subject, predicate, obj);
    endOfLine(input.ReadChar());
    return ret;
  }

  private int readUnicodeEscape(bool extended) {
    int ch = input.ReadChar();
    if (ch=='U') {
      if (input.ReadChar()!='0') {
 throw new ParserException();
}
      if (input.ReadChar()!='0') {
 throw new ParserException();
}
      int a = toHexValue(input.ReadChar());
      int b = toHexValue(input.ReadChar());
      int c = toHexValue(input.ReadChar());
      int d = toHexValue(input.ReadChar());
      int e = toHexValue(input.ReadChar());
      int f = toHexValue(input.ReadChar());
      if (a<0||b<0||c<0||d<0||e<0||f< 0) {
 throw new ParserException();
}
      ch=(a << 20)|(b << 16)|(c << 12)|(d << 8)|(e << 4)|(f);
      // NOTE: The following makes the code too strict
      //if (ch<0x10000) {
 //throw new ParserException();
//}
    } else if (ch=='u') {
      int a = toHexValue(input.ReadChar());
      int b = toHexValue(input.ReadChar());
      int c = toHexValue(input.ReadChar());
      int d = toHexValue(input.ReadChar());
      if (a<0||b<0||c<0||d< 0) {
 throw new ParserException();
}
      ch=(a << 12)|(b << 8)|(c << 4)|(d);
      // NOTE: The following makes the code too strict
      //if (ch == 0x09 || ch == 0x0a || ch == 0x0d ||
      //    (ch >= 0x20 && ch <= 0x7e)) {
  //  throw new ParserException();
  // }
    } else if (ch=='t') {
 return '\t';
  } else if (extended && ch=='n') {
 return '\n';
  } else if (extended && ch=='r') {
 return '\r';
  } else if (extended && ch=='\\') {
 return '\\';
  } else if (extended && ch=='"') {
 return '\"';
} else {
 throw new ParserException();
}
    // Reject surrogate code points
    // as Unicode escapes
    if ((ch & 0xf800) == 0xd800) {
 throw new ParserException();
}
    return ch;
  }

  private bool skipWhitespace() {
    var haveWhitespace = false;
    input.setSoftMark();
    while (true) {
      int ch = input.ReadChar();
      if (ch != 0x09 && ch != 0x20) {
        if (ch >= 0) {
          input.moveBack(1);
        }
        return haveWhitespace;
      }
      haveWhitespace = true;
    }
  }

  private int toHexValue(int a) {
    if (a>= '0' && a<= '9') {
 return a-'0';
}
    return (a>= 'a' && a<= 'f') ? (a+10-'a') : ((a>= 'A' && a<= 'F') ?
      (a+10-'A') : (-1));
  }
}
}