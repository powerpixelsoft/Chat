using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat.Utils
{
	public interface ILogger
	{
		void Write(String message, Exception exception = null);
		void WriteAsync(String message, Exception exception = null);
	}
}
