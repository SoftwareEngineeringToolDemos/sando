﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Xml.Linq;
using Sando.Core.Extensions;
using Sando.Core.Tools;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.ProgramElementContracts;
using Sando.Indexer;
using Sando.Indexer.Documents;
using Sando.Indexer.IndexState;
using Sando.Core.Logging.Events;
using Sando.Parser;


namespace Sando.UI.Monitoring
{
    public class IndexUpdateManager
	{
        private readonly DocumentIndexer _currentIndexer;

        public event IndexUpdated indexUpdated;
        public delegate void IndexUpdated(ReadOnlyCollection<ProgramElement> updatedElements);

        public IndexUpdateManager()
        {
            _currentIndexer = ServiceLocator.Resolve<DocumentIndexer>();
        }

        public void Update(string filePath, XElement xElement)
        {
            var fileInfo = new FileInfo(filePath);            
            try
            {
                var parsed = new List<ProgramElement>();
                var codeParser = ExtensionPointsRepository.Instance.GetParserImplementation(fileInfo.Extension);
                if (codeParser != null)
                {
                    parsed.AddRange(codeParser.Parse(filePath, xElement));
                }
                if (codeParser == null || !codeParser.GetType().Equals(typeof(TextFileParser)))
                {
                    //parses everything with the TextFileParser (double parsing things that were parsed with SrcML)
                    var textFileParser = new TextFileParser();
                    parsed.AddRange(textFileParser.Parse(filePath, xElement));
                }

                var unresolvedElements = parsed.FindAll(pe => pe is CppUnresolvedMethodElement);
                if (unresolvedElements.Count > 0)
                {
                    //first generate program elements for all the included headers
                    var headerElements = CppHeaderElementResolver.GenerateCppHeaderElements(filePath, unresolvedElements);

                    //then try to resolve
                    foreach (CppUnresolvedMethodElement unresolvedElement in unresolvedElements)
                    {
                        var document = CppHeaderElementResolver.GetDocumentForUnresolvedCppMethod(unresolvedElement, headerElements);
                        if (document != null)
                        {
                            //writeLog( "- DI.AddDocument()");
                            _currentIndexer.AddDocument(document);
                        }
                    }
                }

                foreach (var programElement in parsed)
                {
                    if (!(programElement is CppUnresolvedMethodElement))
                    {
                        var document = DocumentFactory.Create(programElement);
                        if (document != null)
                        {
                            //writeLog( "- DI.AddDocument()");
                            _currentIndexer.AddDocument(document);
                        }
                    }
                }
                if(indexUpdated!=null)
                    indexUpdated(parsed.AsReadOnly());
            }
            catch (Exception e)
            {
                LogEvents.UIIndexUpdateError(this, e);
            }
        }

	}

   
}
