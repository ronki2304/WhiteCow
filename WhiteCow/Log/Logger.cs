using System;
using System.IO;
using System.Collections;
namespace WhiteCow.Log
{
    /// <summary>
    /// logging class
    /// </summary>
    public sealed class Logger
    {
		private static volatile Logger instance;
		private static object syncRoot = new Object();
        private Queue msgQueue;

        private Logger()
        {
            msgQueue = new Queue();
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

        private void LogErrorToFile(String Message)
        {
            if (!Directory.Exists("Log"))
               Directory.CreateDirectory("Log");

            //write last log before the error
            while (msgQueue.Count!=0)
				File.AppendAllText(Path.Combine("ErrorLog", $"{DateTime.Now.ToString("yyyyMMdd")}_Log.txt"), String.Concat(
                    DateTime.Now, " ", msgQueue.Dequeue(), Environment.NewLine));


            File.AppendAllText(Path.Combine("ErrorLog",$"{DateTime.Now.ToString("yyyyMMdd")}_Log.txt"),String.Concat(
                DateTime.Now," ",Message,Environment.NewLine));
        }

        public void LogError(Exception ex)
        {
            LogError(ex.ToString());
        }

        public void LogError(String Message)
        {
            Message = String.Concat(DateTime.Now, " Error ", Message);
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(Message);
            Console.ForegroundColor = ConsoleColor.White;
            LogErrorToFile(Message.Replace(Environment.NewLine," "));
                        addElement(Message);
            LogToFile(Message);

        }

        void LogToFile(String Message)
        {
			if (!Directory.Exists("Log"))
				Directory.CreateDirectory("Log");
			File.AppendAllText(Path.Combine("Log", $"{DateTime.Now.ToString("yyyyMMdd")}_Log.txt"), String.Concat(
			   DateTime.Now, " ", Message, Environment.NewLine));
        }
        public void LogWarning(String Message)
        {
            Message = String.Concat(DateTime.Now, " Warning ", Message);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(Message);
            addElement(Message);
			Console.ForegroundColor = ConsoleColor.White;
            LogToFile(Message);
        }

        public void LogInfo(String Message)
        {
            Message = String.Concat(DateTime.Now, " Info ", Message);
			Console.WriteLine(Message); 
            addElement(Message);
            LogToFile(Message);

		}

        /// <summary>
        /// add eleet to the queue limited to 10
        /// </summary>
        /// <param name="message">Message.</param>
        void addElement(String message)
        {
            msgQueue.Enqueue(message);
            if (msgQueue.Count > 10)
                msgQueue.Dequeue();
        }
    }
}
