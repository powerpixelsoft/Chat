using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Utils
{
	public class FileLogger : ILogger
	{
		private readonly String filePath;
		private readonly String fileName;
		private readonly String folderPath;
		private readonly String backUpFolderPath;
		private String finalPath;
		private long fileSize;
		private Boolean isSizeSensitive;

		public FileLogger(String filePath, Boolean isSizeSensitive = false)
		{
			this.filePath = filePath;

			//get parts of the path
			String[] filePathParts = filePath.Split(new String[] { "\\" }, StringSplitOptions.RemoveEmptyEntries);
			this.fileName = filePathParts.Last();
			this.folderPath = filePath.TrimEnd(fileName.ToCharArray());
			this.backUpFolderPath = folderPath;
			this.isSizeSensitive = isSizeSensitive;
		}


		public void Write(String message, Exception exception = null)
		{
			//check if the path is valid
			Boolean isFolderPresent = System.IO.Directory.Exists(this.folderPath);

			//check permissions
			if (FileHandler.IsAllowedToWrite(this.folderPath))
			{
				if (!isFolderPresent)
				{
					//create folder
					System.IO.Directory.CreateDirectory(this.folderPath);
				}

				//set final path
				finalPath = System.IO.Path.Combine(this.folderPath, this.fileName);
			}
			else
			{
				finalPath = System.IO.Path.Combine(this.backUpFolderPath, this.fileName);
			}

			//compose log message
			String timeStamp = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}";

			StringBuilder exceptionInfoBuilder = null;
			String exceptionMessage = String.Empty;

			if(exception != null)
			{
				List<Exception> exceptions = new List<Exception>() { exception };
				exceptionInfoBuilder = new StringBuilder();

				//exctract inner exc
				ExtractInnerExceptions(exception, exceptions);

				int n = 0;
				foreach (Exception exc in exceptions)
				{
					String excMessage = exc.Message;
					String excStackTrace = exc.StackTrace;

					exceptionInfoBuilder.AppendLine($" > Exception [lvl: { n-- }]: { exc.GetType().Name }");
					exceptionInfoBuilder.AppendLine($"   Message: { excMessage }");
					exceptionInfoBuilder.AppendLine($"   StackTrace: { excStackTrace }");
				}

				exceptionMessage = exceptionInfoBuilder.ToString();
			}

			message = $" > Message: { message }";

			//final message
			String finalMessage = String.Format
			(
				LoggerData.messageFormat,
				Environment.NewLine,
				timeStamp,
				message,
				exceptionMessage
			);

			try
			{
				//check file
				if (!System.IO.File.Exists(finalPath))
				{
					System.IO.FileStream stream = System.IO.File.Create(finalPath);
					stream.Close();
					stream.Dispose();
				}


				//cleanup and archive if exeeds allowed length
				if (isSizeSensitive)
				{
					using (System.IO.FileStream stream = System.IO.File.OpenRead(finalPath))
					{
						fileSize = stream.Length;
					}

					if (fileSize > 4000000)
					{
						using
						(
							System.IO.FileStream fStreamCreate =
								System.IO.File.Create(finalPath.TrimEnd('.', 't', 'x', 't') + "_" +
								                      DateTime.Now.ToString("yy_MM_dd_HH_mm_ss") + ".txt.gz")
						)
						{
							//read text into a temp file
							String temp = System.IO.File.ReadAllText(finalPath);

							//get bytes from the text
							byte[] bytes = new byte[Encoding.ASCII.GetByteCount(temp)];
							bytes = Encoding.ASCII.GetBytes(temp);

							//create zipped file
							using (GZipStream zipStream = new GZipStream(fStreamCreate, CompressionMode.Compress))
								zipStream.Write(bytes, 0, bytes.Length);

							//clean existing log file
							System.IO.File.WriteAllText(finalPath, String.Empty);
						}
					}
				}

				//write
				using (System.IO.StreamWriter stream = System.IO.File.AppendText(finalPath))
				{
					stream.Write(finalMessage);
				}
			}
			catch (Exception e)
			{
				Console.Write(e);
			}
		}

		public void WriteAsync(String message, Exception exception = null)
		{
			
		}

		private void ExtractInnerExceptions(Exception exception, List<Exception> exceptions)
		{
			if (exception.InnerException != null)
			{
				exceptions.Add(exception.InnerException);
				ExtractInnerExceptions(exception.InnerException, exceptions);
			}
		}
	}
}
