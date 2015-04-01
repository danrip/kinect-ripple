using System;

namespace RippleDictionary
{
    public class CalibrationConfiguration
    {
        public CalibrationConfiguration(string tFrontDistance, string tBackDistance, string tLeftDistance, string tRightDistance, string tPrimaryScreenWidth, string tPrimaryScreenHeight)
        {
            FrontDistance = Convert.ToDouble(tFrontDistance);
            BackDistance = Convert.ToDouble(tBackDistance);
            LeftDistance = Convert.ToDouble(tLeftDistance);
            RightDistance = Convert.ToDouble(tRightDistance);
            PrimaryScreenWidth = Convert.ToDouble(tPrimaryScreenWidth);
            PrimaryScreenHeight = Convert.ToDouble(tPrimaryScreenHeight);
        }

        public double FrontDistance { get; set; }

        public double BackDistance { get; set; }

        public double LeftDistance { get; set; }

        public double RightDistance { get; set; }

        public double PrimaryScreenWidth { get; set; }

        public double PrimaryScreenHeight { get; set; }
    }
}
