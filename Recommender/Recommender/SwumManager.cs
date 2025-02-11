﻿using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using ABB.Swum;
using ABB.Swum.Nodes;
using ABB.SrcML;
using Sando.Core.Logging;
using Sando.Core.Logging.Persistence;
using Sando.Core.Logging.Events;
using System.Timers;
using Sando.DependencyInjection;
using ABB.VisualStudio;
using System.Threading.Tasks;


namespace Sando.Recommender {
    /// <summary>
    /// Builds SWUM for the methods and method calls in a srcML file.
    /// </summary>
    public class SwumManager {
        private static SwumManager _instance;
        private const string DefaultCacheFile = "swum-cache.txt";
        private readonly XName[] _functionTypes = new XName[] { SRC.Function, SRC.Constructor, SRC.Destructor };
        private SwumBuilder _builder;

        public SwumDataStructure SwumDataStore { get; private set; }

        /// <summary>
        /// Private constructor for a new SwumManager.
        /// </summary>
        private SwumManager() {
            _builder = new UnigramSwumBuilder { Splitter = new CamelIdSplitter() };
            SwumDataStore = new SwumDataStructure();
            CacheLoaded = false;
            Timer printer = new Timer();
            printer.Interval = 1000 * 60 * 10;
            printer.Elapsed += printer_Elapsed;
            printer.Start();
        }

        void printer_Elapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                var scheduler = ServiceLocator.Resolve<ITaskManagerService>().GlobalScheduler;
                 Action action =  () =>
                {
                    try
                    {
                        PrintSwumCache();
                    }
                    catch (InvalidOperationException ee)
                    {
                        //ignore when someone else is using the file
                    }
                };                 
                 Task.Factory.StartNew(action, new System.Threading.CancellationToken(), TaskCreationOptions.None, scheduler);
            }
            catch (Exception eee)
            {
                //ignore, as periodic update is not a feature we want to crash on.
            }
        }

        /// <summary>
        /// Gets the singleton instance of SwumManager.
        /// </summary>
        public static SwumManager Instance { 
            get {
                if(_instance == null) {
                    _instance = new SwumManager();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Gets or sets the SwumBuilder used to construct SWUM.
        /// </summary>
        public SwumBuilder Builder {
            get { return _builder; }
            set { _builder = value; }
        }

        /// <summary>
        /// The SrcMLArchive to retrieve SrcML files from
        /// </summary>
        public SrcMLArchive Archive { get; set; }

        /// <summary>
        /// The SrcMLGenerator to use to convert source files to SrcML.
        /// This is only used if Archive is null.
        /// </summary>
        public SrcMLGenerator Generator { get; set; }
    
        /// <summary>
        /// The path to the cache file on disk.
        /// </summary>
        public string CachePath { get; private set; }

        /// <summary>
        /// Indicates whether a cache file has been successfully loaded.
        /// </summary>
        public bool CacheLoaded { get; private set; }

        /// <summary>
        /// Sets the CachePath and initializes the SWUM data from the cache file in the given directory, if it exists.
        /// Any previously constructed SWUMs will be deleted.
        /// </summary>
        /// <param name="cacheDirectory">The path for the directory containing the SWUM cache file.</param>
        public void Initialize(string cacheDirectory) {
            Initialize(cacheDirectory, true);
        }

        /// <summary>
        /// Sets the CachePath and initializes the SWUM data from the cache file in the given directory, if desired.
        /// Any previously constructed SWUMs will be deleted.
        /// </summary>
        /// <param name="cacheDirectory">The path for the directory containing the SWUM cache file.</param>
        /// <param name="useCache">True to use the existing cache file, if any. False to not load any cache file.</param>
        public void Initialize(string cacheDirectory, bool useCache) {
            SwumDataStore.Clear();
            CachePath = Path.Combine(cacheDirectory, DefaultCacheFile);

            if(useCache) {
                if(!File.Exists(CachePath)) {
					LogEvents.SwumCacheFileNotExist(this, CachePath);
                    return;
                }
                ReadSwumCache(CachePath);
                CacheLoaded = true;
            }
        }

        /// <summary>
        /// Generates SWUMs for the method definitions within the given source file.
        /// </summary>
        /// <param name="sourcePath">The path to the source file.</param>
        public void AddSourceFile(string sourcePath) {
            //Don't try to process files that SrcML can't handle
            if(Archive != null && !Archive.IsValidFileExtension(sourcePath)) { return; }
            var fileExt = Path.GetExtension(sourcePath);
            if(fileExt == null || (Generator != null && !Generator.ExtensionMapping.ContainsKey(fileExt))) {
                return;
            }

            sourcePath = Path.GetFullPath(sourcePath);
            XElement fileElement;
            if(Archive != null) {
                fileElement = Archive.GetXElementForSourceFile(sourcePath);
                if(fileElement == null) {
                    LogEvents.SwumFileNotFoundInArchive(this, sourcePath);
                }
            } else if(Generator != null) {
                string outFile = Path.GetTempFileName();
                try {
                    Generator.GenerateSrcMLFromFile(sourcePath, outFile);
                    fileElement = SrcMLElement.Load(outFile);
                    if(fileElement == null) {
                        LogEvents.SwumErrorGeneratingSrcML(this, sourcePath);
                    }
                } finally {
                    File.Delete(outFile);
                }
            } else {
                throw new InvalidOperationException("SwumManager - Archive and Generator are both null");
            }

            try {
                if(fileElement != null) {
                    AddSwumForMethodDefinitions(fileElement, sourcePath);
                }
            } catch(Exception e) {
                LogEvents.SwumErrorCreatingSwum(this, sourcePath, e);
            }
        }

        /// <summary>
        /// Generates SWUMs for the method definitions within the given source file.
        /// </summary>
        /// <param name="sourcePath">The path to the source file.</param>
        /// <param name="sourceXml">The SrcML for the given source file.</param>
        public void AddSourceFile(string sourcePath, XElement sourceXml) {
            try {
                if(sourceXml != null) {
                    if (!sourcePath.EndsWith("xaml"))
                    {
                        AddSwumForMethodDefinitions(sourceXml, sourcePath);
                        AddSwumForFieldDefinitions(sourceXml, sourcePath);
                    }
                }
            } catch(Exception e) {
                LogEvents.SwumErrorCreatingSwum(this, sourcePath, e);
            }
        }

        /// <summary>
        /// Generates SWUMs for the method definitions within the given SrcML file
        /// </summary>
        /// <param name="fileUnit">A SrcML file unit.</param>
        public void AddSrcMLFile(XElement fileUnit) {
            var fileName = SrcMLElement.GetFileNameForUnit(fileUnit);
            AddSwumForMethodDefinitions(fileUnit, fileName);
        }

        /// <summary>
        /// Regenerates SWUMs for the methods in the given source file. Any previously-generated SWUMs for the file will first be removed.
        /// </summary>
        /// <param name="sourcePath">The path of the file to update.</param>
        public void UpdateSourceFile(string sourcePath) {
            SwumDataStore.RemoveSourceFile(sourcePath);
            AddSourceFile(sourcePath);
        }

        /// <summary>
        /// Regenerates SWUMs for the methods in the given source file. Any previously-generated SWUMs for the file will first be removed.
        /// </summary>
        /// <param name="sourcePath">The path of the file to update.</param>
        /// <param name="sourceXml">The SrcML for the new version of the file.</param>
        public void UpdateSourceFile(string sourcePath, XElement sourceXml) {
            SwumDataStore.RemoveSourceFile(sourcePath);
            AddSourceFile(sourcePath, sourceXml);
        }

        /// <summary>
        /// Prints the SWUM cache to the file specified in CachePath.
        /// </summary>
        public void PrintSwumCache() {
            PrintSwumCache(string.IsNullOrWhiteSpace(CachePath) ? DefaultCacheFile : CachePath);
        }

        /// <summary>
        /// Prints the SWUM cache to the specified file.
        /// </summary>
        /// <param name="path">The path to print the SWUM cache to.</param>
        public void PrintSwumCache(string path) {
            if(string.IsNullOrWhiteSpace(path)) {
                throw new ArgumentException("Path is empty or null.", "path");
            }
            using(StreamWriter sw = new StreamWriter(path)) {
                PrintSwumCache(sw);
            }
        }

        /// <summary>
        /// Prints the SWUM cache to the specified output stream.
        /// </summary>
        /// <param name="output">A TextWriter to print the SWUM cache to.</param>
        public void PrintSwumCache(TextWriter output)
        {
            var allSwumData = SwumDataStore.GetAllSwumDataBySignature();
            foreach(var kvp in allSwumData) {
                output.WriteLine("{0}|{1}", kvp.Key, kvp.Value.ToString());
            }
        }

        /// <summary>
        /// Initializes the cache of SWUM data from a file. Any existing SWUM data will be cleared before reading the file.
        /// </summary>
        /// <param name="path">The path to the SWUM cache file.</param>
        public void ReadSwumCache(string path) {
            using (var cacheFile = new StreamReader(path))
            {
                SwumDataStore.Clear();

                //read each SWUM entry from the cache file
                string entry;
                while ((entry = cacheFile.ReadLine()) != null)
                {
                    //the expected format is <signature>|<SwumDataRecord.ToString()>
                    string[] fields = entry.Split(new[] {'|'}, 2);
                    if (fields.Length != 2)
                    {
                        Debug.WriteLine(string.Format("Too few fields in SWUM cache entry: {0}", entry));
                        continue;
                    }
                    try
                    {
                        int sig = int.Parse( fields[0].Trim());
                        string data = fields[1].Trim();
                        var swumRecord = SwumDataRecord.Parse(data);
                        SwumDataStore.AddRecord(sig, swumRecord);
                    }
                    catch (FormatException fe)
                    {
                        Debug.WriteLine(string.Format("Improperly formatted SwumDataRecord in Swum cache entry: {0}",
                            entry));
                        Debug.WriteLine(fe.Message);
                    }
                }
            }
        }

        public Dictionary<int, SwumDataRecord> GetAllSwumBySignature()
        {
            return SwumDataStore.GetAllSwumDataBySignature();
        }

        public List<SwumDataRecord> GetSwumForTerm(string term)
        {
            return SwumDataStore.GetSwumDataForTerm(term);
        }

        public bool ContainsFile(string sourcePath)
        {
            return SwumDataStore.ContainsFile(sourcePath);
        }

        public void RemoveSourceFile(string sourcePath)
        {
            SwumDataStore.RemoveSourceFile(sourcePath);
        }

        public void Clear()
        {
            SwumDataStore.Clear();
        }

        #region Protected methods

        protected void AddSwumForFieldDefinitions(XElement file, string fileName)
        {
            //compute SWUM on each field
            foreach (var fieldDecl in (from declStmt in file.Descendants(SRC.DeclarationStatement)
                                       where !declStmt.Ancestors().Any(n => _functionTypes.Contains(n.Name))
                                       select declStmt.Element(SRC.Declaration)))
            {
                foreach (var nameElement in fieldDecl.Elements(SRC.Name))
                {
                    string fieldName = nameElement.Elements(SRC.Name).Any() ? nameElement.Elements(SRC.Name).Last().Value : nameElement.Value;

                    FieldDeclarationNode fdn = new FieldDeclarationNode(fieldName, ContextBuilder.BuildFieldContext(fieldDecl));
                    _builder.ApplyRules(fdn);
                    //var signature = string.Format("{0}:{1}:{2}", fileName, fieldDecl.Value, declPos);
                    //TODOMemory - change to hash or something
                    var signature = nameElement.GetXPath(false).GetHashCode();
                    var swumRecord = ProcessSwumNode(fdn);
                    swumRecord.FileNameHashes.Add(fileName.GetHashCode());
                    SwumDataStore.AddRecord(signature, swumRecord);
                }
            }
        }


        /// <summary>
        /// Constructs SWUMs for each of the methods defined in <paramref name="unitElement"/> and adds them to the cache.
        /// </summary>
        /// <param name="unitElement">The root element for the file unit to be processed.</param>
        /// <param name="filePath">The path for the file represented by <paramref name="unitElement"/>.</param>
        /// <exception cref="System.ArgumentNullException"><paramref name="unitElement"/> is null.</exception>
        protected void AddSwumForMethodDefinitions(XElement unitElement, string filePath) {
            if(unitElement == null) { throw new ArgumentNullException("unitElement"); }
            if(unitElement.Name != SRC.Unit) {
                throw new ArgumentException("Must be a SRC.Unit element", "unitElement");
            }

            //iterate over each method definition in the SrcML file
            var fileAttribute = unitElement.Attribute("filename");
            if(fileAttribute != null) {
                filePath = fileAttribute.Value;
            }
            var functions = from func in unitElement.Descendants()
                            where _functionTypes.Contains(func.Name) && !func.Ancestors(SRC.Declaration).Any()
                            select func;
            foreach (XElement func in functions)
            {
                //construct SWUM on the function (if necessary)
                int sig = 
                    SrcMLElement.GetMethodSignature(func).GetHashCode();
                var swumRecord = SwumDataStore.GetSwumForSignature(sig);
                if (swumRecord != null)
                {
                    //update the SwumDataRecord with the filename of the duplicate method
                    swumRecord.FileNameHashes.Add(filePath.GetHashCode());
                    SwumDataStore.AddRecord(sig, swumRecord);
                }
                else
                {
                    MethodDeclarationNode mdn = ConstructSwumFromMethodElement(func);
                    swumRecord = ProcessSwumNode(mdn);
                    swumRecord.FileNameHashes.Add(filePath.GetHashCode());
                    SwumDataStore.AddRecord(sig, swumRecord);
                }

            }
        }

        /// <summary>
        /// Constructs SWUM on the given srcML method element. 
        /// </summary>
        /// <param name="methodElement">The srcML element to use. This can be either a Function, Constructor or Destructor.</param>
        /// <returns>A MethodDeclarationNode with SWUM rules applied to it.</returns>
        protected MethodDeclarationNode ConstructSwumFromMethodElement(XElement methodElement) {
            return ConstructSwumFromMethodElement(methodElement, null);
        }

        /// <summary>
        /// Constructs SWUM on the given srcML method element. 
        /// </summary>
        /// <param name="methodElement">The srcML element to use. This can be either a Function, Constructor or Destructor.</param>
        /// <param name="className">The class on which this method is declared.</param>
        /// <returns>A MethodDeclarationNode with SWUM rules applied to it.</returns>
        protected MethodDeclarationNode ConstructSwumFromMethodElement(XElement methodElement, string className) {
            if(!_functionTypes.Contains(methodElement.Name)) {
                throw new ArgumentException("Element not a valid method type.", "methodElement");
            }

            string funcName = SrcMLElement.GetNameForMethod(methodElement).Value;
            MethodContext mc = ContextBuilder.BuildMethodContext(methodElement);
            //set the declaring class name, if it's been passed in
            //this is necessary because the xml from the database for inline methods won't have the surrounding class xml
            if(string.IsNullOrEmpty(mc.DeclaringClass) && !string.IsNullOrEmpty(className)) {
                mc.DeclaringClass = className;
            }

            MethodDeclarationNode mdn = new MethodDeclarationNode(funcName, mc);
            _builder.ApplyRules(mdn);
            return mdn;
        }

        /// <summary>
        /// Constructs a method signature based on a method call.
        /// </summary>
        /// <param name="name">The name of the method being called.</param>
        /// <param name="mc">A MethodContext object populated with data from the method call.</param>
        /// <returns>A method signature.</returns>
        protected string GetMethodSignatureFromCall(string name, MethodContext mc) {
            if(name == null) { throw new ArgumentNullException("name"); }
            if(name == string.Empty) { throw new ArgumentException("The method name must be non-empty.", "name"); }
            if(mc == null) { throw new ArgumentNullException("mc"); }
            
            StringBuilder sig = new StringBuilder();
            if(mc.IsStatic) {
                sig.Append("static");
            }
            if(!string.IsNullOrEmpty(mc.IdType)) {
                sig.AppendFormat(" {0}", mc.IdType);
            }
            //add method name
            if(!string.IsNullOrEmpty(mc.DeclaringClass)) {
                sig.AppendFormat(" {0}::{1}(", mc.DeclaringClass, name);
            } else {
                sig.AppendFormat(" {0}(", name);
            }
            //add method parameters
            if(mc.FormalParameters.Count > 0) {
                for(int i = 0; i < mc.FormalParameters.Count - 1; i++) {
                    sig.AppendFormat("{0}, ", mc.FormalParameters[i].ParameterType);
                }
                sig.Append(mc.FormalParameters.Last().ParameterType);
            }
            sig.Append(")");
            return sig.ToString().TrimStart(' ');
        }

        /// <returns>A SwumDataRecord containing <paramref name="swumNode"/> and various data extracted from it.</returns>
        protected SwumDataRecord ProcessSwumNode(FieldDeclarationNode swumNode)
        {
            var record = new SwumDataRecord();
            record.SwumNodeName = swumNode.Name;
            return record;
        }


        /// <summary>
        /// Performs additional processing on a MethodDeclarationNode to put the data in the right format for the Comment Generator.
        /// </summary>
        /// <param name="swumNode">The MethodDeclarationNode from SWUM to process.</param>
        /// <returns>A SwumDataRecord containing <paramref name="swumNode"/> and various data extracted from it.</returns>
        protected SwumDataRecord ProcessSwumNode(MethodDeclarationNode swumNode) {
            var record = new SwumDataRecord();
            record.SwumNodeName = swumNode.Name;
            //set Action
            if(swumNode.Action != null) {
                record.Action = swumNode.Action.ToPlainString();
                record.ParsedAction = swumNode.Action.GetParse();
            }
            //TODO: action is not lowercased. Should it be?

            //set Theme
            if(swumNode.Theme != null) {
                if(swumNode.Theme is EquivalenceNode && ((EquivalenceNode)swumNode.Theme).EquivalentNodes.Any()) {
                    var firstNode = ((EquivalenceNode)swumNode.Theme).EquivalentNodes[0];
                    record.Theme = firstNode.ToPlainString().ToLower();
                    record.ParsedTheme = firstNode.GetParse();
                } else {
                    record.Theme = swumNode.Theme.ToPlainString().ToLower();
                    record.ParsedTheme = swumNode.Theme.GetParse();
                }
            }

            //set Indirect Object
            if(string.Compare(record.Action, "set", StringComparison.InvariantCultureIgnoreCase) == 0) {
                //special handling for setter methods?
                //TODO: should this set the IO to the declaring class? will that work correctly for sando?
                
            } else {
                if(swumNode.SecondaryArguments != null && swumNode.SecondaryArguments.Any()) {
                    var IONode = swumNode.SecondaryArguments.First();
                    if(IONode.Argument is EquivalenceNode && ((EquivalenceNode)IONode.Argument).EquivalentNodes.Any()) {
                        var firstNode = ((EquivalenceNode)IONode.Argument).EquivalentNodes[0];
                        record.IndirectObject = firstNode.ToPlainString().ToLower();
                        record.ParsedIndirectObject = firstNode.GetParse();
                    } else {
                        record.IndirectObject = IONode.Argument.ToPlainString().ToLower();
                        record.ParsedIndirectObject = IONode.Argument.GetParse();
                    }
                } 
            }

            return record;
        }

        #endregion Protected methods
    }
}
