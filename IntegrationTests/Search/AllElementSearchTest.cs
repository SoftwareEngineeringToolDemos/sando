﻿using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Snowball;
using NUnit.Framework;
using Sando.Core;
using Sando.DependencyInjection;
using Sando.ExtensionContracts.ProgramElementContracts;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.Indexer;
using Sando.Indexer.Searching;
using Sando.Indexer.Searching.Criteria;
using Sando.SearchEngine;
using Sando.UI.Monitoring;
using UnitTestHelpers;
using Sando.Recommender;
using Sando.Core.Tools;

namespace Sando.IntegrationTests.Search
{
	[TestFixture]
    public class AllElementsSearchTest : AutomaticallyIndexingTestClass
	{

        [Test]
        public void SearchRespectsAccessLevelCriteriaInternal()
        {
            var codeSearcher = new CodeSearcher();
            string keywords = "SingleUsageTypeCriteriaToString";
            SimpleSearchCriteria searchCriteria = new SimpleSearchCriteria()
            {
                AccessLevels = new SortedSet<AccessLevel>() { AccessLevel.Internal },
                SearchByAccessLevel = true,
                SearchTerms = new SortedSet<string>(keywords.Split(' '))
            };
            List<CodeSearchResult> codeSearchResults = codeSearcher.Search(searchCriteria);
            var methodSearchResult = codeSearchResults.Find(el =>
                                                                el.ProgramElement.ProgramElementType == ProgramElementType.Method &&
                                                                (el.ProgramElement.Name == "SingleUsageTypeCriteriaToString"));
            if (methodSearchResult == null)
            {
                Assert.Fail("Failed to find relevant search result for search: " + keywords);
            }
           
            var method = methodSearchResult.ProgramElement as MethodElement;
            Assert.AreEqual(method.AccessLevel, AccessLevel.Internal, "Method access level differs!");
            Assert.AreEqual(method.Arguments, "StringBuilder stringBuilder UsageType usageType", "Method arguments differs!");
            Assert.NotNull(method.Body, "Method body is null!");
            //Assert.True(method.ClassId != null && method.ClassId != Guid.Empty, "Class id is invalid!");
            //Assert.AreEqual(method.ClassName, "SimpleSearchCriteria", "Method class name differs!");
            Assert.AreEqual(method.DefinitionLineNumber, 141, "Method definition line number differs!");
            Assert.True(method.FullFilePath.EndsWith("\\TestFiles\\MethodElementTestFiles\\Searcher.cs".ToLowerInvariant()), "Method full file path is invalid!");
            Assert.AreEqual(method.Name, "SingleUsageTypeCriteriaToString", "Method name differs!");
            Assert.AreEqual(method.ProgramElementType, ProgramElementType.Method, "Program element type differs!");
            Assert.AreEqual(method.ReturnType, "void", "Method return type differs!");
            Assert.False(String.IsNullOrWhiteSpace(method.RawSource), "Method snippet is invalid!");
        }

		[Test]
		public void SearchRespectsAccessLevelCriteria()
		{
            var codeSearcher = new CodeSearcher();
			string keywords = "usage type";
            SimpleSearchCriteria searchCriteria = new SimpleSearchCriteria()
			{
				AccessLevels = new SortedSet<AccessLevel>() { AccessLevel.Private },
				SearchByAccessLevel = true,
				SearchTerms = new SortedSet<string>(keywords.Split(' '))
			};
			List<CodeSearchResult> codeSearchResults = codeSearcher.Search(searchCriteria);
			Assert.AreEqual(2, codeSearchResults.Count, "Invalid results number");
			var methodSearchResult = codeSearchResults.Find(el =>
																el.ProgramElement.ProgramElementType == ProgramElementType.Method &&
																(el.ProgramElement.Name == "UsageTypeCriteriaToString"));
			if(methodSearchResult == null)
			{
				Assert.Fail("Failed to find relevant search result for search: " + keywords);
			}
			var method = methodSearchResult.ProgramElement as MethodElement;
			Assert.AreEqual(method.AccessLevel, AccessLevel.Private, "Method access level differs!");
			Assert.AreEqual(method.Arguments, "StringBuilder stringBuilder bool searchByUsageType", "Method arguments differs!");
			Assert.NotNull(method.Body, "Method body is null!");
			//Assert.True(method.ClassId != null && method.ClassId != Guid.Empty, "Class id is invalid!");
			//Assert.AreEqual(method.ClassName, "SimpleSearchCriteria", "Method class name differs!");
			Assert.AreEqual(method.DefinitionLineNumber, 96, "Method definition line number differs!");
			Assert.True(method.FullFilePath.EndsWith("\\TestFiles\\MethodElementTestFiles\\Searcher.cs".ToLowerInvariant()), "Method full file path is invalid!");
			Assert.AreEqual(method.Name, "UsageTypeCriteriaToString", "Method name differs!");
			Assert.AreEqual(method.ProgramElementType, ProgramElementType.Method, "Program element type differs!");
			Assert.AreEqual(method.ReturnType, "void", "Method return type differs!");
			Assert.False(String.IsNullOrWhiteSpace(method.RawSource), "Method snippet is invalid!");

	
		}

		[Test]
		public void SearchWorksNormallyForTermsWhichEndsWithSpace()
		{
			try
			{
				var codeSearcher = new CodeSearcher();
				string keywords = "  usage ";
				List<CodeSearchResult> codeSearchResults = codeSearcher.Search(keywords);
			}
			catch(Exception ex)
			{
				Assert.Fail(ex.Message);
			}
		}

		[Test]
		public void SearchReturnsElementsUsingCrossFieldMatching()
		{
			var codeSearcher = new CodeSearcher();
			string keywords = "fetch output argument";
            SimpleSearchCriteria searchCriteria = new SimpleSearchCriteria()
			{
				SearchTerms = new SortedSet<string>(keywords.Split(' '))
			};
			List<CodeSearchResult> codeSearchResults = codeSearcher.Search(searchCriteria);

			var methodSearchResult = codeSearchResults.Find(el =>
																el.ProgramElement.ProgramElementType == ProgramElementType.Method &&
																(el.ProgramElement.Name == "FetchOutputStream"));
			if(methodSearchResult == null)
			{
				Assert.Fail("Failed to find relevant search result for search: " + keywords);
			}
			var method = methodSearchResult.ProgramElement as MethodElement;
			Assert.AreEqual(method.AccessLevel, AccessLevel.Public, "Method access level differs!");
			Assert.AreEqual(method.Arguments, "A B string fileName Image image", "Method arguments differs!");
			Assert.NotNull(method.Body, "Method body is null!");
			Assert.True(method.ClassId != null && method.ClassId != Guid.Empty, "Class id is invalid!");
            Assert.AreEqual(method.ClassName, "ImageCapture", "Method class name differs!");
			Assert.AreEqual(method.DefinitionLineNumber, 83, "Method definition line number differs!");
            Assert.True(method.FullFilePath.EndsWith("\\TestFiles\\MethodElementTestFiles\\ImageCapture.cs".ToLowerInvariant()), "Method full file path is invalid!");
			Assert.AreEqual(method.Name, "FetchOutputStream", "Method name differs!");
			Assert.AreEqual(method.ProgramElementType, ProgramElementType.Method, "Program element type differs!");
			Assert.AreEqual(method.ReturnType, "void", "Method return type differs!");
			Assert.False(String.IsNullOrWhiteSpace(method.RawSource), "Method snippet is invalid!");
		}

        public override string GetIndexDirName()
        {
            return "AllElementSearchTest";
        }

        public override string GetFilesDirectory()
        {
            return "..\\..\\IntegrationTests\\TestFiles\\MethodElementTestFiles";
        }

        public override TimeSpan? GetTimeToCommit()
        {
            return TimeSpan.FromSeconds(3);
        }
		



		private string indexPath;
		//private static SolutionMonitor monitor;
		private static SolutionKey key;
	}
}
