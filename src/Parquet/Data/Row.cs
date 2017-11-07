﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using Parquet.File;

namespace Parquet.Data
{
   /// <summary>
   /// Represents a row
   /// </summary>
   public class Row
   {
      private object[] _values;

      /// <summary>
      /// Initializes a new instance of the <see cref="Row"/> class.
      /// </summary>
      /// <param name="values">The values.</param>
      public Row(IEnumerable<object> values)
      {
         _values = values.ToArray();
      }

      /// <summary>
      /// Initializes a new instance of the <see cref="Row"/> class.
      /// </summary>
      /// <param name="values">The values.</param>
      public Row(params object[] values)
      {
         _values = values;
      }

      /// <summary>
      /// Gets the number of values in this row
      /// </summary>
      public int Length => _values.Length;

      /// <summary>
      /// Gets the row value by index
      /// </summary>
      public object this[int i]
      {
         get
         {
            return _values[i];
         }
      }

      /// <summary>
      /// Gets the value as boolean
      /// </summary>
      /// <param name="i">Value index</param>
      public bool GetBoolean(int i)
      {
         return Get<bool>(i);
      }
      /// <summary>
      /// Gets the value as integer
      /// </summary>
      /// <param name="i">Value index</param>
      public int GetInt(int i)
      {
         return Get<int>(i);
      }

      /// <summary>
      /// Gets the value as float
      /// </summary>
      /// <param name="i">Value index</param>
      public float GetFloat(int i)
      {
         return Get<float>(i);
      }

      /// <summary>
      /// Gets the value as long
      /// </summary>
      /// <param name="i">Value index</param>
      public long GetLong(int i)
      {
         return Get<long>(i);
      }

      /// <summary>
      /// Gets the value as double
      /// </summary>
      /// <param name="i">Value index</param>
      public double GetDouble(int i)
      {
         return Get<double>(i);
      }

      /// <summary>
      /// Gets the value as big integer
      /// </summary>
      /// <param name="i">Value index</param>
      public BigInteger GetBigInt(int i)
      {
         return Get<BigInteger>(i);
      }

      /// <summary>
      /// Gets the value as byte array
      /// </summary>
      /// <param name="i">Value index</param>
      public byte[] GetByteArray(int i)
      {
         return Get<byte[]>(i);
      }

      /// <summary>
      /// Gets the value as string
      /// </summary>
      /// <param name="i">Value index</param>
      public string GetString(int i)
      {
         return Get<string>(i);
      }

      /// <summary>
      /// Gets the value as <see cref="DateTimeOffset"/>
      /// </summary>
      /// <param name="i">Value index</param>
      public DateTimeOffset GetDateTimeOffset(int i)
      {
         return Get<DateTimeOffset>(i);
      }

      /// <summary>
      /// Returns true if value at column <paramref name="i"/> is NULL.
      /// </summary>
      public bool IsNullAt(int i)
      {
         return _values[i] == null;
      }

      /// <summary>
      /// Gets the value trying to cast to <typeparamref name="T"/>
      /// </summary>
      /// <param name="i">Value index</param>
      /// <exception cref="ArgumentException">Cannot cast <typeparamref name="T"/></exception>
      public T Get<T>(int i)
      {
         object v = _values[i];

         if (v == null) return default(T);

         if(!(v is T))
         {
            throw new ArgumentException($"value at {i} is of type '{v.GetType()}' and cannot be casted to '{typeof(T)}'");
         }

         return (T)v;
      }

      /// <summary>
      /// Gets the raw values.
      /// </summary>
      public object[] RawValues => _values;

      internal static Row Compress(IEnumerable<SchemaElement> schema, IEnumerable<Row> rows)
      {
         List<Row> rowsList = rows.ToList();
         var rv = new List<object>();

         int i = 0;
         foreach(SchemaElement se in schema)
         {
            IList values = se.CreateValuesList(rowsList.Count);
            foreach(Row row in rowsList)
            {
               values.Add(row[i]);
            }
            rv.Add(values);
            i += 1;
         }

         return new Row(rv);
      }

      /// <summary>
      /// Returns a <see cref="string" /> that represents this instance.
      /// </summary>
      /// <returns>
      /// A <see cref="string" /> that represents this instance.
      /// </returns>
      public override string ToString()
      {
         var sb = new StringBuilder();

         bool isFirst = true;
         sb.Append("{");
         foreach (object v in _values)
         {
            if(isFirst)
            {
               isFirst = false;
            }
            else
            {
               sb.Append(";");
            }

            FormatValue(v, sb);
         }
         sb.Append("}");

         return sb.ToString();
      }

      private static void FormatValue(object v, StringBuilder sb)
      {
         if (v == null)
         {
            sb.Append("<null>");
         }
         else if(v is IDictionary dic)
         {
            sb.Append("[");
            bool first = true;
            foreach(DictionaryEntry pair in dic)
            {
               if(first)
               {
                  first = false;
               }
               else
               {
                  sb.Append(";");
               }

               FormatValue(pair.Key, sb);
               sb.Append("=>");
               FormatValue(pair.Value, sb);
            }
            sb.Append("]");
         }
         else if (v is Row row)
         {
            sb.Append(row.ToString());
         }
         else if ((!TypePrimitive.IsSimple(v.GetType())) && v is IEnumerable ien)
         {
            sb.Append("[");
            bool first = true;
            foreach (object cv in ien)
            {
               if(first)
               {
                  first = false;
               }
               else
               {
                  sb.Append(";");
               }

               FormatValue(cv, sb);
            }
            sb.Append("]");
         }
         else
         {
            sb.Append(v.ToString());
         }

      }
   }
}
