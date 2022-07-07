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


            ArgumentsVerification(args);
            ProcessMatching(args);


        }


        private static void ArgumentsVerification(string[] args)
        {
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

                //check if matching types arguments are valid (ignore case)
                if (!Enum.TryParse<MatchingTypesEnum>(args[i], true, out MatchingTypesEnum matchingType))
                {
                    throw new ArgumentException($"Invalid matching type argument '{args[i]}'");
                }

                args[i] = matchingType.ToString(); ;

            }
        }

        private static void ProcessMatching(string[] args)
        {


            //read the csv file input
            var records = ReadCsvFile(args);


            //Matches all records based on given matching-type arguments
            FindMatchesByType(args, records);


            //write the csv file output
            WriteCsvFile(records.OrderBy(x => x.Uid).ToList());



        }

        private static void FindMatchesByType(string[] args, List<dynamic> records)
        {
            //foreach filter
            for (int i = 0; i < args.Length - 1; i++)
            {

                //Example : Phone , Email
                var filter = args[i];

                //Group all  records by current matching type (filter)
                var groupedByFilter = records
                    .GroupBy(x =>
                                 string.IsNullOrEmpty(((IDictionary<string, object>)x)[filter].ToString())
                                 ? Guid.NewGuid().ToString()
                                 : ((IDictionary<string, object>)x)[filter].ToString()
                            )
                    .Select(group => group.ToList())
                    .ToList();


                foreach (var group in groupedByFilter)
                {
                    //If group contains a record that was already matched previously (isMatched=true) then set that id to every records in that group
                    //Else, generate a new Id for the whole group, because none were matched previously
                    var matched_record = group.FirstOrDefault(x => x.isMatched == true);
                    var unique_identifier = matched_record == null ? Guid.NewGuid().ToString() : matched_record.Uid;


                    foreach (var record in group)
                    {
                        //update the record's id 
                        record.Uid = unique_identifier;

                        //only set the record's isMatched=true if the group contains more than just one record
                        //(otherwise its not a match because there is only one record in that group..)
                        if (group.Count > 1)
                        {
                            record.isMatched = true;
                        }
                    }
                }

                //Flattent the list back to normal 
                records = groupedByFilter.SelectMany(x => x).ToList();
            }




        }

        private static List<dynamic> ReadCsvFile(string[] args)
        {
            List<dynamic> records;
            using (var reader = new StreamReader(args[args.Length - 1]))
            //Fill up the record list from the csv file 
            using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
            {


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


    }


}
