namespace CrunchyDuck.Math.MathFilters {
	public abstract class MathFilter {
		public abstract bool CanCount { get; }

		public abstract float Count();

		public abstract ReturnType Parse(string command, out object result);

		//public abstract ReturnType ParseType(string command);
	}

	public enum ReturnType {
		Null,
		ThingFilter,
		ThingDefFilter,
		PawnFilter,
		Count,
	}
}
