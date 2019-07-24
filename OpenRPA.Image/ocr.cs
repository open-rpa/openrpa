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
        public static ImageElement[] OcrImage2(Emgu.CV.OCR.Tesseract _ocr, Emgu.CV.Mat image, string wordlimit)
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
            var index = 0;
            var wordlimitindex = 0;
            var chars = new List<Emgu.CV.OCR.Tesseract.Character>();
            var result = new List<ImageElement>();
            var wordresult = new List<ImageElement>();
            var wordchars = new List<Emgu.CV.OCR.Tesseract.Character>();
            while (index < characters.Length)
            {
                if(!string.IsNullOrEmpty(wordlimit))
                {
                    if(characters[index].Text == wordlimit[wordlimitindex].ToString())
                    {
                        wordchars.Add(characters[index]);
                        wordlimitindex++;
                        if (wordchars.Count == wordlimit.Length)
                        {
                            var res = new ImageElement(Rectangle.Empty);
                            wordchars.ForEach(x => res.Text += x.Text);
                            res.Confidence = wordchars[0].Cost;
                            Rectangle rect = new Rectangle(wordchars[0].Region.X, wordchars[0].Region.Y, wordchars[0].Region.Width, wordchars[0].Region.Height);
                            rect.Width = (wordchars[wordchars.Count - 1].Region.X - wordchars[0].Region.X) + wordchars[wordchars.Count - 1].Region.Width;
                            rect.Height = (wordchars[wordchars.Count - 1].Region.Y - wordchars[0].Region.Y) + wordchars[wordchars.Count - 1].Region.Height;
                            res.Rectangle = rect;
                            wordresult.Add(res);
                            wordchars.Clear();
                            wordlimitindex = 0;
                        }
                    } else
                    {
                        wordchars.Clear();
                        wordlimitindex = 0;
                    }

                }
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
                result.Add(res);

            }
            if (!string.IsNullOrEmpty(wordlimit)) return wordresult.ToArray();
            return result.ToArray();
        }



    }

}
