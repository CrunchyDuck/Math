using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchyDuck.Math {
	public static class Extensions {
		public static string replacedCharacters = " -'";
		public static string ToParameter(this string str) {
			foreach (char c in replacedCharacters) {
				str = str.Replace(" ", "_");
				str = str.Replace("-", "_");
			}
			str = str.ToLower();
			return str;
		}

		public static string ToCategory(this string str) {
			return "c_" + str.ToParameter();
		}

		public static bool HasMethod(this object objectToCheck, string methodName) {
			var type = objectToCheck.GetType();
			return type.GetMethod(methodName) != null;
		}
	}
}
