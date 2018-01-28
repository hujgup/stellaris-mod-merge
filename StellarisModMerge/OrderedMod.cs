using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Echo.StellarisModMerge {
	public class OrderedMod {
		public OrderedMod(Mod mod, int pos) {
			Mod = mod;
			Position = pos;
		}
		public Mod Mod {
			get;
		}
		public int Position {
			get;
		}
	}
}
