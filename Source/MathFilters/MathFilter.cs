namespace CrunchyDuck.Math.MathFilters {
	abstract class MathFilter {
		public abstract bool CanCount { get; }

		public abstract float Count();
		public abstract ReturnType Parse(string command, out object result);
	}

	enum ReturnType {
		Null,
		ThingFilter,
		ThingDefFilter,
		PawnFilter,
		Count,
	}
}
