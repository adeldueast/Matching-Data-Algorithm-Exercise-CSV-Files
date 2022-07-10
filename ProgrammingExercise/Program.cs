using CsvHelper;
using CsvHelper.Configuration;
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


        private static string[] ArgumentsVerification(string[] arguments, string[] headerRow)
        {

            List<string> args2 = new List<string>();

            //Check number of args entered
            if (arguments.Length < 2)
            {
                throw new ArgumentOutOfRangeException("The program should take at least two parameters");
            }


            for (int i = arguments.Length - 1; i >= 0; i--)
            {
                //check if file entered exists
                if (i == arguments.Length - 1)
                {

                    if (!File.Exists(arguments[i]))
                    {
                        throw new FileNotFoundException(arguments[i]);
                    }

                    continue;
                }



                //Adds 
                headerRow.Where(c => c.StartsWith(arguments[i], StringComparison.OrdinalIgnoreCase)).ToList().ForEach(x =>
                {
                    if (!args2.Contains(x))
                    {
                        args2.Add(x);
                    }

                });





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

        private static (List<dynamic>, IDictionary<string, HashSet<int>>) ReadCsvFile(ref string[] args)
        {
            List<dynamic> records = new List<dynamic>();
            //dasds
            IDictionary<string, HashSet<int>> records_values = new Dictionary<string, HashSet<int>>();

            Regex phone_regex = new Regex("[^a-zA-Z0-9]");

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
                    rec["Uid"] = string.Empty;
                    rec["isMatched"] = false;

                    foreach (var filter in args)
                    {
                        var filterValue = rec[filter] as string;

                        if (!string.IsNullOrEmpty(filterValue))
                        {
                            if (filter.StartsWith("Phone", StringComparison.OrdinalIgnoreCase))
                            {
                                filterValue = (phone_regex.Replace(filterValue, ""));

                            }


                            //555-
                            if (records_values.ContainsKey(filterValue))
                            {
                                records_values[filterValue].Add(index);
                                continue;
                            }

                            records_values.Add(filterValue, new HashSet<int> { index });

                        }
                    }

                    index++;

                }


            }

            return (records, records_values);
        }


        private static void FindMatchesByType(string[] args, List<dynamic> records, IDictionary<string, HashSet<int>> records_values)
        {


            //Grouping by values to be matched (phones and emails or others)


            foreach (var key in records_values)
            {
                var groupCount = key.Value.Count;
                string unique_identifier;

                ICollection<dynamic> group = new List<dynamic>();

                foreach (var index in key.Value)
                {

                    group.Add(records[index]);

                }

                var isMatchedRecord = group.AsEnumerable().FirstOrDefault(r => r.isMatched == true);

                if (isMatchedRecord == null)
                {
                    unique_identifier = Guid.NewGuid().ToString();
                }
                else
                {
                    unique_identifier = isMatchedRecord.Uid;
                }


                foreach (var record in group)
                {
                    record.Uid = unique_identifier;
                    if (groupCount > 1)
                    {
                        record.isMatched = true;
                    }
                }
            }



            foreach (var record in records)
            {
                Console.Write($"{record.Uid} - ");
                foreach (var filter in args)
                {
                    if (((IDictionary<string, object>)record).ContainsKey(filter))
                    {
                        Console.Write(((IDictionary<string, object>)record)[filter].ToString());
                    }
                }
                Console.WriteLine();

            }

        }


        private static void WriteCsvFile(List<dynamic> records)
        {
            using (var writer = new StreamWriter("output.csv"))
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

        #region HELPER METHODS
        private static string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }
        private static string StripAllButDigits(string s)
        {
            return (s == null) ? string.Empty : Regex.Replace(s, @"\\D", string.Empty);
        }

        #endregion

    }


}
