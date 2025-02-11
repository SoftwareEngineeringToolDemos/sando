﻿/**
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

/* Conversion from Java by Ben Martz, 25 Apr 2008 */

using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Analysis;
using System.IO;
using System.Text.RegularExpressions;

namespace Portal.LuceneInterface
{
    /**
     * Splits words into subwords and performs optional transformations on subword groups.
     * Words are split into subwords with the following rules:
     *  - split on intra-word delimiters (by default, all non alpha-numeric characters).
     *     - "Wi-Fi" -> "Wi", "Fi"
     *  - split on case transitions
     *     - "PowerShot" -> "Power", "Shot"
     *  - split on letter-number transitions
     *     - "SD500" -> "SD", "500"
     *  - leading and trailing intra-word delimiters on each subword are ignored
     *     - "//hello---there, 'dude'" -> "hello", "there", "dude"
     *  - trailing "'s" are removed for each subword
     *     - "O'Neil's" -> "O", "Neil"
     *     - Note: this step isn't performed in a separate filter because of possible subword combinations.
     *
     * The <b>combinations</b> parameter affects how subwords are combined:
     *  - combinations="0" causes no subword combinations.
     *     - "PowerShot" -> 0:"Power", 1:"Shot"  (0 and 1 are the token positions)
     *  - combinations="1" means that in addition to the subwords, maximum runs of non-numeric subwords are catenated and produced at the same position of the last subword in the run.
     *     - "PowerShot" -> 0:"Power", 1:"Shot" 1:"PowerShot"
     *     - "A's+B's&C's" -> 0:"A", 1:"B", 2:"C", 2:"ABC"
     *     - "Super-Duper-XL500-42-AutoCoder!" -> 0:"Super", 1:"Duper", 2:"XL", 2:"SuperDuperXL", 3:"500" 4:"42", 5:"Auto", 6:"Coder", 6:"AutoCoder"
     *
     *  One use for WordDelimiterFilter is to help match words with different subword delimiters.
     *  For example, if the source text contained "wi-fi" one may want "wifi" "WiFi" "wi-fi" "wi+fi"
     *  queries to all match.
     *  One way of doing so is to specify combinations="1" in the analyzer
     *  used for indexing, and combinations="0" (the default) in the analyzer
     *  used for querying.  Given that the current StandardTokenizer
     *  immediately removes many intra-word delimiters, it is recommended that
     *  this filter be used after a tokenizer that does not do this
     *  (such as WhitespaceTokenizer).
     *
     *  @author yonik
     *  @version $Id: WordDelimiterFilter.java 472574 2006-11-08 18:25:52Z yonik $
     */
    public class WordDelimiterFilter : TokenFilter
    {
        private byte[] charTypeTable;

        public static byte LOWER = 0x01;
        public static byte UPPER = 0x02;
        public static byte DIGIT = 0x04;
        public static byte SUBWORD_DELIM = 0x08;

        // combinations: for testing, not for setting bits
        public static byte ALPHA = 0x03;
        public static byte ALPHANUM = 0x07;

        // TODO: should there be a WORD_DELIM category for
        // chars that only separate words (no catenation of subwords
        // will be done if separated by these chars?)
        // "," would be an obvious candidate...

        static byte[] defaultWordDelimTable;
        static WordDelimiterFilter()
        {
            byte[] tab = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                byte code = 0;
                if (Char.IsLower((char)i)) code |= LOWER;
                else if (Char.IsUpper((char)i)) code |= UPPER;
                else if (Char.IsDigit((char)i)) code |= DIGIT;
                if (code == 0) code = SUBWORD_DELIM;
                tab[i] = code;
            }
            defaultWordDelimTable = tab;
        }

        /**
         * If 1, causes parts of words to be generated:
         * <p/>
         * "PowerShot" => "Power" "Shot"
         */
        int generateWordParts;

        /**
         * If 1, causes number subwords to be generated:
         * <p/>
         * "500-42" => "500" "42"
         */
        int generateNumberParts;

        /**
         * If 1, causes maximum runs of word parts to be catenated:
         * <p/>
         * "wi-fi" => "wifi"
         */
        int catenateWords;

        /**
         * If 1, causes maximum runs of number parts to be catenated:
         * <p/>
         * "500-42" => "50042"
         */
        int catenateNumbers;

        /**
         * If 1, causes all subword parts to be catenated:
         * <p/>
         * "wi-fi-4000" => "wifi4000"
         */
        int catenateAll;

        /**
         *
         * @param in Token stream to be filtered.
         * @param charTypeTable
         * @param generateWordParts If 1, causes parts of words to be generated: "PowerShot" => "Power" "Shot"
         * @param generateNumberParts If 1, causes number subwords to be generated: "500-42" => "500" "42"
         * @param catenateWords  1, causes maximum runs of word parts to be catenated: "wi-fi" => "wifi"
         * @param catenateNumbers If 1, causes maximum runs of number parts to be catenated: "500-42" => "50042"
         * @param catenateAll If 1, causes all subword parts to be catenated: "wi-fi-4000" => "wifi4000"
         */
        public WordDelimiterFilter(TokenStream inStream, byte[] charTypeTable, int generateWordParts, int generateNumberParts, int catenateWords, int catenateNumbers, int catenateAll)
            : base(inStream)
        {
            this.generateWordParts = generateWordParts;
            this.generateNumberParts = generateNumberParts;
            this.catenateWords = catenateWords;
            this.catenateNumbers = catenateNumbers;
            this.catenateAll = catenateAll;
            this.charTypeTable = charTypeTable;
        }

        /**
         * @param in Token stream to be filtered.
         * @param generateWordParts If 1, causes parts of words to be generated: "PowerShot" => "Power" "Shot"
         * @param generateNumberParts If 1, causes number subwords to be generated: "500-42" => "500" "42"
         * @param catenateWords  1, causes maximum runs of word parts to be catenated: "wi-fi" => "wifi"
         * @param catenateNumbers If 1, causes maximum runs of number parts to be catenated: "500-42" => "50042"
         * @param catenateAll If 1, causes all subword parts to be catenated: "wi-fi-4000" => "wifi4000"
         */
        public WordDelimiterFilter(TokenStream inStream, int generateWordParts, int generateNumberParts, int catenateWords, int catenateNumbers, int catenateAll)
            : base(inStream)
        {
            this.generateWordParts = generateWordParts;
            this.generateNumberParts = generateNumberParts;
            this.catenateWords = catenateWords;
            this.catenateNumbers = catenateNumbers;
            this.catenateAll = catenateAll;
            this.charTypeTable = defaultWordDelimTable;
        }

        int CharType(char ch)
        {
            if (ch < charTypeTable.Length)
            {
                return charTypeTable[ch];
            }
            else if (Char.IsLower(ch))
            {
                return LOWER;
            }
            else if (Char.IsLetter(ch))
            {
                return UPPER;
            }
            else if (Char.IsNumber(ch))
            {
                return DIGIT;
            }
            else
            {
                return SUBWORD_DELIM;
            }
        }

        // use the type of the first char as the type
        // of the token.
        private int TokType(Token t)
        {
            return CharType(t.TermText()[0]);
        }

        // There isn't really an efficient queue class, so we will
        // just use an array for now.
        private List<Token> queue = new List<Token>(4);
        private int queuePos = 0;

        // temporary working queue
        private List<Token> tlist = new List<Token>(4);


        private Token NewTok(Token orig, int start, int end)
        {
            // end appears to be erroneously named
            return new Token(orig.TermText().Substring(start, end - start),
                    orig.StartOffset() + start,
                    orig.StartOffset() + end,
                    orig.Type());
        }


        private Token NewSpecialTok(Token orig, int start, int end)
        {
            // end appears to be erroneously named
            return new AbbreviationToken(orig.TermText().Substring(start, end - start),
                    orig.StartOffset() + start,
                    orig.StartOffset() + end,
                    orig.Type());
        }

        private class AbbreviationToken : Token
        {

            public AbbreviationToken(string p1, int p2, int p3, string p4) :base(p1,p2,p3,p4)
            {
            }

        }

        public override Token Next()
        {

            // check the queue first
            if (queuePos < queue.Count)
            {
                return queue[queuePos++];
            }

            // reset the queue if it had been previously used
            if (queuePos != 0)
            {
                queuePos = 0;
                queue.Clear();
            }


            // optimize for the common case: assume there will be
            // no subwords (just a simple word)
            //
            // Would it actually be faster to check for the common form
            // of isLetter() isLower()*, and then backtrack if it doesn't match?

            int origPosIncrement;
            while (true)
            {
                Token t = input.Next();
                if (t == null) return null;

                String s = t.TermText();
                int start = 0;
                int end = s.Length;
                if (end == 0) continue;

                origPosIncrement = t.GetPositionIncrement();

                // Avoid calling charType more than once for each char (basically
                // avoid any backtracking).
                // makes code slightly more difficult, but faster.
                char ch = s[start];
                int type = CharType(ch);

                int numWords = 0;

                while (start < end)
                {
                    // first eat delimiters at the start of this subword
                    while ((type & SUBWORD_DELIM) != 0 && ++start < end)
                    {
                        ch = s[start];
                        type = CharType(ch);
                    }

                    int pos = start;

                    // save the type of the first char of the subword
                    // as a way to tell what type of subword token this is (number, word, etc)
                    int firstType = type;
                    int lastType = type;  // type of the previously read char

                    while (pos < end)
                    {

                        if (type != lastType)
                        {
                            // check and remove "'s" from the end of a token.
                            // the pattern to check for is
                            //   ALPHA "'" ("s"|"S") (SUBWORD_DELIM | END)
                            if ((lastType & ALPHA) != 0)
                            {
                                if (ch == '\'' && pos + 1 < end
                                        && (s[pos + 1] == 's' || s[pos + 1] == 'S'))
                                {
                                    int subWordEnd = pos;
                                    if (pos + 2 >= end)
                                    {
                                        // end of string detected after "'s"
                                        pos += 2;
                                    }
                                    else
                                    {
                                        // make sure that a delimiter follows "'s"
                                        char ch2 = s[pos + 2];
                                        int type2 = CharType(ch2);
                                        if ((type2 & SUBWORD_DELIM) != 0)
                                        {
                                            // if delimiter, move position pointer
                                            // to it (skipping over "'s"
                                            ch = ch2;
                                            type = type2;
                                            pos += 2;
                                        }
                                    }

                                    queue.Add(NewTok(t, start, subWordEnd));
                                    if ((firstType & ALPHA) != 0) numWords++;
                                    break;
                                }
                            }

                            // For case changes, only split on a transition from
                            // lower to upper case, not vice-versa.
                            // That will correctly handle the
                            // case of a word starting with a capital (won't split).
                            // It will also handle pluralization of
                            // an uppercase word such as FOOs (won't split).
                            if ((lastType & UPPER) != 0 && (type & LOWER) != 0)
                            {
                                // UPPER->LOWER: Don't split
                                //In the case "XMLElement", trying to get "XML" & "Element", get the last cap letter as a token too
                                //from "XMLElement" -> "XMLElement XML Element lement"
                                if (pos - start > 1)
                                {
                                    queue.Add(NewSpecialTok(t, start, pos));
                                    if ((firstType & ALPHA) != 0) numWords++;
                                    break;
                                }
                            }
                            else
                            {
                                // NOTE: this code currently assumes that only one flag
                                // is set for each character now, so we don't have
                                // to explicitly check for all the classes of transitions
                                // listed below.

                                // LOWER->UPPER
                                // ALPHA->NUMERIC
                                // NUMERIC->ALPHA
                                // *->DELIMITER
                                queue.Add(NewTok(t, start, pos));
                                if ((firstType & ALPHA) != 0) numWords++; 
                                break;
                            }
                        }

                        if (++pos >= end)
                        {
                            if (start == 0)
                            {
                                // the subword is the whole original token, so
                                // return it unchanged.
                                return t;
                            }

                            Token newtok = NewTok(t, start, pos);

                            // optimization... if this is the only token,
                            // return it immediately.
                            if (queue.Count == 0)
                            {
                                newtok.SetPositionIncrement(origPosIncrement);
                                return newtok;
                            }

                            queue.Add(newtok);
                            if ((firstType & ALPHA) != 0) numWords++;
                            break;
                        }
                        lastType = type;
                        ch = s[pos];
                        type = CharType(ch);
                    }

                    // start of the next subword is the current position
                    start = pos;
                }

                // System.out.println("##########TOKEN=" + s + " ######### WORD DELIMITER QUEUE=" + str(queue));

                int numtok = queue.Count;

                // We reached the end of the current token.
                // If the queue is empty, we should continue by reading
                // the next token
                if (numtok == 0)
                {
                    continue;
                }

                // if number of tokens is 1, always return the single tok
                if (numtok == 1)
                {
                    break;
                }

                int numNumbers = numtok - numWords;

                // check conditions under which the current token
                // queue may be used as-is (no catenations needed)
                if (catenateAll == 0    // no "everything" to catenate
                  && (catenateWords == 0 || numWords <= 1)   // no words to catenate
                  && (catenateNumbers == 0 || numNumbers <= 1)    // no numbers to catenate
                  && (generateWordParts != 0 || numWords == 0)  // word generation is on
                  && (generateNumberParts != 0 || numNumbers == 0)) // number generation is on
                {
                    break;
                }


                // swap queue and the temporary working list, then clear the
                // queue in preparation for adding all combinations back to it.
                List<Token> tmp = tlist;
                tlist = queue;
                queue = tmp;
                queue.Clear();

                if (numWords == 0)
                {
                    // all numbers
                    AddCombos(s, tlist, 0, numtok, generateNumberParts != 0, catenateNumbers != 0 || catenateAll != 0, 1);
                    if (queue.Count > 0) break; else continue;
                }
                else if (numNumbers == 0)
                {
                    // all words
                    AddCombos(s, tlist, 0, numtok, generateWordParts != 0, catenateWords != 0 || catenateAll != 0, 1);
                    if (queue.Count > 0) break; else continue;
                }
                else if (generateNumberParts == 0 && generateWordParts == 0 && catenateNumbers == 0 && catenateWords == 0)
                {
                    // catenate all *only*
                    // OPT:could be optimized to add to current queue...
                    AddCombos(s, tlist, 0, numtok, false, catenateAll != 0, 1);
                    if (queue.Count > 0) break; else continue;
                }

                //
                // Find all adjacent tokens of the same type.
                //
                Token tok = tlist[0];
                bool isWord = true; // (TokType(tok) & ALPHA) != 0;
                bool wasWord = isWord;

                for (int i = 0; i < numtok; )
                {
                    int j;
                    for (j = i + 1; j < numtok; j++)
                    {
                        wasWord = isWord;
                        tok = tlist[j];
                        isWord = true; //(TokType(tok) & ALPHA) != 0;
                        if (isWord != wasWord) break;
                    }
                    if (wasWord)
                    {
                        AddCombos(s, tlist, i, j, generateWordParts != 0, catenateWords != 0, 1);
                    }
                    else
                    {
                        AddCombos(s, tlist, i, j, generateNumberParts != 0, catenateNumbers != 0, 1);
                    }
                    i = j;
                }

                //// take care catenating all subwords
                //if (catenateAll != 0)
                //{
                //    AddCombos(s, tlist, 0, numtok, false, true, 0);
                //}

                // NOTE: in certain cases, queue may be empty (for instance, if catenate
                // and generate are both set to false).  Only exit the loop if the queue
                // is not empty.
                if (queue.Count > 0) break;
            }

            // System.out.println("##########AFTER COMBINATIONS:"+ str(queue));

            queuePos = 1;
            Token tok2 = queue[0];
            tok2.SetPositionIncrement(origPosIncrement);
            return tok2;
        }


        // index "a","b","c" as  pos0="a", pos1="b", pos2="c", pos2="abc"
        private void AddCombos(string s, List<Token> lst, int start, int end, bool generateSubwords, bool catenateSubwords, int posOffset)
        {
            if (end - start == 1)
            {
                // always generate a word alone, even if generateSubwords=0 because
                // the catenation of all the subwords *is* the subword.
                queue.Add(lst[start]);
                return;
            }

            var generateOnlyDashSubwords = IsGUID(s);

            StringBuilder sb = null;
            StringBuilder sbWithUnderscores = null;
            if (catenateSubwords) sb = new StringBuilder();
            bool catenateSubwordsWithUnderScores = false;
            bool startWithUnderscore = false;
            var both = ShouldCatenateSubwords(s, lst, start, end);
            catenateSubwordsWithUnderScores = both.Item1;
            startWithUnderscore = both.Item2;
            if (catenateSubwordsWithUnderScores) sbWithUnderscores = new StringBuilder();
            Token firstTok = null;
            Token lastToken = null;
            Token tok = null;
            lastToken = null;
            int subwordIndexForGuids = 0;
            for (int i = start; i < end; i++)
            {
                tok = lst[i];
                if (catenateSubwords)
                {
                    if (i == start) firstTok = tok;
                    sb.Append(tok.TermText());
                    if (catenateSubwordsWithUnderScores)
                    {                        
                        if (lastToken != null)
                            if (lastToken.EndOffset() + 1 == tok.StartOffset())
                                sbWithUnderscores.Append(s[GetMinusOnePosition(firstTok,tok)]);
                        if (startWithUnderscore)
                            {
                                sbWithUnderscores.Append("_");
                                startWithUnderscore = false;
                            }
                        sbWithUnderscores.Append(tok.TermText());
                    }
                }
                if (generateSubwords)
                {
                    if (generateOnlyDashSubwords)
                    {
                        if (
                            (lastToken != null && lastToken.EndOffset() + 1 == tok.StartOffset()) //at a dash in guid
                            ||
                            (i + 1 == end) //at the end of guid
                            )
                        {
                            StringBuilder lastFewGuidTokens = new StringBuilder();
                            //get last few tokens
                            for (int index = subwordIndexForGuids; index < i; index++)
                            {
                                var subword = lst[index];
                                lastFewGuidTokens.Append(subword.Term());
                            }
                            if (i + 1 == end) //at the end of guid
                                lastFewGuidTokens.Append(lst[i].Term()); //add last one
                            Token concatTok = new Token(lastFewGuidTokens.ToString(),
                                    lst[subwordIndexForGuids].StartOffset(),
                                    lst[i - 1].EndOffset(),
                                    lst[subwordIndexForGuids].Type());
                            queue.Add(concatTok);
                            subwordIndexForGuids = i;
                        }
                    }
                    else
                        queue.Add(tok);
                }
                lastToken = tok;
            }

            if (catenateSubwords)
            {
                Token concatTok = new Token(sb.ToString(),
                        firstTok.StartOffset(),
                        tok.EndOffset(),
                        firstTok.Type());
                // if we indexed some other tokens, then overlap concatTok with the last.
                // Otherwise, use the value passed in as the position offset.
                concatTok.SetPositionIncrement(generateSubwords == true ? 0 : posOffset);
                queue.Add(concatTok);
                if (catenateSubwordsWithUnderScores)
                {
                    Token underscoresToken = new Token(sbWithUnderscores.ToString(),
                        firstTok.StartOffset(),
                        tok.EndOffset(),
                        firstTok.Type());
                    queue.Add(underscoresToken);
                }
            }
            AbbreviationToken lastOne = null;
            for (int i = start; i < end; i++)
            {
                tok = lst[i];
                if (lastOne != null)
                {
                    Token concatTok = new Token(lastOne.Term().Substring(lastOne.Term().Length -1)+tok.Term(),
                        tok.StartOffset()-1,
                        tok.EndOffset(),
                        tok.Type());
                    queue.Add(concatTok);
                    if (lastOne.Term().Length >= 3)
                    {
                        Token abbreviation = new Token(lastOne.Term().Substring(0,lastOne.Term().Length - 1),
                            lastOne.StartOffset(),
                            lastOne.EndOffset() - 1,
                            lastOne.Type());
                        queue.Add(abbreviation);
                    }
                }
                if (tok as AbbreviationToken!=null)
                {
                    lastOne = tok as AbbreviationToken;
                }
                else
                {
                    lastOne = null;
                }
            }
        }

        private static string guid = @"[a-f0-9]*";
        private static Regex guidRegex = new Regex(guid, RegexOptions.None);


        private bool IsGUID(string s)
        {
            //61e80ffa-f99b-46ac-8dd0-f3f4171568f3
            //7e03caf3-06ed-4ff5-962a-effa1fb2f383            
            if (s.Length == 36 && s.IndexOf('-') == 8 && s[13] == '-')
                return true;
            //7e03caf306ed4ff5962aeffa1fb2f383
            if (s.Length == 32)
            {
                if (guidRegex.IsMatch(s,0))
                    return true;
            }
            return false;
        }

        private int GetMinusOnePosition(Token firstTok, Token tok)
        {
            var started = firstTok.StartOffset();
            var current = tok.StartOffset();
            return (current - started -1);
        }

        private static Tuple<bool,bool> ShouldCatenateSubwords(string s, List<Token> lst, int start, int end)
        {
            bool catenateSubwordsWithUnderScores = false;
            bool startWithUnderscore = false;
            if (s.IndexOf('_') != -1 || s.IndexOf('-') != -1)
                catenateSubwordsWithUnderScores = true;
            if (s.StartsWith("_"))
            {
                catenateSubwordsWithUnderScores = true;
                startWithUnderscore = true;
            }
            return new Tuple<bool, bool>(catenateSubwordsWithUnderScores, startWithUnderscore);
        }


        // questions:
        // negative numbers?  -42 indexed as just 42?
        // dollar sign?  $42
        // percent sign?  33%
        // downsides:  if source text is "powershot" then a query of "PowerShot" won't match!

        public static MappingCharFilter GetCharMapper(TextReader r)
        {
            var map = new NormalizeCharMap();

            string[] symbols = {  
"[",
"\\",
"]",
"^",
"!",
"\"",        
"#",
"$",
"%",
"&",
"'",
"(",
")",
"*",
"+",
",",
".",
"/",
":",
";",
"<",
"=",
">",
"?",
"@",
"{",
"|",
"}",
"~"};
            foreach (var symbol in symbols)
                map.Add(symbol, " ");
            var mappingCharFilter = new MappingCharFilter(map, r);
            return mappingCharFilter;
        }

    }
}
