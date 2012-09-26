namespace BuildSniffer
{
	using System.Collections.Generic;

	public static class Extensions
	{
		/// <summary>
		/// Prepends an item onto an IEnumerable without enumerating the IEnumerable directly.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="self">The self.</param>
		/// <param name="itemToPrepend">The item to prepend.</param>
		/// <returns></returns>
		public static IEnumerable<T> Prepend<T>(this IEnumerable<T> self, T itemToPrepend)
		{
			yield return itemToPrepend;
			foreach (var item in self)
			{
				yield return item;
			}
		}
	}
}
