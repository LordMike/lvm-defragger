namespace ConsoleApp11
{
    class Segment
    {
        public SegmentType Type { get; set; }

        public string LV { get; set; }

        public uint LVStart { get; set; }

        public string Device { get; set; }

        public uint DeviceOffset { get; set; }

        public uint Size { get; set; }

        public override string ToString()
        {
            if (Type == SegmentType.Free)
                return $"{Type} :: {Device}#{DeviceOffset}+{Size}";

            return $"{Type} :: {Device}#{DeviceOffset}+{Size} {LV}#{LVStart}";
        }
    }
}