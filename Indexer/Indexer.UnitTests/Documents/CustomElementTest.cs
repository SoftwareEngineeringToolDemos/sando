﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Documents;
using NUnit.Framework;
using Sando.Indexer.Documents;

namespace Sando.Indexer.UnitTests.Documents
{
    [TestFixture]
    public class CustomElementTest
    {

        [Test]
        public void LuceneDocToCustomProgramElement()
        {
            //test ReadProgramElementFromDocument            
            var customSandoDocument = new CustomDocument(MyCustomProgramElementForTesting.GetLuceneDocument(), typeof(MyCustomProgramElementForTesting));
            var customProgramElement = customSandoDocument.ReadProgramElementFromDocument();
            var myCustomProgramElementForTesting = customProgramElement as MyCustomProgramElementForTesting;
            Assert.IsTrue(myCustomProgramElementForTesting != null);
            Assert.IsTrue(myCustomProgramElementForTesting.A.Equals("A's value"));
            Assert.IsTrue(myCustomProgramElementForTesting.B.Equals("B's value"));
            Assert.IsTrue(myCustomProgramElementForTesting.C.Equals("C's value"));
        }



        [Test]
        public void UnMarshallCustomElement()
        {

        }

    }
}
