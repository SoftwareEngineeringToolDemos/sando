﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Sando.Core.Tools;
using Sando.DependencyInjection;

namespace Sando.Core.QueryRefomers
{
    /// <summary>
    /// This is the listener when improved terms are ready.
    /// </summary>
    /// <param name="improvedTerms"></param>
    public delegate void ImprovedQueryReady(IEnumerable<IReformedQuery> improvedTerms);

    public class QueryReformerManager : IInitializable
    {
       
        private readonly DictionaryBasedSplitter dictionary;
      
        public QueryReformerManager(DictionaryBasedSplitter dictionary)
        {
            this.dictionary = dictionary;
        }

        public void Initialize(string nonUsedDirectory)
        {
            SeSpecificThesaurus.GetInstance().Initialize(nonUsedDirectory);
            GeneralEnglishThesaurus.GetInstance().Initialize(nonUsedDirectory);
        }

        public void ReformTermsAsynchronously(IEnumerable<String> terms, ImprovedQueryReady callback)
        {
            var worker = new BackgroundWorker { WorkerReportsProgress = false, 
                WorkerSupportsCancellation = false };
            worker.DoWork += (sender, args) => callback.Invoke(ReformTermsSynchronously(terms));
            worker.RunWorkerAsync();
        }

        public IEnumerable<IReformedQuery> ReformTermsSynchronously(IEnumerable<string> terms)
        {
            var termList = terms.ToList();
            if (termList.Any())
            {
                var builder = new ReformedQueryBuilder(dictionary);
                foreach (string term in termList)
                {
                    var neigbors = GetNeighbors(termList, term);
                    builder.AddReformedTerms(FindBetterTerms(term, neigbors));
                }
                return GetReformedQuerySorter().SortReformedQueries
                    (builder.GetAllPossibleReformedQueriesSoFar());
            }
            return Enumerable.Empty<IReformedQuery>();
        }



        private IReformedQuerySorter GetReformedQuerySorter()
        {
            return ReformedQuerySorters.GetReformedQuerySorter(QuerySorterType.SCORE);
        }

        private IEnumerable<ReformedWord> FindBetterTerms(string word, IEnumerable<string> neigbors)
        {
            if (!dictionary.DoesWordExist(word, DictionaryOption.IncludingStemming) 
                && !word.IsWordQuoted() && !word.IsWordFlag())
            {
                var list = new List<ReformedWord>();
                list.AddRange(FindSplitWords(word));
                list.AddRange(FindShapeSimilarWordsInLocalDictionary(word));
                list.AddRange(FindSynonymsInDictionaries(word));
                list.AddRange(FindCoOccurredTerms(word, neigbors));
                list = list.RemoveRedundance().ToList();
                return list.Any() ? list : ToolHelpers.CreateNonChangedTerm(word);
            }
            return ToolHelpers.CreateNonChangedTerm(word);
        }
        private const int TERM_MINIMUM_LENGTH = 2;

        private IEnumerable<ReformedWord> FindSplitWords(string word)
        {
            var reformer = new SplitterReformer(dictionary);
            return reformer.GetReformedTarget(word).ToList();
        }

        private IEnumerable<ReformedWord> FindShapeSimilarWordsInLocalDictionary(String word)
        {
            var reformer = new TypoCorrectionReformer(dictionary);
            return reformer.GetReformedTarget(word).ToList();
        }

        private IEnumerable<ReformedWord> FindSynonymsInDictionaries(String word)
        {
            var reformer = new SeThesaurusWordReformer(dictionary);
            var list = reformer.GetReformedTarget(word).ToList();
            list.AddRange(new GeneralThesaurusWordReformer(dictionary).GetReformedTarget(word));
            return list;
        }

        private IEnumerable<String> GetNeighbors(IEnumerable<String> words, String target)
        {
            return words.Where(w => !w.Equals(target));
        }
        
        private IEnumerable<ReformedWord> FindCoOccurredTerms(String word, IEnumerable<String> neighbors)
        {
            var reformer = new CoOccurrenceBasedReformer(dictionary);
            reformer.SetContextWords(neighbors);
            return reformer.GetReformedTarget(word);
        }
    }
}
