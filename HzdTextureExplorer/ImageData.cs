using System;
using System.IO;

namespace HzdTextureExplorer
{
    public class ImageData
    {
        private long BasePosition;
        private ulong EmbeddedPosition;

        public readonly ImageType Type;
        public ImageSize Size;
        public uint Slices;

        public uint MipMaps;
        public uint StreamMipMaps;
        

        public byte[] Magic;

        public byte[] Hash; // maybe?

        public uint StreamSize;
        public uint EmbeddedSize;

        public readonly ImageFormat Format;

        public byte[] EmbeddedData;

        public UInt64 StreamStart;
        public UInt64 StreamLength;

        public String CacheString;

        public uint Width
        {
            get
            {
                return Size.Width;
            }
        }
        
        public uint Height
        {
            get
            {
                return Size.Height;
            }
        }

        public bool HasStreamableData
        {
            get
            {
                return StreamSize > 0;
            }
        }

        public bool HasEmbeddedData
        {
            get
            {
                return EmbeddedSize > 0;
            }
        }

        public ImageData(FileStream stream, BinaryReader reader, long size)
        {
            BasePosition = stream.Position;
            if (size == 0)
                return;

            Type = new ImageType(reader);
            Size = ImageSize.Read14bits(reader);

            Slices = reader.ReadUInt16();
            MipMaps = reader.ReadByte();
            Format = new ImageFormat(reader);
            Magic = reader.ReadBytes(4); // 0x00 0xA9 0xFF 0x00

            if(!(Magic[0] == 0 && Magic[1] == 0xa9 && Magic[2] == 0xff && Magic[3] == 0x00))
                throw new HzDException("Invalid magic in texture");

            Hash = reader.ReadBytes(16);

            uint chunkSize = reader.ReadUInt32();

            EmbeddedSize = reader.ReadUInt32();
            StreamSize = reader.ReadUInt32();

            if(StreamSize > 0)
            {
                StreamMipMaps = reader.ReadUInt32();
                uint cacheSize = reader.ReadUInt32();
                char[] cacheString = reader.ReadChars((int)cacheSize);
                CacheString = new string(cacheString);
                StreamStart = reader.ReadUInt64();
                StreamLength = reader.ReadUInt64();
            }
            else
            {
                const int ImageParamsSize = 8; // 2 uints Size with and without stream
                // padding:
                reader.ReadBytes((int)(chunkSize - (ImageParamsSize + EmbeddedSize)));
            }

            EmbeddedPosition = (ulong)stream.Position;

            EmbeddedData = reader.ReadBytes((int)EmbeddedSize);

            long currentPos = BasePosition + size;
            if (stream.Position != currentPos)
                throw new HzDException("Read incorrect size in Texture");
        }

        public void UpdateFromFile(string filename, HzDCore core)
        {
            FileStream file = File.OpenRead(filename);
            uint arraySize = Slices>0?Slices:1;

            // Read header
            Pfim.DdsHeader header = new Pfim.DdsHeader(file);
            Pfim.DdsHeaderDxt10 dxt10Header = null;
            if(header.PixelFormat.FourCC == Pfim.CompressionAlgorithm.DX10)
            {
                // Also read dxt10 header
                dxt10Header = new Pfim.DdsHeaderDxt10(file);
            }

            if(dxt10Header.ArraySize != arraySize)
            {
                throw new HzDException($"Array size of imported dds file don't match. Must be {arraySize}, but was {dxt10Header.ArraySize}");
            }

            if(header.Width != Width || header.Height != Height)
            {
                throw new HzDException($"Dimensions of imported dds file don't match. Must be {Width}x{Height}, but was {header.Width}x{header.Height}");
            }

            if(header.MipMapCount < MipMaps)
            {
                throw new HzDException($"Imported dds has too few mipsmaps, needs at least {MipMaps}. (had only {header.MipMapCount})");
            }

            if (header.PixelFormat.Size != 32)
                throw new HzDException($"Invalid PixelFormat in dds. Expected size to be 32, but was {header.PixelFormat.Size}");

            if (header.PixelFormat.PixelFormatFlags != Pfim.DdsPixelFormatFlags.Fourcc)
                throw new HzDException("PixelFormat missing FourcCC flag");

            string fileDdsFormat = "";
            if (header.PixelFormat.FourCC == Pfim.CompressionAlgorithm.DX10)
                fileDdsFormat = dxt10Header.DxgiFormat.ToString();
            else
                fileDdsFormat = header.PixelFormat.FourCC.ToString();

            if (Format.Format == ImageFormat.Formats.BC1)
            {
                if (!(header.PixelFormat.FourCC == Pfim.CompressionAlgorithm.D3DFMT_DXT1 
                    || (header.PixelFormat.FourCC == Pfim.CompressionAlgorithm.DX10 && 
                        (dxt10Header.DxgiFormat == Pfim.DxgiFormat.BC1_UNORM || dxt10Header.DxgiFormat == Pfim.DxgiFormat.BC1_UNORM_SRGB)
                        ))
                    )
                {
                    throw new HzDException($"Invalid PixelFormat {fileDdsFormat} in dds. Expected BC1");
                }
            }
            else if (Format.Format == ImageFormat.Formats.BC3)
            {
                if (!(header.PixelFormat.FourCC == Pfim.CompressionAlgorithm.D3DFMT_DXT3
                    || (header.PixelFormat.FourCC == Pfim.CompressionAlgorithm.DX10 && 
                        (dxt10Header.DxgiFormat == Pfim.DxgiFormat.BC3_UNORM || dxt10Header.DxgiFormat == Pfim.DxgiFormat.BC3_UNORM_SRGB)
                        ))
                    )
                {
                    throw new HzDException($"Invalid PixelFormat {fileDdsFormat} in dds. Expected BC3");
                }
            }
            else if (Format.Format == ImageFormat.Formats.BC4U)
            {
                if (!(
                    header.PixelFormat.FourCC == Pfim.CompressionAlgorithm.BC4U ||
                    (header.PixelFormat.FourCC == Pfim.CompressionAlgorithm.DX10 && dxt10Header.DxgiFormat == Pfim.DxgiFormat.BC4_UNORM)
                    ))
                {
                    throw new HzDException($"Invalid PixelFormat {fileDdsFormat} in dds. Expected BC4U");
                }
            }
            else if (Format.Format == ImageFormat.Formats.BC5U)
            {
                if (!(
                    header.PixelFormat.FourCC == Pfim.CompressionAlgorithm.BC5U ||
                    (header.PixelFormat.FourCC == Pfim.CompressionAlgorithm.DX10 && dxt10Header.DxgiFormat == Pfim.DxgiFormat.BC5_UNORM)
                    ))
                {
                    throw new HzDException($"Invalid PixelFormat {fileDdsFormat} in dds. Expected BC5U");
                }
            }
            else if (Format.Format == ImageFormat.Formats.BC6U)
            {
                if (header.PixelFormat.FourCC != Pfim.CompressionAlgorithm.DX10 || dxt10Header.DxgiFormat != Pfim.DxgiFormat.BC6H_UF16)
                {
                    throw new HzDException($"Invalid PixelFormat {fileDdsFormat} in dds. Expected BC6U");
                }

            }
            else if (Format.Format == ImageFormat.Formats.BC6S)
            {
                if (header.PixelFormat.FourCC != Pfim.CompressionAlgorithm.DX10 || dxt10Header.DxgiFormat != Pfim.DxgiFormat.BC6H_SF16)
                {
                    throw new HzDException($"Invalid PixelFormat {fileDdsFormat} in dds. Expected BC6S");
                }

            }
            else if (Format.Format == ImageFormat.Formats.BC7)
            {
                if (header.PixelFormat.FourCC != Pfim.CompressionAlgorithm.DX10 || dxt10Header.DxgiFormat != Pfim.DxgiFormat.BC7_UNORM)
                {
                    throw new HzDException($"Invalid PixelFormat {fileDdsFormat} in dds. Expected BC7");
                }
            }
            else if (Format.Format == ImageFormat.Formats.RGBA_8888)
            {
                if (header.PixelFormat.FourCC != Pfim.CompressionAlgorithm.DX10 || dxt10Header.DxgiFormat != Pfim.DxgiFormat.R8G8B8A8_UNORM)
                {
                    throw new HzDException($"Invalid PixelFormat {fileDdsFormat} in dds. Expected RGBA_8888");
                }
            }
            else
            {
                throw new HzDException($"Unimplemented format {fileDdsFormat} in core file texture.");
            }

            if (HasStreamableData)
            {
                byte[] imageData = new byte[StreamSize];
                int readBytes = file.Read(imageData, 0, (int)StreamSize);
                if (readBytes != StreamSize)
                    throw new HzDException($"Could not read {StreamSize} bytes from image, only {readBytes} bytes read.");

                core.UpdateImage(this, imageData);
            }
            if (HasEmbeddedData)
            {
                byte[] imageData = new byte[EmbeddedSize];
                int readBytes = file.Read(imageData, 0, (int)EmbeddedSize);
                if (readBytes != EmbeddedSize)
                    throw new HzDException($"Could not read {EmbeddedSize} bytes from image, only {readBytes} bytes read.");

                EmbeddedData = imageData;

                core.WriteCoreData(EmbeddedPosition, imageData);
            }

        }
    }
}
