namespace MHPatcher
{
    public class Patch
    {
        public string Name { get; init; }
        public bool IsEnabled { get; set; }
        public string Offset { get; init; }
        public string Data { get; init; }

        /// <summary>
        /// Optional. Patches that share a group depend on each other and must be applied together.
        /// </summary>
        public string Group { get; init; }

        public int OffsetValue => Convert.ToInt32(Offset, 16);
        public byte[] DataBytes => Convert.FromHexString(Data);
    }
}
