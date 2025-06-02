// Copyright (c) Armidale Software
// SPDX-License-Identifier: MIT
using GedcomCommon;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace Gedcom7
{
    public class GedzipFile : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the GedzipFile class.
        /// </summary>
        /// <param name="gedcomRegistriesPath">Optional path to GEDCOM registries. If null, default registries will be used.</param>

        public GedzipFile(string gedcomRegistriesPath = null)
        {
            if (gedcomRegistriesPath != null && !Directory.Exists(gedcomRegistriesPath))
            {
                throw new DirectoryNotFoundException($"GEDCOM registries directory not found: {gedcomRegistriesPath}");
            }
            GedcomRegistriesPath = gedcomRegistriesPath;
        }

        // Data members.
        private string GedcomRegistriesPath { get; set; }
        public string Path { get; private set; }
        public GedcomFile GedcomFile { get; private set; }
        private ZipArchive ZipArchive { get; set; }
        public void Dispose()
        {
            ZipArchive.Dispose();
        }

        /// <summary>
        /// Load a GEDZIP file from a specified path.
        /// </summary>
        /// <param name="pathToFile">Path to GEDZIP file to load</param>
        /// <returns>List of 0 or more error messages</returns>
        public List<string> LoadFromPath(string pathToFile)
        {
            // Validate that extension is .gdz.
            if (!pathToFile.EndsWith(".gdz", StringComparison.InvariantCultureIgnoreCase))
            {
                return new List<string>() { pathToFile + " must have a .gdz extension" };
            }

            this.Path = pathToFile;
            if (!File.Exists(pathToFile))
            {
                return new List<string>() { "File not found: " + pathToFile };
            }

            this.ZipArchive = ZipFile.OpenRead(pathToFile);

            foreach (ZipArchiveEntry entry in this.ZipArchive.Entries)
            {
                if (entry.FullName == "gedcom.ged")
                {
                    using (Stream stream = entry.Open())
                    {
                        this.GedcomFile = new GedcomFile();
                        using (StreamReader streamReader = new StreamReader(stream))
                        {
                            List<string> errors = this.GedcomFile.LoadFromStreamReader(streamReader, GedcomVersion.V70, GedcomRegistriesPath);
                            return errors;
                        }
                    }
                }
            }

            return new List<string>() { "gedcom.ged not found in " + pathToFile };
        }

        /// <summary>
        /// Check whether a given file path exists inside the GEDZIP file.
        /// </summary>
        /// <param name="filePath">File path to check for</param>
        /// <returns></returns>
        private bool ContainsReferencedFile(string filePath)
        {
            foreach (ZipArchiveEntry entry in this.ZipArchive.Entries)
            {
                if (entry.FullName == filePath)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check whether a URI reference refers to a local file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private bool IsLocalFileReference(string filePath)
        {
            if (filePath.StartsWith("ftp:") ||
                filePath.StartsWith("http:") ||
                filePath.StartsWith("https:"))
            {
                // Web-accessible file.
                return false;
            }

            if (filePath.StartsWith("file:///") ||
                filePath.StartsWith("file://localhost/") ||
                (filePath.StartsWith("file:") && !filePath.StartsWith("file://")))
            {
                // Local file URI reference.
                return true;
            }

            if (filePath.StartsWith("file:"))
            {
                // Non-local file URI reference.
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check whether this file is valid GEDZIP.
        /// </summary>
        /// <returns>List of 0 or more error messages</returns>
        public List<string> Validate()
        {
            // Validate gedcom.ged.
            List<string> errors = this.GedcomFile.Validate();
            if (errors.Count > 0)
            {
                return errors;
            }

            // Validate that the gedcom.ged file is actually a 7.0 file.
            GedcomStructure head = this.GedcomFile.Head;
            GedcomStructure gedc = head.FindFirstSubstructure("GEDC");
            GedcomStructure vers = gedc.FindFirstSubstructure("VERS");
            if (vers.LineVal != "7.0") {
                errors.Add(vers.ErrorMessage("gedcom.ged is version " + vers.LineVal + " and should be 7.0"));
            }

            // Validate that referenced local files exist in the archive.
            List<string> referencedFilePaths = this.GedcomFile.GetReferencedFiles();
            foreach (string filePath in referencedFilePaths)
            {
                if (IsLocalFileReference(filePath))
                {
                    string unescapedFilePath = Uri.UnescapeDataString(filePath);
                    if (!this.ContainsReferencedFile(unescapedFilePath))
                    {
                        errors.Add(System.IO.Path.GetFileName(this.Path) + " is missing " + unescapedFilePath);
                    }
                }
            }

            return errors;
        }
    }
}
