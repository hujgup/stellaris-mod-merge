using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Echo.StellarisModMerge {
	public static class ConsoleUtils {
		private static void Recolor(string text, ConsoleColor newColor, Action<string> writer) {
			ConsoleColor old = Console.ForegroundColor;
			Console.ForegroundColor = newColor;
			writer(text);
			Console.ForegroundColor = old;
		}
		public static void ErrorLn(string text) {
			Recolor(text, ConsoleColor.Red, Console.WriteLine);
		}
		public static void WarnLn(string text) {
			Recolor(text, ConsoleColor.Yellow, Console.WriteLine);
		}
		public static void Error(string text) {
			Recolor(text, ConsoleColor.Red, Console.Write);
		}
		public static void Warn(string text) {
			Recolor(text, ConsoleColor.Yellow, Console.Write);
		}
	}
}
