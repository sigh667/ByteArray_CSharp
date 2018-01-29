using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Xml;
using System.Collections;
using System.Xml.Serialization;
using SevenZip;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace Utils
{
    public class undefined : IComparable
    { 
        public int CompareTo(object obj)
        {
            if(obj is undefined)
            {
                return 0;
            }
            return -1;
        }
    };

    public enum Endian
    {
        BIG_ENDIAN = 0,
        LITTLE_ENDIAN = 1
    }
    public class ByteArray : IDataInput,IDataOutput,IDisposable
    {
        private MemoryStream _memoryStream;
        List<Object> _objectReferences;
        List<string> _stringReferences;
        List<ClassDefinition> _classDefinitions;
        private BinaryReader _reader;
        private BinaryWriter _writer;
        private static ConcurrentDictionary<Type, AMF3SerializationDefine> _typeExchange;
        private Endian _endian;

        private static TimezoneCompensation timezoneCompensation = TimezoneCompensation.Server;
        /// <summary>
        /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
        /// </summary>
        public enum TimezoneCompensation
        {
            /// <summary>
            /// No timezone compensation.
            /// </summary>
            [XmlEnum(Name = "none")]
            None = 0,
            /// <summary>
            /// Auto timezone compensation.
            /// </summary>
            [XmlEnum(Name = "auto")]
            Auto = 1,
            /// <summary>
            /// Convert to the server timezone.
            /// </summary>
            [XmlEnum(Name = "server")]
            Server = 2,
            /// <summary>
            /// Ignore UTCKind for DateTimes received from the client code.
            /// </summary>
            [XmlEnum(Name = "ignoreUTCKind")]
            IgnoreUTCKind = 3
        }

        private enum AMF3SerializationDefine
        {
            Undefined = 0,
            Null = 1,
            BooleanFalse = 2,
            BooleanTrue = 3,
            Int = 4,
            Number = 5,
            String = 6,
            XMLDoc = 7,
            Date = 8,
            Array = 9,
            Object = 10,
            XML = 11,
            ByteArray = 12,
            VectorInt = 13,
            VectorUint = 14,
            VectorNumber = 15,
            VectorObject = 16,
            Dictionary = 17
        };

        /// <summary>
        /// Initializes a new instance of the ByteArray class.
        /// </summary>
        public ByteArray()
        {
            _memoryStream = new MemoryStream();
            this._init();
        }

        /// <summary>
        /// Initializes a new instance of the ByteArray class.
        /// </summary>
        /// <param name="ms">The MemoryStream from which to create the current ByteArray.</param>
        public ByteArray(MemoryStream __ms)
        {
            this._memoryStream = __ms;
            this._init();
        }

        /// <summary>
        /// Initializes a new instance of the ByteArray class.
        /// </summary>
        /// <param name="buffer">The array of unsigned bytes from which to create the current ByteArray.</param>
        public ByteArray(byte[] buffer, int __startPosition = 0, int __length = -1)
        {
            if(__length == -1)
            {
                __length = buffer.Length;
            }
            this._memoryStream = new MemoryStream();
            _memoryStream.Write(buffer, 0, buffer.Length);
            _memoryStream.Position = 0;
            this._init();
        }

        private void _init()
        {
            _reader = new BinaryReader(_memoryStream);
            _writer = new BinaryWriter(_memoryStream);
            //_amf0ObjectReferences = new List<Object>(5);
            _objectReferences = new List<Object>(15);
            _stringReferences = new List<string>();
            _classDefinitions = new List<ClassDefinition>(2);
            _endian = Endian.LITTLE_ENDIAN;
            if (_typeExchange == null)
            {
                _typeExchange = new ConcurrentDictionary<Type, AMF3SerializationDefine>();
                _typeExchange.TryAdd(typeof(undefined), AMF3SerializationDefine.Undefined);
                _typeExchange.TryAdd(typeof(Nullable), AMF3SerializationDefine.Null);
                _typeExchange.TryAdd(typeof(bool), AMF3SerializationDefine.BooleanFalse);
                _typeExchange.TryAdd(typeof(byte), AMF3SerializationDefine.Int);
                _typeExchange.TryAdd(typeof(sbyte), AMF3SerializationDefine.Int);
                _typeExchange.TryAdd(typeof(short), AMF3SerializationDefine.Int);
                _typeExchange.TryAdd(typeof(ushort), AMF3SerializationDefine.Int);
                _typeExchange.TryAdd(typeof(int), AMF3SerializationDefine.Int);
                _typeExchange.TryAdd(typeof(double), AMF3SerializationDefine.Number);
                _typeExchange.TryAdd(typeof(float), AMF3SerializationDefine.Number);
                _typeExchange.TryAdd(typeof(string), AMF3SerializationDefine.String);
                _typeExchange.TryAdd(typeof(XmlDocument), AMF3SerializationDefine.XML);
                _typeExchange.TryAdd(typeof(DateTime), AMF3SerializationDefine.Date);
                _typeExchange.TryAdd(typeof(ArrayList), AMF3SerializationDefine.Array);
                _typeExchange.TryAdd(typeof(IASObjectDefinition), AMF3SerializationDefine.Object);
				//_typeExchange.TryAdd(typeof(XmlNode), AMF3SerializationDefine.XML);
				_typeExchange.TryAdd(typeof(ByteArray), AMF3SerializationDefine.ByteArray);
                _typeExchange.TryAdd(typeof(List<int>), AMF3SerializationDefine.VectorInt);
                _typeExchange.TryAdd(typeof(List<uint>), AMF3SerializationDefine.VectorUint);
                _typeExchange.TryAdd(typeof(List<double>), AMF3SerializationDefine.VectorNumber);
                _typeExchange.TryAdd(typeof(List<float>), AMF3SerializationDefine.VectorNumber);
                _typeExchange.TryAdd(typeof(List<object>), AMF3SerializationDefine.VectorObject);
                _typeExchange.TryAdd(typeof(Hashtable), AMF3SerializationDefine.Dictionary);
            }
        }

        public Endian endian
        {
            set
            {
                this._endian = value;
            }
            get
            {
                return this._endian;
            }
        }

        /// <summary>
        /// Gets the length of the ByteArray object, in bytes.
        /// </summary>
        public uint Length
        {
            get
            {
                return (uint)_memoryStream.Length;
            }
        }

        /// <summary>
        /// Gets or sets the current position, in bytes, of the file pointer into the ByteArray object.
        /// </summary>
        public uint Position
        {
            get { return (uint)_memoryStream.Position; }
            set { _memoryStream.Position = value; }
        }

        /// <summary>
        /// The number of bytes of data available for reading from the current position in the byte array to the end of the array.
        /// </summary>
        /// <value>The number of bytes of data available for reading from the current position.</value>
        public uint bytesAvailable
        {
            get { return Length - Position; }
        }

        /// <summary>
        /// Returns the array of unsigned bytes from which this ByteArray was created.
        /// </summary>
        /// <returns>The byte array from which this ByteArray was created, or the underlying array if a byte array was not provided to the ByteArray constructor during construction of the current instance.</returns>
        public byte[] GetBuffer()
        {
            return _memoryStream.GetBuffer();
        }

        /// <summary>
        /// Writes the ByteArray contents to a byte array, regardless of the Position property.
        /// </summary>
        /// <returns>A new byte array.</returns>
        /// <remarks>
        /// This method omits unused bytes in the underlying MemoryStream from the ByteArray.
        /// This method returns a copy of the contents of the underlying MemoryStream as a byte array. 
        /// </remarks>
        public byte[] ToArray()
        {
            return _memoryStream.ToArray();
        }

        /// <summary>
        /// Compresses the byte array using zlib compression. The entire byte array is compressed.
        /// </summary>
        /// <param name="algorithm">The compression algorithm to use when compressing. Valid values are defined as constants in the CompressionAlgorithm class. The default is to use zlib format.</param>
        /// <remarks>
        /// After the call, the Length property of the ByteArray is set to the new length. The position property is set to the end of the byte array.
        /// </remarks>
        public void Compress(CompressionAlgorithm algorithm = CompressionAlgorithm.Zlib)
        {
            switch (algorithm)
            {
                case CompressionAlgorithm.Deflate:
                    {
                        byte[] buffer = _memoryStream.ToArray();
                        MemoryStream ms = new MemoryStream();
                        DeflateStream deflateStream = new DeflateStream(ms, CompressionMode.Compress, true);
                        deflateStream.Write(buffer, 0, buffer.Length);
                        deflateStream.Flush();
                        deflateStream.Close();
                        deflateStream.Dispose();
                        _memoryStream.Close();
                        _memoryStream.Dispose();
                        _memoryStream = ms;
                        break;
                    }
                case CompressionAlgorithm.Zlib:
                    {
                        byte[] buffer = _memoryStream.ToArray();
                        MemoryStream ms = new MemoryStream();
                        ZlibStream zlibStream = new ZlibStream(ms, CompressionMode.Compress, true);
                        zlibStream.Write(buffer, 0, buffer.Length);
                        zlibStream.Flush();
                        zlibStream.Close();
                        zlibStream.Dispose();
                        _memoryStream.Close();
                        _memoryStream.Dispose();
                        _memoryStream = ms;
                        break;
                    }
                case CompressionAlgorithm.LZMA:
                    {
                        CoderPropID[] propIDs = 
				        {
					        CoderPropID.DictionarySize,
					        CoderPropID.PosStateBits,
					        CoderPropID.LitContextBits,
					        CoderPropID.LitPosBits,
					        CoderPropID.Algorithm,
					        CoderPropID.NumFastBytes,
					        CoderPropID.MatchFinder,
					        CoderPropID.EndMarker
				        };
                                object[] properties = 
				        {
					        (Int32)(1 << 23),
					        (Int32)(2),
					        (Int32)(3),
					        (Int32)(0),
					        (Int32)(1),
					        (Int32)(128),
					        "bt4",
					        false
				        };

                        MemoryStream lzmaStream = new MemoryStream();
                        SevenZip.Compression.LZMA.Encoder encoder = new SevenZip.Compression.LZMA.Encoder();
                        encoder.SetCoderProperties(propIDs, properties);
                        encoder.WriteCoderProperties(lzmaStream);
                        Int64 fileSize = this._memoryStream.Length;
                        
                        for (int i = 0; i < 8; i++)
                            lzmaStream.WriteByte((Byte)(fileSize >> (8 * i)));
                        lzmaStream.Flush();
                        encoder.Code(this._memoryStream, lzmaStream, -1, -1, null);
                        lzmaStream.Flush();
                        _memoryStream.Close();
                        _memoryStream.Dispose();
                        _memoryStream = lzmaStream;
                        break;
                    }
            }
        }

        /// <summary>
        /// Decompresses the byte array. The byte array must have been previously compressed with the Compress() method.
        /// </summary>
        /// <param name="algorithm">The compression algorithm to use when decompressing. This must be the same compression algorithm used to compress the data. Valid values are defined as constants in the CompressionAlgorithm class. The default is to use zlib format.</param>
        public void Uncompress(CompressionAlgorithm algorithm = CompressionAlgorithm.Zlib)
        {
            switch(algorithm)
            {
                case CompressionAlgorithm.Deflate:
                    {
                        Position = 0;
                        DeflateStream deflateStream = new DeflateStream(_memoryStream, CompressionMode.Decompress, false);
                        MemoryStream ms = new MemoryStream();
                        byte[] buffer = new byte[1024];
                        while (true)
                        {
                            int readCount = deflateStream.Read(buffer, 0, buffer.Length);
                            if (readCount > 0)
                                ms.Write(buffer, 0, readCount);
                            else
                                break;
                        }
                        deflateStream.Close();
                        _memoryStream.Close();
                        _memoryStream.Dispose();
                        _memoryStream = ms;
                        _memoryStream.Position = 0;
                        break;
                    }
                case CompressionAlgorithm.Zlib:
                    {
                        //The zlib format is specified by RFC 1950. Zlib also uses deflate, plus 2 or 6 header bytes, and a 4 byte checksum at the end. 
                        //The first 2 bytes indicate the compression method and flags. If the dictionary flag is set, then 4 additional bytes will follow.
                        //Preset dictionaries aren't very common and we don't support them
                        Position = 0;
                        ZlibStream deflateStream = new ZlibStream(_memoryStream, CompressionMode.Decompress, false);
                        MemoryStream ms = new MemoryStream();
                        byte[] buffer = new byte[1024];
                        // Chop off the first two bytes
                        //int b = _memoryStream.ReadByte();
                        //b = _memoryStream.ReadByte();
                        while (true)
                        {
                            int readCount = deflateStream.Read(buffer, 0, buffer.Length);
                            if (readCount > 0)
                                ms.Write(buffer, 0, readCount);
                            else
                                break;
                        }
                        deflateStream.Close();
                        _memoryStream.Close();
                        _memoryStream.Dispose();
                        _memoryStream = ms;
                        _memoryStream.Position = 0;
                        break;
                    }
                case CompressionAlgorithm.LZMA:
                    {
                        Position = 0;
                        MemoryStream ms = new MemoryStream();
                        byte[] properties = new byte[5];
                        if (_memoryStream.Read(properties, 0, 5) != 5)
                            throw (new Exception("input .lzma is too short"));
                        SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
                        decoder.SetDecoderProperties(properties);
                        long outSize = 0;
                        for (int i = 0; i < 8; i++)
                        {
                            int v = _memoryStream.ReadByte();
                            if (v < 0)
                                throw (new Exception("Can't Read 1"));
                            outSize |= ((long)(byte)v) << (8 * i);
                        }
                        long compressedSize = _memoryStream.Length - _memoryStream.Position;
                        decoder.Code(_memoryStream, ms, compressedSize, outSize, null);
                        _memoryStream.Close();
                        _memoryStream.Dispose();
                        _memoryStream = ms;
                        _memoryStream.Position = 0;
                        break;
                    }
            }
        }

        internal byte[] _readStreamBytesEndian(int length)
        {
            if (this.endian == Endian.LITTLE_ENDIAN)
            {
                return _readStreamBytesLittleEndian(length);
            }
            else
            {
                return _readStreamBytesBigEndian(length);
            }
        }

        private byte[] _readStreamBytesBigEndian(int length)
        {
            byte[] value = _readStreamBytesLittleEndian(length);
            byte[] __reverse = new byte[length];
            for (int i = length - 1, j = 0; i >= 0; i--, j++)
            {
                __reverse[j] = value[i];
            }
            return __reverse;
        }

        private byte[] _readStreamBytesLittleEndian(int length)
        {
            return this._reader.ReadBytes(length);

        }

        internal void _writeStreamBytesEndian(byte[] bytes)
        {
            if (this.endian == Endian.LITTLE_ENDIAN)
            {
                _writeStreamBytesLittleEndian(bytes);
            }
            else
            {
                _writeStreamBytesBigEndian(bytes);
            }
        }

        private void _writeStreamBytesBigEndian(byte[] bytes)
        {
            if (bytes == null)
            {
                return;
            }
            for (int i = bytes.Length - 1; i >= 0; i--)
            {
                this._memoryStream.WriteByte(bytes[i]);
            }
        }

        private void _writeStreamBytesLittleEndian(byte[] bytes)
        {
            if (bytes == null)
            {
                return;
            }
            this._memoryStream.Write(bytes, 0, bytes.Length);
        }

        public sbyte readByte()
        {
            sbyte value = (sbyte)this._memoryStream.ReadByte();
            return value;
        }

        public void writeByte(sbyte __byte)
        {
            this._memoryStream.WriteByte((byte)__byte);
        }

        public byte readUnsignedByte()
        {
            return (byte)this._memoryStream.ReadByte();
        }

        public void writeUnsignedByte(byte __ubyte)
        {
            this._memoryStream.WriteByte(__ubyte);
        }

        public bool readBoolean()
        {
            return this._memoryStream.ReadByte() == 1;
        }

        public void writeBoolean(bool __boolean)
        {
            this._memoryStream.WriteByte(__boolean ? ((byte)1) : ((byte)0));
        }

        public int readInt()
        {
            byte[] bytes = this._readStreamBytesEndian(4);
            int value = ((int)(bytes[3] << 24) | ((int)bytes[2] << 16) | ((int)bytes[1] << 8) | (int)bytes[0]);
            return value;
        }

        public void writeInt(int __int)
        {
            byte[] bytes = BitConverter.GetBytes(__int);
            _writeStreamBytesEndian(bytes);
        }

        public short readShort()
        {
            byte[] bytes = this._readStreamBytesEndian(2);
            return (short)((bytes[1] << 8) | bytes[0]);
        }

        public void writeShort(short __short)
        {
            byte[] bytes = BitConverter.GetBytes((short)__short);
            _writeStreamBytesEndian(bytes);
        }

        public ushort readUnsignedShort()
        {
            byte[] bytes = this._readStreamBytesEndian(2);
            return BitConverter.ToUInt16(bytes, 0);
            //return (ushort)(((bytes[1] & 0xff) << 8) | (bytes[0] & 0xff));
        }

        public void writeUnsignedShort(ushort __ushort)
        {
            byte[] bytes = BitConverter.GetBytes((ushort)__ushort);
            _writeStreamBytesEndian(bytes);
        }

        public uint readUnsignedInt()
        {
            byte[] bytes = this._readStreamBytesEndian(4);
            return BitConverter.ToUInt32(bytes, 0);
        }

        public void writeUnsignedInt(uint __uint)
        {
            byte[] bytes = new byte[4];

            bytes[3] = (byte)(0xFF & (__uint >> 24));
            bytes[2] = (byte)(0xFF & (__uint >> 16));
            bytes[1] = (byte)(0xFF & (__uint >> 8));
            bytes[0] = (byte)(0xFF & (__uint >> 0));

            this._writeStreamBytesEndian(bytes);
        }

        private byte[] _readStreamBytes(uint length)
        {
            byte[] __bytes = this._reader.ReadBytes((int)length);
            return __bytes;
        }

        private double readDoubleStreamLittleEndian()
        {
            return BitConverter.ToDouble(_readStreamBytesBigEndian(8), 0);
        }

        public double readDouble()
        {
            return BitConverter.ToDouble(_readStreamBytesEndian(8), 0);
        }

        public void writeDouble(double __double)
        {
            byte[] bytes = BitConverter.GetBytes(__double);
            _writeStreamBytesEndian(bytes);
        }

        private void writeDoubleStreamBigEngian(double __double)
        {
            byte[] bytes = BitConverter.GetBytes(__double);
            _writeStreamBytesBigEndian(bytes);
        }

        public float readFloat()
        {
            return BitConverter.ToSingle(this._readStreamBytesEndian(4), 0);
        }

        public void writeFloat(float __float)
        {
            byte[] bytes = BitConverter.GetBytes(__float);
            _writeStreamBytesEndian(bytes);
        }

        public void readBytes(ByteArray __bytes, uint offset, uint length)
        {
            byte[] readContent = new byte[length];
            this._memoryStream.Read(readContent, (int)offset, (int)length);
            __bytes.writeBytes(new ByteArray(readContent), 0, (uint)readContent.Length);
        }

        public void writeBytes(ByteArray __bytes, uint offset, uint length)
        {
            this._memoryStream.Write(__bytes.ToArray(), (int)offset, (int)length);
            //byte[] writeBytes = new byte[length];
            //Array.Copy(__bytes.ToArray(), this.Position, writeBytes, (int)offset, (int)length);
            //this._writeStreamBytesEndian(writeBytes);
        }

        public string readUTFBytes(int length)
        {
            if (length == 0)
            {
                return string.Empty;
            }
            UTF8Encoding utf8 = new UTF8Encoding(false, true);
            byte[] encodedBytes = this._reader.ReadBytes(length);
            string decodedString = utf8.GetString(encodedBytes, 0, encodedBytes.Length);
            return decodedString;
        }

        public void writeUTFBytes(string __utf)
        {
            //Length - max 65536.
            UTF8Encoding utf8Encoding = new UTF8Encoding();
            byte[] buffer = utf8Encoding.GetBytes(__utf);
            if (buffer.Length > 0)
            {
                this._writer.Write(buffer);
            }
        }

        public string readUTF()
        {
            int length = this.readShort();
            return this.readUTFBytes(length);
        }

        public void writeUTF(string __utf)
        {
            //null string is not accepted
            //in case of custom serialization leads to TypeError: Error #2007: Parameter value must be non-null.  at flash.utils::ObjectOutput/writeUTF()

            //Length - max 65536.
            UTF8Encoding utf8Encoding = new UTF8Encoding();
            int byteCount = utf8Encoding.GetByteCount(__utf);
            byte[] buffer = utf8Encoding.GetBytes(__utf);
            this.writeShort((short)byteCount);
            if (buffer.Length > 0)
            {
                this._writer.Write(buffer);
            }
        }

        public int readU29Int()
        {
            return ReadAMF3IntegerData();
        }

        private int ReadAMF3IntegerData()
        {
            int acc = this._reader.ReadByte();
            int tmp;
            if (acc < 128)
                return acc;
            else
            {
                acc = (acc & 0x7f) << 7;
                tmp = this._reader.ReadByte();
                if (tmp < 128)
                    acc = acc | tmp;
                else
                {
                    acc = (acc | tmp & 0x7f) << 7;
                    tmp = this._reader.ReadByte();
                    if (tmp < 128)
                        acc = acc | tmp;
                    else
                    {
                        acc = (acc | tmp & 0x7f) << 8;
                        tmp = this._reader.ReadByte();
                        acc = acc | tmp;
                    }
                }
            }
            //To sign extend a value from some number of bits to a greater number of bits just copy the sign bit into all the additional bits in the new format.
            //convert/sign extend the 29bit two's complement number to 32 bit
            int mask = 1 << 28; // mask
            int r = -(acc & mask) | acc;
            return r;

            //The following variation is not portable, but on architectures that employ an 
            //arithmetic right-shift, maintaining the sign, it should be fast. 
            //s = 32 - 29;
            //r = (x << s) >> s;
        }

        public void writeU29Int(int __int)
        {
            this.WriteAMF3IntegerData(__int);
        }

        public void WriteAMF3IntegerData(int value)
        {
            //Sign contraction - the high order bit of the resulting value must match every bit removed from the number
            //Clear 3 bits 
            value &= 0x1fffffff;
            if (value < 0x80)
                this._memoryStream.WriteByte((byte)value);
            else
                if (value < 0x4000)
                {
                    this._memoryStream.WriteByte((byte)(value >> 7 & 0x7f | 0x80));
                    this._memoryStream.WriteByte((byte)(value & 0x7f));
                }
                else
                {
                    if (value < 0x200000)
                    {
                        this._memoryStream.WriteByte((byte)(value >> 14 & 0x7f | 0x80));
                        this._memoryStream.WriteByte((byte)(value >> 7 & 0x7f | 0x80));
                        this._memoryStream.WriteByte((byte)(value & 0x7f));
                    }
                    else
                    {
                        this._memoryStream.WriteByte((byte)(value >> 22 & 0x7f | 0x80));
                        this._memoryStream.WriteByte((byte)(value >> 15 & 0x7f | 0x80));
                        this._memoryStream.WriteByte((byte)(value >> 8 & 0x7f | 0x80));
                        this._memoryStream.WriteByte((byte)(value & 0xff));
                    }
                }
        }

        public object readObject()
        {
            this._objectReferences.Clear();
            this._stringReferences.Clear();
            this._classDefinitions.Clear();
            return this.ReadAMF3Data();
        }

        private void writeAMF3Undefined()
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.Undefined);
        }

        private void WriteAMF3Null()
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.Null);
        }

        private void writeAMF3Boolean(bool __bool)
        {
            writeUnsignedByte((byte)(__bool ? AMF3SerializationDefine.BooleanTrue : AMF3SerializationDefine.BooleanFalse));
        }

        private void writeAMF3Int(int __value)
        {
            if (__value >= -268435456 && __value <= 268435455)//check valid range for 29bits
            {
                writeUnsignedByte((byte)AMF3SerializationDefine.Int);
                WriteAMF3IntegerData(__value);
            }
            else
            {
                WriteAMF3Double((double)__value);
            }
        }

        private void WriteAMF3Double(double value)
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.Number);
            writeDoubleStreamBigEngian(value);
        }

        private void writeAMF3String(object __object, bool withFlag = false)
        {
            if(withFlag)
            {
                writeUnsignedByte((byte)AMF3SerializationDefine.String);
            }
            if ((__object as string) == string.Empty)
            {
                WriteAMF3IntegerData(1);
            }
            else
            {
                string value = (string)__object;
                int __index = this._stringReferences.IndexOf(value);// findObjectIndex(value); 
                if (__index == -1)
                {
                    _stringReferences.Add(value);
                    //_objectReferences.Add(value);
                    UTF8Encoding utf8Encoding = new UTF8Encoding();
                    int byteCount = utf8Encoding.GetByteCount(value);
                    int handle = byteCount;
                    handle = handle << 1;
                    handle = handle | 1;
                    WriteAMF3IntegerData(handle);
                    byte[] buffer = utf8Encoding.GetBytes(value);
                    if (buffer.Length > 0)
                    {
                        this._writer.Write(buffer);
                    }
                }
                else
                {
                    int handle = __index;
                    handle = handle << 1;
                    WriteAMF3IntegerData(handle);
                }
            }
        }

        private void writeAMF3XMLDoc(XmlDocument value)
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.XMLDoc);
            string xml = string.Empty;
            if (value.DocumentElement != null && value.DocumentElement.OuterXml != null)
            {
                xml = value.DocumentElement.OuterXml;
            }
            if (xml == string.Empty)
            {
                WriteAMF3IntegerData(1);
            }
            else
            {
                int __index = findObjectIndex(value);
                if (__index == -1)
                {
                    _objectReferences.Add(value);
                    UTF8Encoding utf8Encoding = new UTF8Encoding();
                    int byteCount = utf8Encoding.GetByteCount(xml);
                    int handle = byteCount;
                    handle = handle << 1;
                    handle = handle | 1;
                    WriteAMF3IntegerData(handle);
                    byte[] buffer = utf8Encoding.GetBytes(xml);
                    if (buffer.Length > 0)
                        this._writer.Write(buffer);
                }
                else
                {
                    int handle = __index;
                    handle = handle << 1;
                    WriteAMF3IntegerData(handle);
                }
            }
        }

        private void writeAMF3Date(DateTime value)
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.Date);
            int __index = findObjectIndex(value);
            if (__index == -1)
            {
                _objectReferences.Add(value);
                int handle = 1;
                WriteAMF3IntegerData(handle);

                // Write date (milliseconds from 1970).
                DateTime timeStart = new DateTime(1970, 1, 1, 0, 0, 0);
                switch (timezoneCompensation)
                {
                    case TimezoneCompensation.IgnoreUTCKind:
                        {
                            //Do not convert to UTC, consider we have it in universal time
                            break;
                        }
                    default:
                        {
                            value = value.ToUniversalTime();
                            break;
                        }
                }

                TimeSpan span = value.Subtract(timeStart);
                writeDoubleStreamBigEngian(span.TotalMilliseconds);
            }
            else
            {
                int handle = __index;
                handle = handle << 1;
                WriteAMF3IntegerData(handle);
            }
        }

        private void writeAMF3Array(ArrayList value)
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.Array);

            int __index = findObjectIndex(value); 
            if (__index == -1)
            {
                _objectReferences.Add(value);
                int handle = value.Count;
                handle = handle << 1;
                handle = handle | 1;
                WriteAMF3IntegerData(handle);
                writeAMF3String(string.Empty);//hash name
                for (int i = 0; i < value.Count; i++)
                {
                    writeObject(value[i], true);
                }
            }
            else
            {
                int handle = __index;
                handle = handle << 1;
                WriteAMF3IntegerData(handle);
            }
        }

        private void writeAMF3OrginObject(IASObjectDefinition value)
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.Object);
            int __index = findObjectIndex(value); 
            if (__index == -1)
            {
                _objectReferences.Add(value);
                __index = -1;
                for(int i = 0;i < this._classDefinitions.Count; i++)
                {
                    if(this._classDefinitions[i].ClassName == value.classDefinition.ClassName)
                    {
                        __index = i;
                        break;
                    }
                }
                
                int handle;
                if (__index == -1)
                {
                    if (value.classDefinition.IsExternalizable)
                    {
                        //U29O-traits-ext
                        handle = 0x7;
                    }
                    else
                    {
                        //U29O-traits
                        handle = value.classDefinition.Members.Length;
                        handle = handle << 4;
                        if (value.classDefinition.IsDynamic)
                        {
                            handle = handle | 0xb;
                        }
                        else
                        {
                            handle = handle | 0x3;
                        }
                    }
                    WriteAMF3IntegerData(handle);
                    writeAMF3String(value.classDefinition.ClassName);
                }
                else
                {
                    //U29O-traits-ref
                    handle = __index;
                    handle = handle << 2;
                    handle = handle | 1;
                    WriteAMF3IntegerData(handle);
                }
                

                //WriteAMF3IntegerData(true ? 1 : 0);
                if (value is IDynamic)
                {
                    foreach (System.Collections.Generic.KeyValuePair<string, object> entry in (value as IDynamic).dynamicRoot)
                    {
                        writeAMF3String(entry.Key);
                        writeObject(entry.Value, true);
                    }
                }
                else
                {
                    Type typeValue = value.GetType();
                    foreach (ClassMember member in value.classDefinition.Members)
                    {
                        writeAMF3String(member.Name);
                        FieldInfo feildInfo = typeValue.GetField(member.Name);
                        if(feildInfo != null)
                        {
                            writeObject(feildInfo.GetValue(value), true);
                        }
                        else
                        {
                            PropertyInfo propertyInfo = typeValue.GetProperty(member.Name);
                            if (propertyInfo != null)
                            {
                                writeObject(propertyInfo.GetValue(value), true);
                            }
                        }
                    }
                }
                writeAMF3String(string.Empty);
            }
            else
            {
                //U29O-ref
                int handle = __index;
                handle = handle << 1;
                WriteAMF3IntegerData(handle);
            }
        }

        private void writeAMF3ByteArray(ByteArray byteArray)
        {
            _objectReferences.Add(byteArray);
            writeUnsignedByte((byte)AMF3SerializationDefine.ByteArray);
            int handle = (int)byteArray.Length;
            handle = handle << 1;
            handle = handle | 1;
            WriteAMF3IntegerData(handle);
            byte[] buffer = byteArray.ToArray();
            if (buffer != null)
            {
                this._memoryStream.Write(buffer, 0, buffer.Length);
            }
        }

        private void writeAMF3XML(XmlDocument value)
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.XML);
            string xml = string.Empty;
            if (value.DocumentElement != null && value.DocumentElement.OuterXml != null)
            {
                xml = value.DocumentElement.OuterXml;
            }
            if (xml == string.Empty)
            {
                WriteAMF3IntegerData(1);
            }
            else
            {
                int __index = findObjectIndex(value); 
                if (__index == -1)
                {
                    _objectReferences.Add(value);
                    UTF8Encoding utf8Encoding = new UTF8Encoding();
                    int byteCount = utf8Encoding.GetByteCount(xml);
                    int handle = byteCount;
                    handle = handle << 1;
                    handle = handle | 1;
                    WriteAMF3IntegerData(handle);
                    byte[] buffer = utf8Encoding.GetBytes(xml);
                    if (buffer.Length > 0)
                        this._writer.Write(buffer);
                }
                else
                {
                    int handle = __index;
                    handle = handle << 1;
                    WriteAMF3IntegerData(handle);
                }
            }
        }

        private void writeAMF3VectorInt(IList<int> value)
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.VectorInt);
            int __index = findObjectIndex(value); 
            if (__index == -1)
            {
                _objectReferences.Add(value);
                int handle = value.Count;
                handle = handle << 1;
                handle = handle | 1;
                WriteAMF3IntegerData(handle);
                WriteAMF3IntegerData(value.IsReadOnly ? 1 : 0);
                for (int i = 0; i < value.Count; i++)
                {
                    byte[] bytes = BitConverter.GetBytes(value[i]);
                    _writeStreamBytesBigEndian(bytes);
                }
            }
            else
            {
                int handle = __index;
                handle = handle << 1;
                WriteAMF3IntegerData(handle);
            }
        }

        private void writeAMF3VectorUint(IList<uint> value)
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.VectorUint);
            int __index = findObjectIndex(value); 
            if (__index == -1)
            {
                _objectReferences.Add(value);
                int handle = value.Count;
                handle = handle << 1;
                handle = handle | 1;
                WriteAMF3IntegerData(handle);
                WriteAMF3IntegerData(value.IsReadOnly ? 1 : 0);
                for (int i = 0; i < value.Count; i++)
                {
                    byte[] bytes = BitConverter.GetBytes(value[i]);
                    _writeStreamBytesBigEndian(bytes);
                }
            }
            else
            {
                int handle = __index;
                handle = handle << 1;
                WriteAMF3IntegerData(handle);
            }
        }

        private void writeAMF3VectorNumber(IList<double> value)
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.VectorNumber);
            int __index = findObjectIndex(value); 
            if (__index == -1)
            {
                _objectReferences.Add(value);
                int handle = value.Count;
                handle = handle << 1;
                handle = handle | 1;
                WriteAMF3IntegerData(handle);
                WriteAMF3IntegerData(value.IsReadOnly ? 1 : 0);
                for (int i = 0; i < value.Count; i++)
                {
                    writeDoubleStreamBigEngian(value[i]);
                }
            }
            else
            {
                int handle = __index;
                handle = handle << 1;
                WriteAMF3IntegerData(handle);
            }
        }

        private void writeAMF3VectorObject(IList<object> value)
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.VectorObject);
            int __index = findObjectIndex(value); 
            if (__index == -1)
            {
                _objectReferences.Add(value);
                int handle = value.Count;
                handle = handle << 1;
                handle = handle | 1;
                WriteAMF3IntegerData(handle);

                WriteAMF3IntegerData(value.IsReadOnly ? 1 : 0);
                writeAMF3String("");
                for (int i = 0; i < value.Count; i++)
                {
                    writeObject(value[i], true);
                }
            }
            else
            {
                int handle = __index;
                handle = handle << 1;
                WriteAMF3IntegerData(handle);
            }
        }

        private void writeAMF3Dictionary(Hashtable value)
        {
            writeUnsignedByte((byte)AMF3SerializationDefine.Dictionary);
            int __index = findObjectIndex(value); 
            if (__index == -1)
            {
                _objectReferences.Add(value);
                int handle = value.Count;
                handle = handle << 1;
                handle = handle | 1;
                WriteAMF3IntegerData(handle);

                WriteAMF3IntegerData(false ? 1 : 0);
                foreach (System.Collections.DictionaryEntry entry in value)
                {
                    writeObject(entry.Key, true);
                    writeObject(entry.Value, true);
                }
            }
            else
            {
                int handle = __index;
                handle = handle << 1;
                WriteAMF3IntegerData(handle);
            }
        }

        public void writeObject(object __object, bool inStruct = false)
        {
            if(!inStruct)
            {
                this._objectReferences.Clear();
                this._stringReferences.Clear();
                this._classDefinitions.Clear();
            }
            writeAMF3Object(__object);
        }

        public void writeAMF3Object(object __object, ClassDefinition __classDefinition = null)
        {
            if (__object == null)
            {
                WriteAMF3Null();
                return;
            }

            AMF3SerializationDefine __typecode = AMF3SerializationDefine.Undefined;
            Type itType = __object.GetType();
            while (itType != null)
            {
                if (_typeExchange.ContainsKey(itType))
                {
                    __typecode = (AMF3SerializationDefine)_typeExchange[__object.GetType()];
                    break;
                }
                else
                {

                    Type[] interfaceTypes = itType.GetInterfaces();
                    bool finded = false;
                    foreach (Type interfaceType in interfaceTypes)
                    {
                        if (_typeExchange.ContainsKey(interfaceType))
                        {
                            __typecode = (AMF3SerializationDefine)_typeExchange[interfaceType];
                            finded = true;
                            break;
                        }
                    }
                    if(finded)
                    {
                        break;
                    }
                }
                itType = itType.BaseType;
            }

            switch (__typecode)
            {
                case AMF3SerializationDefine.Undefined:
                    {
                        writeAMF3Undefined();
                        return;
                    }
                case AMF3SerializationDefine.BooleanFalse:                 //Boolean
                case AMF3SerializationDefine.BooleanTrue:
                    {
                        bool value = (bool)__object;
                        writeAMF3Boolean(value);
                        return;
                    }
                case AMF3SerializationDefine.Int:                 //int
                    {
                        int value = Convert.ToInt32(__object);
                        writeAMF3Int(value);
                        return;
                    }
                case AMF3SerializationDefine.Number:                 //Number
                    {
                        double value = Convert.ToDouble(__object);
                        WriteAMF3Double(value);
                        return;
                    }
                case AMF3SerializationDefine.String:                 //String
                    {
                        string value = (string)__object;
                        writeAMF3String(value, true);
                        return;
                    }
                case AMF3SerializationDefine.XMLDoc:                 //XML
                    {
                        XmlDocument value = (XmlDocument)__object;
                        writeAMF3XMLDoc(value);
                        return;
                    }
                case AMF3SerializationDefine.Date:                 //Date
                    {
                        DateTime value = (DateTime)__object;
                        writeAMF3Date(value);
                        return;
                    }
                case AMF3SerializationDefine.Array:                 //Array
                    {
                        ArrayList value = (ArrayList)__object;
                        writeAMF3Array(value);
                        break;
                    }
                case AMF3SerializationDefine.Object:                //Object
                    {
                        IASObjectDefinition value = (IASObjectDefinition)__object;
                        writeAMF3OrginObject(value);
                        break;
                    }
                case AMF3SerializationDefine.XML:
                    {
                        XmlDocument value = (XmlDocument)__object;
                        writeAMF3XML(value);
                        break;
                    }
                case AMF3SerializationDefine.ByteArray:                //ByteArray
                    {
                        ByteArray byteArray = (ByteArray)__object;
                        writeAMF3ByteArray(byteArray);
                        return;
                    }
                case AMF3SerializationDefine.VectorInt:                //Vector.<int>
                    {
                        IList<int> value = (IList<int>)__object;
                        writeAMF3VectorInt(value);
                        return;
                    }
                case AMF3SerializationDefine.VectorUint:                //Vector.<uint>
                    {
                        IList<uint> value = (IList<uint>)__object;
                        writeAMF3VectorUint(value);
                        return;
                    }
                case AMF3SerializationDefine.VectorNumber:                //Vector.<Number>
                    {
                        IList<double> value;
                        if (__object is List<float>)
                        {
                            value = new List<double>();
                            for (int i = 0; i < (__object as IList<float>).Count; i++)
                            {
                                value.Add((double)(__object as IList<float>)[i]);
                            }
                        }
                        else
                        {
                            value = (IList<double>)__object;
                        }
                        writeAMF3VectorNumber(value);
                        return;
                    }
                case AMF3SerializationDefine.VectorObject:                //Vector.<Object>
                    {
                        IList<object> value = (IList<object>)__object;
                        writeAMF3VectorObject(value);
                        return;
                    }
                case AMF3SerializationDefine.Dictionary:                //Dictionary
                    {
                        Hashtable value = (Hashtable)__object;
                        writeAMF3Dictionary(value);
                        return;
                    }
                default:
                    {
                        throw new Exception("Type Not Support");
                    }
            }
        }

        public void Dispose()
        {
            this._memoryStream.Close();
            this._memoryStream.Dispose();
            this._objectReferences.Clear();
            this._objectReferences = null;
            this._stringReferences.Clear();
            this._stringReferences = null;
            this._classDefinitions.Clear();
            this._classDefinitions = null;
            this._reader.Close();
            this._reader.Dispose();
            this._writer.Close();
            this._writer.Dispose();
        }

        internal ClassDefinition ReadClassDefinition(int handle)
        {
            ClassDefinition classDefinition = null;
            //an inline object
            bool inlineClassDef = ((handle & 1) != 0);
            handle = handle >> 1;
            if (inlineClassDef)
            {
                //inline class-def
                string typeIdentifier = this.ReadAMF3Data(AMF3SerializationDefine.String) as string;
                //flags that identify the way the object is serialized/deserialized
                bool externalizable = ((handle & 1) != 0);
                handle = handle >> 1;
                bool dynamic = ((handle & 1) != 0);
                handle = handle >> 1;
                ClassMember[] members = new ClassMember[handle];
                for (int i = 0; i < handle; i++)
                {
                    string name = this.ReadAMF3Data(AMF3SerializationDefine.String) as string;
                    ClassMember classMember = new ClassMember(name, BindingFlags.Default, MemberTypes.Custom, null);
                    members[i] = classMember;
                }
                classDefinition = new ClassDefinition(typeIdentifier, members, externalizable, dynamic);
                AddClassReference(classDefinition);
            }
            else
            {
                //A reference to a previously passed class-def
                classDefinition = ReadClassReference(handle);
            }
            return classDefinition;
        }

        private object ReadAMF3Data()
        {
            byte typeCode = this._reader.ReadByte();
            return this.ReadAMF3Data((AMF3SerializationDefine)typeCode);
        }

        private object ReadAMF3Data(AMF3SerializationDefine typeMarker)
        {
            object returnResult;
            switch (typeMarker)
            {
                case AMF3SerializationDefine.Undefined:
                    {
                        //null
                        returnResult = new undefined();
                        break;
                    }
                case AMF3SerializationDefine.Null:
                    {
                        //null
                        returnResult = null;
                        break;
                    }
                case AMF3SerializationDefine.BooleanFalse:
                    {
                        //boolean
                        returnResult = false;
                        break;
                    }
                case AMF3SerializationDefine.BooleanTrue:
                    {
                        //boolean
                        returnResult = true;
                        break;
                    }
                case AMF3SerializationDefine.Int:
                    {
                        //int
                        returnResult = this.ReadAMF3IntegerData();
                        break;

                    }
                case AMF3SerializationDefine.Number:
                    {
                        //number
                        returnResult = this.readDoubleStreamLittleEndian();
                        break;

                    }
                case AMF3SerializationDefine.String:
                    {
                        //string
                        int handle = ReadAMF3IntegerData();
                        bool inline = ((handle & 1) != 0);
                        handle = handle >> 1;
                        if (inline)
                        {
                            int length = handle;
                            if (length == 0)
                                return string.Empty;
                            string str = readUTFBytes(length);
                            AddAMF3StringReference(str);
                            returnResult = str;
                        }
                        else
                        {
                            return ReadAMF3StringReference(handle);
                        }
                        break;
                    }
                case AMF3SerializationDefine.XMLDoc:
                    {
                        //xml
                        int handle = ReadAMF3IntegerData();
                        bool inline = ((handle & 1) != 0);
                        handle = handle >> 1;
                        string xml = string.Empty;
                        if (inline)
                        {
                            if (handle > 0)//length
                                xml = this.readUTFBytes(handle);
                            
                            XmlDocument xmlDocument = new XmlDocument();
                            if (xml != null && xml != string.Empty)
                                xmlDocument.LoadXml(xml);
                            AddAMF3ObjectReference(xmlDocument);
                            returnResult = xmlDocument;
                        }
                        else
                        {
                            return ReadAMF3ObjectReference(handle) as XmlDocument;
                        }
                        break;
                    }
                case AMF3SerializationDefine.Date:
                    {
                        //date
                        int handle = ReadAMF3IntegerData();
                        bool inline = ((handle & 1) != 0);
                        handle = handle >> 1;
                        if (inline)
                        {
                            //double milliseconds = this.readDouble();
                            double milliseconds = this.readDoubleStreamLittleEndian();
                            DateTime start = new DateTime(1970, 1, 1, 0, 0, 0);

                            DateTime date = start.AddMilliseconds(milliseconds);
                            date = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                            switch (timezoneCompensation)
                            {
                                case TimezoneCompensation.None:
                                    //No conversion by default
                                    break;
                                case TimezoneCompensation.Auto:
                                    //Not applicable for AMF3
                                    break;
                                case TimezoneCompensation.Server:
                                    //Convert to local time
                                    date = date.ToLocalTime();
                                    break;
                            }
                            AddAMF3ObjectReference(date);
                            returnResult = date;
                        }
                        else
                        {
                            return (DateTime)ReadAMF3ObjectReference(handle);
                        }
                        break;
                    }
                case AMF3SerializationDefine.Array:
                    {
                        //array
                        int handle = ReadAMF3IntegerData();
                        bool inline = ((handle & 1) != 0); handle = handle >> 1;
                        if (inline)
                        {
                            Dictionary<string, object> hashtable = null;
                            string key = this.ReadAMF3Data(AMF3SerializationDefine.String) as string;
                            while (key != null && key != string.Empty)
                            {
                                if (hashtable == null)
                                {
                                    hashtable = new Dictionary<string, object>();
                                    AddAMF3ObjectReference(hashtable);
                                }
                                object value = ReadAMF3Data();
                                hashtable.Add(key, value);
                                key = this.ReadAMF3Data(AMF3SerializationDefine.String) as string;
                            }
                            //Not an associative array
                            if (hashtable == null)
                            {
                                ArrayList array = new ArrayList(handle);
                                AddAMF3ObjectReference(array);
                                for (int i = 0; i < handle; i++)
                                {
                                    //Grab the type for each element.
                                    byte typeCode = this._reader.ReadByte();
                                    object value = ReadAMF3Data((AMF3SerializationDefine)typeCode);
                                    array.Add(value);
                                }
                                returnResult = array;
                            }
                            else
                            {
                                for (int i = 0; i < handle; i++)
                                {
                                    object value = ReadAMF3Data();
                                    hashtable.Add(i.ToString(), value);
                                }
                                returnResult = hashtable;
                            }
                        }
                        else
                        {
                            return ReadAMF3ObjectReference(handle);
                        }
                        break;
                    }
                case AMF3SerializationDefine.Object:
                    {
                        //object
                        int handle = ReadAMF3IntegerData();
                        bool inline = ((handle & 1) != 0);
                        handle = handle >> 1;
                        if (inline)
                        {
                            ClassDefinition classDefinition = ReadClassDefinition(handle);
                            returnResult = classDefinition.getClass();
                            
                            AddAMF3ObjectReference(returnResult);
                            
                            if (classDefinition.IsExternalizable)
                            {
                                if (returnResult is IExternalizable)
                                {
                                    IExternalizable externalizable = returnResult as IExternalizable;
                                    externalizable.readExternal(this);
                                }
                                else
                                {
                                    throw new Exception("returnResult must be IExternalizable");
                                }
                            }
                            else
                            {
                                for (int i = 0; i < classDefinition.MemberCount; i++)
                                {
                                    string key = classDefinition.Members[i].Name;
                                    object value = ReadAMF3Data();
                                    if (returnResult is ASObject)
                                    {
                                        (returnResult as ASObject).dynamicRoot[key] = value;
                                    }
                                    else
                                    {
                                        Type type = returnResult.GetType();
                                        
                                        PropertyInfo propertyInfo = type.GetProperty(key);
                                        FieldInfo fieldInfo = type.GetField(key);
                                        if (propertyInfo != null)
                                        {
                                            propertyInfo.SetValue(returnResult, value);
                                        }
                                        else if(fieldInfo != null)
                                        {
                                            fieldInfo.SetValue(returnResult, value);
                                        }
                                        else if (returnResult is IDynamic)
                                        {
                                            (returnResult as IDynamic).dynamicRoot[key] = value;
                                        }
                                        else
                                        {
                                            throw new Exception("Type Property or Field Must Exsit Or Type Must Be IDynamic!");
                                        }
                                    }
                                }
                                if (classDefinition.IsDynamic)
                                {
                                    string key = ReadAMF3Data(AMF3SerializationDefine.String) as string;
                                    while (key != null && key != string.Empty)
                                    {
                                        object value = ReadAMF3Data();
                                        if (returnResult is ASObject)
                                        {
                                            (returnResult as ASObject).dynamicRoot[key] = value;
                                        }
                                        else
                                        {
                                            Type type = returnResult.GetType();

                                            PropertyInfo propertyInfo = type.GetProperty(key);
                                            FieldInfo fieldInfo = type.GetField(key);
                                            if (propertyInfo != null)
                                            {
                                                propertyInfo.SetValue(returnResult, value);
                                            }
                                            else if (fieldInfo != null)
                                            {
                                                fieldInfo.SetValue(returnResult, value);
                                            }
                                            else if (returnResult is IDynamic)
                                            {
                                                (returnResult as IDynamic).dynamicRoot[key] = value;
                                            }
                                            else
                                            {
                                                throw new Exception("Type Property or Field Must Exsit Or Type Must Be IDynamic!");
                                            }
                                        }
                                        key = ReadAMF3Data(AMF3SerializationDefine.String) as string;
                                    }
                                }
                            }
                        }
                        else
                        {
                            //U290-ref
                            return ReadAMF3ObjectReference(handle);
                        }
                        break;
                    }
                case AMF3SerializationDefine.XML:
                    {
                        //xml
                        int handle = ReadAMF3IntegerData();
                        bool inline = ((handle & 1) != 0);
                        handle = handle >> 1;
                        string xml = string.Empty;
                        if (inline)
                        {
                            if (handle > 0)//length
                                xml = this.readUTFBytes(handle);
                            XmlDocument xmlDocument = new XmlDocument();
                            if (xml != null && xml != string.Empty)
                                xmlDocument.LoadXml(xml);
                            AddAMF3ObjectReference(xmlDocument);
                            returnResult = xmlDocument;
                        }
                        else
                        {
                            return ReadAMF3ObjectReference(handle) as XmlDocument;
                        }
                        break;
                    }
                case AMF3SerializationDefine.ByteArray:
                    {
                        //bytearray
                        int handle = ReadAMF3IntegerData();
                        bool inline = ((handle & 1) != 0);
                        handle = handle >> 1;
                        if (inline)
                        {
                            int length = handle;
                            byte[] buffer = this._reader.ReadBytes(length);
                            ByteArray ba = new ByteArray(buffer);
                            AddAMF3ObjectReference(ba);
                            returnResult = ba;
                        }
                        else
                        {
                            return ReadAMF3ObjectReference(handle) as ByteArray;
                        }
                        break;
                    }
                case AMF3SerializationDefine.VectorInt:
                    {
                        //vector.<int>
                        int handle = ReadAMF3IntegerData();
                        bool inline = ((handle & 1) != 0); handle = handle >> 1;
                        if (inline)
                        {
                            List<int> list = new List<int>(handle);
                            AddAMF3ObjectReference(list);
                            int @fixed = ReadAMF3IntegerData();
                            for (int i = 0; i < handle; i++)
                            {
                                byte[] buffer = this._reader.ReadBytes(4);
                                list.Add((int)((buffer[0] << 24) | (buffer[1] << 16) | (buffer[2] << 8) | buffer[3]));
                            }
                            returnResult = (@fixed == 1 ? list.AsReadOnly() as IList<int> : list);
                        }
                        else
                        {
                            return ReadAMF3ObjectReference(handle) as List<int>;
                        }
                        break;
                    }
                case AMF3SerializationDefine.VectorUint:
                    {
                        //vector.<uint>
                        int handle = ReadAMF3IntegerData();
                        bool inline = ((handle & 1) != 0); handle = handle >> 1;
                        if (inline)
                        {
                            List<uint> list = new List<uint>(handle);
                            AddAMF3ObjectReference(list);
                            int @fixed = ReadAMF3IntegerData();
                            for (int i = 0; i < handle; i++)
                            {
                                //todo
                                list.Add((uint)readInt());
                            }
                            returnResult = ( @fixed == 1 ? list.AsReadOnly() as IList<uint> : list);
                        }
                        else
                        {
                            return ReadAMF3ObjectReference(handle) as List<uint>;
                        }
                        break;
                    }
                case AMF3SerializationDefine.VectorNumber:
                    {
                        //vector.<doubel>
                        int handle = ReadAMF3IntegerData();
                        bool inline = ((handle & 1) != 0); handle = handle >> 1;
                        if (inline)
                        {
                            List<double> list = new List<double>(handle);
                            AddAMF3ObjectReference(list);
                            int @fixed = ReadAMF3IntegerData();
                            for (int i = 0; i < handle; i++)
                            {
                                list.Add(this.readDoubleStreamLittleEndian());
                            }
                            returnResult = ( @fixed == 1 ? list.AsReadOnly() as IList<double> : list);
                        }
                        else
                        {
                            return ReadAMF3ObjectReference(handle) as List<double>;
                        }
                        break;
                    }
                case AMF3SerializationDefine.VectorObject:
                    {
                        //vector.<object>
                        int handle = ReadAMF3IntegerData();
                        bool inline = ((handle & 1) != 0); handle = handle >> 1;
                        if (inline)
                        {
                            int @fixed = ReadAMF3IntegerData();
                            string typeIdentifier = this.ReadAMF3Data(AMF3SerializationDefine.String) as string;
                            IList list = new List<object>();
                            AddAMF3ObjectReference(list);
                            for (int i = 0; i < handle; i++)
                            {
                                byte typeCode = this._reader.ReadByte();
                                object obj = ReadAMF3Data((AMF3SerializationDefine)typeCode);
                                list.Add(obj);
                            }
                            if (@fixed == 1)
                                return list.GetType().GetMethod("AsReadOnly").Invoke(list, null) as IList;

                            returnResult = list;
                        }
                        else
                        {
                            return ReadAMF3ObjectReference(handle) as List<object>;
                        }
                        break;
                    }
                case AMF3SerializationDefine.Dictionary:
                    {
                        //dictionary
                        int handle = ReadAMF3IntegerData();
                        bool inline = ((handle & 1) != 0); handle = handle >> 1;
                        if (inline)
                        {
                            bool @weakKeys = readBoolean();
                            Hashtable result = new Hashtable(handle);
                            AddAMF3ObjectReference(result);
                            for (int i = 0; i < handle; i++)
                            {
                                object key = this.ReadAMF3Data();
                                object value = this.ReadAMF3Data();
                                result[key] = value;
                            }
                            returnResult = result;
                        }
                        else
                        {
                            return ReadAMF3ObjectReference(handle) as Hashtable;
                        }
                        break;
                    }
                default:
                    {
                        throw new Exception("Not Support Type");
                    }
            }
            return returnResult;
        }

        private int findObjectIndex(object value)
        {
            int __index = this._objectReferences.IndexOf(value); 
            if (__index != -1)
            {
                return __index;
            }
            
            for (int i = 0; i < this._objectReferences.Count; i++ )
            {
                if(compareEqual(this._objectReferences[i], value))
                {
                    return i;
                }
            }
            return -1;
        }

        private bool compareEqual(object a, object b)
        {
            if(a == b)
            {
                return true;
            }
            else if(a == null && b == null)
            {
                return true;
            }
            else if(a is undefined && b is undefined)
            {
                return true;
            }
            else if((a is float || a is double) && (b is float || b is double))
            {
                return Math.Abs((double)a - (double)b) <= 1e-6;
            }
            else if((a is byte || a is sbyte || a is short || a is ushort || a is int) && (b is byte || b is sbyte || b is short || b is ushort || b is int))
            {
                return (int)a == (int)b;
            }
            else
            {
                Type aType = a.GetType();
                Type bType = b.GetType();
                if(aType != bType)
                {
                    return false;
                }

                if(a is DateTime)
                {
                    if ((((DateTime)a).CompareTo((DateTime)b)) == 0)
                    {
                        return true;
                    }
                }
                else if(a is ByteArray)
                {
                    if ((a as ByteArray).Length == (b as ByteArray).Length)
                    {
                        byte[] __a = (a as ByteArray).ToArray();
                        byte[] __b = (b as ByteArray).ToArray();
                        for (int j = 0; j < __a.Length; j++)
                        {
                            if (__a[j] != __b[j])
                            {
                                continue;
                            }
                        }
                        return true;
                    }
                }
                else if (a is IList)
                {
                    if ((a as IList).Count == (b as IList).Count)
                    {
                        for (int j = 0; j < (b as IList).Count; j++)
                        {
                            if (!compareEqual((a as IList)[j], (b as IList)[j]))
                            {
                                return false;
                            }
                        }
                        return true;
                    }
                }
                else if(a is Dictionary<string, object>)
                {
                    foreach(KeyValuePair<string, object> key in (a as Dictionary<string, object>))
                    {
                        if((b as Dictionary<string, object>).ContainsKey(key.Key))
                        {
                            if(!compareEqual((a as Dictionary<string, object>)[key.Key], (b as Dictionary<string, object>)[key.Key]))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else if(a is Hashtable)
                {
                    foreach (DictionaryEntry entry in (a as Hashtable))
                    {
                        if ((b as Hashtable).ContainsKey(entry.Key))
                        {
                            if (!compareEqual((a as Hashtable)[entry.Key], (b as Hashtable)[entry.Key]))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            foreach (DictionaryEntry entryChildInB in (b as Hashtable))
                            {
                                if (compareEqual(entry.Key, entryChildInB.Key) && compareEqual((a as Hashtable)[entry.Key], (b as Hashtable)[entryChildInB.Key]))
                                {
                                    return true;
                                }
                            }
                            return false;
                        }
                    }
                    return true;
                }
                else if(a is XmlDocument)
                {
                    MemoryStream aStream = new MemoryStream();
                    XmlTextWriter aWriter = new XmlTextWriter(aStream,Encoding.UTF8);
                    aWriter.Formatting = Formatting.Indented;
                    (a as XmlDocument).Save(aWriter);

                    MemoryStream bStream = new MemoryStream();
                    XmlTextWriter bWriter = new XmlTextWriter(bStream, Encoding.UTF8);
                    bWriter.Formatting = Formatting.Indented;
                    (b as XmlDocument).Save(bWriter);

                    aWriter.Close();
                    aWriter.Dispose();
                    bWriter.Close();
                    bWriter.Dispose();

                    if (aStream.Length != bStream.Length)
                    {
                        aStream.Close();
                        aStream.Dispose();
                        bStream.Close();
                        bStream.Dispose();
                        return false;
                    }

                    byte[] aBuffer = aStream.ToArray();
                    byte[] bBuffer = bStream.ToArray();

                    aStream.Close();
                    aStream.Dispose();
                    bStream.Close();
                    bStream.Dispose();

                    for(int i = 0; i < aBuffer.Length; i++)
                    {
                        if(aBuffer[i] != bBuffer[i])
                        {
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    if (a is IASObjectDefinition)
                    {
                        ClassDefinition aClassDefinintion = (a as IASObjectDefinition).classDefinition;
                        for (int j = 0; j < aClassDefinintion.MemberCount; j++)
                        {
                            FieldInfo aFieldMember = a.GetType().GetField(aClassDefinintion.Members[j].Name);
                            if (aFieldMember != null)
                            {
                                FieldInfo bFieldMember = b.GetType().GetField(aClassDefinintion.Members[j].Name);
                                object aValue = aFieldMember.GetValue(a);
                                object bValue = bFieldMember.GetValue(b);
                                if (!compareEqual(aValue, bValue))
                                {
                                    return false;
                                }
                            }
                            else
                            {
                                PropertyInfo aPropertyInfo = a.GetType().GetProperty(aClassDefinintion.Members[j].Name);
                                if(aPropertyInfo != null)
                                {
                                    PropertyInfo bPropertyInfo = b.GetType().GetProperty(aClassDefinintion.Members[j].Name);
                                    object aValue = aPropertyInfo.GetValue(a);
                                    object bValue = bPropertyInfo.GetValue(b);
                                    if (!compareEqual(aValue, bValue))
                                    {
                                        return false;
                                    }
                                }
                            }
                        }
                    }
                    if(a is IExternalizable)
                    {
                        ByteArray aBytes = new ByteArray();
                        ByteArray bBytes = new ByteArray();
                        (a as IExternalizable).writeExternal(aBytes);
                        (b as IExternalizable).writeExternal(bBytes);
                        if(!compareEqual(aBytes, bBytes))
                        {
                            aBytes.Dispose();
                            bBytes.Dispose();
                            return false;
                        }
                    }
                    if(a is IDynamic)
                    {
                        Dictionary<string, object> aDyn = (a as IDynamic).dynamicRoot;
                        Dictionary<string, object> bDyn = (b as IDynamic).dynamicRoot;
                        if(!compareEqual(aDyn, bDyn))
                        {
                            return false;
                        }
                    }
                    if(!(a is IExternalizable || a is IDynamic || a is IASObjectDefinition))
                    {
                        return false;
                    }
                    return true;
                }
            }
            return false;
        }

        private void AddAMF3StringReference(string instance)
        {
            _stringReferences.Add(instance);
        }

        private string ReadAMF3StringReference(int index)
        {
            return _stringReferences[index];
        }

        private void AddAMF3ObjectReference(object instance)
        {
            _objectReferences.Add(instance);
        }

        private object ReadAMF3ObjectReference(int index)
        {
            return _objectReferences[index];
        }

        internal void AddClassReference(ClassDefinition classDefinition)
        {
            _classDefinitions.Add(classDefinition);
        }

        internal ClassDefinition ReadClassReference(int index)
        {
            return _classDefinitions[index] as ClassDefinition;
        }

        public string ComputeMD5()
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] retVal = md5.ComputeHash(this.GetBuffer());
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < retVal.Length; i++)
            {
                sb.Append(retVal[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }

    public enum CompressionAlgorithm
    {
        /// <summary>
        /// Defines the string to use for the deflate compression algorithm.
        /// </summary>
        Deflate,

        /// <summary>
        /// Defines the string to use for the zlib compression algorithm.
        /// </summary>
        Zlib,

        /// <summary>
        /// Defines the string to use for the zlib compression algorithm.
        /// </summary>
        LZMA
    };

    public class ZlibStream : DeflateStream
    {
        private bool _hasRead = false;

        /// <summary>
        /// Largest prime smaller than 65536.
        /// </summary>
        const int ModAdler = 65521;

        // NMAX is the largest n such that 255n(n+1)/2 + (n+1)(BASE-1) <= 2^32-1
        const int NMAX = 5552;

        uint _checksum;
        readonly CompressionMode _mode;
        private readonly bool _leaveOpen;
        Stream _stream;

        /// <summary>  
        /// Initializes a new instance of the System.IO.Compression.ZlibStream class using the specified stream and System.IO.Compression.CompressionMode value.
        /// </summary>  
        /// <param name="stream">The stream to compress or decompress.</param>
        /// <param name="mode">One of the System.IO.Compression.CompressionMode values that indicates the action to take.</param>
        public ZlibStream(Stream stream, CompressionMode mode)
            : this(stream, mode, false)
        {
        }

        /// <summary>  
        /// Initializes a new instance of the System.IO.Compression.ZlibStream class using the specified stream and System.IO.Compression.CompressionMode value. 
        /// </summary>  
        /// <param name="stream">The stream to compress or decompress.</param>
        /// <param name="mode">One of the System.IO.Compression.CompressionMode values that indicates the action to take.</param>
        /// <param name="leaveOpen">true to leave the stream open; otherwise, false.</param>  
        public ZlibStream(Stream stream, CompressionMode mode, bool leaveOpen)
            : base(stream, mode, true)
        {
            _stream = stream;
            _leaveOpen = leaveOpen;
            _mode = mode;
            if (mode == CompressionMode.Compress)
            {
                //A zlib stream has the following structure:
                //   0   1
                // +---+---+
                // |CMF|FLG|   (more-->)
                // +---+---+
                //
                // (if FLG.FDICT set)
                //
                //   0   1   2   3
                // +---+---+---+---+
                // |     DICTID    |   (more-->)
                // +---+---+---+---+
                //
                // +=====================+---+---+---+---+
                // |...compressed data...|    ADLER32    |
                // +=====================+---+---+---+---+

                //CMF (Compression Method and flags)
                //bits 0 to 3  CM     Compression method
                //bits 4 to 7  CINFO  Compression info
                //CM = 8 denotes the "deflate" compression method with a window size up to 32K.
                //CINFO (Compression info) For CM = 8, CINFO is the base-2 logarithm of the LZ77 window size, minus eight (CINFO=7 indicates a 32K window size).
                //  FLG (FLaGs)
                //     This flag byte is divided as follows:
                //
                //        bits 0 to 4  FCHECK  (check bits for CMF and FLG)
                //        bit  5       FDICT   (preset dictionary)
                //        bits 6 to 7  FLEVEL  (compression level)

                //deflate implementation, those bytes are 0x58 and 0x85
                //we use a window size of 8K and the value of FLEVEL should be 2 (default algorithm)
                //some comments indicating this is interpreted as max window size, so you might also try setting this to 7, corresponding to a window size of 32K.
                byte[] header = { 0x78, 0xda };
                //byte[] header = { 0x58, 0x85 };
                //byte[] header = { 0x78, 0x9c };
                _stream.Write(header, 0, header.Length);
                _checksum = 1;
            }
        }

        public override int Read(byte[] array, int offset, int count)
        {
            //The zlib format is specified by RFC 1950. Zlib also uses deflate, plus 2 or 6 header bytes, and a 4 byte checksum at the end. 
            //The first 2 bytes indicate the compression method and flags. If the dictionary flag is set, then 4 additional bytes will follow.
            //Preset dictionaries aren't very common and we don't support them.
            if (_hasRead == false)
            {
                // Chop off the first two bytes
                int h1 = _stream.ReadByte();
                int h2 = _stream.ReadByte();
                _hasRead = true;
            }
            return base.Read(array, offset, count);
        }

        public override IAsyncResult BeginWrite(byte[] array, int offset, int count, AsyncCallback asyncCallback, object asyncState)
        {
            IAsyncResult result = base.BeginWrite(array, offset, count, asyncCallback, asyncState);
            _checksum = Adler32(_checksum, array, offset, count);
            return result;
        }

        public override void Write(byte[] array, int offset, int count)
        {
            base.Write(array, offset, count);
            _checksum = Adler32(_checksum, array, offset, count);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing && _mode == CompressionMode.Compress && _stream != null)
            {
                _stream.WriteByte((byte)((_checksum >> 24) & 0xff));
                _stream.WriteByte((byte)((_checksum >> 16) & 0xff));
                _stream.WriteByte((byte)((_checksum >> 8) & 0xff));
                _stream.WriteByte((byte)(_checksum & 0xff));
                
                //_stream.Write(BitConverter.GetBytes(_checksum), 0, 4);
                if (!_leaveOpen) _stream.Close();
                _stream = null;
            }
        }

        private static uint Adler32(uint adler, byte[] buffer, int offset, int length)
        {
            if (buffer == null)
                return 1;

            int s1 = (int)(adler & 0xffff);
            int s2 = (int)((adler >> 16) & 0xffff);

            while (length > 0)
            {
                int k = length < NMAX ? length : NMAX;
                length -= k;
                while (k >= 16)
                {
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    s1 += buffer[offset++]; s2 += s1;
                    k -= 16;
                }
                if (k != 0)
                {
                    do
                    {
                        s1 += buffer[offset++];
                        s2 += s1;
                    }
                    while (--k != 0);
                }
                s1 %= ModAdler;
                s2 %= ModAdler;
            }
            return (uint)((s2 << 16) | s1);
        }
    }

    public class ASObject : IASObjectDefinition,IDynamic
    {
        private Dictionary<string, object> _dynamicRoot; 
        private ClassDefinition _classDefinition;
        public ASObject()
        {
            _classDefinition = new ClassDefinition("", new ClassMember[0], false, true);
            _dynamicRoot = new Dictionary<string, object>();
        }
        public ClassDefinition classDefinition
        {
            set
            {
                _classDefinition = value;
            }
            get
            {
                return this._classDefinition;
            }
        }

        public Dictionary<string, object> dynamicRoot
        {
            set
            {
                _dynamicRoot = value;
            }
            get
            {
                return _dynamicRoot;
            }
        }
    }

    public interface IASObjectDefinition
    {
        ClassDefinition classDefinition { set; get; }
    }

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class ClassDefinition
    {
        private string _className;
        private ClassMember[] _members;
        private bool _externalizable;
        private bool _dynamic;

        internal static ClassMember[] EmptyClassMembers = new ClassMember[0];

        internal ClassDefinition(string className, ClassMember[] members, bool externalizable, bool dynamic)
        {
            _className = className;
            _members = members;
            _externalizable = externalizable;
            _dynamic = dynamic;
        }

        /// <summary>
        /// Gets the class name.
        /// </summary>
        public string ClassName { get { return _className; } }
        /// <summary>
        /// Gets the class member count.
        /// </summary>
        public int MemberCount
        {
            get
            {
                if (_members == null)
                    return 0;
                return _members.Length;
            }
        }
        /// <summary>
        /// Gets the array of class members.
        /// </summary>
        public ClassMember[] Members { get { return _members; } }
        /// <summary>
        /// Indicates whether the class is externalizable.
        /// </summary>
        public bool IsExternalizable { get { return _externalizable; } }
        /// <summary>
        /// Indicates whether the class is dynamic.
        /// </summary>
        public bool IsDynamic { get { return _dynamic; } }
        /// <summary>
        /// Indicates whether the class is typed (not anonymous).
        /// </summary>
        public bool IsTypedObject { get { return (_className != null && _className != string.Empty); } }

        private static Dictionary<string, Type> _registerClassAliases = new Dictionary<string,Type>();
        public static void registerClassAlias(string aliasName, Type classObject)
        {
            _registerClassAliases[aliasName] = classObject;
        }
        public object getClass()
        {
            Type type = null;
            
            if(!string.IsNullOrEmpty(_className))
            {
                if(_registerClassAliases.Keys.Contains(_className))
                {
                    type = _registerClassAliases[_className];
                }
                else
                {
                    Assembly[] ass = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (Assembly assembly in ass)
                    {
                        Type assemblyClassType = assembly.GetType(_className);
                        if (assemblyClassType != null)
                        {
                            type = assemblyClassType;
                            break;
                        }
                    }
                }
            }
            
            if(type != null)
            {
                return Activator.CreateInstance(type);
            }
            else
            {
                return new ASObject();
            }
        }
    }

    /// <summary>
    /// This type supports the Fluorine infrastructure and is not intended to be used directly from your code.
    /// </summary>
    public sealed class ClassMember
    {
        string _name;
        BindingFlags _bindingFlags;
        MemberTypes _memberType;
        /// <summary>
        /// Cached member custom attributes.
        /// </summary>
        object[] _customAttributes;

        internal ClassMember(string name, BindingFlags bindingFlags, MemberTypes memberType, object[] customAttributes)
        {
            _name = name;
            _bindingFlags = bindingFlags;
            _memberType = memberType;
            _customAttributes = customAttributes;
        }
        /// <summary>
        /// Gets the member name.
        /// </summary>
        public string Name
        {
            get { return _name; }
        }
        /// <summary>
        /// Gets the member binding flags.
        /// </summary>
        public BindingFlags BindingFlags
        {
            get { return _bindingFlags; }
        }
        /// <summary>
        /// Gets the member type.
        /// </summary>
        public MemberTypes MemberType
        {
            get { return _memberType; }
        }
        /// <summary>
        /// Gets member custom attributes.
        /// </summary>
        public object[] CustomAttributes
        {
            get { return _customAttributes; }
        }
    }

    public interface IExternalizable
    {
        void writeExternal(ByteArray output);
        void readExternal(ByteArray input);
    }

    public interface IDynamic
    {
        Dictionary<string, object> dynamicRoot { set; get; }
    }

    public interface IWeightData
    {
    }

    public interface ISerializable
    {
        IDataOutput toBytes(IDataOutput __targetBytes = null);
        void fromBytes(IDataInput __serializationBytes, IWeightData __weight = null);
    }

    public interface IDataInput
    {
        uint bytesAvailable { get; }
        Endian endian { set; get; }
        bool readBoolean();
        sbyte readByte();
        void readBytes(ByteArray __bytes, uint offset, uint length);
        double readDouble();
        float readFloat();
        int readInt();
        object readObject();
        short readShort();
        byte readUnsignedByte();
        uint readUnsignedInt();
        ushort readUnsignedShort();
        string readUTF();
        string readUTFBytes(int length);
        int readU29Int();
    }

    public interface IDataOutput
    {
        Endian endian { set; get; }
        void writeBoolean(bool __boolean);
        void writeByte(sbyte __byte);
        void writeUnsignedByte(byte __ubyte);
        void writeBytes(ByteArray __bytes, uint offset, uint length);
        void writeDouble(double __double);
        void writeFloat(float __float);
        void writeInt(int __int);
        void writeObject(object __object, bool inStruct = false);
        void writeShort(short __short);
        void writeUnsignedShort(ushort __ushort);
        void writeUnsignedInt(uint __uint);
        void writeUTF(string __utf);
        void writeUTFBytes(string __utf);
        void writeU29Int(int __int);
    }
}
