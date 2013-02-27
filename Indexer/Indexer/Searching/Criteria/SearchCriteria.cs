﻿namespace Sando.Indexer.Searching.Criteria
{
	public abstract class SearchCriteria
	{

        public static int DefaultNumberOfSearchResultsReturned = 40;

		public SearchCriteria()
		{
            NumberOfSearchResultsReturned = DefaultNumberOfSearchResultsReturned;
		}

		public abstract string ToQueryString();
		
		public override bool Equals(object obj)
		{
			if(obj is SearchCriteria)
				return this.ToQueryString().Equals(((SearchCriteria)obj).ToQueryString());
			else
				return false;
		}

		public override int GetHashCode()
		{
			return this.ToQueryString().GetHashCode();
		}

		public int NumberOfSearchResultsReturned { get; set; }
	}
}
