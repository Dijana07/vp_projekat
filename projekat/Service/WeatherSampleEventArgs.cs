using Common;
using System;

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
