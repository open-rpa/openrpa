using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Image
{
    public class ocr
    {
        public static void TesseractDownloadLangFile(String folder, String lang)
        {
            if (!System.IO.Directory.Exists(folder))
            {
                System.IO.Directory.CreateDirectory(folder);
            }
            string dest = System.IO.Path.Combine(folder, String.Format("{0}.traineddata", lang));
            if (!System.IO.File.Exists(dest))
                using (System.Net.WebClient webclient = new System.Net.WebClient())
                {
                    // string source = string.Format("https://github.com/tesseract-ocr/tessdata/blob/4592b8d453889181e01982d22328b5846765eaad/{0}.traineddata?raw=true", lang);
                    string source = string.Format("https://github.com/tesseract-ocr/tessdata/blob/master/{0}.traineddata?raw=true", lang);
                    System.Diagnostics.Trace.WriteLine(String.Format("Downloading file from '{0}' to '{1}'", source, dest));
                    webclient.DownloadFile(source, dest);
                    System.Diagnostics.Trace.WriteLine(String.Format("Download completed"));
                }
        }

        public static string OcrImage(Emgu.CV.OCR.Tesseract _ocr, Mat image)
        {
            //Bgr drawCharColor = new Bgr(Color.Red);
            var imageColor = new Mat();
            if (image.NumberOfChannels == 1)
                CvInvoke.CvtColor(image, imageColor, ColorConversion.Gray2Bgr);
            else
                image.CopyTo(imageColor);
            _ocr.SetImage(imageColor);
            _ocr.AnalyseLayout();

            if (_ocr.Recognize() != 0) throw new Exception("Failed to recognizer image");
            Emgu.CV.OCR.Tesseract.Character[] characters = _ocr.GetCharacters();
            if (characters.Length == 0)
            {
                try
                {
                    Mat imgGrey = new Mat();
                    CvInvoke.CvtColor(image, imgGrey, ColorConversion.Bgr2Gray);
                    Mat imgThresholded = new Mat();
                    CvInvoke.Threshold(imgGrey, imgThresholded, 65, 255, ThresholdType.Binary);
                    _ocr.SetImage(imgThresholded);
                    characters = _ocr.GetCharacters();
                    imageColor = imgThresholded;
                    if (characters.Length == 0)
                    {
                        CvInvoke.Threshold(image, imgThresholded, 190, 255, ThresholdType.Binary);
                        _ocr.SetImage(imgThresholded);
                        characters = _ocr.GetCharacters();
                        imageColor = imgThresholded;
                    }
                    imgGrey.Dispose();
                    imgThresholded.Dispose();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Trace.WriteLine(ex.ToString());
                    return null;
                }
            }
            return _ocr.GetUTF8Text();
        }
    }
}
