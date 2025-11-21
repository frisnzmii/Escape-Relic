using Myfirstprogramme;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace OOPSep25Week4
{
    internal class SensorData
    {
        /***** attributes/data/properties *****/

        // array of SensorReading objects
        public SensorReading[] temperature;

        public SensorData()
        {  // create an array of size 10 for temperature
            temperature = new SensorReading[10];
        }

        // constructor - 1 parameter: size of array
        public SensorData(int sz)
        {
            temperature = new SensorReading[sz];
        }


        /***** methods/operations/behaviors *****/
        public float calcAverage()
        {
            float avg = 0;
            for (int i = 0; i < 10; i++)
            {
                avg += temperature[i].GetVal();
            }
            avg /= 10;

            return avg;
        }

        public float[] getHigherTemperature(float threshold)
        {

            float[] results = new float[10];
            int num = 0;

            for (int i = 0; i < 10; i++)
            {
                if (temperature[i].GetTime() > threshold)
                    results[num++] = temperature[i].GetVal();
            }

            return results;
        }

        public void readData()
        {
            Console.WriteLine("Enter 10 temperature data: ");
            for (int i = 0; i < 10; i++)
            {
                string tempstr = Console.ReadLine();
                string timestr = Console.ReadLine();

                temperature[i] = new SensorReading(float.Parse(tempstr),
                                                    int.Parse(timestr));
            }
        }

    }
}