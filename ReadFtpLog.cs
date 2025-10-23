using Tsg.Sys;

namespace ReadFtpDirectory
{
    public class ReadFtpLog : DefaultLog
    {
		protected override string LogFileName
		{
			get
			{
				return "ReadFtp.log";
			}
		}

		public new static ReadFtpLog Manager
		{
			get
			{
				return (ReadFtpLog)Logger.Manager[typeof(ReadFtpLog)];
			}
		}
	}
}
