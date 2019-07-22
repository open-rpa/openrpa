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
        //static List<Rect> RunTextRecog(string inFile)
        //{
        //    List<Rect> boundRect = new List<Rect>();
        //    using (Mat img = new Mat(inFile))
        //    using (Mat img_gray = new Mat())
        //    using (Mat img_sobel = new Mat())
        //    using (Mat img_threshold = new Mat())
        //    {
        //        Cv2.CvtColor(img, img_gray, ColorConversionCodes.BGR2GRAY);
        //        Cv2.Sobel(img_gray, img_sobel, MatType.CV_8U, 1, 0, 3, 1, 0, BorderTypes.Default);
        //        Cv2.Threshold(img_sobel, img_threshold, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.Binary);
        //        using (Mat element = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(10, 15)))
        //        {
        //            Cv2.MorphologyEx(img_threshold, img_threshold, MorphTypes.Close, element);
        //            Point[][] edgesArray = img_threshold.Clone().FindContoursAsArray(RetrievalModes.External, ContourApproximationModes.ApproxNone);
        //            foreach (Point[] edges in edgesArray)
        //            {
        //                Point[] normalizedEdges = Cv2.ApproxPolyDP(edges, 17, true);
        //                Rect appRect = Cv2.BoundingRect(normalizedEdges);
        //                boundRect.Add(appRect);
        //            }
        //        }
        //    }
        //    return boundRect;
        //}
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
        public static Emgu.CV.OCR.Tesseract.Character[] OcrImageCharacters(Emgu.CV.OCR.Tesseract _ocr, Mat image)
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
            //if (characters.Length == 0)
            //{
            //    try
            //    {
            //        Mat imgGrey = new Mat();
            //        CvInvoke.CvtColor(image, imgGrey, ColorConversion.Bgr2Gray);
            //        Mat imgThresholded = new Mat();
            //        CvInvoke.Threshold(imgGrey, imgThresholded, 65, 255, ThresholdType.Binary);
            //        _ocr.SetImage(imgThresholded);
            //        characters = _ocr.GetCharacters();
            //        imageColor = imgThresholded;
            //        if (characters.Length == 0)
            //        {
            //            CvInvoke.Threshold(image, imgThresholded, 190, 255, ThresholdType.Binary);
            //            _ocr.SetImage(imgThresholded);
            //            characters = _ocr.GetCharacters();
            //            imageColor = imgThresholded;
            //        }
            //        imgGrey.Dispose();
            //        imgThresholded.Dispose();
            //    }
            //    catch (Exception ex)
            //    {
            //        System.Diagnostics.Trace.WriteLine(ex.ToString());
            //        return null;
            //    }
            //}
            return characters;
        }
    }
}
