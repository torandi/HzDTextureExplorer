﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace HzdTextureExplorer
{
    public class HzDCore
    {
        private List<Texture> m_textures = new List<Texture>();
        private List<TextureSet> m_textureSet = new List<TextureSet>();
        private List<UITexture> m_uiTextures = new List<UITexture>();
        private List<Misc> m_misc = new List<Misc>();

        private string m_file;

        public String Path
        {
            get
            {
                return m_file;
            }
        }

        public List<Texture> Textures
        {
            get
            {
                return m_textures;
            }
        }

        public HzDCore(string path)
        {
            FileStream core = File.OpenRead(path);
            m_file = path;

            ReadCore(core);
        }

        private void ReadCore(FileStream core)
        {
            int headerSize = 12;

            BinaryReader reader = new BinaryReader(core);

            while((core.Position + headerSize) < core.Length)
            {
                UInt64 typeHash = reader.ReadUInt64();

                switch(typeHash)
                {
                    case TextureSet.TypeHash:
                        m_textureSet.Add(TextureSet.Read(this, core, reader));
                        break;
                    case Texture.TypeHash:
                        m_textures.Add(Texture.Read(this, core, reader));
                        break;
                    case UITexture.TypeHash:
                        m_uiTextures.Add(UITexture.Read(this, core, reader));
                        break;
                    default:
                        m_misc.Add(Misc.Read(this, core, reader));
                        break;
                }
            }
        }

        private byte[] ReadStreamData(UInt64 offset, UInt64 size)
        {
            FileStream file = File.OpenRead(m_file + ".stream");
            BinaryReader reader = new BinaryReader(file);
            file.Position = (long)offset;
            return reader.ReadBytes((int)size);
        }

        public void WriteTexture(Texture texture, BinaryWriter writer)
        {

            Helper.WriteDdsHeader(writer, texture.ImageData.Width, texture.ImageData.Height, texture.ImageData.Format);
            writer.Write(ReadStreamData(texture.ImageData.StreamStart, texture.ImageData.StreamEnd));
            writer.Flush();
        }

        public Stream OpenTexture(Texture texture)
        {
            MemoryStream stream = new MemoryStream((int)(148 + texture.ImageData.StreamSize));
            BinaryWriter writer = new BinaryWriter(stream);

            WriteTexture(texture, writer);

            stream.Position = 0;

            return stream;
        }

    }

    public class BaseItem
    {
        protected UInt32 Size;
        protected Guid Id;

        protected long BasePosition;

        protected HzDCore Core;

        protected BaseItem(HzDCore core)
        {
            Core = core;
        }

        protected virtual void ReadInternal(FileStream stream, BinaryReader reader)
        {
            Size = reader.ReadUInt32();
            BasePosition = stream.Position;
            byte[] guid = reader.ReadBytes(16); // sizeof guid
            Id = new Guid(guid);
        }

        //  Skips this item
        protected void Skip(BinaryReader reader)
        {
            Size = reader.ReadUInt32();
            reader.ReadBytes((int)Size);
        }

        protected long RemainingSize(FileStream stream)
        {
            return Size - (stream.Position - BasePosition);
        }
    }

    class Helper
    {
        public static String ReadString(BinaryReader reader)
        {
            UInt32 len = reader.ReadUInt32();
            if (len == 0)
                return "";

            reader.ReadUInt32(); // hash

            char[] chars = reader.ReadChars((int)len);
            return new string(chars);
        }

        public static void WriteDdsHeader(BinaryWriter writer, UInt32 width, UInt32 height, ImageFormat format)
        {
            const UInt32 DDSD_CAPS = 0x1;
            const UInt32 DDSD_HEIGHT = 0x2;
            const UInt32 DDSD_WIDTH = 0x4;
            const UInt32 DDSD_PIXELFORMAT = 0x1000;
            const UInt32 dummy = 0;

            byte[] magic = new byte[]{ (byte)'D', (byte)'D', (byte)'S', (byte)' ' };
            writer.Write(magic);
            const UInt32 HeaderSize = 124;
            writer.Write(HeaderSize);
            writer.Write(DDSD_CAPS | DDSD_HEIGHT | DDSD_WIDTH | DDSD_PIXELFORMAT);
            writer.Write(height);
            writer.Write(width);
            writer.Write(dummy); // pitch
            writer.Write(dummy); // depth
            writer.Write((UInt32)1); // mipmap count
            for (uint i = 0; i < 11; ++i)
                writer.Write(dummy); // reserved

            Pfim.DdsPixelFormat ddsFormat;


            // pixelformat:
            switch (format.Format)
            {
                case ImageFormat.Formats.BC5U:
                    ddsFormat.Size = 32;
                    ddsFormat.PixelFormatFlags = Pfim.DdsPixelFormatFlags.Fourcc;
                    ddsFormat.FourCC = Pfim.CompressionAlgorithm.ATI2;
                    ddsFormat.RGBBitCount = 0;
                    ddsFormat.RBitMask = 0;
                    ddsFormat.GBitMask = 0;
                    ddsFormat.BBitMask = 0;
                    ddsFormat.ABitMask = 0;
                    break;
                case ImageFormat.Formats.BC7:
                    ddsFormat.Size = 32;
                    ddsFormat.PixelFormatFlags = Pfim.DdsPixelFormatFlags.Fourcc;
                    ddsFormat.FourCC = Pfim.CompressionAlgorithm.DX10;
                    ddsFormat.RGBBitCount = 0;
                    ddsFormat.RBitMask = 0;
                    ddsFormat.GBitMask = 0;
                    ddsFormat.BBitMask = 0;
                    ddsFormat.ABitMask = 0;
                    break;
                default:
                    throw new HzDException("Only BC5U and BC7 support right now");
            }

            writer.Write(ddsFormat.Size);
            writer.Write((uint)ddsFormat.PixelFormatFlags);
            writer.Write((uint)ddsFormat.FourCC);
            writer.Write(ddsFormat.RGBBitCount);
            writer.Write(ddsFormat.RBitMask);
            writer.Write(ddsFormat.GBitMask);
            writer.Write(ddsFormat.BBitMask);
            writer.Write(ddsFormat.ABitMask);

            for (uint i = 0; i < 5; ++i)
                writer.Write(dummy); // caps and reserved2

            if(ddsFormat.FourCC == Pfim.CompressionAlgorithm.DX10)
            {
                writer.Write((uint)Pfim.DxgiFormat.BC7_UNORM);
                writer.Write((uint)Pfim.D3D10ResourceDimension.D3D10_RESOURCE_DIMENSION_TEXTURE2D);
                writer.Write(dummy); // misc flag
                writer.Write((uint)1); // array size
                writer.Write(dummy); // alpha mode
            }
        }
    }

    public struct ImageFormat
    {
        public enum Formats
        {
            INVALID = 0x4C,
            RGBA_5551 = 0x0,
            RGBA_5551_REV = 0x1,
            RGBA_4444 = 0x2,
            RGBA_4444_REV = 0x3,
            RGB_888_32 = 0x4,
            RGB_888_32_REV = 0x5,
            RGB_888 = 0x6,
            RGB_888_REV = 0x7,
            RGB_565 = 0x8,
            RGB_565_REV = 0x9,
            RGB_555 = 0xA,
            RGB_555_REV = 0xB,
            RGBA_8888 = 0xC,
            RGBA_8888_REV = 0xD,
            RGBE_REV = 0xE,
            RGBA_FLOAT_32 = 0xF,
            RGB_FLOAT_32 = 0x10,
            RG_FLOAT_32 = 0x11,
            R_FLOAT_32 = 0x12,
            RGBA_FLOAT_16 = 0x13,
            RGB_FLOAT_16 = 0x14,
            RG_FLOAT_16 = 0x15,
            R_FLOAT_16 = 0x16,
            RGBA_UNORM_32 = 0x17,
            RG_UNORM_32 = 0x18,
            R_UNORM_32 = 0x19,
            RGBA_UNORM_16 = 0x1A,
            RG_UNORM_16 = 0x1B,
            R_UNORM_16 = 0x1C, // Old: INTENSITY_16
            RGBA_UNORM_8 = 0x1D,
            RG_UNORM_8 = 0x1E,
            R_UNORM_8 = 0x1F, // Old: INTENSITY_8
            RGBA_NORM_32 = 0x20,
            RG_NORM_32 = 0x21,
            R_NORM_32 = 0x22,
            RGBA_NORM_16 = 0x23,
            RG_NORM_16 = 0x24,
            R_NORM_16 = 0x25,
            RGBA_NORM_8 = 0x26,
            RG_NORM_8 = 0x27,
            R_NORM_8 = 0x28,
            RGBA_UINT_32 = 0x29,
            RG_UINT_32 = 0x2A,
            R_UINT_32 = 0x2B,
            RGBA_UINT_16 = 0x2C,
            RG_UINT_16 = 0x2D,
            R_UINT_16 = 0x2E,
            RGBA_UINT_8 = 0x2F,
            RG_UINT_8 = 0x30,
            R_UINT_8 = 0x31,
            RGBA_INT_32 = 0x32,
            RG_INT_32 = 0x33,
            R_INT_32 = 0x34,
            RGBA_INT_16 = 0x35,
            RG_INT_16 = 0x36,
            R_INT_16 = 0x37,
            RGBA_INT_8 = 0x38,
            RG_INT_8 = 0x39,
            R_INT_8 = 0x3A,
            RGB_FLOAT_11_11_10 = 0x3B,
            RGBA_UNORM_10_10_10_2 = 0x3C,
            RGB_UNORM_11_11_10 = 0x3D,
            DEPTH_FLOAT_32_STENCIL_8 = 0x3E,
            DEPTH_FLOAT_32_STENCIL_0 = 0x3F,
            DEPTH_24_STENCIL_8 = 0x40,
            DEPTH_16_STENCIL_0 = 0x41,
            BC1 = 0x42, // Old: S3TC1
            BC2 = 0x43, // Old: S3TC3
            BC3 = 0x44, // Old: S3TC5
            BC4U = 0x45,
            BC4S = 0x46,
            BC5U = 0x47,
            BC5S = 0x48,
            BC6U = 0x49,
            BC6S = 0x4A,
            BC7 = 0x4B
        };
        byte Unknown;
        public Formats Format; // byte
        byte Unknown2;
        byte Unknown3;

        public ImageFormat(BinaryReader reader)
        {
            Unknown = reader.ReadByte();
            Format = (Formats)reader.ReadByte();
            Unknown2 = reader.ReadByte();
            Unknown3 = reader.ReadByte();
        }

        public override string ToString()
        {
            return Format.ToString();
        }
    }

    public class ImageData
    {
        public ushort Unknown1;
        public ushort Width;
        public byte WidthCrop;
        public ushort Height;
        public byte HeightCrop;
        public ushort Unknown2;

        public uint UnknownStreamParam;

        public byte[] Magic;

        public byte[] Hash; // maybe?

        public uint StreamSize;
        public uint LocalSize;

        public readonly ImageFormat Format;

        public byte[] ImageContent;

        public UInt64 StreamStart;
        public UInt64 StreamEnd; // Length, same as StreamSize ?

        public String CacheString;


        public ImageData(FileStream stream, BinaryReader reader, long size)
        {
            long start = stream.Position;
            if (size == 0)
                return;

            Unknown1 = reader.ReadUInt16();
            ushort raw = reader.ReadUInt16();
            Width = (ushort)(raw & 0x3fff); // 14 bits
            WidthCrop = (byte)((raw >> 14) & 0x3); // 2 bits
            raw = reader.ReadUInt16();
            Height = (ushort)(raw & 0x3fff); // 14 bits
            HeightCrop = (byte)((raw >> 14) & 0x3); // 2 bits

            Unknown2 = reader.ReadUInt16();
            Format = new ImageFormat(reader);
            Magic = reader.ReadBytes(4); // 0x00 0xA9 0xFF 0x00

            if(!(Magic[0] == 0 && Magic[1] == 0xa9 && Magic[2] == 0xff && Magic[3] == 0x00))
                throw new HzDException("Invalid magic in texture");

            Hash = reader.ReadBytes(16);

            uint chunkSize = reader.ReadUInt32();

            LocalSize = reader.ReadUInt32();
            StreamSize = reader.ReadUInt32();

            if(StreamSize > 0)
            {
                UnknownStreamParam = reader.ReadUInt32();
                uint cacheSize = reader.ReadUInt32();
                char[] cacheString = reader.ReadChars((int)cacheSize);
                CacheString = new string(cacheString);
                StreamStart = reader.ReadUInt64();
                StreamEnd = reader.ReadUInt64(); // Length?
            }
            else
            {
                const int ImageParamsSize = 8; // 2 uints Size with and without stream
                // padding:
                reader.ReadBytes((int)(chunkSize - (ImageParamsSize + LocalSize)));
            }
            ImageContent = reader.ReadBytes((int)LocalSize);



            long currentPos = start + size;
            if (stream.Position != currentPos)
                throw new HzDException("Read incorrect size in Texture");
        }
    }

    public class TextureSet : BaseItem
    {
        public const UInt64 TypeHash = 0xE02735CED4F1CDF;

        public TextureSet(HzDCore core)
            : base(core)
        { }

        public static TextureSet Read(HzDCore core, FileStream stream, BinaryReader reader)
        {
            TextureSet result = new TextureSet(core);
            result.ReadInternal(stream, reader);
            return result;
        }

        protected override void ReadInternal(FileStream stream, BinaryReader reader)
        {
            base.Skip(reader);
        }
    }

    public class UITexture : BaseItem
    {

        public const UInt64 TypeHash = 0x9C78E9FDC6042A60;
        public static UITexture Read(HzDCore core, FileStream stream, BinaryReader reader)
        {
            UITexture result = new UITexture(core);
            result.ReadInternal(stream, reader);
            return result;
        }
        protected override void ReadInternal(FileStream stream, BinaryReader reader)
        {
            base.Skip(reader);
        }

        public UITexture(HzDCore core)
            :base(core)
        { }

    }

    public class Texture : BaseItem
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
                    m_ddsImage = new DDSImage(Core.OpenTexture(this));

                return m_ddsImage;
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

            // Preload texture
            m_ddsImage = new DDSImage(Core.OpenTexture(this));
        }

        public void WriteDds(string fileName)
        {
            FileStream file = File.OpenWrite(fileName);
            BinaryWriter writer = new BinaryWriter(file);
            Core.WriteTexture(this, writer);
            file.Close();
        }

    }

    class Misc : BaseItem
    {
        public static Misc Read(HzDCore core, FileStream stream, BinaryReader reader)
        {
            Misc result = new Misc(core);
            result.ReadInternal(stream, reader);
            return result;
        }
        protected override void ReadInternal(FileStream stream, BinaryReader reader)
        {
            base.Skip(reader);
        }

        public Misc(HzDCore core)
            : base(core)
        { }

    }
}
