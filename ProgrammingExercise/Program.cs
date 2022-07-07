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
        static void Main(string[] args)
        {
            Stopwatch sw    = Stopwatch.StartNew();
            sw.Start();


            ProcessMatching(ref args);

            sw.Stop();
            Console.WriteLine($"{sw.Elapsed.TotalMinutes} minutes elapsed");
            Console.Beep(); Console.Beep(); Console.Beep();
            Console.Beep();


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


            //Matches all records based on given matching-type arguments
            FindMatchesByType(args, records);


            //write the csv file output
            WriteCsvFile(records.OrderBy(x => x.Uid).ToList());



        }

        private static void FindMatchesByType(string[] args, List<dynamic> records)
        {
            List<IGrouping<string, dynamic>> groupedByFilterCopy = null;

            //foreach filter
            for (int i = 0; i <= args.Length - 1; i++)
            {

                //Example : Email1 , Email2
                var filter = args[i];

                //Group all  records by current matching type (filter)
                var groupedByFilter = records
                    .GroupBy(x =>
                                 string.IsNullOrEmpty(((IDictionary<string, object>)x)[filter].ToString())
                                 ? Guid.NewGuid().ToString()
                                 : ((IDictionary<string, object>)x)[filter].ToString()
                            )
                    .ToList();
                //.Select(group => group.ToList())




                foreach (var group in groupedByFilter)
                {
                    string unique_identifier;
                    var groupCount = group.Count();

                    // Check if any of the record is already Matched
                    var matched_records = group.Where(record => record.isMatched == true).ToList();

                    //Matches found
                    if (matched_records.Count > 0)
                    {
                        unique_identifier = Guid.NewGuid().ToString();

                        //a list of ids..
                        var ids_of_already_matched_records = matched_records.Select(r => r.Uid).ToList();
                        // I want to get all the records where their Id matches any in the list above from the groupByList
                        var records_to_update = groupedByFilter.SelectMany(group => group.Where(record => ids_of_already_matched_records.Contains(record.Uid))).ToList();


                        foreach (var rec in records_to_update)
                        {
                            rec.Uid = unique_identifier;
                            rec.isMatched = true;
                        }

                        foreach (var rec in group)
                        {
                            rec.Uid = unique_identifier;
                            rec.isMatched = true;

                        }


                    }
                    //No Matches
                    else
                    {

                        if (groupedByFilterCopy is null)
                        {
                            //Todo: creer new Id .. a revoir..
                            unique_identifier = Guid.NewGuid().ToString();
                            foreach (var record in group)
                            {
                                record.Uid = unique_identifier;
                                if (groupCount > 1)
                                {
                                    record.isMatched = true;
                                }
                            }
                        }
                        else
                        {

                           

                            var existing_group = groupedByFilterCopy.FirstOrDefault(g => g.Key == group.Key);
                            if (existing_group != null)
                            {


                                //exist
                                var id = existing_group.First().Uid;
                                var records_to_update = groupedByFilter.SelectMany(group => group.Where(record => record.Uid == id)).ToList();
                                foreach (var rec in records_to_update)
                                {
                                    rec.isMatched = true;
                                }
                             
                                foreach (var rec in group)
                                {
                                    rec.Uid = id;
                                    rec.isMatched = true;

                                }



                            }
                            else
                            {
                                unique_identifier = Guid.NewGuid().ToString();
                                foreach (var rec in group)
                                {
                                    rec.Uid = unique_identifier;
                                    if (group.Count() > 1)
                                    {
                                        rec.isMatched = true;
                                    }
                                   

                                }
                            }





                        }



                    }



                }

                var previousFilter = filter;


                if (i != args.Length - 1)
                {
                    if (args[i + 1].StartsWith(filter.Substring(0, 3)))
                    {
                        //Copy the previous groupedByFilter List
                        groupedByFilterCopy = new List<IGrouping<string, dynamic>>(groupedByFilter);
                    }
                    else
                    {
                        groupedByFilterCopy = null;
                    }

                    //Flattent the list back to normal 
                    records = groupedByFilter.SelectMany(x => x).ToList();
                }
            }




        }

        private static List<dynamic> ReadCsvFile(ref string[] args)
        {
            List<dynamic> records;
            using (var reader = new StreamReader(args[args.Length - 1]))
            //Fill up the record list from the csv file 
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {
                csv.Read();
                csv.ReadHeader();

                args = ArgumentsVerification(args, csv.HeaderRecord);

                records = csv.GetRecords<dynamic>().ToList();
                foreach (var record in records)
                {
                    var obj = record as ExpandoObject;

                    if (obj != null)
                    {

                        ((IDictionary<string, object>)obj)["Uid"] = Guid.NewGuid().ToString();
                        ((IDictionary<string, object>)obj)["isMatched"] = false;

                        //var keys = obj.Select(a => a.Key).ToList();
                        //var values = obj.Select(a => a.Value).ToList();
                    }
                }
            }

            return records;
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
