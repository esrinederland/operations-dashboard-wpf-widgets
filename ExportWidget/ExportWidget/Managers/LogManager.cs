using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExportWidget.Managers
{
    public class LogManager
    {
        private string Dir { get; set; }

        private string FilePath { get; set; }

        public LogManager(string directory)
        {
            this.Dir = directory;

            this.CreateLogFile();
        }

        private void CreateLogFile() 
        {
            string filePath = string.Format("{0}\\export_log.txt", Dir);
            
            if (!File.Exists(filePath))
            {
                File.Create(filePath).Close();

                this.FilePath = filePath;
            }

            IList<string> lines = new List<string>();

            lines.Add(string.Format("{0} - Logfile created", DateTime.Now));

            lines.Add(System.Environment.NewLine);

            File.AppendAllLines(filePath, lines);
        }

        public void UpdateLog(string message) 
        {
            IList<string> lines = new List<string>();

            lines.Add(string.Format("{0} - {1}", DateTime.Now, message));

            lines.Add(System.Environment.NewLine);

            File.AppendAllLines(this.FilePath, lines);
        }
    }
}
