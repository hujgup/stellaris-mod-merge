using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.InteropServices;

namespace Echo.StellarisModMerge {
	public static class Program {
		private delegate bool ConsoleEventDelegate(int eventType);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool SetConsoleCtrlHandler(ConsoleEventDelegate callback, bool add);

		private static string PadStr(string str, int targetLength) {
			bool onLeft = true;
			while (str.Length < targetLength) {
				str = onLeft ? " " + str : str + " ";
				onLeft = !onLeft;
			}
			return str;
		}
		private static void ClearTemp() {
			var info = new DirectoryInfo(Globals.TempFolder);
			foreach (DirectoryInfo subdir in info.GetDirectories()) {
				subdir.Delete(true);
			}
		}
		public static void Main(string[] args) {
			var mods = new OrderedDictionary<string, Mod>();
			SetConsoleCtrlHandler(eventType => {
				ClearTemp();
				return false;
			}, true);

			if (Directory.Exists(Globals.TempFolder)) {
				ClearTemp();
			} else {
				Directory.CreateDirectory(Globals.TempFolder);
			}

			bool keepOpen = true;
			do {
				Console.Write("StellarisModMerge> ");
				string cmdStr = Console.ReadLine();
				if (cmdStr != null) {
					try {
						var cmd = new ShellLikeCommand(cmdStr);
						if (cmd.Args.Count > 0) {
							Console.WriteLine("");
							ShellLikeCommand.Arg arg0 = cmd.Args[0];
							bool valid;
							switch (arg0.Text) {
								case "load":
									if (cmd.Args.Count > 3) {
										ConsoleUtils.ErrorLn("Command \"load\" doesn't take more than 2 arguments.");
									} else if (cmd.Args.Count == 1) {
										ConsoleUtils.ErrorLn("Command\"load\" no mod ID or search string specified.");
									} else {
										string id = null;
										bool isName = false;
										ShellLikeCommand.Arg arg1 = cmd.Args[1];
										bool req3Args = false;
										valid = true;
										if (arg1.Type == ShellLikeCommand.ArgType.Switch) {
											if (arg1.Text == "s") {
												isName = true;
												req3Args = true;
											} else {
												ConsoleUtils.ErrorLn("Command \"load\" unknown switch " + arg1 + ".");
												valid = false;
											}
										} else {
											id = arg1.Text;
										}
										if (valid) {
											if (cmd.Args.Count == 3) {
												ShellLikeCommand.Arg arg2 = cmd.Args[2];
												if (arg2.Type == ShellLikeCommand.ArgType.Text) {
													if (id != null) {
														ConsoleUtils.ErrorLn("Command \"load\" unknown 2nd argument: " + arg2 + ".");
														valid = false;
													} else {
														id = arg2.Text;
													}
												} else if (id == null) {
													ConsoleUtils.ErrorLn("Command \"load\" no mod ID or search string specified.");
													valid = false;
												} else if (arg2.Text == "s") {
													isName = true;
												} else {
													ConsoleUtils.ErrorLn("Command \"load\" unknown switch " + arg2 + ".");
													valid = false;
												}
											} else if (req3Args) {
												ConsoleUtils.ErrorLn("Command \"load\" no mod ID or search string specified.");
												valid = false;
											}
										}
										if (valid) {
											keepOpen = ModLoader.Load(id, isName, mods);
										}
									}
									break;
								case "compile":
									// TODO: Allow version specifier
									if (cmd.Args.Count > 3) {
										ConsoleUtils.ErrorLn("Command \"compile\" doesn't take more than 2 arguments.");
									} else {
										valid = true;
										string version = "";
										try {
											version = ModCompiler.LatestVersion;
										} catch (Exception e) {
											ConsoleUtils.ErrorLn(e.Message);
											ConsoleUtils.WarnLn("Compilation aborted.");
											valid = false;
										}
										if (valid) {
											bool clear = false;
											bool expectVersion = false;
											for (int i = 1; valid && i < cmd.Args.Count; i++) {
												ShellLikeCommand.Arg arg = cmd.Args[i];
												if (expectVersion) {
													if (arg.Type == ShellLikeCommand.ArgType.Text) {
														version = arg.Text.ToLower();
														if (version.StartsWith("v")) {
															version = version.Substring(1);
														}
														expectVersion = false;
													} else {
														valid = false;
													}
												} else if (arg.Type == ShellLikeCommand.ArgType.Switch) {
													if (arg.Text == "c") {
														clear = true;
													} else if (arg.Text == "v") {
														expectVersion = true;
													}
												} else {
													ConsoleUtils.ErrorLn("Command \"compile\" unexpected text argument: " + arg + ".");
													valid = false;
												}
											}
											if (expectVersion) {
												ConsoleUtils.ErrorLn("Version not specified after -v switch - compilation aborted.");
												valid = false;
											}
											if (valid) {
												if (ModCompiler.ValidateVersion(version)) {
													keepOpen = ModCompiler.Compile(mods, version, clear);
												}
											}
										}
									}
									break;
								case "clear":
									if (cmd.Args.Count > 1) {
										ConsoleUtils.ErrorLn("Command \"clear\" doesn't take any arguments.");
									} else {
										mods.Clear();
										Console.WriteLine("Unloaded all mods.");
									}
									break;
								case "list":
									valid = true;
									bool showFileName = false;
									bool showTargetVersion = false;
									for (int i = 1; valid && i < cmd.Args.Count; i++) {
										ShellLikeCommand.Arg arg = cmd.Args[i];
										if (arg.Type == ShellLikeCommand.ArgType.Text) {
											ConsoleUtils.ErrorLn("Command \"list\" doesn't take any text arguments.");
											valid = false;
										} else {
											switch (arg.Text) {
												case "f":
													if (showFileName) {
														ConsoleUtils.ErrorLn("Command \"list\" duplicate switch " + arg + ".");
														valid = false;
													} else {
														showFileName = true;
													}
													break;
												case "v":
													if (showTargetVersion) {
														ConsoleUtils.ErrorLn("Command \"list\" duplicate switch " + arg + ".");
														valid = false;
													} else {
														showTargetVersion = true;
													}
													break;
												default:
													ConsoleUtils.ErrorLn("Command \"list\" unknown switch " + arg + ".");
													valid = false;
													break;
											}
										}
									}
									if (valid) {
										if (mods.Count > 0) {
											var ids = new string[mods.Count];
											var names = new string[mods.Count];
											var versions = new string[mods.Count];
											int i = 0;
											foreach (KeyValuePair<string, Mod> kvp in mods) {
												ids[i] = new FileInfo(kvp.Value.ModFile).Name;
												names[i] = kvp.Value.Name ?? "$NONAME";
												versions[i] = kvp.Value.VersionString;
												i++;
											}
											const string idHead = "MOD FILE";
											const string nameHead = "MOD NAME";
											const string versionHead = "GAME VERSION";
											int idLength = Math.Max(idHead.Length, ids.Max(id => id.Length));
											int nameLength = Math.Max(nameHead.Length, names.Max(name => name.Length));
											int versionLength = Math.Max(versionHead.Length, versions.Max(version => version.Length));
											int sumLength = nameLength;
											if (showFileName) {
												Console.Write(PadStr(idHead, idLength) + " | ");
												sumLength += idLength + 3;
											}
											Console.Write(PadStr(nameHead, nameLength));
											if (showTargetVersion) {
												Console.Write(" | " + PadStr(versionHead, versionLength));
												sumLength += versionLength + 3;
											}
											Console.WriteLine();
											for (i = 0; i < sumLength; i++) {
												Console.Write('-');
											}
											Console.WriteLine();
											for (i = 0; i < ids.Length; i++) {
												if (showFileName) {
													Console.Write(PadStr(ids[i], idLength) + " | ");
												}
												Console.Write(PadStr(names[i], nameLength));
												if (showTargetVersion) {
													Console.Write(" | " + PadStr(versions[i], versionLength));
												}
												Console.WriteLine();
											}
										} else {
											Console.WriteLine("No mods have been loaded.");
										}
									}
									break;
								case "exit":
									if (cmd.Args.Count > 1) {
										ConsoleUtils.ErrorLn("Command \"exit\" doesn't take any arguments.");
									} else {
										keepOpen = false;
									}
									break;
								case "help":
									if (cmd.Args.Count > 1) {
										ConsoleUtils.ErrorLn("Command \"help\" doesn't take any arguments.");
									} else {
										Console.WriteLine("## LIST OF COMMANDS ##");
										Console.WriteLine("");
										Console.WriteLine("load => Load a mod into the system. The order this command is called in defines load order - later mods will override older mods.");
										Console.WriteLine("  -s: Specifies that arg1 is a mod name search string.");
										Console.WriteLine("  arg1: The mod to load (file name if -s is not present, search string if it is).");
										Console.WriteLine("");
										Console.WriteLine("compile => Compiles all loaded mods into one super-mod.");
										Console.WriteLine("  -c: Clears all loaded mods when compilation is done.");
										Console.WriteLine("");
										Console.WriteLine("exit => Exits the program.");
										Console.WriteLine("");
										Console.WriteLine("clear => Clears all loaded mods.");
										Console.WriteLine("");
										Console.WriteLine("help => This command.");
									}
									break;
								default:
									ConsoleUtils.WarnLn("Unknown command \"" + arg0.Text + "\". Type \"help\" for a list of commands.");
									break;
							}
							Console.WriteLine("");
						}
					} catch (ArgumentException e) {
						Console.WriteLine("");
						ConsoleUtils.ErrorLn("Invalid command: " + e.Message);
						Console.WriteLine("");
					}
				} else {
					// Ctrl-C
					keepOpen = false;
				}
			} while (keepOpen);
			ClearTemp();
		}
	}
}
