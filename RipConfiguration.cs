namespace DriveRipper
{
    public class MakeMkv
    {
        public string Path { get; set; }
    }

    public class Handbrake
    {
        public string Path { get; set; }
    }

    public class RipConfiguration
    {
        public string ThisPc { get; set; }
        public string RipDirectory { get; set; }
        public string OutputDirectory { get; set; }
        public MakeMkv MakeMkv { get; set; }
        public Handbrake Handbrake { get; set; }
    }
}
