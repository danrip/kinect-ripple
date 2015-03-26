using System;
using Microsoft.Office.Core;
using System.IO;
using Microsoft.Office.Interop.PowerPoint;
using RippleCommonUtilities;
using Shape = Microsoft.Office.Interop.PowerPoint.Shape;

namespace RippleScreenApp.Utilities
{
    public static class PrinterHelper
    {
        public static void PrintDiscountCoupon(string companyName, string discountProductName, string discountValueOnProduct)
        {
            var copiesToPrint = 1;
            var printTemplateFileName = @"\Assets\Docs\PrinterReceipt.pptx";
            var qrCodeImageName = @"\Assets\Images\QREncode.jpg";
            var printFileName = @"\Assets\Docs\printReceipt.pptx";
            var printTemplateFilePath = Helper.GetAssetURI(printTemplateFileName);
            var qrCodeImagepath = Helper.GetAssetURI(qrCodeImageName);
            var printReceiptFilePath = Helper.GetAssetURI(printFileName);
            Presentation work = null;
            var app = new Application();

            try
            {
                if (File.Exists(printReceiptFilePath))
                {
                    File.Delete(printReceiptFilePath);
                }
                if (File.Exists(qrCodeImagepath))
                {
                    File.Delete(qrCodeImagepath);
                }

                var presprint = app.Presentations;
                work = presprint.Open(printTemplateFilePath, MsoTriState.msoCTrue, MsoTriState.msoCTrue, MsoTriState.msoFalse);
                work.PrintOptions.PrintInBackground = MsoTriState.msoFalse;
                var slide = work.Slides[1];
                foreach (var item in slide.Shapes)
                {
                    var shape = (Shape)item;

                    if (shape.HasTextFrame == MsoTriState.msoTrue)
                    {
                        if (shape.TextFrame.HasText == MsoTriState.msoTrue)
                        {
                            var textRange = shape.TextFrame.TextRange;
                            var text = textRange.Text;
                            if (text.Contains("10%"))
                            {
                                text = text.Replace("10", discountValueOnProduct);
                                shape.TextFrame.TextRange.Text = text;
                            }
                            else if (text.Contains("Microsoft"))
                            {
                                text = text.Replace("Microsoft", companyName);
                                shape.TextFrame.TextRange.Text = text;
                            }
                            else if (text.Contains("Windows Phone 8"))
                            {
                                text = text.Replace("Windows Phone 8", discountProductName);
                                shape.TextFrame.TextRange.Text = text;
                            }

                        }
                    }
                    else
                    {
                        if (shape.Name.ToString() == "Picture 2")
                        {
                            shape.Delete();
                            //Add QRCode to print
                            HelperMethods.GenerateQRCode("http://projectripple.azurewebsites.net/Ripple.aspx", qrCodeImagepath);
                            slide.Shapes.AddPicture(qrCodeImagepath, MsoTriState.msoFalse, MsoTriState.msoTrue, 560, 90, 80, 80);
                        }
                    }
                }

                work.SaveAs(printReceiptFilePath);
                work.PrintOut();
                work.Close();
                app.Quit();

                //delete the PrintReceipt File
                File.Delete(printReceiptFilePath);
                //Delete the QRCOde image
                File.Delete(qrCodeImagepath);
            }
            catch (Exception ex)
            {
                work.Close();
                app.Quit();
                //delete the PrintReceipt File
                File.Delete(printReceiptFilePath);
                //Delete the QRCOde image
                File.Delete(qrCodeImagepath);
                LoggingHelper.LogTrace(1, "Went wrong in  Print Discount Coupon at Screen side: {0}", ex.Message);
            }
        }
    }
}
