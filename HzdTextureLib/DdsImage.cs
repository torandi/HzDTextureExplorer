﻿using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.PixelFormats;

namespace HzdTextureLib
{
    public class DDSImage
    {
        private readonly Pfim.IImage m_image;

        private Image m_unpacked = null;
        private MemoryStream m_stream = null;

        public Pfim.IImage DdsImage
        {
            get
            {
                return m_image;
            }
        }

        public MemoryStream Stream
        {
            get
            {
                return m_stream;
            }
        }

        public DDSImage(string file)
        {
            m_image = Pfim.Pfimage.FromFile(file);
            Process();
        }

        public DDSImage(Stream stream)
        {
            if (stream == null)
                throw new Exception("DDSImage ctor: Stream is null");

            m_image = Pfim.Dds.Create(stream, new Pfim.PfimConfig());
            Process();
        }

        public DDSImage(byte[] data)
        {
            if (data == null || data.Length <= 0)
                throw new Exception("DDSImage ctor: no data");

            m_image = Pfim.Dds.Create(data, new Pfim.PfimConfig());
            Process();
        }

        private void Process()
        {
            if (m_image == null)
                throw new Exception("DDSImage image creation failed");

            if (m_image.Compressed)
                m_image.Decompress();

            if (m_image.Format == Pfim.ImageFormat.Rgba32)
            {
                Unpack<Bgra32>();
            }
            else if (m_image.Format == Pfim.ImageFormat.Rgb24)
            {
                Unpack<Bgr24>();
            }
            else if (m_image.Format == Pfim.ImageFormat.Rgb8)
            {
                Unpack<L8>();
            }
            else
                throw new Exception("Unsupported pixel format (" + m_image.Format + ")");

            m_stream = new MemoryStream();

            WritePng(m_stream);
        }

        public void WritePng(Stream stream)
        {
            var encoder = new PngEncoder
            {
                ColorType = PngColorType.RgbWithAlpha,
                BitDepth = PngBitDepth.Bit8,
                TransparentColorMode = PngTransparentColorMode.Preserve
            };

            m_unpacked.SaveAsPng(stream, encoder);
        }
        public void WriteTga(Stream stream)
        {
            var encoder = new TgaEncoder
            {
                BitsPerPixel = TgaBitsPerPixel.Pixel32,
            };

            m_unpacked.SaveAsTga(stream, encoder);
        }

        private void Unpack<T>()
            where T : unmanaged, IPixel<T>
        {
            m_unpacked = Image.LoadPixelData<T>(m_image.Data, m_image.Width, m_image.Height);
        }

    }
}
