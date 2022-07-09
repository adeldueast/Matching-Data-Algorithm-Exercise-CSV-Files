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


            //read the csv file input
            var records = ReadCsvFile(ref args);

            sw.Start();
            //Matches all records based on given matching-type arguments
            FindMatchesByType(args, records);
            sw.Stop();
            Console.WriteLine($"{sw.Elapsed.TotalMilliseconds} milliseconds elapsed");

            //write the csv file output
            WriteCsvFile(records.OrderBy(x => x.Uid).ToList());



        }
        private static string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }
        private static List<dynamic> ReadCsvFile(ref string[] args)
        {

            List<dynamic> records;


            //Fill up the record list from the csv file 
            using (var reader = new StreamReader(args[args.Length - 1]))
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
                            rec[filter] = RemoveWhitespace(filterValue);
                        }

                    }



                    index++;
                }
            }

            return (records);
        }


        private static void FindMatchesByType(string[] args, List<dynamic> records)
        {



            var ienumerable_records = records.AsEnumerable();

            //foreach filter
            foreach (var filter in args)
            {

                //Group all  records by current matching type (filter)
                var groupedBy = ienumerable_records.GroupBy(record =>
                    string.IsNullOrEmpty(((IDictionary<string, object>)record)[filter].ToString())
                                 ? Guid.NewGuid().ToString()
                                 : ((IDictionary<string, object>)record)[filter].ToString()
                              );

                
                foreach (var group in groupedBy)
                {
                    //this gets the original records REFERENCES from the original list 
                    var groupSortedByOriginalisMatched = group.Select(record =>
                           records[record.originalIndex]
                    );

                    var groupCount = groupSortedByOriginalisMatched.Count();

                    string unique_identifier = groupSortedByOriginalisMatched.FirstOrDefault(record => record.isMatched == true)?.Uid;

                    if (unique_identifier is null)
                    {
                        unique_identifier = Guid.NewGuid().ToString();
                    }

                    foreach (var originalRecord in groupSortedByOriginalisMatched)
                    {

                        originalRecord.Uid = unique_identifier;

                        if (groupCount > 1)
                        {
                            originalRecord.isMatched = true;
                        }
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
