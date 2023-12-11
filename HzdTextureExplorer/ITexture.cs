using System;
using System.Collections.Generic;
using System.IO;

namespace HzdTextureExplorer
{
    public struct InfoItem
    {
        public InfoItem(String title, String value)
        {
            Title = title;
            Value = value;
        }

        public String Title;
        public String Value;
    }
    public interface ITexture
    {
        public String Name
        {
            get;
        }

        public IList<InfoItem> Info
        {
            get;
        }

        public DDSImage Image
        {
            get;
        }

        public abstract void WriteDds(string path);
        public abstract void UpdateImageData(string path);

        public void WritePng(string path)
        {
            FileStream file = File.OpenWrite(path);
            try
            {
                Image.WritePng(file);
            }
            catch
            {
                file.Close();
                throw;
            }
            file.Close();
        }

        public void WriteTga(string path)
        {
            FileStream file = File.OpenWrite(path);
            try
            {
                Image.WriteTga(file);
            }
            catch
            {
                file.Close();
                throw;
            }
            file.Close();
        }
    }
}
