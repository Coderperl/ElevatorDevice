using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ElevatorDevice.Models
{
    internal class ElevatorItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public DateTime LastInspection { get; set; }
        public DateTime NextInspection { get; set; }
        public string MaximumWeight { get; set; }
        public bool Reboot { get; set; }
        public bool ShutDown { get; set; }
        public bool Door { get; set; }
        public int Floor { get; set; }
        public string ElevatorStatus { get; set; }
    }
}
