﻿using System;
using System.IO;
using System.Linq;
using System.Windows.Documents;
using Configuration.OptionsPages;
using Sando.Core;
using Sando.DependencyInjection;
using Sando.Core.Tools;
using Sando.Indexer.Searching.Criteria;
using Sando.UI.Monitoring;
using Sando.UI.View;
using System.Windows.Threading;
using Microsoft.VisualStudio.Shell;
using System.Collections.Generic;

namespace Sando.UI.Options
{
    public class SandoOptionsProvider : ISandoOptionsProvider
    {
        public SandoOptions GetSandoOptions()
        {
            var uiPackage = ServiceLocator.Resolve<UIPackage>();
            var solutionKey = ServiceLocator.Resolve<SolutionKey>();
            bool firstRun = false;

            var sandoDialogPage = uiPackage.GetSandoDialogPage();

            var extensionPointsPluginDirectoryPath = PathManager.Instance.GetExtensionRoot();
            sandoDialogPage.ExtensionPointsPluginDirectoryPath = extensionPointsPluginDirectoryPath;

            var numberOfSearchResultsReturned = SearchCriteria.DefaultNumberOfSearchResultsReturned;
            if (!String.IsNullOrWhiteSpace(sandoDialogPage.NumberOfSearchResultsReturned))
            {
                if (!int.TryParse(sandoDialogPage.NumberOfSearchResultsReturned, out numberOfSearchResultsReturned) || numberOfSearchResultsReturned < 0)
                    numberOfSearchResultsReturned = SearchCriteria.DefaultNumberOfSearchResultsReturned;
            }
             
            var allowDataCollectionLogging = true;
            if (!bool.TryParse(sandoDialogPage.AllowDataCollectionLogging, out allowDataCollectionLogging))
            {
                bool usersAnswer = false;
                usersAnswer = ThreadHelper.Generic.Invoke(() => ShowWelcomePopup());                                
                allowDataCollectionLogging = usersAnswer;
                firstRun = true;
            }

            var fileExtensionsList = IndexUpdateManager.DefaultFileExtensionsList;
            if (sandoDialogPage.FileExtensionsToIndexList != null && sandoDialogPage.FileExtensionsToIndexList.Any())
            {
                fileExtensionsList = sandoDialogPage.FileExtensionsToIndexList;
            }
            else
            {
                firstRun = true;
            }

            if (firstRun)
            {
                SaveNewSettings(sandoDialogPage, extensionPointsPluginDirectoryPath, numberOfSearchResultsReturned,
                    allowDataCollectionLogging, fileExtensionsList);
            }

            var sandoOptions = new SandoOptions(extensionPointsPluginDirectoryPath, numberOfSearchResultsReturned, allowDataCollectionLogging, fileExtensionsList);
            return sandoOptions;
        }

        private void SaveNewSettings(SandoDialogPage sandoDialogPage, string extensionPointsPluginDirectoryPath, int numberOfSearchResultsReturned, 
                                    bool allowDataCollectionLogging, List<string> fileExtensionsList)
        {
            if (allowDataCollectionLogging)
                sandoDialogPage.AllowDataCollectionLogging = Boolean.TrueString;
            else
                sandoDialogPage.AllowDataCollectionLogging = Boolean.FalseString;
            sandoDialogPage.ExtensionPointsPluginDirectoryPath = extensionPointsPluginDirectoryPath;
            sandoDialogPage.NumberOfSearchResultsReturned = numberOfSearchResultsReturned+String.Empty;
            sandoDialogPage.FileExtensionsToIndexList = fileExtensionsList;
            sandoDialogPage.SaveSettingsToStorage();
        }

 

        private bool ShowWelcomePopup()
        {
            IntroToSando intro = new IntroToSando();
            intro.ShowDialog();
            return intro.UploadAllowed;
        }
    }
}