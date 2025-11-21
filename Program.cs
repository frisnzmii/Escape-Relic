using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSystem
{
    internal class File
    {
        public static int MAX_LGTH = 100;

        private string name;
        public string type { get; set; }
        public DateTime Created {  get; set; }
        public DateTime LastModified {  get; set; }
        public DateTime LastAccessed {  get; set; }
        public string Name
        {
            get
            { return name; }
            set
            {
                if (name.Length < File.MAX_LGTH)
                    name = value;
            }

        }
        public string Type
        {
            
            get { return name; }
        }

        public long Length { get; set; }
        public string Type { get; set; }
    }
}
