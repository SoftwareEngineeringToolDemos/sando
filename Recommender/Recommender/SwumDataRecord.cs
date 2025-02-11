﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ABB.Swum.Nodes;

namespace Sando.Recommender {
    /// <summary>
    /// Contains various data determined by constructing SWUM on a method.
    /// </summary>
    public class SwumDataRecord {
        public string SwumNodeName { get; set; }
        public string Action { get; set; }
        public PhraseNode ParsedAction { get; set; }
        public string Theme { get; set; }
        public PhraseNode ParsedTheme { get; set; }
        public string IndirectObject { get; set; }
        public PhraseNode ParsedIndirectObject { get; set; }
        public ISet<int> FileNameHashes { get; private set; }
        public int Signature { get; set; }

        /// <summary>
        /// Creates a new empty SwumDataRecord.
        /// </summary>
        public SwumDataRecord() {
            SwumNodeName = string.Empty;
            Action = string.Empty;
            ParsedAction = null;
            Theme = string.Empty;
            ParsedTheme = null;
            IndirectObject = string.Empty;
            ParsedIndirectObject = null;
            FileNameHashes = new HashSet<int>();
            Signature = -1;
        }

        /// <summary>
        /// Returns a string representation of the object.
        /// </summary>
        public override string ToString() {
            return string.Format("{0}|{1}|{2}|{3}|{4}", ParsedAction, ParsedTheme, ParsedIndirectObject, string.Join(";", FileNameHashes), SwumNodeName);
        }

        /// <summary>
        /// Creates a SwumDataRecord from a string representation
        /// </summary>
        /// <param name="source">The string to parse the SwumDataRecord from.</param>
        /// <returns>A new SwumDataRecord.</returns>
        public static SwumDataRecord Parse(string source) {
            if(source == null) {
                throw new ArgumentNullException("source");
            }

            string[] fields = source.Split('|');
            if(fields.Length != 5) {
                throw new FormatException(string.Format("Wrong number of |-separated fields. Expected 4, saw {0}", fields.Length));
            }
            var sdr = new SwumDataRecord();            
            if(!string.IsNullOrWhiteSpace(fields[0])) {
                sdr.ParsedAction = PhraseNode.Parse(fields[0].Trim());
                sdr.Action = sdr.ParsedAction.ToPlainString();
            }
            if(!string.IsNullOrWhiteSpace(fields[1])) {
                sdr.ParsedTheme = PhraseNode.Parse(fields[1].Trim());
                sdr.Theme = sdr.ParsedTheme.ToPlainString();
            }
            if(!string.IsNullOrWhiteSpace(fields[2])) {
                sdr.ParsedIndirectObject = PhraseNode.Parse(fields[2].Trim());
                sdr.IndirectObject = sdr.ParsedIndirectObject.ToPlainString();
            }
            if(!string.IsNullOrWhiteSpace(fields[3])) {
                foreach(var file in fields[3].Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)) {
                        sdr.FileNameHashes.Add(int.Parse(file));
                }
            }
            if (!string.IsNullOrWhiteSpace(fields[4]))
            {
                sdr.SwumNodeName = fields[4].Trim();
            }
            return sdr;
        }
    }
}
