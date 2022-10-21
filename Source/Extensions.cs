using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchyDuck.Math {
	public static class Extensions {
		public static string ToParameter(this string str) {
			str = str.Replace(" ", "_");
			str = str.ToLower();
			return str;
		}
	}
}
