using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Core;
using JetBrains.Annotations;

namespace Echo.StellarisModMerge {
	public class Mod : IDisposable {
		private static readonly string _userMods = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"/documents/Paradox Interactive/Stellaris/";
		private static readonly string _modTemp = Globals.TempFolder + "mods/";
		public const string DefinitionExtension = ".mod";
		public static readonly string DefinitionFolder = _userMods + @"mod/";
		public static readonly IReadOnlyList<string> MergeableExtensions = new List<string>() {
			".txt",
			".yml",
			".asset",
			".fnt",
			".shader",
			".gfx",
			".gui",
			".py",
			".sh",
			".vdf"
		};
		private readonly int _hash;
		private readonly List<string> _textFiles;
		private readonly List<string> _otherFiles;
		public Mod(string modFile) {
			_textFiles = new List<string>();
			_otherFiles = new List<string>();
			Console.WriteLine("Reading mod definition file...");
			_hash = modFile.GetHashCode() ^ DateTime.UtcNow.GetHashCode();
			string modData;
			try {
				modData = File.ReadAllText(modFile);
			} catch (FileNotFoundException e) {
				throw new ArgumentException("Mod definition file does not exist.", e);
			}

			Console.WriteLine("Getting Stellaris version mod is targeting...");
			var versionRegex = new Regex(@"^\s*supported_version\s*=", RegexOptions.Multiline);
			Match versionMatch = versionRegex.Match(modData);
			if (versionMatch.Success) {
				Version = GetConcreteVersion(ReadCString(modData, versionMatch));
				VersionString = GetVersionString(Version);
				Console.WriteLine("Mod is targeting " + VersionString + ".");
			} else {
				throw new ArgumentException("Provided file did not provide a target Stellaris version.");
			}

			Console.WriteLine("Getting mod files...");
			var pathRegex = new Regex(@"^\s*path\s*=", RegexOptions.Multiline);
			Match pathMatch = pathRegex.Match(modData);
			if (pathMatch.Success) {
				Console.WriteLine("Mod files are uncompressed, copying to temp folder...");
				string path = ReadCString(modData, pathMatch);
				if (!Path.IsPathRooted(path)) {
					path = _userMods + path;
				}
				var info = new DirectoryInfo(path);
				ExtractedPath = _modTemp + _hash + "/";
				CopyDirectory(info, new DirectoryInfo(ExtractedPath));
				Console.WriteLine("Files copied.");
			} else {
				var archiveRegex = new Regex(@"^\s*archive\s*=", RegexOptions.Multiline);
				Match archiveMatch = archiveRegex.Match(modData);
				if (archiveMatch.Success) {
					Console.WriteLine("Mod files are compressed, extracting to temp folder...");
					string zipPath = ReadCString(modData, archiveMatch);
					if (!Path.IsPathRooted(zipPath)) {
						zipPath = _userMods + zipPath;
					}
					using (FileStream fs = File.OpenRead(zipPath)) {
						var zf = new ZipFile(fs);
						ExtractedPath = _modTemp + _hash + "/";
						try {
							foreach (ZipEntry entry in zf) {
								var entryInfo = new FileInfo(entry.Name);
								if (entry.IsFile && entryInfo.Extension != DefinitionExtension) {
									Stream zs = zf.GetInputStream(entry);
									string outFile = ExtractedPath + entry.Name;
									Directory.CreateDirectory(new FileInfo(outFile).DirectoryName);
									using (FileStream w = File.Create(outFile)) {
										var buffer = new byte[4096];
										StreamUtils.Copy(zs, w, buffer);
									}
									RegisterFile(entryInfo, outFile);
								}
							}
						} finally {
							zf.Close();
						}
					}
					Console.WriteLine("Files extracted.");
				} else {
					throw new ArgumentException("Provided file did not provide a mod path or archive.");
				}
			}

			Console.WriteLine("Getting mod name...");
			if (_nameCache.ContainsKey(modFile)) {
				Name = _nameCache[modFile];
			} else {

			}
			Name = FindNameInData(modData, modFile) ?? "$NONAME";
			if (Name != null) {
				Console.WriteLine("Mod is named \"" + Name + "\".");
			}
			ModFile = modFile;
		}
		public string Name {
			get;
		}
		public string ModFile {
			get;
		}
		public int[] Version {
			get;
		}
		public string VersionString {
			get;
		}
		public string ExtractedPath {
			get;
		}
		public IReadOnlyList<string> TextFiles {
			get => _textFiles;
		}
		public IReadOnlyList<string> OtherFiles {
			get => _otherFiles;
		}
		[CanBeNull]
		private static string FindNameInData(string modData, string modFile) {
			var nameRegex = new Regex(@"^\s*name\s*=", RegexOptions.Multiline);
			Match nameMatch = nameRegex.Match(modData);
			string res = nameMatch.Success ? ReadCString(modData, nameMatch) : null;
			if (res == null) {
				ConsoleUtils.WarnLn("Mod \"" + modFile + "\" does not have a name.");
			}
			return res;
		}
		private static string ReadCString(string all, Match match) {
			return StringUtils.ReadCString(all, match.Index + match.Groups[0].Length).Trim();
		}
		private static readonly Dictionary<string, string> _nameCache = new Dictionary<string, string>();
		[CanBeNull]
		public static string FindName(string modFile) {
			if (!_nameCache.ContainsKey(modFile)) {
				_nameCache.Add(modFile, FindNameInData(File.ReadAllText(modFile), modFile));
			}
			return _nameCache[modFile];
		}
		public static string ResolvePath(string modFile) {
			if (!Path.IsPathRooted(modFile)) {
				modFile = DefinitionFolder + modFile;
			}
			return modFile.Replace('\\', '/').ToLower();
		}
		public static int[] GetConcreteVersion(string version) {
			if (version.StartsWith("v")) {
				version = version.Substring(1);
			}
			string[] vSplit = version.Split('.');
			int i;
			for (i = vSplit.Length - 1; i >= 0; i--) {
				string vCmp = vSplit[i];
				if (vCmp != "*") {
					break;
				} else {
					vSplit[i] = "0";
				}
			}
			return vSplit.Select(x => Convert.ToInt32(x)).ToArray();
		}
		public static string GetVersionString(int[] version) {
			return "v" + string.Join(".", version);
		}
		private void CopyDirectory(DirectoryInfo source, DirectoryInfo dest) {
			DirectoryUtils.Copy(source, dest, file => file.Extension != DefinitionExtension, RegisterFile);
		}
		private void RegisterFile(FileInfo info, string file) {
			(MergeableExtensions.Contains(info.Extension) ? _textFiles : _otherFiles).Add(file);
		}
		public override int GetHashCode() {
			return _hash;
		}
		~Mod() {
			Dispose();
		}
		public void Dispose() {
			Directory.Delete(ExtractedPath, true);
			GC.SuppressFinalize(this);
		}
	}
}
