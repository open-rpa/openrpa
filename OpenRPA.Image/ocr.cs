using Emgu.CV;
using Emgu.CV.CvEnum;
using OpenRPA.Interfaces;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

        public static string OcrImage(Emgu.CV.OCR.Tesseract _ocr, Emgu.CV.Mat image)
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
                    Log.Error(ex.ToString());
                    return null;
                }
            }
            return _ocr.GetUTF8Text();
        }
        public static ImageElement[] OcrImage2(Emgu.CV.OCR.Tesseract _ocr, Emgu.CV.Mat image)
        {
            var result = new List<ImageElement>();
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
            //        Log.Error(ex.ToString());
            //        return null;
            //    }
            //}

            // var text = _ocr.GetUTF8Text();
            var index = 0;
            var chars = new List<Emgu.CV.OCR.Tesseract.Character>();
            while (index < characters.Length)
            {
                if (characters[index].Text == " " || characters[index].Text == "\r" || characters[index].Text == "\n")
                {
                    if(chars.Count > 0 )
                    {
                        var res = new ImageElement(Rectangle.Empty);
                        chars.ForEach(x => res.Text += x.Text);
                        res.Confidence = chars[0].Cost;
                        Rectangle rect = new Rectangle(chars[0].Region.X, chars[0].Region.Y, chars[0].Region.Width, chars[0].Region.Height);
                        rect.Width = (chars[chars.Count - 1].Region.X - chars[0].Region.X) + chars[chars.Count - 1].Region.Width;
                        rect.Height = (chars[chars.Count - 1].Region.Y - chars[0].Region.Y) + chars[chars.Count - 1].Region.Height;

                        res.Rectangle = rect;

                        // res.Rectangle = rect; //  new Rectangle(chars[0].Region.X, chars[0].Region.Y, chars[word.Length - 1].Region.Width, chars[word.Length - 1].Region.Height);
                        result.Add(res);

                    }
                    index++;
                    chars.Clear();
                    continue;
                }
                chars.Add(characters[index]);
                index++;
            }
            if (chars.Count > 0)
            {
                var res = new ImageElement(Rectangle.Empty);
                chars.ForEach(x => res.Text += x.Text);
                res.Confidence = chars[0].Cost;
                Rectangle rect = new Rectangle(chars[0].Region.X, chars[0].Region.Y, chars[0].Region.Width, chars[0].Region.Height);
                rect.Width = (chars[chars.Count - 1].Region.X - chars[0].Region.X) + chars[chars.Count - 1].Region.Width;
                rect.Height = (chars[chars.Count - 1].Region.Y - chars[0].Region.Y) + chars[chars.Count - 1].Region.Height;

                res.Rectangle = rect;

                // res.Rectangle = rect; //  new Rectangle(chars[0].Region.X, chars[0].Region.Y, chars[word.Length - 1].Region.Width, chars[word.Length - 1].Region.Height);
                result.Add(res);

            }

            //foreach (var word in text.Split(' '))
            //{

            //    for(var i = index; chars.Count < word.Length; i++)
            //    {
            //        chars.Add(characters[index]);
            //        index++;
            //    }
            //    var res = new textcomponent();
            //    res.Text = word;
            //    res.Confidence = chars[0].Cost;

            //    index++; // for the space
            //}
            // result.ForEach(x => Console.WriteLine(x));
            return result.ToArray();
        }
        //public static textcomponent[] GetTextcomponents(string tessdata, string lang, System.Drawing.Bitmap img)
        //{
        //    img = SetGrayscale(img);
        //    img = RemoveNoise(img);

        //    var result = new List<textcomponent>();
        //    OpenCvSharp.Rect[] textLocations = null;
        //    string[] componentTexts = null;
        //    float[] confidences = null;
        //    using (var engine = OpenCvSharp.Text.OCRTesseract.Create(tessdata, lang))
        //    {
        //        var org = OpenCvSharp.Extensions.BitmapConverter.ToMat(img);
        //        // var org = OpenCvSharp.Extensions.BitmapConverter.ToMat(img);
        //        var _img = org.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY); // GRAY2BGR
        //        // if(__img.Type() != OpenCvSharp.MatType.CV_8UC1 && __img.Type() != OpenCvSharp.MatType.CV_8UC3)
        //        org.ImWrite(@"c:\temp\dump-org.png");
        //        _img.ImWrite(@"c:\temp\dump-img.png");
        //        string stringresult = null;
        //        engine.Run(_img, out stringresult, out textLocations, out componentTexts, out confidences, OpenCvSharp.Text.ComponentLevels.Word);
        //    }
        //    for (var i = 0; i < textLocations.Length; i++)
        //    {
        //        result.Add(new ImageElement(textLocations[i], componentTexts[i].Trim(), confidences[i]));
        //    }
        //    result.ForEach(x => Console.WriteLine(x));
        //    return result.ToArray();
        //}
        //public static ImageElement[] GetTextcomponents(string tessdata, string lang, string filename)
        //{
        //    var result = new List<ImageElement>();
        //    OpenCvSharp.Rect[] textLocations = null;
        //    string[] componentTexts = null;
        //    float[] confidences = null;
        //    using (var engine = OpenCvSharp.Text.OCRTesseract.Create(tessdata, lang))
        //    {
        //        var _img = OpenCvSharp.Cv2.ImRead(filename);

        //        // var org = OpenCvSharp.Extensions.BitmapConverter.ToMat(img);
        //        // var _img = org.CvtColor(OpenCvSharp.ColorConversionCodes.GRAY2BGR);
        //        // if(__img.Type() != OpenCvSharp.MatType.CV_8UC1 && __img.Type() != OpenCvSharp.MatType.CV_8UC3)
        //        // org.ImWrite(@"c:\temp\dump-org.png");
        //        _img.ImWrite(@"c:\temp\dump-img.png");
        //        string stringresult = null;
        //        engine.Run(_img, out stringresult, out textLocations, out componentTexts, out confidences, OpenCvSharp.Text.ComponentLevels.Word);
        //    }
        //    for (var i = 0; i < textLocations.Length; i++)
        //    {
        //        result.Add(new textcomponent(textLocations[i], componentTexts[i].Trim(), confidences[i]));
        //    }
        //    result.ForEach(x => Console.WriteLine(x));
        //    return result.ToArray();
        //}






        //Resize
        public static  Bitmap Resize(Bitmap bmp, int newWidth, int newHeight)
        {

            Bitmap temp = (Bitmap)bmp;

            Bitmap bmap = new Bitmap(newWidth, newHeight, temp.PixelFormat);

            double nWidthFactor = (double)temp.Width / (double)newWidth;
            double nHeightFactor = (double)temp.Height / (double)newHeight;

            double fx, fy, nx, ny;
            int cx, cy, fr_x, fr_y;
            Color color1 = new Color();
            Color color2 = new Color();
            Color color3 = new Color();
            Color color4 = new Color();
            byte nRed, nGreen, nBlue;

            byte bp1, bp2;

            for (int x = 0; x < bmap.Width; ++x)
            {
                for (int y = 0; y < bmap.Height; ++y)
                {

                    fr_x = (int)Math.Floor(x * nWidthFactor);
                    fr_y = (int)Math.Floor(y * nHeightFactor);
                    cx = fr_x + 1;
                    if (cx >= temp.Width) cx = fr_x;
                    cy = fr_y + 1;
                    if (cy >= temp.Height) cy = fr_y;
                    fx = x * nWidthFactor - fr_x;
                    fy = y * nHeightFactor - fr_y;
                    nx = 1.0 - fx;
                    ny = 1.0 - fy;

                    color1 = temp.GetPixel(fr_x, fr_y);
                    color2 = temp.GetPixel(cx, fr_y);
                    color3 = temp.GetPixel(fr_x, cy);
                    color4 = temp.GetPixel(cx, cy);

                    // Blue
                    bp1 = (byte)(nx * color1.B + fx * color2.B);

                    bp2 = (byte)(nx * color3.B + fx * color4.B);

                    nBlue = (byte)(ny * (double)(bp1) + fy * (double)(bp2));

                    // Green
                    bp1 = (byte)(nx * color1.G + fx * color2.G);

                    bp2 = (byte)(nx * color3.G + fx * color4.G);

                    nGreen = (byte)(ny * (double)(bp1) + fy * (double)(bp2));

                    // Red
                    bp1 = (byte)(nx * color1.R + fx * color2.R);

                    bp2 = (byte)(nx * color3.R + fx * color4.R);

                    nRed = (byte)(ny * (double)(bp1) + fy * (double)(bp2));

                    bmap.SetPixel(x, y, System.Drawing.Color.FromArgb
            (255, nRed, nGreen, nBlue));
                }
            }



            bmap = SetGrayscale(bmap);
            bmap = RemoveNoise(bmap);

            return bmap;

        }


        //SetGrayscale
        public static Bitmap  SetGrayscale(Bitmap img)
        {

            Bitmap temp = (Bitmap)img;
            Bitmap bmap = (Bitmap)temp.Clone();
            Color c;
            for (int i = 0; i < bmap.Width; i++)
            {
                for (int j = 0; j < bmap.Height; j++)
                {
                    c = bmap.GetPixel(i, j);
                    byte gray = (byte)(.299 * c.R + .587 * c.G + .114 * c.B);

                    bmap.SetPixel(i, j, Color.FromArgb(gray, gray, gray));
                }
            }
            return (Bitmap)bmap.Clone();

        }
        //RemoveNoise
        public static Bitmap RemoveNoise(Bitmap bmap)
        {

            for (var x = 0; x < bmap.Width; x++)
            {
                for (var y = 0; y < bmap.Height; y++)
                {
                    var pixel = bmap.GetPixel(x, y);
                    if (pixel.R < 162 && pixel.G < 162 && pixel.B < 162)
                        bmap.SetPixel(x, y, Color.Black);
                    else if (pixel.R > 162 && pixel.G > 162 && pixel.B > 162)
                        bmap.SetPixel(x, y, Color.White);
                }
            }

            return bmap;
        }

    }
    //public class textcomponent
    //{
    //    public textcomponent() { }
    //    public textcomponent(OpenCvSharp.Rect rect, string text, float confidence) {
    //        Rectangle = new System.Drawing.Rectangle(rect.X, rect.Y, rect.Width, rect.Height);
    //        Text = text;
    //        Confidence = confidence;
    //    }
    //    public System.Drawing.Rectangle Rectangle { get; set; }
    //    public string Text { get; set; }
    //    public float Confidence { get; set; }
    //    public override string ToString()
    //    {
    //        // return Rectangle.ToString() + "/" + Confidence + " " + Text;
    //        return Rectangle.ToString() + " " + Text;
    //    }
    //}



}
