using System;
using System.IO;

namespace WhiteCow.Log
{
    /// <summary>
    /// logging class
    /// </summary>
    public sealed class Logger
    {
		private static volatile Logger instance;
		private static object syncRoot = new Object();


        private Logger()
        {
        }

        public static Logger Instance
        {
			get
			{
				if (instance == null)
				{
					lock (syncRoot)
					{
						if (instance == null)
                            instance = new Logger();
					}
				}

				return instance;
			}
        }

        private void LogToFile(String Message)
        {
            if (!Directory.Exists("Log"))
               Directory.CreateDirectory("Log");

            File.AppendAllText($"Log\\{DateTime.Now.ToString("yyyyMMdd")}_Log.txt",String.Concat(
                DateTime.Now," ",Message,Environment.NewLine));
        }

        public void LogError(Exception ex)
        {
            LogError(ex.ToString());
        }

        public void LogError(String Message)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(String.Concat(DateTime.Now," Error ",Message));
            Console.ForegroundColor = ConsoleColor.White;
            LogToFile(Message.Replace(Environment.NewLine," "));

        }


        public void LogWarning(String Message)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
			Console.WriteLine(String.Concat(DateTime.Now, " Warning ", Message));
			Console.ForegroundColor = ConsoleColor.White;
        }

        public void LogInfo(String Message)
        {
			Console.WriteLine(String.Concat(DateTime.Now, " Info ", Message));
        }
    }
}
