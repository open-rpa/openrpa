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
        public static void TesseractDownloadLangFile(string folder, string lang)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            string dest = Path.Combine(folder, String.Format("{0}.traineddata", lang));
            if (!File.Exists(dest))
                using (System.Net.WebClient webclient = new System.Net.WebClient())
                {
                    // string source = string.Format("https://github.com/tesseract-ocr/tessdata/blob/4592b8d453889181e01982d22328b5846765eaad/{0}.traineddata?raw=true", lang);
                    string source = string.Format("https://github.com/tesseract-ocr/tessdata/blob/master/{0}.traineddata?raw=true", lang);
                    System.Diagnostics.Trace.WriteLine(string.Format("Downloading file from '{0}' to '{1}'", source, dest));
                    webclient.DownloadFile(source, dest);
                    System.Diagnostics.Trace.WriteLine(string.Format("Download completed"));
                }
        }
        public static string OcrImage(Emgu.CV.OCR.Tesseract _ocr, Emgu.CV.Mat image)
        {
            using (var imageColor = new Mat())
            using (Mat imgGrey = new Mat())
            using (Mat imgThresholded = new Mat())
            {
                if (image.NumberOfChannels == 1)
                    CvInvoke.CvtColor(image, imageColor, ColorConversion.Gray2Bgr);
                else
                    image.CopyTo(imageColor);
                //Interfaces.Image.Util.SaveImageStamped(imageColor.Bitmap, "OcrImage-Color");
                _ocr.SetImage(imageColor);
                _ocr.AnalyseLayout();
                if (_ocr.Recognize() != 0) throw new Exception("Failed to recognizer image");
                Emgu.CV.OCR.Tesseract.Character[] characters = _ocr.GetCharacters();
                Log.Debug("GetCharacters found " + characters.Length + " with colors");
                if (characters.Length == 0)
                {
                    CvInvoke.CvtColor(image, imgGrey, ColorConversion.Bgr2Gray);
                    //Interfaces.Image.Util.SaveImageStamped(imgGrey.Bitmap, "OcrImage-Gray");
                    _ocr.SetImage(imgGrey);
                    _ocr.AnalyseLayout();
                    if (_ocr.Recognize() != 0) throw new Exception("Failed to recognizer image");
                    characters = _ocr.GetCharacters();
                    Log.Debug("GetCharacters found " + characters.Length + " with grey scaled");
                    if (characters.Length == 0)
                    {
                        CvInvoke.Threshold(imgGrey, imgThresholded, 65, 255, ThresholdType.Binary);
                        //Interfaces.Image.Util.SaveImageStamped(imgThresholded.Bitmap, "OcrImage-Thresholded");
                        _ocr.SetImage(imgThresholded);
                        _ocr.AnalyseLayout();
                        if (_ocr.Recognize() != 0) throw new Exception("Failed to recognizer image");
                        characters = _ocr.GetCharacters();
                        Log.Debug("GetCharacters found " + characters.Length + " thresholded");

                    }
                }
                return _ocr.GetUTF8Text().TrimEnd(Environment.NewLine.ToCharArray());
            }
        }
        public static ImageElement[] OcrImage2(Emgu.CV.OCR.Tesseract _ocr, Emgu.CV.Mat image, string wordlimit, bool casesensitive)
        {
            using (var imageColor = new Mat())
            using (Mat imgGrey = new Mat())
            {
                if (image.NumberOfChannels == 1)
                    CvInvoke.CvtColor(image, imageColor, ColorConversion.Gray2Bgr);
                else
                    image.CopyTo(imageColor);
                // _ocr.SetImage(imageColor);
                CvInvoke.CvtColor(image, imgGrey, ColorConversion.Bgr2Gray);
                _ocr.SetImage(imgGrey);
                _ocr.AnalyseLayout();
                if (_ocr.Recognize() != 0) throw new Exception("Failed to recognizer image");
                Emgu.CV.OCR.Tesseract.Character[] characters = _ocr.GetCharacters();
                var index = 0;
                var wordlimitindex = 0;
                var chars = new List<Emgu.CV.OCR.Tesseract.Character>();
                var result = new List<ImageElement>();
                var wordresult = new List<ImageElement>();
                var wordchars = new List<Emgu.CV.OCR.Tesseract.Character>();
                Rectangle desktop = new Rectangle(0, 0, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
                Rectangle imagerect = new Rectangle(0, 0, image.Width, image.Height);
                while (index < characters.Length)
                {
                    if (!string.IsNullOrEmpty(wordlimit))
                    {
                        if ((characters[index].Text == wordlimit[wordlimitindex].ToString()) ||
                            (!casesensitive && characters[index].Text.ToLower() == wordlimit[wordlimitindex].ToString().ToLower()))
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
                                if (!desktop.Contains(rect))
                                {
                                    Log.Error("Found element outside desktop !!!!!");
                                }
                                if (!imagerect.Contains(rect))
                                {
                                    Log.Error("Found element outside desktop !!!!!");
                                }
                                Log.Debug("Found: " + res.Text + " at " + res.Rectangle.ToString());
                            }
                        }
                        else
                        {
                            wordchars.Clear();
                            wordlimitindex = 0;
                        }

                    }
                    if (characters[index].Text == " " || characters[index].Text == "\r" || characters[index].Text == "\n")
                    {
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

}
