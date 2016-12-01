package com.upokecenter.util;
/*
Written in 2013 by Peter Occil.
Any copyright is dedicated to the Public Domain.
http://creativecommons.org/publicdomain/zero/1.0/

If you like this, you should donate to Peter O.
at: http://peteroupc.github.io/
*/

    /**
     * Not documented yet.
     */
  public final class RDFTerm {
    /**
     * Type value for a blank node.
     */
    public static final int BLANK = 0;  // type is blank node name, literal is blank

    /**
     * Type value for an IRI (Internationalized Resource Identifier.).
     */
    public static final int IRI = 1;  // type is IRI, literal is blank

    /**
     * Type value for a string with a language tag.
     */
    public static final int LANGSTRING = 2;  // literal is given

    /**
     * Type value for a piece of data serialized to a string.
     */
    public static final int TYPEDSTRING = 3;  // type is IRI, literal is given

    private static void escapeBlankNode(String str, StringBuilder builder) {
      int length = str.length();
      String hex = "0123456789ABCDEF";
      for (int i = 0; i < length; ++i) {
        int c = str.charAt(i);
        if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
            (c > 0 && c >= '0' && c <= '9')) {
          builder.append((char)c);
        } else if ((c & 0xfc00) == 0xd800 && i + 1 < length &&
                str.charAt(i + 1) >= 0xdc00 && str.charAt(i + 1) <= 0xdfff) {
          // Get the Unicode code point for the surrogate pair
          c = 0x10000 + ((c - 0xd800) << 10) + (str.charAt(i + 1) - 0xdc00);
          builder.append("U00");
          builder.append(hex.charAt((c >> 20) & 15));
          builder.append(hex.charAt((c >> 16) & 15));
          builder.append(hex.charAt((c >> 12) & 15));
          builder.append(hex.charAt((c >> 8) & 15));
          builder.append(hex.charAt((c >> 4) & 15));
          builder.append(hex.charAt(c & 15));
          ++i;
        } else {
          builder.append("u");
          builder.append(hex.charAt((c >> 12) & 15));
          builder.append(hex.charAt((c >> 8) & 15));
          builder.append(hex.charAt((c >> 4) & 15));
          builder.append(hex.charAt(c & 15));
        }
      }
    }

    private static void escapeLanguageTag(String str, StringBuilder builder) {
      int length = str.length();
      boolean hyphen = false;
      for (int i = 0; i < length; ++i) {
        int c = str.charAt(i);
        if (c >= 'A' && c <= 'Z') {
          builder.append((char)(c + 0x20));
        } else if (c >= 'a' && c <= 'z') {
          builder.append((char)c);
        } else if (hyphen && c >= '0' && c <= '9') {
          builder.append((char)c);
        } else if (c == '-') {
          builder.append((char)c);
          hyphen = true;
          if (i + 1 < length && str.charAt(i + 1) == '-') {
            builder.append('x');
          }
        } else {
          builder.append('x');
        }
      }
    }

    private static void escapeString(
  String str,
  StringBuilder builder,
  boolean uri) {
      int length = str.length();
      String Hex = "0123456789ABCDEF";
      for (int i = 0; i < length; ++i) {
        int c = str.charAt(i);
        if (c == 0x09) {
          builder.append("\\t");
        } else if (c == 0x0a) {
          builder.append("\\n");
        } else if (c == 0x0d) {
          builder.append("\\r");
        } else if (c == 0x22) {
          builder.append("\\\"");
        } else if (c == 0x5c) {
          builder.append("\\\\");
        } else if (uri && c == '>') {
          builder.append("%3E");
        } else if (c >= 0x20 && c <= 0x7e) {
          builder.append((char)c);
        } else if ((c & 0xfc00) == 0xd800 && i + 1 < length &&
                str.charAt(i + 1) >= 0xdc00 && str.charAt(i + 1) <= 0xdfff) {
          // Get the Unicode code point for the surrogate pair
          c = 0x10000 + ((c - 0xd800) << 10) + (str.charAt(i + 1) - 0xdc00);
          builder.append("\\U00");
          builder.append(Hex.charAt((c >> 20) & 15));
          builder.append(Hex.charAt((c >> 16) & 15));
          builder.append(Hex.charAt((c >> 12) & 15));
          builder.append(Hex.charAt((c >> 8) & 15));
          builder.append(Hex.charAt((c >> 4) & 15));
          builder.append(Hex.charAt(c & 15));
          ++i;
        } else {
          builder.append("\\u");
          builder.append(Hex.charAt((c >> 12) & 15));
          builder.append(Hex.charAt((c >> 8) & 15));
          builder.append(Hex.charAt((c >> 4) & 15));
          builder.append(Hex.charAt(c & 15));
        }
      }
    }

    private String typeOrLanguage = null;
    private String value = null;
    private int kind;

    /**
     * Predicate for RDF types.
     */
    public static final RDFTerm A =
        fromIRI("http://www.w3.org/1999/02/22-rdf-syntax-ns#type");

    /**
     * Predicate for the first object in a list.
     */
    public static final RDFTerm FIRST = fromIRI(
        "http://www.w3.org/1999/02/22-rdf-syntax-ns#first");

    /**
     * Object for nil, the end of a list, or an empty list.
     */
    public static final RDFTerm NIL = fromIRI(
        "http://www.w3.org/1999/02/22-rdf-syntax-ns#nil");

    /**
     * Predicate for the remaining objects in a list.
     */
    public static final RDFTerm REST = fromIRI(
        "http://www.w3.org/1999/02/22-rdf-syntax-ns#rest");

    /**
     * Object for false.
     */
    public static final RDFTerm FALSE = fromTypedString(
        "false",
        "http://www.w3.org/2001/XMLSchema#boolean");

    /**
     * Object for true.
     */
    public static final RDFTerm TRUE = fromTypedString(
        "true",
        "http://www.w3.org/2001/XMLSchema#boolean");

    /**
     * Not documented yet.
     * @param name The parameter {@code name} is not documented yet.
     * @return A RDFTerm object.
     * @throws java.lang.NullPointerException The parameter {@code name} is null.
     */
    public static RDFTerm fromBlankNode(String name) {
      if (name == null) {
        throw new NullPointerException("name");
      }
      if (name.length() == 0) {
        throw new IllegalArgumentException("name is empty.");
      }
      RDFTerm ret = new RDFTerm();
      ret.kind = BLANK;
      ret.typeOrLanguage = null;
      ret.value = name;
      return ret;
    }

    /**
     * Not documented yet.
     * @param iri The parameter {@code iri} is not documented yet.
     * @return A RDFTerm object.
     * @throws java.lang.NullPointerException The parameter {@code iri} is null.
     */
    public static RDFTerm fromIRI(String iri) {
      if (iri == null) {
        throw new NullPointerException("iri");
      }
      RDFTerm ret = new RDFTerm();
      ret.kind = IRI;
      ret.typeOrLanguage = null;
      ret.value = iri;
      return ret;
    }

    /**
     * Not documented yet.
     * @param str The parameter {@code str} is not documented yet.
     * @param languageTag The parameter {@code languageTag} is not documented yet.
     * @return A RDFTerm object.
     * @throws java.lang.NullPointerException The parameter {@code str} or {@code
     * languageTag} is null.
     */
    public static RDFTerm fromLangString(String str, String languageTag) {
      if (str == null) {
        throw new NullPointerException("str");
      }
      if (languageTag == null) {
        throw new NullPointerException("languageTag");
      }
      if (languageTag.length() == 0) {
        throw new IllegalArgumentException("languageTag is empty.");
      }
      RDFTerm ret = new RDFTerm();
      ret.kind = LANGSTRING;
      ret.typeOrLanguage = languageTag;
      ret.value = str;
      return ret;
    }

    /**
     * Not documented yet.
     * @param str The parameter {@code str} is not documented yet.
     * @return A RDFTerm object.
     */
    public static RDFTerm fromTypedString(String str) {
      return fromTypedString(str, "http://www.w3.org/2001/XMLSchema#String");
    }

    /**
     * Not documented yet.
     * @param str The parameter {@code str} is not documented yet.
     * @param iri The parameter {@code iri} is not documented yet.
     * @return A RDFTerm object.
     * @throws java.lang.NullPointerException The parameter {@code str} or {@code
     * iri} is null.
     */
    public static RDFTerm fromTypedString(String str, String iri) {
      if (str == null) {
        throw new NullPointerException("str");
      }
      if (iri == null) {
        throw new NullPointerException("iri");
      }
      if (iri.length() == 0) {
        throw new IllegalArgumentException("iri is empty.");
      }
      RDFTerm ret = new RDFTerm();
      ret.kind = TYPEDSTRING;
      ret.typeOrLanguage = iri;
      ret.value = str;
      return ret;
    }

    /**
     * Not documented yet.
     * @return The return value is not documented yet.
     */
    @Override public final boolean equals(Object obj) {
      if (this == obj) {
        return true;
      }
      if (obj == null) {
        return false;
      }
      if (GetType() != obj.getClass()) {
        return false;
      }
      RDFTerm other = (RDFTerm)obj;
      if (this.kind != other.kind) {
        return false;
      }
      if (this.typeOrLanguage == null) {
        if (other.typeOrLanguage != null) {
          return false;
        }
      } else if (!this.typeOrLanguage.equals(other.typeOrLanguage)) {
        return false;
      }
      if (this.value == null) {
        return other.value != null;
      } else {
        return !this.value.equals(other.value);
      }
    }

    /**
     * Not documented yet.
     * @return A 32-bit signed integer.
     */
    public int getKind() {
      return this.kind;
    }

    /**
     * Gets the language tag or data type for this RDF literal.
     * @return A text string.
     */
    public String getTypeOrLanguage() {
      return this.typeOrLanguage;
    }

    /**
     * Gets the IRI, blank node identifier, or lexical form of an RDF literal.
     * @return A text string.
     */
    public String getValue() {
      return this.value;
    }

    /**
     * Not documented yet.
     * @return The return value is not documented yet.
     */
    @Override public final int hashCode() {
      {
        int prime = 31;
        int result = prime + this.kind;
        result = (prime * result) + ((this.typeOrLanguage == null) ? 0 :
                this.typeOrLanguage.hashCode());
        boolean isnull = this.value == null;
        result = (prime * result) + (isnull ? 0 : this.value.hashCode());
        return result;
      }
    }

    /**
     * Gets whether this term is a blank node.
     * @return A Boolean object.
     */
    public boolean isBlank() {
      return this.kind == BLANK;
    }

    /**
     * Not documented yet.
     * @param str The parameter {@code str} is not documented yet.
     * @return A Boolean object.
     */
    public boolean isIRI(String str) {
      return this.kind == IRI && str != null && str.equals(this.value);
    }

    /**
     * Not documented yet.
     * @return A Boolean object.
     */
    public boolean isOrdinaryString() {
   return this.kind == TYPEDSTRING && "http://www.w3.org/2001/XMLSchema#String"
        .equals(this.typeOrLanguage);
    }

    /**
     * Gets a string representation of this RDF term in N-Triples format. The
     * string will not end in a line break.
     * @return The return value is not documented yet.
     */
    @Override public final String toString() {
      StringBuilder builder = null;
      if (this.kind == BLANK) {
        builder = new StringBuilder();
        builder.append("_:");
        escapeBlankNode(this.value, builder);
      } else if (this.kind == LANGSTRING) {
        builder = new StringBuilder();
        builder.append("\"");
        escapeString(this.value, builder, false);
        builder.append("\"@");
        escapeLanguageTag(this.typeOrLanguage, builder);
      } else if (this.kind == TYPEDSTRING) {
        builder = new StringBuilder();
        builder.append("\"");
        escapeString(this.value, builder, false);
        builder.append("\"");
        if (!"http://www.w3.org/2001/XMLSchema#String"
              .equals(this.typeOrLanguage)) {
          builder.append("^^<");
          escapeString(this.typeOrLanguage, builder, true);
          builder.append(">");
        }
      } else if (this.kind == IRI) {
        builder = new StringBuilder();
        builder.append("<");
        escapeString(this.value, builder, true);
        builder.append(">");
      } else {
        return "<>";
      }
      return builder.toString();
    }
  }
