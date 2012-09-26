namespace BuildSniffer
{
	using System;

	[Serializable]
	public class BuildItem
	{
		public bool IsDuplicate { get; set; }
		public string Name { get; set; }

		public override string ToString()
		{
			return this.Name + (this.IsDuplicate ? @" [Duplicate]" : string.Empty);
		}
	}
}
