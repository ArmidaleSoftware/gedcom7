// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using System;
using System.Collections.Generic;

namespace Gedcom7
{
    class GedValidate
    {
        const int MaxErrors = 100;

        static void ShowErrors(List<string> errors)
        {
            if (errors.Count == 0)
            {
                errors.Add("No errors found");
            }
            string text = string.Join("\n", errors.Take(MaxErrors));
            Console.WriteLine(text);
            if (errors.Count >= MaxErrors)
            {
                Console.WriteLine("Stopped after " + MaxErrors + " errors");
            }
        }

        static int Validate(string sourcePath)
        {
            var gedcomFile = new GedcomFile();
            List<string> errors = gedcomFile.LoadFromPath(sourcePath);

            if (errors.Count < MaxErrors)
            {
                errors.AddRange(gedcomFile.Validate());
            }

            ShowErrors(errors);
            return errors.Count;
        }

        static int Main(string[] args)
        {
            if (args.Length == 1)
            {
                return Validate(args[0]);
            }

            Console.WriteLine("usage: GedValidate <filename>");
            Console.WriteLine("          to check a file as being a valid FamilySearch GEDCOM 7 or GEDZIP file");
            return 1;
        }
    }
}
