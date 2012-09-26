namespace BuildSniffer
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Text;
	using System.Threading.Tasks;
	using System.Xml.Linq;
	using System.Xml.XPath;
	using Microsoft.Build.Framework;

	public class Project
	{
		#region Constructors

		public Project(string fileName, ILogger logger)
		{
			this.OriginalDirectory = Path.GetDirectoryName(fileName);
			this.Logger = logger;
			this.ReadFromFile(fileName);
		}

		#endregion

		#region Properties

		public XDocument Document { get; protected set; }
		protected ILogger Logger { get; set; }
		protected string OriginalDirectory { get; set; }

		#endregion

		#region Public Methods

		/// <summary>
		/// Takes a list of XML tags to ignore in the build script, and removes them from the actual document to be processed.
		/// </summary>
		/// <param name="itemsToIgnore">The items to ignore.</param>
		/// <returns></returns>
		public Project IgnoreItems(params string[] itemsToIgnore)
		{
			var items = GetAllTags(itemsToIgnore);
			foreach (var item in items)
			{
				item.Remove();
			}
			return this;
		}

		/// <summary>
		/// Builds all of the targets in this project file.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<TargetResult> BuildAll()
		{
			var results = new List<TargetResult>();
			string targetTagName = @"Target",
				nameTagName = @"Name";

			foreach (var target in this.GetAllTags(targetTagName).Where(x => x.Attribute(nameTagName) != null))
			{
				var logger = new SolutionCapturingLogger();
				var name = target.Attribute(nameTagName).Value;
				if (this.Build(name, new ILogger[] { logger }) && logger.SolutionsBuilt.FirstOrDefault() != null)
				{
					results.Add(new TargetResult()
					{
						Name = name,
						ItemsBuilt = logger.SolutionsBuilt
					});
				}	
			}
			return results;
		}

		#region Build Overloads

		/// <summary>
		/// Builds this project, using the default targets and the given loggers.
		/// </summary>
		/// <param name="loggers">An enumerator over all loggers to be used during the build.</param>
		/// <returns>
		/// Returns true on success; false otherwise.
		/// </returns>
		public bool Build(IEnumerable<ILogger> loggers)
		{
			var result = false;
			this.SwapMSBuildTasks();
			using (var reader = this.Document.CreateReader())
			{
				reader.MoveToContent();
				var innerProject = new Microsoft.Build.Evaluation.Project(reader);
				result = innerProject.Build(loggers.Prepend(this.Logger));
				reader.Close();
			}
			return result;
		}

		/// <summary>
		/// Builds this project, using the default targets and the given logger.
		/// </summary>
		/// <param name="logger">The logger to be used during the build.</param>
		/// <returns>
		/// Returns true on success; false otherwise.
		/// </returns>
		public bool Build(ILogger logger)
		{
			return this.Build(new ILogger[] { this.Logger, logger });
		}

		/// <summary>
		/// Builds this project, building the given target.
		/// </summary>
		/// <param name="target">The target to be built.</param>
		/// <returns>
		/// Returns true on success; false otherwise.
		/// </returns>
		public bool Build(string target)
		{
			return this.Build(new string[] { target }, new ILogger[] { this.Logger });
		}

		/// <summary>
		/// Builds this project, building the given targets.
		/// </summary>
		/// <param name="targets">An array of targets to be built.</param>
		/// <returns>
		/// Returns true on success; false otherwise.
		/// </returns>
		public bool Build(string[] targets)
		{
			return this.Build(targets, new ILogger[] { this.Logger });
		}

		/// <summary>
		/// Builds this project, building the given target and using the given loggers.
		/// </summary>
		/// <param name="target">The target to be built.</param>
		/// <param name="loggers">The loggers to be used during the build.</param>
		/// <returns>
		/// Returns true on success; false otherwise.
		/// </returns>
		public bool Build(string target, IEnumerable<ILogger> loggers)
		{
			return this.Build(new string[] { target }, loggers);
		}

		/// <summary>
		/// Builds this project, building the given targets and using the given loggers.
		/// </summary>
		/// <param name="targets">The targets to be built.</param>
		/// <param name="loggers">The loggers to be used during the build.</param>
		/// <returns>
		/// Returns true on success; false otherwise.
		/// </returns>
		public bool Build(string[] targets, IEnumerable<ILogger> loggers)
		{
			var result = false;
			this.SwapMSBuildTasks();
			using (var reader = this.Document.CreateReader())
			{
				reader.MoveToContent();
				var innerProject = new Microsoft.Build.Evaluation.Project(reader);
				result = innerProject.Build(targets, loggers.Prepend(this.Logger));
				reader.Close();
			}
			return result;
		}

		#endregion

		#endregion

		#region Helper Methods
		/// <summary>
		/// Reads from file.
		/// </summary>
		/// <param name="fileName">Name of the file.</param>
		protected virtual void ReadFromFile(string fileName)
		{
			this.Document = XDocument.Load(fileName);
			this.ConcretizeRelativeImports();
		}

		/// <summary>
		/// Concretizes the relative paths in Import tags
		/// </summary>
		private void ConcretizeRelativeImports()
		{
			string importTag = @"Import",
				projectAttr = @"Project";
			// Heuristicy hackity-hack
			var imports = from import in this.GetAllTags(importTag)
						  where import.Attribute(projectAttr) != null
						  && !import.Attribute(projectAttr).Value.Contains(@":\")
						  select import;
			foreach (var import in imports)
			{
				import.Attribute(projectAttr).Value = Path.Combine(this.OriginalDirectory, import.Attribute(projectAttr).Value);
			}
		}

		/// <summary>
		/// Gets all XML tags with a simple name, from the default namespace.
		/// </summary>
		/// <param name="tagName">Name of the tag.</param>
		/// <returns></returns>
		protected IEnumerable<XElement> GetAllTags(params string[] tagNames)
		{
			var defaultNs = this.Document.Root.GetDefaultNamespace();
			var listElements = new List<XElement>();
			foreach (var tagName in tagNames)
			{
				listElements.AddRange(Document.Root.Descendants(defaultNs + tagName));
			}
			return listElements;
		}

		/// <summary>
		/// Swaps the message for MS build.
		/// </summary>
		/// <param name="p">The p.</param>
		private static void SwapMessageForMSBuild(XElement p)
		{
			var element = new XElement(p.Name.Namespace + @"Message");
			element.Add(new XAttribute(@"Text", p.Attribute(@"Projects").Value));
			p.Parent.Add(element);
			p.Remove();
		}

		/// <summary>
		/// Swaps the MS build tasks.
		/// </summary>
		internal void SwapMSBuildTasks()
		{
			var msBuilds = this.GetAllTags(@"MSBuild");
			foreach (var build in msBuilds)
			{
				SwapMessageForMSBuild(build);
			}
		}

		#endregion
	}
}
