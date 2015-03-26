﻿using System.Drawing;
using System.Drawing.Imaging;
using com.google.zxing;
using com.google.zxing.qrcode;
using System.Collections;
using System;
using System.Windows;
using com.google.zxing.qrcode.decoder;

namespace RippleCommonUtilities
{
    public static class HelperMethods
    {
        public static Bitmap GenerateQRCode(string URL)
        {
            var writer = new QRCodeWriter();
            var hints = new Hashtable();

            hints.Add(EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.M);
            hints.Add("Version", "7");
            var byteIMGNew = writer.encode(URL, BarcodeFormat.QR_CODE, 350, 350, hints);
            var imgNew = byteIMGNew.Array;
            var bmp1 = new Bitmap(byteIMGNew.Width, byteIMGNew.Height);
            var g1 = Graphics.FromImage(bmp1);
            g1.Clear(Color.White);
            for (var i = 0; i <= imgNew.Length - 1; i++)
            {
                for (var j = 0; j <= imgNew[i].Length - 1; j++)
                {
                    if (imgNew[j][i] == 0)
                    {
                        g1.FillRectangle(Brushes.Black, i, j, 1, 1);
                    }
                    else
                    {
                        g1.FillRectangle(Brushes.White, i, j, 1, 1);
                    }
                }
            }
            return bmp1;
        }

        public static void GenerateQRCode(string URL, String TargetPath)
        {
            var writer = new QRCodeWriter();
            var hints = new Hashtable();

            hints.Add(EncodeHintType.ERROR_CORRECTION, ErrorCorrectionLevel.M);
            hints.Add("Version", "7");
            var byteIMGNew = writer.encode(URL, BarcodeFormat.QR_CODE, 350, 350, hints);
            var imgNew = byteIMGNew.Array;
            var bmp1 = new Bitmap(byteIMGNew.Width, byteIMGNew.Height);
            var g1 = Graphics.FromImage(bmp1);
            g1.Clear(Color.White);
            for (var i = 0; i <= imgNew.Length - 1; i++)
            {
                for (var j = 0; j <= imgNew[i].Length - 1; j++)
                {
                    if (imgNew[j][i] == 0)
                    {
                        g1.FillRectangle(Brushes.Black, i, j, 1, 1);
                    }
                    else
                    {
                        g1.FillRectangle(Brushes.White, i, j, 1, 1);
                    }
                }
            }
            bmp1.Save(TargetPath, ImageFormat.Jpeg);
        }

        public static void ClickOnFloorToGetFocus()
        {
            var middleWidth = Convert.ToInt32(Math.Floor((double)((int)SystemParameters.PrimaryScreenWidth / 2)));
            var middleHeight = Convert.ToInt32(Math.Floor((double)((int)SystemParameters.PrimaryScreenHeight / 2)));
            OSNativeMethods.SendMouseInput(middleWidth, middleHeight, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, true);
            OSNativeMethods.SendMouseInput(middleWidth, middleHeight, (int)SystemParameters.PrimaryScreenWidth, (int)SystemParameters.PrimaryScreenHeight, false);
        }
    }
}
