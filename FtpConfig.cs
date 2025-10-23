namespace ReadFtpDirectory
{
    public class FtpConfig
    {
        public string Url { get; set; }
        public string User { get; set; }
        public string Pass { get; set; }
        public int TimeOut { get; set; }
        public string TargetDirectory { get; set; }
    }
}
