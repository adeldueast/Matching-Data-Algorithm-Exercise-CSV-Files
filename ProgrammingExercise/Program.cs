using CsvHelper;
using CsvHelper.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;

namespace ProgrammingExercise
{
    internal class Program
    {
        private static Stopwatch sw = new Stopwatch();



        static void Main(string[] args)
        {




            ProcessMatching(ref args);


            Console.ReadKey();


        }

        private static string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        private static string[] ArgumentsVerification(string[] args, string[] headerRow)
        {

            List<string> args2 = new List<string>();

            //Check number of args entered
            if (args.Length < 2)
            {
                throw new ArgumentOutOfRangeException("The program should take at least two parameters");
            }
            for (int i = args.Length - 1; i >= 0; i--)
            {
                //check if file entered exists
                if (i == args.Length - 1)
                {
                    //check if files exist
                    if (!File.Exists(args[i]))
                    {
                        throw new FileNotFoundException(args[i]);
                    }

                    continue;
                }


                headerRow.Where(c => c.Contains(args[i])).ToList().ForEach(x =>
                {
                    if (!args2.Contains(x))
                    {
                        args2.Add(x);
                    }
                });





                ////check if matching types arguments are valid (ignore case)
                //if (!Enum.TryParse<MatchingTypesEnum>(args[i], true, out MatchingTypesEnum matchingType))
                //{
                //    throw new ArgumentException($"Invalid matching type argument '{args[i]}'");
                //}




                //args[i] = matchingType.ToString(); ;

            }

            return args2.ToArray();
        }

        private static void ProcessMatching(ref string[] args)
        {
            sw.Start();
            //read the csv file input
            var lists = ReadCsvFile(ref args);

            sw.Stop();
            Console.WriteLine($"READING {sw.Elapsed.TotalMilliseconds} milliseconds");


            sw.Restart();
            //Matches all records based on given matching-type arguments
            FindMatchesByType(args, lists.Item1, lists.Item2);
            sw.Stop();
            Console.WriteLine($"PROCESSING {sw.Elapsed.TotalMilliseconds} milliseconds");


            sw.Restart();
            //write the csv file output
            WriteCsvFile(lists.Item1.OrderBy(x => x.Uid).ToList());
            sw.Stop();
            Console.WriteLine($"WRITING {sw.Elapsed.TotalMilliseconds} milliseconds");



        }

        private static (List<dynamic>, IEnumerable<(string, int)>) ReadCsvFile(ref string[] args)
        {
            List<dynamic> records = new List<dynamic>();
            ICollection<(string, int)> records_values = new List<(string, int)>();


            //Reads the file in args
            using (var reader = new StreamReader(args[args.Length - 1]))
            //Fill up the record list from the csv file 
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();
                args = ArgumentsVerification(args, csv.HeaderRecord);
                records = csv.GetRecords<dynamic>().ToList();


                int index = 0;
                foreach (var record in records)
                {
                    var rec = ((IDictionary<string, object>)record);

                    rec["Uid"] = Guid.NewGuid().ToString();
                    rec["isMatched"] = false;
                    rec["originalIndex"] = index;

                    foreach (var filter in args)
                    {
                        var filterValue = rec[filter] as string;
                        if (!string.IsNullOrEmpty(filterValue))
                        {
                            filterValue = RemoveWhitespace(filterValue);
                            records_values.Add((filterValue, index));
                        }
                    }

                    index++;

                }


            }

            return (records, records_values.AsEnumerable());
        }

        private static void FindMatchesByType(string[] args, List<dynamic> records, IEnumerable<(string, int)> records_values)
        {


            //Grouping by values to be matched (phones and emails or others)
            var groupedBy = records_values.GroupBy(x => x.Item1);


            foreach (var group in groupedBy)
            {


                     /* THIS IS THE CORE DIFFERENCE */
                //this gets the original records REFERENCES from the original list 
                var original_records = group.Select(record =>
                       //transorm current group's record to original records using Item2 wich is the index in the original list
                       records[record.Item2]
                );


                var groupCount = original_records.Count();

                string unique_identifier = original_records.FirstOrDefault(record =>
                  record.isMatched == true
                 )?.Uid;

                if (unique_identifier is null)
                {
                    unique_identifier = Guid.NewGuid().ToString();
                }

                foreach (var record in original_records)
                {

                    record.Uid = unique_identifier;

                    if (groupCount > 1)
                    {
                        record.isMatched = true;
                    }
                }
            }





            //foreach (var record in records)
            //{
            //    Console.Write($"{record.Uid} - ");
            //    foreach (var filter in args)
            //    {
            //        if (((IDictionary<string, object>)record).ContainsKey(filter))
            //        {
            //            Console.Write(((IDictionary<string, object>)record)[filter].ToString());
            //        }
            //    }
            //    Console.WriteLine();
            //}

        }

        static ExpandoObject DeepCopy(ExpandoObject original)
        {
            var clone = new ExpandoObject();

            var _original = (IDictionary<string, object>)original;
            var _clone = (IDictionary<string, object>)clone;

            foreach (var kvp in _original)
                _clone.Add(kvp.Key, kvp.Value is ExpandoObject ? DeepCopy((ExpandoObject)kvp.Value) : kvp.Value);

            return clone;
        }

        private static void WriteCsvFile(List<dynamic> records)
        {
            using (var writer = new StreamWriter("outputBOSS.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {




                var recordDictionary = (IDictionary<string, object>)records.First();

                var properties = recordDictionary.Keys;

                foreach (var property in properties)
                {
                    if (property != "isMatched")
                    {
                        csv.WriteField(property);
                    }
                }

                csv.NextRecord();

                foreach (var record in records)
                {
                    var expanded = (IDictionary<string, object>)record;

                    foreach (var property in properties)
                    {
                        if (property != "isMatched")
                        {
                            csv.WriteField(expanded[property]);
                        }
                    }

                    csv.NextRecord();
                }

            }

        }


    }


}
