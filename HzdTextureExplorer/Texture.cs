using System;
using System.Collections.Generic;
using System.IO;

namespace HzdTextureExplorer
{
    public class Texture : BaseItem, ITexture
    {
        public const UInt64 TypeHash = 0xF2E1AFB7052B3866;
        private String m_name = null;
        private ImageData m_data = null;

        private DDSImage m_ddsImage = null;

        public String Name 
        {
            get
            {
                return m_name;
            }
        }

        public ImageData ImageData
        {
            get
            {
                return m_data;
            }
        }

        public DDSImage Image
        {
            get
            {
                if (m_ddsImage == null)
                    m_ddsImage = new DDSImage(Core.OpenImage(ImageData));

                return m_ddsImage;
            }
        }
        public IList<InfoItem> Info
        {
            get
            {
                List<InfoItem> items = new List<InfoItem>();
                Helper.AddImageInfo(items, m_data);
                return items;
            }
        }

        public Texture(HzDCore core)
            : base(core)
        { }

        public static Texture Read(HzDCore core, FileStream stream, BinaryReader reader)
        {
            Texture result = new Texture(core);
            result.ReadInternal(stream, reader);

            return result;
        }
        
        protected override void ReadInternal(FileStream stream, BinaryReader reader)
        {
            base.ReadInternal(stream, reader);

            m_name = Helper.ReadString(reader);
            m_data = new ImageData(stream, reader, RemainingSize(stream));
        }

        public virtual void WriteDds(string path)
        {
            FileStream file = File.OpenWrite(path);
            BinaryWriter writer = new BinaryWriter(file);
            try
            {
                Core.ReadImage(ImageData, writer, true);
            }
            catch
            {
                file.Close();
                throw;
            }
            file.Close();
        }

        public virtual void UpdateImageData(string fileName)
        {
            ImageData.UpdateFromFile(fileName, Core);
        }
    }
}
