﻿using System.Collections.Generic;

namespace Sando.Core.Extensions.Configuration
{
	public class ExtensionPointsConfiguration
	{
		public string PluginDirectoryPath { get; set; }
		public List<ParserExtensionPointsConfiguration> ParsersConfiguration { get; set; }
		public BaseExtensionPointConfiguration WordSplitterConfiguration { get; set; }
		public BaseExtensionPointConfiguration ResultsReordererConfiguration { get; set; }
		public BaseExtensionPointConfiguration QueryWeightsSupplierConfiguration { get; set; }
		public BaseExtensionPointConfiguration QueryRewriterConfiguration { get; set; }
        public BaseExtensionPointConfiguration IndexFilterManagerConfiguration { get; set; }
	}
}
