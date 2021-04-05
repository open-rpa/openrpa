using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Image
{
    using Emgu.CV;
    using Emgu.CV.CvEnum;
    using Emgu.CV.Structure;
    using Emgu.CV.Util;
    using OpenRPA.Interfaces;
    using System.Drawing;
    static class Matches
    {
        // https://stackoverflow.com/questions/8218997/how-to-detect-the-sun-from-the-space-sky-in-opencv/8221251#8221251
        // https://stackoverflow.com/questions/30867391/how-to-call-opencvs-matchtemplate-method-from-c-sharp


        public static Bitmap DrawRectangle(Bitmap Source, Rectangle rect)
        {
            var img = new Image<Bgr, byte>(Source);
            img.Draw(rect, new Bgr(0, 0, 255), 2);
            return img.ToBitmap();
        }

        public static Rectangle FindMatch(Bitmap Source, Bitmap Template, double Threshold)
        {
            return FindMatch(new Image<Bgr, byte>(Source), new Image<Bgr, byte>(Template), Threshold);
        }
        public static Rectangle FindMatch(Image<Bgr, byte> Source, Image<Bgr, byte> Template, double Threshold)
        {
            try
            {
                using (Image<Gray, float> result = Source.MatchTemplate(Template, TemplateMatchingType.CcoeffNormed))
                {
                    double[] minValues, maxValues;
                    Point[] minLocations, maxLocations;
                    result.MinMax(out minValues, out maxValues, out minLocations, out maxLocations);
                    // between 0.75 and 0.95 would be good.
                    if (maxValues[0] > Threshold)
                    {
                        Rectangle match = new Rectangle(maxLocations[0], Template.Size);
                        return match;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }
            return Rectangle.Empty;
        }

        public static Rectangle[] FindMatches(Bitmap Source, Bitmap Template, double Threshold, int maxResults, bool asGray)
        {
            try
            {
                if (Template.Width > Source.Width) throw new ArgumentException("Template is wider than the source");
                if (Template.Height > Source.Height) throw new ArgumentException("Template is higher than the source");
                if (asGray)
                {
                    using (var source = new Image<Gray, byte>(Source))
                    {
                        using (var template = new Image<Gray, byte>(Template))
                        {
                            //Interfaces.Image.Util.SaveImageStamped(source.Bitmap, "c:\\temp", "FindMatches-source");
                            //Interfaces.Image.Util.SaveImageStamped(template.Bitmap, "c:\\temp", "FindMatches-template");
                            //rpaactivities.image.util.saveImage(source, "FindMatches-source");
                            //rpaactivities.image.util.saveImage(template, "FindMatches-template");
                            var result = FindMatches(source, template, Threshold, maxResults);
                            //image.util.showImage(template);
                            return result;
                        }
                    }
                }
                else
                {
                    using (var source = new Image<Bgr, byte>(Source))
                    {
                        using (var template = new Image<Bgr, byte>(Template))
                        {
                            //Interfaces.Image.Util.SaveImageStamped(source.Bitmap, "c:\\temp", "FindMatches-source");
                            //Interfaces.Image.Util.SaveImageStamped(template.Bitmap, "c:\\temp", "FindMatches-template");
                            //rpaactivities.image.util.saveImage(source, "FindMatches-source");
                            //rpaactivities.image.util.saveImage(template, "FindMatches-template");
                            var result = FindMatches(source, template, Threshold, maxResults);
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Debug(ex.ToString());
            }
            return new Rectangle[] { };
        }
        private static Rectangle[] FindMatches<TColor, TDepth>(Image<TColor, TDepth> Source, Image<TColor, TDepth> Template, double Threshold, int maxResults, bool inverted = false)
        where TColor : struct, IColor
        where TDepth : new()
        {
            var result = new List<Rectangle>();
            using (var Matches = Source.MatchTemplate(Template, TemplateMatchingType.CcoeffNormed))
            //using (var Matches = Source.MatchTemplate(Template, TemplateMatchingType.CcorrNormed))
            {
                try
                {
                    int matchcount = 0;
                    for (int y = 0; y < Matches.Data.GetLength(0); y++)
                    {
                        for (int x = 0; x < Matches.Data.GetLength(1); x++)
                        {
                            Double certain = Matches.Data[y, x, 0];
                            if (Matches.Data[y, x, 0] >= Threshold) //Check if its a valid match
                            {
                                matchcount++;
                                bool canadd = true;
                                if (matchcount > maxResults) canadd = false;
                                if (canadd)
                                {
                                    var rect = new Rectangle(new Point(x, y), new Size(Template.Width, Template.Height));
                                    foreach (var _r in result.ToList())
                                    {
                                        if ((_r.Contains(rect) || _r.IntersectsWith(rect)) && !_r.Equals(rect))
                                        {
                                            canadd = false;
                                        }
                                    }
                                    if (canadd) result.Add(rect);
                                }
                            }
                        }
                    }
                }
                catch (Exception)
                {

                    throw;
                }

            }
            if (result.Count > 0)
            {
                //var imageDump = socketService.instance.settings.imagedump;
                //if (imageDump)
                //{
                //    using (var _b = Source.ToBitmap())
                //    using (var saveimage = new Image<Emgu.CV.Structure.Bgr, Byte>(_b))
                //    {
                //        foreach (var match in result)
                //        {
                //            saveimage.Draw(match, new Bgr(System.Drawing.Color.Red), 2);
                //        }
                //        rpaactivities.image.util.saveImage(saveimage, "FindMatches-hits");
                //    }
                //}
            }

            if (result.Count == 0 && inverted == false)
            {
                //using (var invertedimage = Template.Not())
                //{
                //    return FindMatches(Source, invertedimage, Threshold, maxResults, true);
                //}
            }
            return result.ToArray();
        }
        //public static Highlighter[] HighlightMatches(Rectangle[] Matches)
        //{
        //    var result = new List<Highlighter>();
        //    foreach (var m in Matches)
        //    {
        //        result.Add(new Highlighter(m, System.Drawing.Color.Red));
        //    }
        //    return result.ToArray();
        //}
        //public static void HighlightMatches(Rectangle[] Matches, TimeSpan Duration)
        //{
        //    foreach (var Match in Matches)
        //    {
        //        Task.Factory.StartNew(() =>
        //        {
        //            var h2 = new Highlighter(Match, System.Drawing.Color.Red);
        //            System.Threading.Thread.Sleep(Duration);
        //            h2.remove();
        //        });
        //    }
        //}
        //public static void HighlightMatch(Rectangle Match, bool Blocking, Color Color, TimeSpan Duration)
        //{
        //    if (!Blocking)
        //    {
        //        Task.Factory.StartNew(() =>
        //        {
        //            var h2 = new Highlighter(Match, System.Drawing.Color.Red);
        //            System.Threading.Thread.Sleep(Duration);
        //            h2.remove();
        //            System.Windows.Forms.Application.DoEvents();
        //        });
        //        return;
        //    }
        //    var h = new Highlighter(Match, System.Drawing.Color.Red);
        //    System.Threading.Thread.Sleep(Duration);
        //    h.remove();
        //    System.Windows.Forms.Application.DoEvents();
        //}






        //https://www.meridium.se/sv/blogg/imagematching-using-opencv/
        public static void FindMatch(Mat modelImage, Mat observedImage, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography, out long score)
        {
            int k = 2;
            double uniquenessThreshold = 0.80;

            homography = null;

            modelKeyPoints = new VectorOfKeyPoint();
            observedKeyPoints = new VectorOfKeyPoint();

            using (UMat uModelImage = modelImage.GetUMat(AccessType.Read))
            using (UMat uObservedImage = observedImage.GetUMat(AccessType.Read))
            {
                var featureDetector = new Emgu.CV.Features2D.KAZE();

                Mat modelDescriptors = new Mat();
                featureDetector.DetectAndCompute(uModelImage, null, modelKeyPoints, modelDescriptors, false);

                Mat observedDescriptors = new Mat();
                featureDetector.DetectAndCompute(uObservedImage, null, observedKeyPoints, observedDescriptors, false);

                // KdTree for faster results / less accuracy
                using (var ip = new Emgu.CV.Flann.KdTreeIndexParams())
                using (var sp = new Emgu.CV.Flann.SearchParams())
                using (var matcher = new Emgu.CV.Features2D.FlannBasedMatcher(ip, sp))
                {
                    matcher.Add(modelDescriptors);

                    matcher.KnnMatch(observedDescriptors, matches, k, null);
                    mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                    mask.SetTo(new MCvScalar(255));
                    Emgu.CV.Features2D.Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

                    // Calculate score based on matches size
                    // ---------------------------------------------->
                    score = 0;
                    for (int i = 0; i < matches.Size; i++)
                    {
                        // if (mask.GetData(i)[0] == 0) continue;
                        foreach (var e in matches[i].ToArray())
                            ++score;
                    }
                    // <----------------------------------------------

                    int nonZeroCount = CvInvoke.CountNonZero(mask);
                    if (nonZeroCount >= 4)
                    {
                        nonZeroCount = Emgu.CV.Features2D.Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints, matches, mask, 1.5, 20);
                        if (nonZeroCount >= 4)
                            homography = Emgu.CV.Features2D.Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints, observedKeyPoints, matches, mask, 2);
                    }
                }

            }
        }



    }
}
