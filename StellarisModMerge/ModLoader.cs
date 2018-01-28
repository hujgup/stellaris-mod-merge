using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Echo.StellarisModMerge {
	public static class ModLoader {
		public static bool Load(string id, bool isName, OrderedDictionary<string, Mod> mods) {
			bool keepOpen = true;
			try {
				string modFile = id + Mod.DefinitionExtension;
				bool valid = true;
				if (isName) {
					Console.WriteLine("Searching for mod...");
					id = id.ToLower();
					var candidates = new Dictionary<string, (string File, int Index)>();
					foreach (string file in Directory.GetFiles(Mod.DefinitionFolder)) {
						string f2 = Mod.ResolvePath(file);
						if (new FileInfo(f2).Extension == Mod.DefinitionExtension) {
							string name = Mod.FindName(f2);
							if (name != null) {
								int index = name.ToLower().IndexOf(id);
								if (index > -1) {
									candidates.Add(name, (File: f2, Index: index));
								}
							}
						}
					}
					if (candidates.Count > 1) {
						Console.WriteLine("Found " + candidates.Count + " candidate mods.");
						Console.WriteLine("");
						var names = new List<KeyValuePair<string, (string File, int Index)>>(candidates);
						names.Sort((a, b) => a.Value.Index - b.Value.Index);
						int i = 1;
						foreach (KeyValuePair<string, (string, int)> name in names) {
							Console.WriteLine(i + ". " + name.Key);
							i++;
						}
						Console.WriteLine("");
						Console.Write("Input number corresponding to the mod you wish to load, or type \"exit\" to abort this load: ");
						bool reading = true;
						int n = -1;
						var ordinalRegex = new Regex(@"^\d+$");
						do {
							string line = Console.ReadLine();
							if (line != null) {
								line = line.Trim();
								if (line == "exit") {
									Console.WriteLine("");
									Console.WriteLine("Aborting load.");
									reading = false;
									valid = false;
								} else {
									Match m = ordinalRegex.Match(line);
									if (m.Success) {
										n = Convert.ToInt32(line);
										if (n >= 1 && n <= names.Count) {
											reading = false;
										} else {
											ConsoleUtils.Warn("Invalid argument. Please specify a number in the range [1, " + names.Count + "] or \"exit\": ");
										}
									} else {
										ConsoleUtils.Warn("Invalid argument. Please specify a number or \"exit\": ");
									}
								}
							} else {
								// Ctrl-C
								reading = false;
								valid = false;
								keepOpen = false;
							}
						} while (reading);
						if (valid && n > 0) {
							n--;
							Console.WriteLine("");
							Console.WriteLine("Selected mod \"" + names[n].Key + "\".");
							modFile = candidates[names[n].Key].File;
						}
					} else if (candidates.Count == 1) {
						Console.Write("Are you searching for \"" + candidates.First().Key + "\"? (y/n): ");
						bool reading = true;
						bool found = false;
						do {
							string line = Console.ReadLine();
							if (line != null) {
								line = line.Trim().ToLower();
								switch (line) {
									case "y":
									case "yes":
										reading = false;
										found = true;
										break;
									case "n":
									case "no":
										reading = false;
										break;
									default:
										ConsoleUtils.Warn("Invalid argument. Please specify y/n: ");
										break;
								}
							} else {
								// Ctrl-C
								reading = false;
								valid = false;
								keepOpen = false;
							}
						} while (reading);
						Console.WriteLine("");
						if (found) {
							Console.WriteLine("Found mod.");
							modFile = candidates.First().Value.File;
						} else {
							ConsoleUtils.WarnLn("Incorrect mod and no alternatives available; aborting load.");
							valid = false;
						}
					} else {
						ConsoleUtils.ErrorLn("Found zero candidate mods.");
						valid = false;
					}
					if (valid) {
						modFile = Mod.ResolvePath(modFile);
					}
				} else {
					if (!modFile.EndsWith(Mod.DefinitionExtension)) {
						modFile += Mod.DefinitionExtension;
					}
					modFile = Mod.ResolvePath(modFile);
					Console.WriteLine("Attempting load of mod \"" + modFile + "\"...");
				}
				if (valid) {
					if (mods.ContainsKey(modFile)) {
						ConsoleUtils.WarnLn("Redundant load: That mod has already been loaded.");
					} else {
						mods.Add(modFile, new Mod(modFile));
						Console.WriteLine("Mod loaded.");
					}
				}
			} catch (Exception e) {
				ConsoleUtils.ErrorLn(e.Message);
				ConsoleUtils.ErrorLn("Mod has not been loaded.");
			}
			return keepOpen;
		}

	}
}
