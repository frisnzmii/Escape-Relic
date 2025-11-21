using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MultiSensors
{
    internal class SensorWind : SensorBase
    {
        List<(float speed,float direction)> wind;
        public SensorWind() { 

        wind = new List<(float speed,float direction)>();

        }
        public override void Read() { }

        public override float CalculateAverage() { return 0; }

        public override void GetHigher() { }
    }
}
