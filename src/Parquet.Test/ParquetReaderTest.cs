﻿using NetBox;
using NetBox.IO;
using Parquet.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Xunit;
using System.Linq;

namespace Parquet.Test
{
   public class ParquetReaderTest : TestBase
   {
      [Fact]
      public void Opening_null_stream_fails()
      {
         Assert.Throws<ArgumentNullException>(() => new ParquetReader(null));
      }

      [Fact]
      public void Opening_small_file_fails()
      {
         Assert.Throws<IOException>(() => new ParquetReader("small".ToMemoryStream()));
      }

      [Fact]
      public void Opening_file_without_proper_head_fails()
      {
         Assert.Throws<IOException>(() => new ParquetReader("PAR2dataPAR1".ToMemoryStream()));
      }

      [Fact]
      public void Opening_file_without_proper_tail_fails()
      {
         Assert.Throws<IOException>(() => new ParquetReader("PAR1dataPAR2".ToMemoryStream()));
      }

      [Fact]
      public void Opening_readable_but_not_seekable_stream_fails()
      {
         Assert.Throws<ArgumentException>(() => new ParquetReader(new ReadableNonSeekableStream(new MemoryStream(Generator.GetRandomBytes(5, 6)))));
      }

      [Fact]
      public void Opening_not_readable_but_seekable_stream_fails()
      {
         Assert.Throws<ArgumentException>(() => new ParquetReader(new NonReadableSeekableStream(new MemoryStream(Generator.GetRandomBytes(5, 6)))));
      }

      [Fact]
      public void Opening_readable_and_seekable_stream_succeeds()
      {
         new ParquetReader(new ReadableAndSeekableStream(new NonReadableSeekableStream("PAR1DATAPAR1".ToMemoryStream())));
      }

      [Fact]
      public void Read_from_offset_in_first_chunk()
      {
         DataSet ds = DataSetGenerator.Generate(30);
         var wo = new WriterOptions { RowGroupsSize = 5 };
         var ro = new ReaderOptions { Offset = 0, Count = 2 };

         var ms = new MemoryStream();
         ParquetWriter.Write(ds, ms, CompressionMethod.None, null, wo);

         ms.Position = 0;
         DataSet ds1 = ParquetReader.Read(ms, null, ro);

         Assert.Equal(30, ds1.TotalRowCount);
         Assert.Equal(2, ds1.RowCount);
         Assert.Equal(0, ds[0][0]);
         Assert.Equal(1, ds[1][0]);
      }

      [Fact]
      public void Read_from_offset_in_second_chunk()
      {
         DataSet ds = DataSetGenerator.Generate(15);
         var wo = new WriterOptions { RowGroupsSize = 5 };
         var ro = new ReaderOptions { Offset = 5, Count = 2 };

         var ms = new MemoryStream();
         ParquetWriter.Write(ds, ms, CompressionMethod.None, null, wo);

         ms.Position = 0;
         DataSet ds1 = ParquetReader.Read(ms, null, ro);

         Assert.Equal(15, ds1.TotalRowCount);
         Assert.Equal(2, ds1.RowCount);
         Assert.Equal(5, ds1[0][0]);
         Assert.Equal(6, ds1[1][0]);
      }

      [Fact]
      public void Read_from_offset_across_chunks()
      {
         DataSet ds = DataSetGenerator.Generate(15);
         var wo = new WriterOptions { RowGroupsSize = 5 };
         var ro = new ReaderOptions { Offset = 4, Count = 2 };

         var ms = new MemoryStream();
         ParquetWriter.Write(ds, ms, CompressionMethod.None, null, wo);

         ms.Position = 0;
         DataSet ds1 = ParquetReader.Read(ms, null, ro);

         Assert.Equal(15, ds1.TotalRowCount);
         Assert.Equal(2, ds1.RowCount);
         Assert.Equal(4, ds1[0][0]);
         Assert.Equal(5, ds1[1][0]);
      }

      [Fact]
      public void Read_from_negative_offset_fails()
      {
         DataSet ds = DataSetGenerator.Generate(15);
         var wo = new WriterOptions { RowGroupsSize = 5 };
         var ro = new ReaderOptions { Offset = -4, Count = 2 };

         var ms = new MemoryStream();
         ParquetWriter.Write(ds, ms, CompressionMethod.None, null, wo);

         ms.Position = 0;
         Assert.Throws<ParquetException>(() => ParquetReader.Read(ms, null, ro));
      }

      [Fact]
      public void Reads_created_by_metadata()
      {
         DataSet ds = DataSetGenerator.Generate(10);

         var ms = new MemoryStream();
         ParquetWriter.Write(ds, ms);

         ms.Position = 0;
         DataSet ds1 = ParquetReader.Read(ms);
         Assert.StartsWith("Parquet.Net", ds1.Metadata.CreatedBy);
      }

      //this only tests that the file is readable as it used to completely crash before
      [Fact]
      public void Reads_compat_nation_impala_file()
      {
         DataSet nation = ParquetReader.ReadFile(GetDataFilePath("nation.impala.parquet"));

         Assert.Equal(25, nation.RowCount);
      }

      //this only tests that the file is readable as it used to completely crash before
      [Fact]
      public void Reads_compat_customer_impala_file()
      {
         /*
          * c_name:
          *    45 pages (0-44)
          */

         DataSet customer = ParquetReader.ReadFile(GetDataFilePath("customer.impala.parquet"));

         Assert.Equal(150000, customer.RowCount);
      }

      [Fact]
      public void Reads_really_mad_nested_file()
      {
         /* Spark schema:
root
|-- addresses: array (nullable = true)
|    |-- element: struct (containsNull = true)
|    |    |-- line1: string (nullable = true)
|    |    |-- name: string (nullable = true)
|    |    |-- openingHours: array (nullable = true)
|    |    |    |-- element: long (containsNull = true)
|    |    |-- postcode: string (nullable = true)
|-- cities: array (nullable = true)
|    |-- element: string (containsNull = true)
|-- comment: string (nullable = true)
|-- id: long (nullable = true)
|-- location: struct (nullable = true)
|    |-- latitude: double (nullable = true)
|    |-- longitude: double (nullable = true)
|-- price: struct (nullable = true)
|    |-- lunch: struct (nullable = true)
|    |    |-- max: long (nullable = true)
|    |    |-- min: long (nullable = true) 
         */


         DataSet ds = ParquetReader.ReadFile(GetDataFilePath("nested.parquet"));

         //much easier to compare mad nestness with .ToString(), but will break when it changes
         Assert.Equal("{[{Dante Road;Head Office;[9;10;11;12;13;14;15;16;17;18];SE11};{Somewhere Else;Small Office;[6;7;19;20;21;22;23];TN19}];[London;Derby];this file contains all the permunations for nested structures and arrays to test Parquet parser;1;{51.2;66.3};{{2;1}}}", ds[0].ToString());
         Assert.Equal("{[{Dante Road;Head Office;[9;10;11;12;13;14;15;16;17;18];SE11};{Somewhere Else;Small Office;[6;7;19;20;21;22;23];TN19}];[London;Derby];this file contains all the permunations for nested structures and arrays to test Parquet parser;1;{51.2;66.3};{{2;1}}}", ds[1].ToString());
      }

      [Fact]
      public void Reads_repeated_struct()
      {
         DataSet ds = ParquetReader.ReadFile(GetDataFilePath("repeatedstruct.parquet"));

         Assert.Equal("{[{UK;London};{US;New York}];1}", ds[0].ToString());

         //DataSet ds1 = ParquetReader.ReadFile("c:\\tmp\\rep.parquet");
      }

      [Fact]
      public void Read_simple_repeated_field()
      {
         /*
root
|-- cities: array (nullable = true)
|    |-- element: string (containsNull = true)
|-- id: long (nullable = true)
          */

         DataSet ds = ParquetReader.ReadFile(GetDataFilePath("simplerepeated.parquet"));

         Assert.Equal(2, ds.Schema.Length);
         Assert.Equal(DataType.Unspecified, ds.Schema[0].DataType);
         Assert.Equal(DataType.Int64, ds.Schema[1].DataType);

         Assert.Equal("cities", ds.Schema[0].Name);
         Assert.Equal("id", ds.Schema[1].Name);

         Assert.False(ds.Schema[0].IsArray);
         Assert.False(ds.Schema[1].IsArray);

         Assert.Equal(1L, ds[0][1]);
         Assert.Equal(ds[0][0], new[] { "London", "Derby", "Paris", "New York" });
      }

      [Fact]
      public void Read_simple_nested_field()
      {
         /*
          * root
|-- city: struct (nullable = true)
|    |-- country: string (nullable = true)
|    |-- isCapital: boolean (nullable = true)
|    |-- name: string (nullable = true)
|-- id: long (nullable = true)
          */

         //Assert.Throws<NotSupportedException>(() => ParquetReader.ReadFile(GetDataFilePath("simplenested.parquet")));
         //return;

         DataSet ds = ParquetReader.ReadFile(GetDataFilePath("simplenested.parquet"));

         Assert.Equal(1, ds.RowCount);
         Assert.Equal(2, ds.ColumnCount);

         Assert.Equal(DataType.Unspecified, ds.Schema[0].DataType);
         Assert.Equal(DataType.Int64, ds.Schema[1].DataType);

         Assert.Equal("city", ds.Schema.ColumnNames[0]);
         Assert.Equal("id", ds.Schema.ColumnNames[1]);

         Row mr = ds[0];

         Row city = mr.Get<Row>(0);
         Assert.Equal("United Kingdom", city[0]);
         Assert.True((bool)city[1]);
         Assert.Equal("London", city[2]);

         Assert.Equal(1L, mr[1]);
      }

      [Fact]
      public void Read_simple_map()
      {
         DataSet ds = ParquetReader.ReadFile(GetDataFilePath("map.parquet"));

         Field ms = ds.Schema[1];
         Assert.Equal("numbers", ms.Name);

         Assert.Equal("{1;[1=>one;2=>two;3=>three]}", ds[0].ToString());
      }

      [Fact]
      public void Read_hardcoded_decimal()
      {
         DataSet ds = ParquetReader.ReadFile(GetDataFilePath("complex-primitives.parquet"));

         Assert.Equal((decimal)1.2, ds[0][1]);
      }

      [Fact]
      public void Read_column_with_all_nulls()
      {
         var ds = new DataSet(new Field<int?>("id"))
         {
            new object[] {null},
            new object[] {null}
         };

         DataSet ds1 = DataSetGenerator.WriteRead(ds);
      }

      [Fact]
      public void Read_all_nulls_no_booleans()
      {
         DataSet ds = ParquetReader.ReadFile(GetDataFilePath("all_nulls_no_booleans.parquet"));
      }

      [Fact]
      public void Read_all_nulls_file()
      {
         DataSet ds = ParquetReader.ReadFile(GetDataFilePath("all_nulls.parquet"));

         Assert.Equal(1, ds.Schema.Length);
         Assert.Equal("lognumber", ds.Schema[0].Name);
         Assert.Equal(1, ds.RowCount);
         Assert.Null(ds[0][0]);
      }

      [Fact]
      public void Read_all_nulls_decimal_column()
      {
         DataSet ds = ParquetReader.ReadFile(GetDataFilePath("decimalnulls.parquet"));
      }

      class ReadableNonSeekableStream : DelegatedStream
      {
         public ReadableNonSeekableStream(Stream master) : base(master)
         {
         }

         public override bool CanSeek => false;

         public override bool CanRead => true;
      }

      class NonReadableSeekableStream : DelegatedStream
      {
         public NonReadableSeekableStream(Stream master) : base(master)
         {
         }

         public override bool CanSeek => true;

         public override bool CanRead => false;
      }

      class ReadableAndSeekableStream : DelegatedStream
      {
         public ReadableAndSeekableStream(Stream master) : base(master)
         {
         }

         public override bool CanSeek => true;

         public override bool CanRead => true;

      }
   }
}
