using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace NetTopologySuite.IO.Dbf
{


    internal static class DbfBinaryExtensions
    {
        public static void WriteDbfVersion(this BinaryBufferWriter tableDescriptor, byte version)
        {
            tableDescriptor.WriteByte(version);
        }
        public static byte ReadDbfVersion(this BinaryBufferReader tableDescriptor)
        {
            return tableDescriptor.ReadByte();
        }


        public static void WriteDbfLastUpdateDate(this BinaryBufferWriter tableDescriptor, DateTime date)
        {
            tableDescriptor.WriteByte((byte)(date.Year - 1900));
            tableDescriptor.WriteByte((byte)date.Month);
            tableDescriptor.WriteByte((byte)date.Day);
        }
        public static DateTime ReadDbfLastUpdateDate(this BinaryBufferReader tableDescriptor)
        {
            var y = tableDescriptor.ReadByte();
            var m = tableDescriptor.ReadByte();
            var d = tableDescriptor.ReadByte();

            if (m < 1)
                m = 1;

            if (m > 12)
                m = 12;

            if (d < 1)
                d = 1;

            if (d > 31)
                d = 31;

            return new DateTime(1900 + y, m, d);
        }


        public static void WriteDbfRecordCount(this BinaryBufferWriter tableDescriptor, int recordCount)
        {
            tableDescriptor.WriteUInt32LittleEndian((uint)recordCount);
        }
        public static int ReadDbfRecordCount(this BinaryBufferReader tableDescriptor)
        {
            return (int)tableDescriptor.ReadUInt32LittleEndian();
        }


        public static void WriteDbfHeaderSize(this BinaryBufferWriter tableDescriptor, int headerSize)
        {
            tableDescriptor.WriteUInt16LittleEndian((ushort)headerSize);
        }
        public static ushort ReadDbfHeaderSize(this BinaryBufferReader tableDescriptor)
        {
            return tableDescriptor.ReadUInt16LittleEndian();
        }


        public static void WriteDbfRecordSize(this BinaryBufferWriter tableDescriptor, int headerSize)
        {
            tableDescriptor.WriteUInt16LittleEndian((ushort)headerSize);
        }
        public static ushort ReadDbfRecordSize(this BinaryBufferReader tableDescriptor)
        {
            return tableDescriptor.ReadUInt16LittleEndian();
        }


        public static void WriteDbfEncoding(this BinaryBufferWriter tableDescriptor, Encoding encoding)
        {
            var ldid = DbfEncoding.GetLanguageDriverId(encoding);
            tableDescriptor.WriteByte(ldid);
        }
        public static Encoding ReadDbfEncoding(this BinaryBufferReader tableDescriptor)
        {
            var ldid = tableDescriptor.ReadByte();
            return DbfEncoding.GetEncodingForLanguageDriverId(ldid);
        }


        public static void WriteDbaseFieldDescriptor(this BinaryBufferWriter fieldDescriptor, DbfField field, Encoding encoding)
        {
            encoding = encoding ?? Encoding.UTF8;
            var name = field.Name.PadRight(Dbf.MaxFieldNameLength, char.MinValue); // Field name must have empty space zero-filled 


            fieldDescriptor.WriteString(name, Dbf.MaxFieldNameLength, encoding);
            fieldDescriptor.WriteNullBytes(1);
            fieldDescriptor.WriteDbaseType(field.FieldType);
            fieldDescriptor.WriteNullBytes(4);
            fieldDescriptor.WriteByte((byte)field.Length);
            fieldDescriptor.WriteByte((byte)field.Precision);
            fieldDescriptor.WriteNullBytes(14);
        }
        public static DbfField ReadDbaseFieldDescriptor(this BinaryBufferReader fieldDescriptor, Encoding encoding)
        {
            encoding = encoding ?? Encoding.UTF8;

            var name = fieldDescriptor.ReadString(Dbf.MaxFieldNameLength, encoding)?.Trim();
            fieldDescriptor.Advance(1); // Reserved (field name terminator)
            var type = fieldDescriptor.ReadDbaseType();
            fieldDescriptor.Advance(4); // Reserved
            var length = fieldDescriptor.ReadByte();
            var precision = fieldDescriptor.ReadByte();
            fieldDescriptor.Advance(14); // Reserved

            if (type == DbfType.Character)
            {
                var textField = new DbfCharacterField(name, length, encoding);
                textField.Encoding = encoding;
                return textField;
            }
            else if (type == DbfType.Date)
            {
                return new DbfDateField(name);
            }
            else if (type == DbfType.Numeric)
            {
                return new DbfNumericField(name, length, precision);
            }
            else if (type == DbfType.Float)
            {
                return new DbfFloatField(name, length, precision);
            }
            else if (type == DbfType.Logical)
            {
                return new DbfLogicalField(name);
            }
            else
            {
                throw new InvalidDataException("Invalid dBASE III field type: " + type);
            }
        }


        private static DbfType ReadDbaseType(this BinaryBufferReader fieldDescriptor)
        {
            var type = fieldDescriptor.ReadByteChar();
            type = char.ToUpper(type);

            if (type == 'S')
                type = 'C';

            return (DbfType)type;
        }
        private static void WriteDbaseType(this BinaryBufferWriter fieldDescriptor, DbfType type)
        {
            fieldDescriptor.WriteByte((byte)type);
        }
    }

}
