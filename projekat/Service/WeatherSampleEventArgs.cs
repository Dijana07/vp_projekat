using Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service
{
    public class WeatherSampleEventArgs : EventArgs
    {
        public WeatherSample Sample { get; }

        public WeatherSampleEventArgs(WeatherSample sample)
        {
            Sample = sample;
        }
    }
}
