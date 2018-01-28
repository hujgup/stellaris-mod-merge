using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Echo.StellarisModMerge {
	public class RecursiveFileIterator : IEnumerable<FileInfo> {
		private class Enumerator : IEnumerator<FileInfo> {
			private readonly Stack<(DirectoryInfo Dir, IEnumerator<DirectoryInfo> Enum)> _stack;
			private readonly DirectoryInfo _rootDir;
			private DirectoryInfo _currentDir;
			private IEnumerator<FileInfo> _currentFileEnum;
			private IEnumerator<DirectoryInfo> _currentSubdirEnum;
			private FileInfo _currentFile;
			public Enumerator(DirectoryInfo rootDir) {
				_rootDir = rootDir;
				_stack = new Stack<(DirectoryInfo Dir, IEnumerator<DirectoryInfo> Enum)>();
			}
			public FileInfo Current {
				get => _currentFile;
			}
			object IEnumerator.Current {
				get => Current;
			}
			private bool MoveNextSubdir() {
				bool res = true;
				if (_currentSubdirEnum.MoveNext()) {
					_stack.Push((Dir: _currentDir, Enum: _currentSubdirEnum));
					_currentDir = _currentSubdirEnum.Current;
					_currentSubdirEnum = null;
					res = MoveNext();
				} else {
					(DirectoryInfo Dir, IEnumerator<DirectoryInfo> Enum) frame = _stack.Pop();
					_currentDir = frame.Dir;
					_currentSubdirEnum = frame.Enum;
					res = MoveNextSubdir();
				}
				return res;
			}
			public bool MoveNext() {
				bool res = true;
				_currentFile = null;
				if (_currentSubdirEnum == null) {
					if (_currentFileEnum == null) {
						_currentFileEnum = _currentDir.GetFiles().Cast<FileInfo>().GetEnumerator();
					}
					if (_currentFileEnum.MoveNext()) {
						_currentFile = _currentFileEnum.Current;
					} else {
						_currentSubdirEnum = _currentDir.GetDirectories().Cast<DirectoryInfo>().GetEnumerator();
						res = MoveNextSubdir();
					}
				} else {
					res = MoveNextSubdir();
				}
				return res;
			}
			public void Reset() {
				_currentDir = _rootDir;
				_currentFileEnum = null;
				_currentSubdirEnum = null;
				_stack.Clear();
			}
			void IDisposable.Dispose() {}
		}

		private readonly DirectoryInfo _rootDir;
		public RecursiveFileIterator(DirectoryInfo rootDir) {
			_rootDir = rootDir;
		}
		public RecursiveFileIterator(string rootDir) : this(new DirectoryInfo(rootDir)) {}
		public static string GetRelativeDirectory(FileInfo file, string root) {
			return file.DirectoryName.Replace(root, "");
		}
		public static string GetRelativeName(FileInfo file, string root) {
			return file.FullName.Replace(root, "");
		}
		public IEnumerator<FileInfo> GetEnumerator() {
			return new Enumerator(_rootDir);
		}
		IEnumerator IEnumerable.GetEnumerator() {
			return GetEnumerator();
		}
	}
}
