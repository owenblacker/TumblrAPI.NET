namespace TumblrAPI
{
	internal class FileParameter
	{
		public byte[] File { get; set; }
		public string FileName { get; set; }
		public string ContentType { get; set; }

		public FileParameter(byte[] file)
			: this(file, null) { }
		public FileParameter(byte[] file, string filename)
			: this(file, filename, null) { }

		public FileParameter(byte[] file, string filename, string contenttype)
		{
			this.File = file;
			this.FileName = filename;
			this.ContentType = contenttype;
		}
	}
}