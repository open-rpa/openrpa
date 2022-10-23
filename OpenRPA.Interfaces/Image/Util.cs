using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRPA.Interfaces.Image
{
    public class Util
    {
        public const int ActivityPreviewImageWidth = 300;
        public const int ActivityPreviewImageHeight = 100;
        public static void SaveImageStamped(Bitmap img, string message)
        {
            SaveImageStamped(img, Interfaces.Extensions.MyPictures, message);
        }
        public static void SaveImageStamped(Bitmap img, string path, string message)
        {
            SaveImage(img, path, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-") + "-" + message + ".png");
        }
        public static void SaveImage(Bitmap img, string path, string filename)
        {
            try
            {
                if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
                img.Save(System.IO.Path.Combine(path, filename));
            }
            catch (Exception ex)
            {
                Log.Error(ex.ToString());
                throw;
            }
        }
        public static Bitmap Screenshot(Rectangle rect)
        {
            return Screenshot(rect.X, rect.Y, rect.Width, rect.Height);
        }
        public static Bitmap Screenshot()
        {
            return Screenshot(0, 0, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
        }
        public static Bitmap Screenshot(int x, int y, int width, int height)
        {
            if (width < 10) width = ActivityPreviewImageWidth;
            if (height < 10) height = ActivityPreviewImageHeight;
            var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap as System.Drawing.Image))
            {
                graphics.CopyFromScreen(x, y, 0, 0, bitmap.Size);
            }
            return bitmap;
        }
        public static Bitmap Screenshot(int x, int y, int width, int height, int maxWidth, int maxHeight)
        {
            if (width < 10) width = ActivityPreviewImageWidth;
            if (height < 10) height = ActivityPreviewImageHeight;
            var bitmap = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(bitmap as System.Drawing.Image))
            {
                graphics.CopyFromScreen(x, y, 0, 0, bitmap.Size);
            }
            if(width > maxWidth || height > maxHeight)
            {
                using(bitmap) // dispose bitmap
                {
                    return Resize(bitmap, maxWidth, maxHeight);
                }
            }
            return bitmap;
        }
        public static void RemoveNoise(ref Bitmap bitmap)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                for (var y = 0; y < bitmap.Height; y++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    if (pixel.R < 162 && pixel.G < 162 && pixel.B < 162)
                        bitmap.SetPixel(x, y, Color.Black);
                    else if (pixel.R > 162 && pixel.G > 162 && pixel.B > 162)
                        bitmap.SetPixel(x, y, Color.White);
                }
            }
        }
        public static void SetGrayscale(ref Bitmap img)
        {
            //var temp = img;
            //var bmap = temp.Clone();
            Color c;
            for (int i = 0; i < img.Width; i++)
            {
                for (int j = 0; j < img.Height; j++)
                {
                    c = img.GetPixel(i, j);
                    byte gray = (byte)(.299 * c.R + .587 * c.G + .114 * c.B);
                    img.SetPixel(i, j, Color.FromArgb(gray, gray, gray));
                }
            }
        }
        public static Bitmap Resize(Bitmap image, int tWidth, int tHeight)
        {
            var brush = new SolidBrush(Color.White);

            float Width = tWidth; float Height = tHeight;

            float scale = Math.Min(Width / image.Width, Height / image.Height);

            var bmp = new Bitmap((int)Width, (int)Height);
            var graph = Graphics.FromImage(bmp);

            // uncomment for higher quality output
            graph.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.High;
            graph.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graph.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            var scaleWidth = (int)(image.Width * scale);
            var scaleHeight = (int)(image.Height * scale);

            graph.FillRectangle(brush, new RectangleF(0, 0, Width, Height));
            graph.DrawImage(image, ((int)Width - scaleWidth) / 2, ((int)Height - scaleHeight) / 2, scaleWidth, scaleHeight);

            return bmp;

        }
        //public static Bitmap Resize(Bitmap bmp, int Width, int Height)
        //{
        //    Bitmap temp = (Bitmap)bmp;
        //    Bitmap bmap = new Bitmap(Width, Height, temp.PixelFormat);
        //    double nWidthFactor = (double)temp.Width / (double)Width;
        //    double nHeightFactor = (double)temp.Height / (double)Height;
        //    double fx, fy, nx, ny;
        //    int cx, cy, fr_x, fr_y;
        //    Color color1 = new Color();
        //    Color color2 = new Color();
        //    Color color3 = new Color();
        //    Color color4 = new Color();
        //    byte nRed, nGreen, nBlue;
        //    byte bp1, bp2;
        //    for (int x = 0; x < bmap.Width; ++x)
        //    {
        //        for (int y = 0; y < bmap.Height; ++y)
        //        {
        //            fr_x = (int)Math.Floor(x * nWidthFactor);
        //            fr_y = (int)Math.Floor(y * nHeightFactor);
        //            cx = fr_x + 1;
        //            if (cx >= temp.Width) cx = fr_x;
        //            cy = fr_y + 1;
        //            if (cy >= temp.Height) cy = fr_y;
        //            fx = x * nWidthFactor - fr_x;
        //            fy = y * nHeightFactor - fr_y;
        //            nx = 1.0 - fx;
        //            ny = 1.0 - fy;

        //            color1 = temp.GetPixel(fr_x, fr_y);
        //            color2 = temp.GetPixel(cx, fr_y);
        //            color3 = temp.GetPixel(fr_x, cy);
        //            color4 = temp.GetPixel(cx, cy);

        //            // Blue
        //            bp1 = (byte)(nx * color1.B + fx * color2.B);
        //            bp2 = (byte)(nx * color3.B + fx * color4.B);
        //            nBlue = (byte)(ny * (double)(bp1) + fy * (double)(bp2));

        //            // Green
        //            bp1 = (byte)(nx * color1.G + fx * color2.G);
        //            bp2 = (byte)(nx * color3.G + fx * color4.G);
        //            nGreen = (byte)(ny * (double)(bp1) + fy * (double)(bp2));

        //            // Red
        //            bp1 = (byte)(nx * color1.R + fx * color2.R);
        //            bp2 = (byte)(nx * color3.R + fx * color4.R);
        //            nRed = (byte)(ny * (double)(bp1) + fy * (double)(bp2));

        //            bmap.SetPixel(x, y, Color.FromArgb(255, nRed, nGreen, nBlue));
        //        }
        //    }
        //    //SetGrayscale(ref bmap);
        //    //RemoveNoise(ref bmap);
        //    return bmap;
        //}
        public static System.Windows.Media.Imaging.BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (var memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                var bitmapimage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();

                return bitmapimage;
            }
        }
        public static System.Windows.Media.Imaging.BitmapImage BitmapToImageSource(Bitmap bitmap, int Width, int Height)
        {
            if (bitmap == null) return null;
            if (bitmap.Width > Width || bitmap.Height > Height)
            {
                bitmap = Resize(bitmap, Width, Height);
            }
            using (var memory = new System.IO.MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                var bitmapimage = new System.Windows.Media.Imaging.BitmapImage();
                bitmapimage.BeginInit();
                bitmapimage.StreamSource = memory;
                bitmapimage.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
                bitmapimage.EndInit();
                return bitmapimage;
            }
        }
        public static string Bitmap2Base64(Bitmap image)
        {
            if (image == null) return null;
            string SigBase64 = string.Empty;
            using (var ms = new System.IO.MemoryStream())
            {
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
                SigBase64 = Convert.ToBase64String(ms.GetBuffer());
            }
            return SigBase64;
        }
        public static Bitmap Base642Bitmap(string base64)
        {
            if (string.IsNullOrEmpty(base64)) return null;
            using (var ms = new System.IO.MemoryStream(Convert.FromBase64String(base64)))
            using (var image = System.Drawing.Image.FromStream(ms, false, true))
                return new System.Drawing.Bitmap(image);
        }
        public static async Task<Bitmap> LoadWorkflowImage(string basepath, string id)
        {
            try
            {
                if (string.IsNullOrEmpty(basepath)) { basepath = Interfaces.Extensions.ProjectsDirectory; }
                var imagepath = System.IO.Path.Combine(basepath, "images");
                if (!System.IO.Directory.Exists(imagepath)) System.IO.Directory.CreateDirectory(imagepath);
                var imagefilepath = System.IO.Path.Combine(imagepath, id + ".png");
                if (!System.IO.File.Exists(imagefilepath) && global.webSocketClient != null ) { await global.webSocketClient.DownloadFileAndSaveAs(id + ".png", id, imagepath, true, true, "", ""); }
                if (System.IO.File.Exists(imagefilepath)) { return new Bitmap(imagefilepath); }
                return null;
            }
            catch (Exception ex)
            {
                
                Log.Debug(ex.ToString());
                return null;
            }
        }
        public static async Task<Bitmap> LoadBitmap(string ImageString) => await LoadBitmap(null, ImageString);
        public static async Task<Bitmap> LoadBitmap(string basepath, string ImageString)
        {
            Bitmap b;
            if (string.IsNullOrEmpty(ImageString)) return null;
            if (System.Text.RegularExpressions.Regex.Match(ImageString, "[a-f0-9]{24}").Success)
            {
                try
                {
                    b = await LoadWorkflowImage(basepath, ImageString);
                }
                catch (Exception)
                {
                    b = null;
                }
            }
            else
            {
                b = Base642Bitmap(ImageString);
            }
            return b;
        }
    }
}
