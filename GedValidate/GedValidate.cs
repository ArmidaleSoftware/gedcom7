// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using GedcomCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Gedcom7
{
    public class GedValidate
    {
        private const int MAX_ERRORS = 100;

        /// <summary>
        /// Displays validation errors to the console, limiting output to MAX_ERRORS.
        /// </summary>
        /// <param name="errors">List of error messages to display</param>
        static void ShowErrors(List<string> errors)
        {
            if (errors.Count == 0)
            {
                Console.WriteLine("No errors found");
            }
            for (int i = 0; i < MAX_ERRORS && i < errors.Count; i++)
            {
                Console.WriteLine(errors[i]);
            }
            if (errors.Count >= MAX_ERRORS)
            {
                Console.WriteLine($"Stopped after {MAX_ERRORS} errors");
            }
            Console.WriteLine();
        }

        static int ValidateGedcom(string sourcePath, string gedcomRegistriesPath)
        {
            var gedcomFile = new GedcomFile();
            List<string> errors = gedcomFile.LoadFromPath(sourcePath, gedcomRegistriesPath);

            if (errors.Count < MAX_ERRORS)
            {
                errors.AddRange(gedcomFile.Validate());
            }

            ShowErrors(errors);
            return errors.Count;
        }

        static int ValidateGedzip(string sourcePath, string gedcomRegistriesPath)
        {
            List<string> errors;
            using (var gedzipFile = new GedzipFile(gedcomRegistriesPath))
            {
                errors = gedzipFile.LoadFromPath(sourcePath);
                if (errors.Count < MAX_ERRORS)
                {
                    errors.AddRange(gedzipFile.Validate());
                }
            }

            ShowErrors(errors);
            return errors.Count;
        }

        static int ValidateFile(string sourcePath, string gedcomRegistriesPath)
        {
            Console.WriteLine($"Validating {sourcePath}:");
            if (sourcePath.EndsWith(".gdz", System.StringComparison.InvariantCultureIgnoreCase))
            {
                return ValidateGedzip(sourcePath, gedcomRegistriesPath);
            }
            else
            {
                return ValidateGedcom(sourcePath, gedcomRegistriesPath);
            }
        }

        static int ShowHelp()
        {
            Console.WriteLine("usage: GedValidate <gedcomRegistriesPath> <filename>");
            Console.WriteLine("          to check a file as being a valid FamilySearch GEDCOM 7 or GEDZIP file");
            Console.WriteLine("          <gedcomRegistriesPath> must be a local path to the GEDCOM-registries repository");
            return 1;
        }

        static int Main(string[] args)
        {
            if (args.Length != 2)
            {
                return ShowHelp();
            }

            string gedcomRegistriesPath = args[0];
            string sourcePath = args[1];
            string searchPattern = @"(?i).*\.(ged|gdz)$"; // Case-insensitive regex pattern for .ged or .gdz files

            try
            {
                if (File.Exists(sourcePath))
                {
                    // If the path is a file, check if it matches the pattern
                    if (Regex.IsMatch(Path.GetFileName(sourcePath), searchPattern))
                    {
                        return ValidateFile(sourcePath, gedcomRegistriesPath);
                    }
                }
                else if (Directory.Exists(sourcePath))
                {
                    // If the path is a directory, enumerate files matching the pattern
                    string[] files = Directory.GetFiles(sourcePath);
                    int errors = 0;
                    foreach (string file in files)
                    {
                        if (Regex.IsMatch(Path.GetFileName(file), searchPattern))
                        {
                            errors += ValidateFile(file, gedcomRegistriesPath);
                        }
                    }
                    return errors;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return 1;
            }
            return ShowHelp();
        }
    }
}
