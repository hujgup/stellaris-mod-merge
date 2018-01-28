using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using LibGit2Sharp;
using JetBrains.Annotations;

namespace Echo.StellarisModMerge {
	public static class ModCompiler {
		private static readonly Dictionary<int, Dictionary<int, Dictionary<int, int>>> _versions = new Dictionary<int, Dictionary<int, Dictionary<int, int>>>();
		private static readonly string _gitTemp = Globals.TempFolder + "git/";
		private static readonly string _outDir = "out/";
		private static readonly Regex _versionRegex = new Regex(@"^(\d+)\.(\d+)(?:\.(\d+))?$");
		public static string LatestVersion {
			get {
				LoadVersionInfo();
				int key = _versions.Keys.Max();
				string res = key.ToString();
				Dictionary<int, Dictionary<int, int>> sub = _versions[key];
				key = sub.Keys.Max();
				res += "." + key;
				Dictionary<int, int> sub2 = sub[key];
				key = sub2.Keys.Max();
				res += "." + key;
				return res;
			}
		}
		private static void LogVersions() {
			foreach (KeyValuePair<int, Dictionary<int, Dictionary<int, int>>> kvp in _versions) {
				Console.WriteLine(kvp.Key + ": {");
				foreach (KeyValuePair<int, Dictionary<int, int>> kvp2 in kvp.Value) {
					Console.WriteLine("  " + kvp2.Key + ": {");
					foreach (KeyValuePair<int, int> kvp3 in kvp2.Value) {
						Console.WriteLine("    " + kvp3.Key + "/" + kvp3.Value);
					}
					Console.WriteLine("  }");
				}
				Console.WriteLine("}");
			}
		}
		private static IDictionary MakeDic(int indent) {
			switch (indent) {
				case 1:
					return new Dictionary<int, Dictionary<int, int>>();
				case 2:
					return new Dictionary<int, int>();
				default:
					throw new ArgumentOutOfRangeException(nameof(indent), "Version file indent cannot exceed 2.");
			}
		}
		private static void LoadVersionInfo() {
			if (_versions.Count == 0) {
				int ctxIndent = 0;
				int prevN = -1;
				var stack = new Stack<IDictionary>();
				IDictionary ctx = _versions;
				int lineNum = 1;
				foreach (string line in File.ReadAllLines("resources/versions.txt")) {
					// ^\t*\d+\s*(//[\s\S]*)?$
					string trimLine = line.TrimStart('\t');
					int indent = line.Length - trimLine.Length;
					int n = Convert.ToInt32(trimLine.Split(new[] { "//" }, StringSplitOptions.None)[0].TrimEnd());
					if (indent > ctxIndent) {
						if (prevN == -1) {
							throw new FormatException("Version file cannot indent more than one level at a time: Old indent was " + ctxIndent + " but new indent was " + indent + " (at line " + lineNum + ").");
						} else {
							IDictionary newCtx = MakeDic(indent);
							ctx.Add(prevN, newCtx);
							stack.Push(ctx);
							ctx = newCtx;
							prevN = n;
						}
					} else if (indent < ctxIndent) {
						if (prevN != -1 && ctxIndent == 2) {
							ctx.Add(prevN, prevN);
						}
						int diff = ctxIndent - indent;
						while (diff > 0) {
							ctx = stack.Pop();
							diff--;
						}
						prevN = n;
					} else {
						if (prevN != -1) {
							if (indent == 2) {
								ctx.Add(prevN, prevN);
							} else {
								throw new FormatException("Version file must specify subversions at indent level " + indent + " (at line " + lineNum + ").");
							}
						}
						prevN = n;
					}
					ctxIndent = indent;
					lineNum++;
				}
				if (ctxIndent == 2 && prevN != -1) {
					ctx.Add(prevN, prevN);
				}
			}
		}
		private static string GetGameFiles(int[] version, [CanBeNull] string modName, out bool valid) {
			string vStr = GetVersionString(version);
			valid = true;
			if (_versions.ContainsKey(version[0])) {
				if (version.Length >= 2) {
					Dictionary<int, Dictionary<int, int>> sub = _versions[version[0]];
					if (sub.ContainsKey(version[1])) {
						if (version.Length == 3) {
							Dictionary<int, int> sub2 = sub[version[1]];
							if (!sub2.ContainsKey(version[2])) {
								ConsoleUtils.ErrorLn("Stellaris " + vStr + " does not exist (required by mod " + modName + ") - compilation aborted.");
								valid = false;
							}
						} else {
							ConsoleUtils.ErrorLn("Stellaris " + vStr + " does not exist (required by mod " + modName + ") - compilation aborted.");
							valid = false;
						}
					} else {
						ConsoleUtils.ErrorLn("Stellaris " + vStr + " does not exist (required by mod " + modName + ") - compilation aborted.");
						valid = false;
					}
				}
			} else {
				ConsoleUtils.ErrorLn("Stellaris " + vStr + " does not exist (required by mod " + modName + ") - compilation aborted.");
				valid = false;
			}
			return "resources/versions/" + vStr + "/";
		}
		public static string GetVersionString(int[] version) {
			return "v" + string.Join(".", version);
		}
		public static bool ValidateVersion(string vStr) {
			bool valid = true;
			try {
				LoadVersionInfo();
			} catch (Exception e) {
				ConsoleUtils.ErrorLn(e.Message);
				ConsoleUtils.WarnLn("Compilation aborted.");
				valid = false;
			}
			if (valid) {
				Match m = _versionRegex.Match(vStr);
				valid = m.Success;
				if (valid) {
					int main = Convert.ToInt32(m.Groups[1].Value);
					valid = _versions.ContainsKey(main);
					if (valid) {
						Dictionary<int, Dictionary<int, int>> sub = _versions[main];
						int major = Convert.ToInt32(m.Groups[2].Value);
						valid = sub.ContainsKey(major);
						if (valid) {
							if (m.Groups.Count == 4) {
								Dictionary<int, int> sub2 = sub[major];
								int minor = Convert.ToInt32(m.Groups[3].Value);
								valid = sub2.ContainsKey(minor);
								if (!valid) {
									ConsoleUtils.ErrorLn("v" + vStr + " no such minor version exists - compilation aborted.");
								}
							}
						} else {
							ConsoleUtils.ErrorLn("v" + vStr + " no such major version exists - compilation aborted.");
						}
					} else {
						ConsoleUtils.ErrorLn("v" + vStr + " no such main version exists - compilation aborted.");
					}
				} else {
					ConsoleUtils.ErrorLn("v" + vStr + " is not a valid version string - compilation aborted.");
				}
			}
			return valid;
		}
		private static void GetConflicts(DirectoryInfo gameDir, DirectoryInfo modDir, List<string> conflicts, string root) {
			{
				Dictionary<string, DirectoryInfo> gameSubDirs = gameDir.GetDirectories().ToDictionary(x => x.Name);
				foreach (DirectoryInfo subDirInfo in modDir.GetDirectories()) {
					if (gameSubDirs.ContainsKey(subDirInfo.Name)) {
						GetConflicts(gameSubDirs[subDirInfo.Name], subDirInfo, conflicts, root);
					}
				}
			}
			Dictionary<string, FileInfo> gameFiles = gameDir.GetFiles().ToDictionary(x => x.Name);
			foreach (FileInfo fileInfo in modDir.GetFiles()) {
				if (gameFiles.ContainsKey(fileInfo.Name)) {
					conflicts.Add(fileInfo.FullName.Replace(root, ""));
				}
			}
		}
		private static List<string> GetConflicts(string gameFiles, Mod mod) {
			var gameInfo = new DirectoryInfo(gameFiles);
			var modInfo = new DirectoryInfo(mod.ExtractedPath);
			var res = new List<string>();
			GetConflicts(gameInfo, modInfo, res, mod.ExtractedPath);
			return res;
		}
		private static int VersionCompare(int[] v1, int[] v2) {
			int res = 0;
			for (int i = 0; res == 0 && i < v1.Length; i++) {
				res = v1[i].CompareTo(v2[i]);
			}
			return res;
		}
		private static string GetCoreBranchName(string vStr) {
			return "CORE_" + vStr;
		}
		private static string GetModBranchName(Mod mod) {
			return "MOD_" + mod.GetHashCode();
		}
		private static string GetMergeBranchName(string vStr) {
			return "MERGE_" + vStr;
		}
		private static long GetTimestamp() {
			DateTime now = DateTime.UtcNow;
			var unix = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
			TimeSpan timestamp = now.Subtract(unix);
			return (long)timestamp.TotalSeconds;
		}
		private static string RandomDirectoryName() {
			return GetTimestamp() + "-" + new Random().Next() + "/";
		}
		private static Repository GitSetUp(Mod initMod) {
			string path = _gitTemp + RandomDirectoryName();
			DirectoryUtils.Copy("resources/stellaris", path);
			var repo = new Repository(path);
			Commands.Checkout(repo, repo.Branches[GetCoreBranchName(initMod.VersionString)]);
			return repo;
		}
		private static string GitTearDown(Repository repo) {
			string outDir = _outDir + RandomDirectoryName();
			Directory.CreateDirectory(outDir);
			DirectoryUtils.Copy(repo.Info.Path, outDir);
			Directory.Delete(outDir + ".git", true);
			Directory.Delete(repo.Info.Path, true);
			repo.Dispose();
			return outDir;
		}
		private static readonly Signature _sig = new Signature("smm-runtime", "hujgup@gmail.com", DateTimeOffset.UtcNow);
		private static Branch ApplyModChanges(Repository repo, Branch ctxBranch, string cvStr, Mod mod) {
			Branch branch = repo.CreateBranch(GetModBranchName(mod));
			Commands.Checkout(repo, branch);
			foreach (FileInfo file in new RecursiveFileIterator(mod.ExtractedPath)) {
				Directory.CreateDirectory(RecursiveFileIterator.GetRelativeDirectory(file, mod.ExtractedPath));
				string relName = RecursiveFileIterator.GetRelativeName(file, mod.ExtractedPath);
				File.Copy(file.FullName, repo.Info.Path + relName, true);
				repo.Index.Add(relName);
			}
			repo.Commit("Added files for mod " + mod.Name, _sig, _sig, _cmtOpts);
			Commands.Checkout(repo, ctxBranch);
			return branch;
		}
		private static readonly CommitOptions _cmtOpts = new CommitOptions {
			AllowEmptyCommit = true
		};
		private static Branch MergeMods(Repository repo, string vStr, IEnumerable<Branch> modBranches) {
			Branch mergeBranch = repo.CreateBranch(GetMergeBranchName(vStr));
			Commands.Checkout(repo, mergeBranch);
			foreach (Branch toMerge in modBranches) {
				repo.Merge(toMerge, _sig, new MergeOptions {
					CommitOnSuccess = false,
					FindRenames = false,
					FileConflictStrategy = CheckoutFileConflictStrategy.Merge
				});
				// TODO
				// Conflict resolution: Prefer toMerge
				repo.Commit("Merged " + toMerge.FriendlyName, _sig, _sig, _cmtOpts);
			}
			return mergeBranch;
		}
		private static Branch ChangeCoreVersion(Repository repo, string cvStr, string nvStr, IEnumerable<Branch> modBranches) {
			Branch mergeBranch = MergeMods(repo, cvStr, modBranches);
			Branch nvBranch = repo.Branches[GetCoreBranchName(nvStr)];
			repo.Merge(nvBranch, _sig, new MergeOptions {
				CommitOnSuccess = false,
				FindRenames = true,
				FileConflictStrategy = CheckoutFileConflictStrategy.Merge
			});
			foreach (Conflict cnf in repo.Index.Conflicts) {
				// TODO
				// Conflict resolution:
				//	If conflict is between CORE prev and CORE curr, prefer CORE curr
				//	Else, prefer mergeBranch
			}
			repo.Commit("Stellaris version change: " + cvStr + " to " + nvStr, _sig, _sig, _cmtOpts);
			// TODO: Return new core branch
			// TODO: Only do mod merges as a final step
			// 1: Branch mods from their target versions
			// 2: Merge mods from the same target versions together
			// 3: Merge merged branches together
			// 4: If final CORE version is not the target version, merge with the CORE for that version
			return mergeBranch;
		}
		public static bool Compile(OrderedDictionary<string, Mod> mods, string targetVersion, bool clear) {
			bool keepOpen = true;
			if (mods.Count > 0) {
				bool valid = true;
				try {
					LoadVersionInfo();
				} catch (Exception e) {
					ConsoleUtils.ErrorLn(e.Message);
					ConsoleUtils.WarnLn("Compilation aborted.");
					valid = false;
				}
				if (valid) {
					int i = 0;
					List<OrderedMod> mods2 = mods.Select(x => new OrderedMod(x.Value, i++)).ToList();
					mods2.Sort((a, b) => {
						int res = VersionCompare(a.Mod.Version, b.Mod.Version);
						return res != 0 ? res : a.Position.CompareTo(b.Position);
					});
					int[] vTarget = Mod.GetConcreteVersion(targetVersion);
					int[] currentVersion = mods2[0].Mod.Version;
					Repository repo = GitSetUp(mods2[0].Mod);
					foreach (OrderedMod mod in mods2) {
						if (VersionCompare(mod.Mod.Version, vTarget) > 0) {
							ConsoleUtils.WarnLn("Mod \"" + mod.Mod.Name + "\" is targeting a newer version of Stellaris then compilation is targeting.");
							ConsoleUtils.WarnLn("(Mod needs " + mod.Mod.VersionString + " but compilation is targeting " + targetVersion + ").");
							ConsoleUtils.WarnLn("Skipping mod...");
						} else {
							int cmp = VersionCompare(mod.Mod.Version, currentVersion);
							if (cmp > 0) {
								ChangeCoreVersion(currentVersion, mod.Mod);
								currentVersion = mod.Mod.Version;
							}
							ApplyMod(mod.Mod);
						}
					}
					string outDir = TearDownGit(repo);



//					int[] version = GetConcreteVersion(targetVersion);
					//string vStr = GetVersionString(version);
					//Console.WriteLine("Compiling against Stellaris " + vStr + "...");
					//string gameFiles = GetGameFiles(version, "$CORE " + vStr, out valid);
					if (valid) {



						/*
						var usedLines = new Dictionary<string, Dictionary<int, Mod>>();
						foreach (KeyValuePair<string, Mod> kvp in mods) {
							Mod mod = kvp.Value;
							//int[] modV = GetConcreteVersion(mod.Version);
							//string modVStr = GetVersionString(modV);
							bool compileThis = true;
							if (vStr == modVStr) {
								List<string> conflicts = GetConflicts(gameFiles, mod);
								ConsoleUtils.WarnLn("Compilation is targeting " + vStr + " but mod \"" + mod.Name + "\" is targeting " + modVStr + ".");
								if (conflicts.Count > 0) {
									ConsoleUtils.WarnLn("Vanilla files have been overridden - compiling this mod may lead to an unstable end product.");
									ConsoleUtils.WarnLn("List of overridden files:");
									foreach (string file in conflicts) {
										ConsoleUtils.WarnLn("  " + file);
									}
								} else {
									ConsoleUtils.WarnLn("No vanilla files have been overriden, but features used by the mod may have changed between versions.");
								}
								ConsoleUtils.Warn("Do you want to compile this mod? (y/n): ");
								bool reading = true;
								do {
									string line = Console.ReadLine();
									if (line != null) {
										line = line.Trim().ToLower();
										switch (line) {
											case "y":
											case "yes":
												compileThis = true;
												reading = false;
												break;
											case "n":
											case "no":
												compileThis = false;
												reading = false;
												break;
											default:
												ConsoleUtils.Warn("Invalid argument. Please specify y/n: ");
												break;
										}
									} else {
										reading = false;
										valid = false;
										keepOpen = false;
									}
								} while (reading);
								Console.WriteLine();
							}
							if (valid) {
								if (compileThis) {
									Console.WriteLine("Compiling \"" + ResolveName(mod.Name) + "\"...");
									ModDiffer.ApplyDiff(gameFiles, mod, usedLines);
								} else {
									Console.WriteLine("Skipping mod...");
								}
							}
						}
						if (valid) {
							if (clear) {
								mods.Clear();
							}
						}
						*/
					}
				}
			} else {
				ConsoleUtils.ErrorLn("No mods have been loaded - compilation aborted.");
			}
			return keepOpen;
		}
	}
}
