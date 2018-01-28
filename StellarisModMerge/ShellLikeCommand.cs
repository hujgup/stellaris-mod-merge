using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace Echo.StellarisModMerge {
	public class ShellLikeCommand {
		public enum ArgType {
			Text,
			Switch
		}

		public class Arg {
			public Arg(ArgType type, string text) {
				Type = type;
				Text = text;
			}
			public ArgType Type {
				get;
			}
			public string Text {
				get;
			}
			public override string ToString() {
				string res = "";
				if (Type == ArgType.Switch) {
					res += "-";
					if (Text.Length > 1) {
						res += "-";
					}
					res += Text;
				} else if (Text.Contains(" ")) {
					res += "\"" + Text + "\"";
				} else {
					res += Text;
				}
				return res;
			}
		}

		private List<Arg> _args;
		public ShellLikeCommand(string command) {
			command = command.Trim();
			_args = new List<Arg>();
			int i = 0;
			bool seenInit = false;
			while (i < command.Length) {
				if (command.IndexOf("\"", i) == i) {
					ValidatePos(seenInit, "Quoted string");
					string cstr = StringUtils.ReadCString(command, i);
					i += cstr.Length + 2;
					_args.Add(new Arg(ArgType.Text, cstr));
				} else if (command.IndexOf("-", i) == i) {
					if (command.IndexOf("--", i) == i) {
						ValidatePos(seenInit, "Word switch");
						i += 2;
						int wsIndex = command.IndexOf(' ', i);
						string wordSwitch = command.Substring(i, NormalizeIndex(wsIndex, command, i));
						i += wordSwitch.Length;
						if (wordSwitch.Length > 0) {
							foreach (char c in wordSwitch) {
								ValidateChar(c, "Word switch");
							}
							_args.Add(new Arg(ArgType.Switch, wordSwitch));
						} else {
							throw new ArgumentException("Word switch was empty.");
						}
					} else {
						ValidatePos(seenInit, "Char switch");
						i++;
						int wsIndex = command.IndexOf(' ', i);
						string charSwitch = command.Substring(i, NormalizeIndex(wsIndex, command, i));
						i += charSwitch.Length;
						if (charSwitch.Length > 0) {
							foreach (char c in charSwitch) {
								ValidateChar(c, "Char switch");
								_args.Add(new Arg(ArgType.Switch, c.ToString()));
							}
						} else {
							throw new ArgumentException("Char switch was empty.");
						}
					}
				} else {
					int wsIndex = command.IndexOf(' ', i);
					string arg = command.Substring(i, NormalizeIndex(wsIndex, command, i));
					foreach (char c in arg) {
						ValidateChar(c, "Unquoted string");
					}
					i += arg.Length;
					_args.Add(new Arg(ArgType.Text, arg));
					seenInit = true;
				}
				while (i < command.Length && command[i] == ' ') {
					i++;
				}
			}
		}
		private static int NormalizeIndex(int index, string str, int offset) {
			return (index > -1 ? index : str.Length) - offset;
		}
		private static void ValidatePos(bool seenInit, string type) {
			if (!seenInit) {
				throw new ArgumentException(type + " cannot appear as the first argument.");
			}
		}
		private static void ValidateChar(char c, string type) {
			if (char.IsWhiteSpace(c)) {
				throw new ArgumentException(type + " char cannot be whitespace.");
			}
		}
		public IReadOnlyList<Arg> Args {
			get => _args;
		}
		public override string ToString() {
			var res = new StringBuilder();
			return string.Join(" ", Args.Select(x => x.ToString()));
		}
	}
}
