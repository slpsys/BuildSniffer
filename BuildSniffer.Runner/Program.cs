using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuildSniffer.Runner
{
	public class Runner 
	{
		static void Main(string[] args)
		{
			var project = new Project(args[0], new SolutionCapturingLogger())
				.IgnoreItems(
					@"Gallio",
					@"Exec",
					@"RemoveDir",
					@"Message",
					@"MakeDir",
					@"Copy",
					@"WriteLinesToFile",
					@"Script"
				);

			foreach (var target in project.BuildAll())
			{
				Console.WriteLine("Target: \"{0}\" is building:", target.Name);
				foreach (var item in target.ItemsBuilt)
				{
					Console.WriteLine("\t=> {0}", item);
				}
			}
		}
	}
}
