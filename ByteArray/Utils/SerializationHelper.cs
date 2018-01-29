using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Utils
{
    public class SerializationHelper
    {
        private static SerializationHelper _instance;
        public SerializationHelper()
        {

        }
        public static SerializationHelper getInstance()
        {
            if(_instance == null)
            {
                _instance = new SerializationHelper();
            }
            return _instance;
        }
        public bool readBoolean(IDataInput __bytes)
        {
            return __bytes.readBoolean();
        }
        public void writeBoolean(IDataOutput __bytes, bool __value)
        {
            __bytes.writeBoolean(__value);
        }
        public byte readByte(IDataInput __bytes)
        {
            return __bytes.readUnsignedByte();
        }
        public void writeByte(IDataOutput __bytes, byte __value)
        {
            __bytes.writeUnsignedByte(__value);
        }
        public sbyte readUnsignedByte(IDataInput __bytes)
        {
            return __bytes.readByte();
        }
        public void writeUnsignedByte(IDataOutput __bytes, sbyte __value)
        {
            __bytes.writeByte(__value);
        }
        public short readShort(IDataInput __bytes)
        {
            return __bytes.readShort();
        }
        public void writeShort(IDataOutput __bytes, short __value)
        {
            __bytes.writeShort(__value);
        }
        public ushort readUnsignedShort(IDataInput __bytes)
        {
            return __bytes.readUnsignedShort();
        }
        public void writeUnsignedShort(IDataOutput __bytes, ushort __value)
        {
            __bytes.writeUnsignedShort(__value);
        }
        public int readU29Int(IDataInput __bytes)
        {
            return __bytes.readU29Int();
        }
        public void writeU29Int(IDataOutput __bytes, int __value)
        {
            __bytes.writeU29Int(__value);
        }
        public int readInt(IDataInput __bytes)
        {
            return __bytes.readInt();
        }
        public void writeInt(IDataOutput __bytes, int __value)
        {
            __bytes.writeInt(__value);
        }
        public uint readUnsignedInt(IDataInput __bytes)
        {
            return __bytes.readUnsignedInt();
        }
        public void writeUnsignedInt(IDataOutput __bytes, uint __value)
        {
            __bytes.writeUnsignedInt(__value);
        }
        public long readLong(IDataInput __bytes)
        {
            return BitConverter.ToInt64(((ByteArray)__bytes)._readStreamBytesEndian(8), 0);
        }
        public void writeLong(IDataOutput __bytes, long __value)
        {
            ((ByteArray)__bytes)._writeStreamBytesEndian(BitConverter.GetBytes(__value));
        }
        public ulong readUnsignedLong(IDataInput __bytes)
        {
            return BitConverter.ToUInt64(((ByteArray)__bytes)._readStreamBytesEndian(8), 0);
        }
        public void writeUnsignedLong(IDataOutput __bytes, ulong __value)
        {
            ((ByteArray)__bytes)._writeStreamBytesEndian(BitConverter.GetBytes(__value));
        }
        public float readFloat(IDataInput __bytes)
        {
            return __bytes.readFloat();
        }
        public void writeFloat(IDataOutput __bytes, float __value)
        {
            __bytes.writeFloat(__value);
        }
        public double readDouble(IDataInput __bytes)
        {
            return __bytes.readDouble();
        }
        public void writeDouble(IDataOutput __bytes, double __value)
        {
            __bytes.writeDouble(__value);
        }
        public string readUTF(IDataInput __bytes)
        {
            return __bytes.readUTF();
        }
        public void writeUTF(IDataOutput __bytes, string __value)
        {
            __bytes.writeUTF(__value);
        }
        public object readObject(IDataInput __bytes)
        {
            return __bytes.readObject();
        }
        public void writeObject(IDataOutput __targetBytes, object __value)
        {
            __targetBytes.writeObject(__value);
        }
        public void readBytes(IDataInput __bytes, ByteArray __target, uint __offset = 0, uint __length = 0)
        {
            __bytes.readBytes(__target, __offset, __length);
        }
        public void writeBytes(IDataOutput __targetBytes, ByteArray __target, uint __offset = 0, uint __length = 0)
        {
            __targetBytes.writeBytes(__target, __offset, __length);
        }

        public void customSerializationVector(IDataOutput __targetBytes, IList __object, bool __writeType = false)
        {
			if(__writeType)
			{
				this.writeUTF(__targetBytes, __object.GetType().ToString());
			}
            this.writeU29Int(__targetBytes, __object.Count);
            for(int i = 0; i < __object.Count; i++)
            {
                this.customSerialization(__targetBytes, __object[i]);
            }
        }

        public object customDeserializationVector(IDataInput __serializationBytes, Type __type)
        {
			Assembly[] ass = AppDomain.CurrentDomain.GetAssemblies();
			string __typeDefine;
            if (__type == null)
			{
				__typeDefine = __serializationBytes.readUTF();
			}
			else
			{
				__typeDefine = __type.ToString();
            }
			string __listTypeString = "System.Collections.Generic.List`1[" + __type.ToString() + "]";
			
            foreach (Assembly assembly in ass)
            {
				Type __childType = assembly.GetType(__typeDefine);
                if (__childType != null)
                {
					var __resultType = typeof(List<>).MakeGenericType(__childType);
					IList __result = (IList)Activator.CreateInstance(__resultType);
					int __length = this.readU29Int(__serializationBytes);
					for (int i = 0; i < __length; i++)
					{
						__result.Add(this.customDeserialization(__serializationBytes, __type));
					}
					return __result;
				}
            }

			return null;
        }
        public void customSerialization(IDataOutput __targetBytes, object __object, bool writeTypeDefine = false)
        {
            if(writeTypeDefine)
            {
                __targetBytes.writeUTF(__object.GetType().FullName);
            }
            if(__object is ISerializable)
            {
                ((ISerializable)__object).toBytes(__targetBytes);
            }
            else if (__object is Enum)
            {
                __targetBytes.writeU29Int((int)__object);
            }
            else
            {
                __targetBytes.writeObject(__object);
            }
        }
        public object customDeserialization(IDataInput __serializationBytes, Type __type)
        {
            if(__type == null)
            {
                string __typeDefine = __serializationBytes.readUTF();
                Assembly[] ass = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly assembly in ass)
                {
                    Type assemblyClassType = assembly.GetType(__typeDefine);
                    if (assemblyClassType != null)
                    {
                        __type = assemblyClassType;
                        break;
                    }
                }
            }
            if(__type.IsEnum)
            {
                return Enum.Parse(__type, __serializationBytes.readU29Int().ToString());
            }
            else if (typeof(ISerializable).IsAssignableFrom(__type))
            {
                ISerializable __object = (ISerializable)Activator.CreateInstance(__type);
                __object.fromBytes(__serializationBytes);
                return __object;
            }
            else
            {
                return __serializationBytes.readObject();
            }
        }

    }
}
