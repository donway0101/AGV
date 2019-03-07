using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AgvControlSystem
{
    /// <summary>
    /// Message come in dispatcher, created a mission,
    /// 
    /// </summary>
    public struct AgvCommand
    {
        /// <summary>
        /// Need to + ";"
        /// </summary>
        public string RawMessage { get; set; }
        public int SenderPort { get; set; }
        public string MessageId { get; set; }
        public CommandType CommandType { get; set; }
        public AgvId AgvId { get; set; }
        public string[] Arg { get; set; }
        public DateTime CreatedTime { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
    }
}
