using System;
using System.Collections.Generic;

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
    }
}
