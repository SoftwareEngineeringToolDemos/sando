﻿/******************************************************************************
 * Copyright (c) 2013 ABB Group
 * All rights reserved. This program and the accompanying materials
 * are made available under the terms of the Eclipse Public License v1.0
 * which accompanies this distribution, and is available at
 * http://www.eclipse.org/legal/epl-v10.html
 *
 * Contributors:
 *    Xiao Qu (ABB Group) - Initial implementation
 *****************************************************************************/
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using log4net;
using Sando.ExtensionContracts.ResultsReordererContracts;
using Sando.SearchEngine;
using System.Collections.Generic;
using Sando.UI.View;
using System.Threading;
using Sando.ExtensionContracts.ServiceContracts;
using Sando.Indexer.Searching.Criteria;
using Sando.ExtensionContracts.SearchContracts;


namespace Sando.UI.Service {
    /// <summary>
    /// Implement the global service class.
    /// This is the class that implements the global service. All it needs to do is to implement 
    /// the interfaces exposed by this service (in this case ISrcMLGlobalService).
    /// This class also needs to implement the SSandoGlobalService interface in order to notify the 
    /// package that it is actually implementing this service.
    /// </summary>
    public class SandoGlobalService : ISandoGlobalService, SSandoGlobalService
    {

        /// <summary>
        /// Store in this variable the service provider that will be used to query for other services.
        /// </summary>
        private IServiceProvider serviceProvider;

        private List<CodeSearchResult> _results;
        protected string _myMessage;
        private IVsStatusbar statusBar;


        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="sp"></param>
        /// <param name="extensionDirectory"></param>
        public SandoGlobalService(IServiceProvider sp) {
            //Trace.WriteLine("Constructing a new instance of SandoGlobalService");
            serviceProvider = sp;
            statusBar = (IVsStatusbar)Package.GetGlobalService(typeof(SVsStatusbar));
        }

        // ISandoGlobalService Members
        #region ISandoGlobalService Members

        public event EventHandler SolutionOpened;
        public event EventHandler IndexUpdating;
        public event EventHandler IndexUpdated;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "Microsoft.Samples.VisualStudio.Services.HelperFunctions.WriteOnOutputWindow(System.IServiceProvider,System.String)")]
        public void GlobalServiceFunction()
        {
            string outputText = " ======================================\n" +
                                "\tGlobalServiceFunction called.\n" +
                                " ======================================\n";
            HelperFunctions.WriteOnOutputWindow(serviceProvider, outputText);
        }

        /// <summary>
        /// Implementation of the function that will call a method of the local service.
        /// Notice that this class will access the local service using as service provider the one
        /// implemented by ServicesPackage.
        /// </summary>
        public int CallLocalService()
        {
            // Query the service provider for the local service.
            // This object is supposed to be build by ServicesPackage and it pass its service provider
            // to the constructor, so the local service should be found.
            ISandoLocalService localService = serviceProvider.GetService(typeof(SSandoLocalService)) as ISandoLocalService;
            if (null == localService)
            {
                // The local service was not found; write a message on the debug output and exit.
                Trace.WriteLine("Can not get the local service from the global one.");
                return -1;
            }

            // Now call the method of the local service. This will write a message on the output window.
            return localService.LocalServiceFunction();
        }

        private void Update(string searchString, IQueryable<CodeSearchResult> results)
        {
            var newResults = new List<CodeSearchResult>();
            foreach (var result in results)
                newResults.Add(result);
            _results = newResults;
        }

        private void UpdateMessage(string message)
        {
            _myMessage = message;
        }

        public List<CodeSearchResult> GetSearchResults(string searchkeywords)
        {
            var manager = SearchManagerFactory.GetNewBackgroundSearchManager();
            manager.SearchResultUpdated += this.Update;
            manager.SearchCompletedMessageUpdated += this.UpdateMessage;

            _results = null;
            var criteria = CriteriaBuilderFactory.GetBuilder().GetCriteria(searchkeywords);
            manager.Search(searchkeywords, criteria);
            int i = 0;
            while (_results == null)
            {
                System.Threading.Thread.Sleep(50);
                i++;
                if (i > 100)
                    break;
            }
            return _results;
        }

        public void AddUISearchResultsListener(ISearchResultListener s)
        {
            //TODO - Delete
        }

        internal void OnIndexUpdating(EventArgs e) {
            var handler = IndexUpdating;
            if (handler != null) {
                // Call the event listeners asynchronously
                //  - All listeners are called on a single thread
                //  - EndInvoke must be called to avoid a thread leak
                handler.BeginInvoke(this, e, (asyncResult) => { handler.EndInvoke(asyncResult); }, null);
            }
        }

        internal void OnIndexUpdated(EventArgs e) {
            var handler = IndexUpdated;
            if (handler != null) {
                // Call the event listeners asynchronously
                //  - All listeners are called on a single thread
                //  - EndInvoke must be called to avoid a thread leak
                handler.BeginInvoke(this, e, (asyncResult) => { handler.EndInvoke(asyncResult); }, null);
            }
        }

        internal void OnSolutionOpened(EventArgs e) {
            var handler = SolutionOpened;
            if (handler != null) {
                // Call the event listeners asynchronously
                //  - All listeners are called on a single thread
                //  - EndInvoke must be called to avoid a thread leak
                handler.BeginInvoke(this, e, (asyncResult) => { handler.EndInvoke(asyncResult); }, null);
            }
        }

        #endregion        

    }
}
