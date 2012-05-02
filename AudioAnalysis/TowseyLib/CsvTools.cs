﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.IO;


namespace TowseyLib
{
    public static class CsvTools
    {


        //READING A TABLE FROM A CSV FILE

         public static void TransferCSVToTable(DataTable dt, string  filePath)
           {
               string[] csvRows = System.IO.File.ReadAllLines(filePath);
               string[] fields = null; 
               foreach(string csvRow in csvRows)
               {
                  fields = csvRow.Split(',');
                  DataRow row = dt.NewRow();
                  row.ItemArray = fields;
                  dt.Rows.Add(row);
               }
           }

         /// <summary>
         /// loads a data table with data in given csv file.
         /// If the column types are not given then default to string
         /// </summary>
         /// <param name="filePath"></param>
         /// <param name="isFirstRowHeader"></param>
         /// <param name="types"></param>
         /// <returns></returns>
         public static DataTable ReadCSVToTable(string filePath, bool isFirstRowHeader, Type[] types)
         {
             string[] csvRows = System.IO.File.ReadAllLines(filePath);
             var dt = new DataTable();
             if (isFirstRowHeader)
             {
                 string[] headers = csvRows[0].Split(',');
                 for (int i = 0; i < headers.Length; i++)
                 {
                     if(types == null) dt.Columns.Add(headers[i], typeof(string));
                     else 
                         if (types.Length <= i) dt.Columns.Add(headers[i], typeof(double));
                     else                       dt.Columns.Add(headers[i], types[i]);
                 }
                 csvRows[0] = null; //remove header row
             }

             string[] fields = null;
             foreach (string csvRow in csvRows)
             {
                 if (csvRow == null) continue; //skip header row
                 fields = csvRow.Split(',');
                 DataRow row = dt.NewRow();
                 row.ItemArray = MakeItemArray(fields, types);
                 dt.Rows.Add(row);
             }
             return dt;
         }

        /// <summary>
        /// reads a CSV file into a Datatable and deduces the data type in each column
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="isFirstRowHeader"></param>
        /// <returns></returns>
         public static DataTable ReadCSVToTable(string filePath, bool isFirstRowHeader)
         {
             string[] csvRows = System.IO.File.ReadAllLines(filePath);

             //convert rows 1-11 toList of strings so can deduce their types.
             var listOfStringArrays = ConvertCSVRowsToListOfStringArrays(csvRows, 1, 30);
             Type[] types = DataTools.GetArrayTypes(listOfStringArrays);

             //initialise the DataTable
             var dt = new DataTable();
             if (isFirstRowHeader)
             {
                 string[] headers = csvRows[0].Split(',');
                 for (int i = 0; i < headers.Length; i++)
                 {
                     dt.Columns.Add(headers[i], types[i]);
                 }
                 csvRows[0] = null; //remove header row
             }
             else
             {
                 for (int i = 0; i < types.Length; i++)
                 {
                     dt.Columns.Add("Field"+i, types[i]);
                 }
             }

             //fill the DataTable
             string[] fields = null;
             foreach (string csvRow in csvRows)
             {
                 if (csvRow == null) continue; //skip header row
                 fields = csvRow.Split(',');
                 DataRow row = dt.NewRow();
                 //row.ItemArray = fields; //use this line only if fields are strings
                 row.ItemArray = MakeItemArray(fields, types);
                 dt.Rows.Add(row);
             }
             return dt;
         }
        /// <summary>
        /// this method is called by the previous method when reading in a CSV file.
        /// It is used only to get csv data into a format that the data type for each field can be determined.
        /// </summary>
        /// <param name="csvRows"></param>
        /// <param name="rowStart"></param>
        /// <param name="rowEnd"></param>
        /// <returns></returns>
        public static List<string[]> ConvertCSVRowsToListOfStringArrays(string[] csvRows, int rowStart, int rowEnd)
        {
             if (rowEnd > csvRows.Length) rowEnd = csvRows.Length;
             List<string[]> listOfStringArrays = new List<string[]>();
             int fieldCount  = csvRows[0].Split(',').Length;
             int arrayLength = rowEnd - rowStart + 1;
             //init arrays
             for (int c = 0; c < fieldCount; c++)
             {
                 listOfStringArrays.Add(new string[arrayLength]);
             }
             // fill the empty arrays
             for (int r = rowStart; r <= rowEnd; r++)
             {
                 string[] fields = csvRows[r].Split(',');
                 for (int f = 0; f < fieldCount; f++) listOfStringArrays[f][r - rowStart] = fields[f];
             }
            return listOfStringArrays;
        }

        public static Object[] MakeItemArray(string[] fields, Type[] types)
        {
            int length = fields.Length;
            Object[] output = new Object[length];
            for (int i = 0; i < length; i++)
            {
                if ((fields[i] == null) || (fields[i] == "")) output[i] = null;
                else
                if (types[i] == typeof(int))
                {
                    output[i] = Int32.Parse(fields[i]);
                }
                else
                if (types[i] == typeof(double))
                {
                    output[i] = Double.Parse(fields[i]);
                }
                else
                if (types[i] == typeof(bool))
                {
                    output[i] = Boolean.Parse(fields[i]);
                }
                else
                output[i] = fields[i];
            }
            return output;
        }


        //#######################################################################################
        //READING A TABLE FROM A CSV FILE

        // using System.Data;
        // using System.Data.OleDb;
        // using System.Globalization;
        // using System.IO;
/*
        static DataTable GetDataTableFromCsv(string path, bool isFirstRowHeader)
        {
            string header = isFirstRowHeader ? "Yes" : "No";

            string pathOnly = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);

            string sql = @"SELECT * FROM [" + fileName + "]";

            using(OleDbConnection connection = new OleDbConnection(
                      @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=" + pathOnly + 
                      ";Extended Properties=\"Text;HDR=" + header + "\""))
            using(OleDbCommand command = new OleDbCommand(sql, connection))
            using(OleDbDataAdapter adapter = new OleDbDataAdapter(command))
            {
                DataTable dataTable = new DataTable();
                dataTable.Locale = CultureInfo.CurrentCulture;
                adapter.Fill(dataTable);
                return dataTable;
            }
        }

 * 
 */ 
 
        //#######################################################################################
        //WRITE A CSV FILE FROM A TABLE
         public static void DataTable2CSV(DataTable dt, string strFilePath)
        {
            // Create the CSV file to which grid data will be exported.
            StreamWriter sw = new StreamWriter(strFilePath, false);

            // First we will write the headers.
            //DataTable dt = m_dsProducts.Tables[0];

            int iColCount = dt.Columns.Count;
            for (int i = 0; i < iColCount; i++)
            {
                sw.Write(dt.Columns[i]);
                if (i < iColCount - 1) sw.Write(","); 
            }

            sw.Write(sw.NewLine);

            // Now write all the rows.
            foreach (DataRow dr in dt.Rows)
            {
                for (int i = 0; i < iColCount; i++)
                {

                    if (!Convert.IsDBNull(dr[i]))
                    {
                        sw.Write(dr[i].ToString());
                    }
                    if (i < iColCount - 1)
                    {
                        sw.Write(",");
                    }
                }
                sw.Write(sw.NewLine);
            }
            sw.Close();
        } // DataTable2CSV()




        //#######################################################################################
        //WRITE A CSV FILE FROM A TABLE

        public static void DataTable2CSV(DataTable table, string strFilePath, string seperateChar)
        {

            StreamWriter sr = null;

            try
            {

            sr = new StreamWriter(strFilePath);
            string seperator = "";
            StringBuilder builder = new StringBuilder();
            foreach (DataColumn col in table.Columns)
            {
                builder.Append(seperator).Append(col.ColumnName);
                seperator = seperateChar;
            }
            sr.WriteLine(builder.ToString());

            foreach (DataRow row in table.Rows)
            {
                seperator = "";
                builder = new StringBuilder();
                foreach (DataColumn col in table.Columns)
                {
                    builder.Append(seperator).Append(row[col.ColumnName]);
                    seperator = seperateChar;
                }

                sr.WriteLine(builder.ToString());

            }
            }
            finally
            {
                if (sr != null)	{ sr.Close();}
            }
        } // DataTable2CSV()


        public static string WriteDataTableRow(DataRow row, string seperateChar)
        {
            string seperator = "";
            StringBuilder builder = new StringBuilder();
            foreach (object item in row.ItemArray)
            {
                builder.Append(seperator).Append(item);
                seperator = seperateChar;
            }
            return builder.ToString();
        } // WriteDataTableRow()



        /// <summary>
        /// returns a list of the column values in a csv file plus the column headings
        /// ASSUMED to be doubles
        /// returns as lists of type double
        /// </summary>
        /// <param name="csvFileName"></param>
        /// <returns></returns>
        public static System.Tuple<List<string>, List<double[]>> ReadCSVFile(string csvFileName)
        {
            string dir = Path.GetDirectoryName(csvFileName);
            string pathSansExtention = Path.GetFileNameWithoutExtension(csvFileName);
            //string opFile = Path.Combine(dir, pathSansExtention + ".png");
            List<string> lines = FileTools.ReadTextFile(csvFileName);

            int lineCount = lines.Count;
            string[] words = lines[0].Split(',');
            int columnCount = words.Length;

            //GET the CSV COLUMN HEADINGS
            List<string> headers = new List<string>();
            for (int c = 0; c < columnCount; c++) headers.Add(words[c]);

            //GET the CSV COLUMN HEADINGS
            //set up the matrix as List of arrays
            List<double[]> values = new List<double[]>();
            for (int c = 0; c < columnCount; c++)
            {
                double[] array = new double[lineCount - 1];
                values.Add(array);
            }

            //fill the arrays
            for (int r = 1; r < lineCount; r++)
            {
                words = lines[r].Split(',');
                for (int c = 0; c < columnCount; c++)
                {
                    values[c][r - 1] = Double.Parse(words[c]);
                }
            }

            return System.Tuple.Create(headers, values);
        }

        /// <summary>
        /// Returns the requested column of data from a CSV file and also returns the column header
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="colNumber"></param>
        /// <param name="header"></param>
        /// <returns></returns>
        public static double[] ReadColumnOfCSVFile(string fileName, int colNumber, out string header)
        {
            List<string> lines = FileTools.ReadTextFile(fileName);
            string[] words = lines[0].Split(',');
            header = words[colNumber];

            double[] array = new double[lines.Count - 1]; //-1 because ignore header
            //read csv data into arrays.
            for (int i = 1; i < lines.Count; i++) //ignore first line = header.
            {
                words = lines[i].Split(',');
                array[i - 1] = Double.Parse(words[colNumber]);
                if (Double.IsNaN(array[i - 1]))
                {
                    array[i - 1] = 0.0;
                }
            }//end 
            return array;
        }

        public static void AddColumnOfValuesToCSVFile(string csvFileName, string header, double[] values, string opFileName)
        {
            List<string> lines = FileTools.ReadTextFile(csvFileName);
            //String.Concat(lines[0], ",", header);
            lines[0] += ("," + header);
            for (int i = 1; i < lines.Count; i++) //ignore first line = header.
            {
                //String.Concat(lines[i], ",", values[i-1]);
                lines[i] += ("," + values[i - 1]);
            }//end 

            FileTools.WriteTextFile(opFileName, lines);
        }




    } //class
}//namespace