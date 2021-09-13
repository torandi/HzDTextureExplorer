using System;
using System.Collections.Generic;
using System.IO;

namespace HzdTextureExplorer
{
    public class UITexture : BaseItem
    {
        public const UInt64 TypeHash = 0x9C78E9FDC6042A60;

        private string[] m_name = new string[2];
        private ImageData[] m_data = new ImageData[2];

        private DDSImage[] m_ddsImage = new DDSImage[2];

        private uint[] m_size = new uint[2];

        private ImageSize m_initialSize;

        public string[] Names { get => m_name; }
        public ImageData[] Datas { get => m_data; }
        public DDSImage[] Images {
            get
            {
                if (m_ddsImage[0] == null)
                {
                    m_ddsImage[0] = new DDSImage(Core.OpenImage(m_data[0]));
                }

                if (m_ddsImage[1] == null)
                {
                    m_ddsImage[1] = new DDSImage(Core.OpenImage(m_data[1]));
                }

                return m_ddsImage;
            }
        }

        public IList<ITexture> TextureItems
        {
            get
            {
                List<ITexture> textures = new List<ITexture>();
                textures.Add(new UITextureWrapper(this, 0));
                textures.Add(new UITextureWrapper(this, 1));
                return textures;
            }
        }

        public static UITexture Read(HzDCore core, FileStream stream, BinaryReader reader)
        {
            UITexture result = new UITexture(core);
            result.ReadInternal(stream, reader);
            return result;
        }
        protected override void ReadInternal(FileStream stream, BinaryReader reader)
        {
            base.ReadInternal(stream, reader);

            m_name[0] = Helper.ReadString(reader);
            m_name[1] = Helper.ReadString(reader);

            if(m_name[0] == m_name[1])
            {
                m_name[0] += "_0";
                m_name[1] += "_1";
            }

            m_initialSize = ImageSize.ReadUint(reader);

            m_size[0] = reader.ReadUInt32();
            m_size[1] = reader.ReadUInt32();

            m_data[0] = new ImageData(stream, reader, m_size[0]);
            m_data[1] = new ImageData(stream, reader, m_size[1]);
        }

        public UITexture(HzDCore core)
            :base(core)
        { }

        internal void WriteDds(uint index, string path)
        {
            FileStream file = File.OpenWrite(path);
            BinaryWriter writer = new BinaryWriter(file);
            try
            {
                Core.ReadImage(m_data[index], writer, true);
            }
            catch
            {
                file.Close();
                throw;
            }
            file.Close();
        }
    }
   public class UITextureWrapper : ITexture
    {
        UITexture m_base;
        uint m_index;

        public UITextureWrapper(UITexture texture, uint index)
        {
            m_base = texture;
            m_index = index;
        }

        public string Name
        {
            get
            {
                return $"{m_base.Names[m_index]}";
            }
        }
        public IList<InfoItem> Info
        {
            get
            {
                List<InfoItem> items = new List<InfoItem>();
                Helper.AddImageInfo(items, m_base.Datas[m_index]);
                return items;
            }
        }

        public DDSImage Image
        {
            get
            {

                return m_base.Images[m_index];
            }
        }

        public virtual void WriteDds(string path)
        {
            m_base.WriteDds(m_index, path);
        }
        public virtual void UpdateImageData(string fileName)
        {
            m_base.Datas[m_index].UpdateFromFile(fileName, m_base.Core);
        }
    }


}
