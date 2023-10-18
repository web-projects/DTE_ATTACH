using System;
using System.Collections.Generic;

namespace DTE_ATTACHER.Config
{
    [Serializable]
    internal class AppConfig
    {
        public Application Application { get; set; }
        public List<ProcessesList> Processes { get; set; }
    }
}
