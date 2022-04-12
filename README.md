# GEDCOM 7 Compatability Checker

This repository contains code for doing FamilySearch GEDCOM 7 compatability checking on Windows.
It contains:

- Gedcom7 - a project that builds a library usable by tools to do FamilySearch GEDCOM 7 file comparisons
- GedCompare - a project that builds a command-line interface that uses the above library

The same Gedcom7 library is used by the online
[FamilySearch GEDCOM 7 Compatability web tool](https://magikeygedcomconverter.azurewebsites.net/Compatability)
which should give the same results as the GedCompare command-line tool here.

# Prerequisites

- Windows 10 or above
- Visual Studio 2019

# GedCompare Usage

```
Usage: GedCompare <filename1> <filename2>
```

For correct usage, filename1 should be the maximal70.ged file from the Tests/samples directory.
Filename2 should be a file generated by importing maximal70.ged into some other program and then
exporting it as FamilySearch GEDCOM 7.
