namespace BuildSniffer 
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;

	public class SolutionCapturingLogger : Microsoft.Build.Utilities.Logger
	{
		private List<string> itemsBuilt = new List<string>();
		private object lockObject = new object();

		/// <summary>
		/// When overridden in a derived class, subscribes the logger to specific events.
		/// </summary>
		/// <param name="eventSource">The available events that a logger can subscribe to.</param>
		public override void Initialize(Microsoft.Build.Framework.IEventSource eventSource)
		{
			eventSource.MessageRaised += eventSource_MessageRaised;
		}

		/// <summary>
		/// Handles the MessageRaised event of the eventSource control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="Microsoft.Build.Framework.BuildMessageEventArgs" /> instance containing the event data.</param>
		private void eventSource_MessageRaised(object sender, Microsoft.Build.Framework.BuildMessageEventArgs e)
		{
			if (!e.SenderName.ToLower().Equals(@"message")
				|| string.IsNullOrEmpty(e.Message))
			{
				return;
			}

			lock (this.lockObject)
			{
				var solutions = e.Message.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				itemsBuilt.AddRange(solutions);
			}
		}

		/// <summary>
		/// Gets the solutions built.
		/// </summary>
		/// <value>
		/// The solutions built.
		/// </value>
		public IEnumerable<BuildItem> SolutionsBuilt
		{
			get
			{
				var alreadySeen = new HashSet<string>();
				string[] ret = null; ;
				lock (this.lockObject)
				{
					ret = new string[this.itemsBuilt.Count];
					this.itemsBuilt.CopyTo(ret, 0);
				}
				foreach (var item in ret)
				{
					var buildItem = new BuildItem() { Name = item, IsDuplicate = true };
					if (!alreadySeen.Contains(item))
					{
						buildItem.IsDuplicate = false;
						alreadySeen.Add(item);
					}
					yield return buildItem;
				}
			}
		}
	}
}
