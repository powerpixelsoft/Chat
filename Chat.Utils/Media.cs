using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Chat.Utils
{
    public static class Media
    {
        public static String[] ImageExtensions = new String[] {".TIFF", ".GIF", ".BMP", ".PNG", ".JPEG", ".JPG" };

        public static MediaType GetMediaType(String extension)
        {
            if (ImageExtensions.Any(x => x.Equals(extension.ToUpper())))
            {
                return MediaType.Image;
            }
            else
            {
                return MediaType.Text;
            }
        }
    }
}
