using System.Text;

namespace Echo.StellarisModMerge {
	public static class StringUtils {
		public static string ReadCString(string text, int startAt) {
			bool seenOpen = false;
			bool escapeNext = false;
			var res = new StringBuilder();
			for (int i = startAt; i < text.Length; i++) {
				char c = text[i];
				if (seenOpen) {
					if (escapeNext) {
						res.Append(c);
						escapeNext = false;
					} else if (c == '\\') {
						escapeNext = true;
					} else if (c == '"') {
						i = text.Length;
					} else {
						res.Append(c);
					}
				} else if (c == '"') {
					seenOpen = true;
				}
			}
			return res.ToString();
		}
	}
}
