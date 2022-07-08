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
        private static Stopwatch sw = Stopwatch.StartNew();



        static void Main(string[] args)
        {

            sw.Start();


            //var rec1 = "johnd@home.com".Split(' ');
            //var rec2 = "janed@home.com johnd@home.com".Split(' ');

            ////var result = rec1.Split(' ').Contains(rec2);
            //var commonElements = rec2.Intersect(rec1).ToArray();

            ProcessMatching(ref args);



            //sw.Stop();
            //Console.WriteLine($"{sw.Elapsed.TotalMinutes} minutes elapsed");
            //Console.Beep();


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


            //read the csv file input
            var records = ReadCsvFile(ref args);


            //Matches all records based on given matching-type arguments
            FindMatchesByType(args, records);


            //write the csv file output
            WriteCsvFile(records.OrderBy(x => x.Uid).ToList());



        }

        private static void FindMatchesByType(string[] args, List<dynamic> records)
        {

            Dictionary<string, List<dynamic>> matches = new Dictionary<string, List<dynamic>>();


            //foreach record
            foreach (var record in records)
            {




                string record_string = string.Empty;
                for (int i = 0; i < args.Length; i++)
                {
                    record_string = record_string + " " + ((IDictionary<string, object>)record)[args[i]].ToString();
                }

                var record_array = record_string.Split(' ', StringSplitOptions.RemoveEmptyEntries);




                var key = matches.Keys.FirstOrDefault(key => (key.Split(' ').Intersect(record_array)).Count() > 0);


                if (key == null)
                {


                    //var rec = matches.Values.SelectMany(x => x)
                    //    .FirstOrDefault(r =>
                    //    ((string.IsNullOrEmpty(((IDictionary<string, object>)record)["Phone1"].ToString())) ? false : record.Phone1 == r.Phone1) ||
                    //    ((string.IsNullOrEmpty(((IDictionary<string, object>)record)["Phone2"].ToString())) ? false : record.Phone2 == r.Phone2) ||
                    //    ((string.IsNullOrEmpty(((IDictionary<string, object>)record)["Email1"].ToString())) ? false : record.Email1 == r.Email1) ||
                    //    ((string.IsNullOrEmpty(((IDictionary<string, object>)record)["Email2"].ToString())) ? false : record.Email2 == r.Email2)
                    //    );

                    var rec = matches.Values.SelectMany(x => x)
                       .FirstOrDefault(r =>
                       ((string.IsNullOrEmpty(((IDictionary<string, object>)record)["Phone"].ToString())) ? false : record.Phone == r.Phone) ||
                       ((string.IsNullOrEmpty(((IDictionary<string, object>)record)["Email"].ToString())) ? false : record.Email == r.Email)
                       );


                    if (rec == null)
                    {
                        matches.Add(string.Join(" ", record_array), new List<dynamic>() { record });
                    }
                    else
                    {

                        record.Uid = rec.Uid;
                        matches.Add(string.Join(" ", record_array), new List<dynamic>() { record });

                    }


                }
                else
                {

                    var matching_records = matches[key];
                    record.Uid = matching_records.First().Uid;
                    matching_records.Add(record);
                }


            }


            sw.Stop();
            Console.WriteLine($"{sw.Elapsed.TotalMinutes} minutes");

            records = matches.Values.SelectMany(x => x).ToList();



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
                    ((IDictionary<string, object>)obj)["Uid"] = Guid.NewGuid().ToString();

                    foreach (var filter in args)
                    {
                        ((IDictionary<string, object>)obj)[filter] = RemoveWhitespace(((IDictionary<string, object>)obj)[filter].ToString());

                    }








                    //var keys = obj.Select(a => a.Key).ToList();
                    //var values = obj.Select(a => a.Value).ToList
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

        private static bool HasProperty(ExpandoObject obj, string propertyName)
        {
            return obj != null && ((IDictionary<String, object>)obj).ContainsKey(propertyName);
        }
    }


}
