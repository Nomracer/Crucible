namespace Crucible.Core
{
    /// <summary>
    /// Element ids. M1 ships three; the full set arrives with the reaction table in M2,
    /// at which point these constants are generated from the authoring data rather than
    /// hand-written.
    /// </summary>
    public static class Elements
    {
        public const byte Empty = 0;
        public const byte Sand = 1;
        public const byte Stone = 2;

        /// <summary>Number of ids currently in use. Palette tables are sized from this.</summary>
        public const int Count = 3;
    }
}
