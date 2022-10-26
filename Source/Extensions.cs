using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchyDuck.Math {
	public static class Extensions {
		public static string ToParameter_old(this string str) {
			str = str.Replace(" ", "_");
			str = str.Replace("-", "_");
			str = str.Replace("'", "_");
			str = str.Replace("(", "_");
			str = str.Replace(")", "_");
			str = str.Replace(".", "_");
			str = str.ToLower();
			return str;
		}

		public static string ToCategory_old(this string str) {
			return "c_" + str.ToParameter_old();
		}

		public static string ToParameter(this string str) {
			str = str.Replace("\"", "_");
			str = str.ToLower();
			return str;
		}

		public static bool HasMethod(this object objectToCheck, string methodName) {
			var type = objectToCheck.GetType();
			return type.GetMethod(methodName) != null;
		}
	}
}
