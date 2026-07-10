// Keepning namespace to have SOS 2 assembly follow conventions
namespace SaveOurShip2
{
	// Interop with external tools via specially defined log lines
	public static class LogInterop
	{
		public const string AntiBoardingFullyGeneratedPattern = "Anti-boarding defenses fully generated for ship";
		public static readonly string AntiBoardingFullyGenerated = AntiBoardingFullyGeneratedPattern + ": {0}";

		public const string AntiBoardingPartiallyGeneratedPattern = "Anti-boarding defenses partially generated for ship";
		public static readonly string AntiBoardingPartiallyGenerated = AntiBoardingPartiallyGeneratedPattern + ": {0}";

		public const string AntiBoardingNotGeneratedPattern = "Anti-boarding defenses not generated for ship";
		public static readonly string AntiBoardingNotGenerated = AntiBoardingNotGeneratedPattern + ": {0}";

		// This isn't the best name, but that's how it used to be logged for years.
		public const string LostShipBattle = "Lost ship battle!";
	}
}

