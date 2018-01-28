using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Echo.StellarisModMerge {
	public static class DirectoryUtils {
		private static readonly Predicate<FileInfo> _defCanCopy = fi => true;
		private static readonly Action<FileInfo, string> _defOnCopy = (fi, of) => {};
		public static void Copy(DirectoryInfo source, DirectoryInfo dest, Predicate<FileInfo> canCopy, Action<FileInfo, string> onCopy) {
			foreach (DirectoryInfo dir in source.GetDirectories()) {
				Copy(dir, dest.CreateSubdirectory(dir.Name), canCopy, onCopy);
			}
			foreach (FileInfo file in source.GetFiles()) {
				if (canCopy(file)) {
					string outFile = dest.FullName + "/" + file.Name;
					File.Copy(file.FullName, outFile);
					onCopy(file, outFile);
				}
			}
		}
		public static void Copy(DirectoryInfo source, DirectoryInfo dest, Predicate<FileInfo> canCopy) {
			Copy(source, dest, canCopy, _defOnCopy);
		}
		public static void Copy(DirectoryInfo source, DirectoryInfo dest, Action<FileInfo, string> onCopy) {
			Copy(source, dest, _defCanCopy, onCopy);
		}
		public static void Copy(DirectoryInfo source, DirectoryInfo dest) {
			Copy(source, dest, _defCanCopy, _defOnCopy);
		}
		public static void Copy(DirectoryInfo source, string dest, Predicate<FileInfo> canCopy, Action<FileInfo, string> onCopy) {
			Copy(source, new DirectoryInfo(dest), canCopy, onCopy);
		}
		public static void Copy(DirectoryInfo source, string dest, Predicate<FileInfo> canCopy) {
			Copy(source, new DirectoryInfo(dest), canCopy, _defOnCopy);
		}
		public static void Copy(DirectoryInfo source, string dest, Action<FileInfo, string> onCopy) {
			Copy(source, new DirectoryInfo(dest), _defCanCopy, onCopy);
		}
		public static void Copy(DirectoryInfo source, string dest) {
			Copy(source, new DirectoryInfo(dest), _defCanCopy, _defOnCopy);
		}
		public static void Copy(string source, DirectoryInfo dest, Predicate<FileInfo> canCopy, Action<FileInfo, string> onCopy) {
			Copy(new DirectoryInfo(source), dest, canCopy, onCopy);
		}
		public static void Copy(string source, DirectoryInfo dest, Predicate<FileInfo> canCopy) {
			Copy(new DirectoryInfo(source), dest, canCopy, _defOnCopy);
		}
		public static void Copy(string source, DirectoryInfo dest, Action<FileInfo, string> onCopy) {
			Copy(new DirectoryInfo(source), dest, _defCanCopy, onCopy);
		}
		public static void Copy(string source, DirectoryInfo dest) {
			Copy(new DirectoryInfo(source), dest, _defCanCopy, _defOnCopy);
		}
		public static void Copy(string source, string dest, Predicate<FileInfo> canCopy, Action<FileInfo, string> onCopy) {
			Copy(new DirectoryInfo(source), new DirectoryInfo(dest), canCopy, onCopy);
		}
		public static void Copy(string source, string dest, Predicate<FileInfo> canCopy) {
			Copy(new DirectoryInfo(source), new DirectoryInfo(dest), canCopy, _defOnCopy);
		}
		public static void Copy(string source, string dest, Action<FileInfo, string> onCopy) {
			Copy(new DirectoryInfo(source), new DirectoryInfo(dest), _defCanCopy, onCopy);
		}
		public static void Copy(string source, string dest) {
			Copy(new DirectoryInfo(source), new DirectoryInfo(dest), _defCanCopy, _defOnCopy);
		}
	}
}
