﻿namespace RippleCalibration
{
    public static class Globals
    {
        public static bool setCoordinates = false;
        public static AppState appState;
        public static bool isvalueAssigned = false;

        public static double X1 = 0.00;
        public static double Y1 = 0.00;

        public static double X2 = 0.00;
        public static double Y2 = 0.00;

        public static double X3 = 0.00;
        public static double Y3 = 0.00;

        public static double X4 = 0.00;
        public static double Y4 = 0.00;

        public static double A1 = 0.00;
        public static double B1 = 0.00;

        public static double A2 = 0.00;
        public static double B2 = 0.00;

        public static double A3 = 0.00;
        public static double B3 = 0.00;

        public static double A4 = 0.00;
        public static double B4 = 0.00;

        public static double frontDistance = 0.00;
        public static double backDistance = 0.00;
        public static double leftDistance = 0.00;
        public static double rightDistance = 0.00;

        public static void ResetCoordinates()
        {
            X1 = 0.00;
            Y1 = 0.00;

            X2 = 0.00;
            Y2 = 0.00;

            X3 = 0.00;
            Y3 = 0.00;

            X4 = 0.00;
            Y4 = 0.00;

            setCoordinates = false;
            appState = AppState.firstCoordinate;
        }
    }
}
