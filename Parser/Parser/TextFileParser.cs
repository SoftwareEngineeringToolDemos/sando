﻿using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using Sando.Core.Logging.Events;
using Sando.ExtensionContracts.ParserContracts;
using Sando.ExtensionContracts.ProgramElementContracts;

namespace Sando.Parser
{
    public class TextFileParser : IParser
    {
        private const int MaxNumberOfTermsInFile = 50000;

        public List<ProgramElement> Parse(string filename)
        {
            var list = new List<ProgramElement>();
            var termSeparators = new char[] {' ', '\n', '\t', '\r'};
            try
            {
                int numberOfTermsRead = 0;
                using (var sr = new StreamReader(filename))
                {
                    string fileText = string.Empty;
                    string line = string.Empty;                    
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (!String.IsNullOrWhiteSpace(line))
                        {
                            fileText += line + Environment.NewLine;
                            numberOfTermsRead += line.Split(termSeparators, StringSplitOptions.RemoveEmptyEntries).Length;
                        }
                        if (numberOfTermsRead >= MaxNumberOfTermsInFile)
                        {
                            break;
                        }
                    }

                    var element = new TextFileElement(filename, fileText, SrcMLParsingUtils.RetrieveSource(fileText));
                    list.Add(element);
                }
            }
            catch (Exception e)
            {
                LogEvents.ParserGenericFileError(this, filename);
            }

            return list;
        }

        public List<ProgramElement> Parse(string fileName, System.Xml.Linq.XElement sourceElements)
        {
            return Parse(fileName);
        }
    }
}
