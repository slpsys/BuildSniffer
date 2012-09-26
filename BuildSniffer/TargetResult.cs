namespace BuildSniffer
{
	using System;
	using System.Collections.Generic;

	[Serializable]
	public class TargetResult
	{
		public string Name { get; set; }
		public IEnumerable<BuildItem> ItemsBuilt { get; set; }
	}
}
